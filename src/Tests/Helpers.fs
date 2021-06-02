[<AutoOpen>]
module TestHelpers

open Farmer
open Microsoft.Rest.Serialization
open System.Text.Json
open BlushingPenguin.JsonPath
open Farmer.Arm.ResourceGroup
open System.Text.Json.Serialization

let deserialzeOptions = JsonSerializerOptions ()
do
  deserialzeOptions.PropertyNameCaseInsensitive <- true
  deserialzeOptions.NumberHandling <- JsonNumberHandling.AllowReadingFromString

[<AutoOpen>]
module TestHelpers =
    let createSimpleDeployment parameters =
        { Location = Location.NorthEurope
          PostDeployTasks = []
          Template = {
              Schema = Arm.ResourceGroup.schema
              Outputs = []
              Parameters = parameters |> List.map SecureParameter
              Resources = []
          }
        }
    let convertTo<'T> = JsonSerializer.Serialize >> JsonSerializer.Deserialize<'T>

let farmerToMs<'T when 'T : null> (serializationSettings:Newtonsoft.Json.JsonSerializerSettings) data =
    data
    |> SafeJsonConvert.SerializeObject
    |> fun json -> SafeJsonConvert.DeserializeObject<'T>(json, serializationSettings)

let getResourceAtIndex serializationSettings index (builder:#IBuilder) =
    builder.BuildResources Location.WestEurope
    |> fun r -> r.[index].JsonModel |> farmerToMs serializationSettings

let private flattenResourcesQuery = sprintf "$..resources.[?(@.type != '%s')]" Arm.ResourceGroup.resourceGroupDeployments.Type
let getResourceTokens deployment =
    let template = 
        Deployment.getTemplate "farmer-resources" deployment 
        |> Writer.toJson
        |> JsonDocument.Parse
    template.SelectTokens flattenResourcesQuery

let getResources (deployment:IDeploymentBuilder) =
    let rec flatten (resList:IArmResource list) = 
        resList |> 
        List.collect
            (function
             | :? ResourceGroupDeployment as rg -> flatten rg.Resources
             | x -> [x]
            )
    let depl = deployment.BuildDeployment "" Option.None
    flatten depl.Template.Resources
    
let getResourcesByName<'a when 'a:>IArmResource > name (deployment:IDeploymentBuilder) : 'a list =
    getResources deployment
    |> List.choose 
        (function
            | :? 'a as x when x.ResourceId.Name.Value = name -> Some x
            | _ -> None)

let getResourceByName<'a when 'a:>IArmResource > name (deployment:IDeploymentBuilder) : 'a =
    getResourcesByName name deployment
    |> function
       | [x] -> x
       | [] -> failwith "No matching resources found"
       | x -> failwithf "More than one matching resource found: %A" x

let getResourceGroupDeploymentFromTemplate<'a> (template:ArmTemplate) =
    let template =
      template
      |> Writer.toJson
      |> JsonDocument.Parse
    let rgd = template.SelectToken (sprintf "$.resources[?(@.type == '%s')].properties.template" Arm.ResourceGroup.resourceGroupDeployments.Type)
    JsonSerializer.Deserialize<'a>(rgd.Value.GetRawText(),deserialzeOptions)
    
let findAzureResources<'T when 'T : null> (serializationSettings:Newtonsoft.Json.JsonSerializerSettings) deployment =
    let tokens = getResourceTokens deployment
    let jsons =  tokens |> Seq.map string
    let desrs =  jsons |> Seq.choose (fun json -> SafeJsonConvert.DeserializeObject<'T>(json, serializationSettings) |> Option.ofObj)
    desrs |> Seq.toList

let findAzureResourcesByType<'T when 'T : null> (resourceType:ResourceType) (serializationSettings:Newtonsoft.Json.JsonSerializerSettings) deployment =
     getResourceTokens deployment
    |> Seq.filter (fun x-> x.GetProperty("type").GetString() = resourceType.Type)
    |> Seq.map string
    |> Seq.choose (fun json -> SafeJsonConvert.DeserializeObject<'T>(json, serializationSettings) |> Option.ofObj)
    |> Seq.toList

type TypedArmTemplate<'ResT> = { Resources : 'ResT array }

let getFirstResourceOrFail (template: TypedArmTemplate<'ResourceType>) =
    if Array.length template.Resources < 1 then
        failwith "Template had no resources"
    template.Resources.[0]

let toTemplate loc (d : IBuilder) =
    arm {
        location loc
        add_resource d
    } 
    |> Deployment.getTemplate "farmer-resources" 

let toTypedTemplate<'ResourceType> loc =
    toTemplate loc
    >> getResourceGroupDeploymentFromTemplate<TypedArmTemplate<'ResourceType>>
    >> getFirstResourceOrFail