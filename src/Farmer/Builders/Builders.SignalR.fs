[<AutoOpen>]
module Farmer.Builders.SignalR

open Farmer
open Farmer.Arm.SignalRService
open Farmer.Builders
open Farmer.Helpers
open Farmer.SignalR

type SignalRConfig =
    {
        Name: ResourceName
        Sku: Sku
        Capacity: int option
        AllowedOrigins: string list
        ServiceMode: ServiceMode
        Tags: Map<string, string>
        UpstreamTemplates: UpstreamTemplate list
    }

    member this.ResourceId = signalR.resourceId this.Name

    interface IBuilder with
        member this.ResourceId = this.ResourceId

        member this.BuildResources location =
            [
                {
                    Name = this.Name
                    Location = location
                    Sku = this.Sku
                    Capacity = this.Capacity
                    AllowedOrigins = this.AllowedOrigins
                    ServiceMode = this.ServiceMode
                    Tags = this.Tags
                    // Order matters for upstream templates and it's more efficient to add to the beginning of a list and then reverse
                    UpstreamTemplates = this.UpstreamTemplates |> List.rev
                }                            
            ]

    member private this.GetKeyExpr field =
        let expr =
            $"listKeys(resourceId('Microsoft.SignalRService/SignalR', '{this.Name.Value}'), providers('Microsoft.SignalRService', 'SignalR').apiVersions[0]).{field}"

        ArmExpression.create (expr, this.ResourceId)

    member this.Key = this.GetKeyExpr "primaryKey"
    member this.ConnectionString = this.GetKeyExpr "primaryConnectionString"

    member this.ConnectionStringInResourceGroup rg =
        let expr =
            $"listKeys(resourceId('{rg}', 'Microsoft.SignalRService/SignalR', '{this.Name.Value}'), providers('Microsoft.SignalRService', 'SignalR').apiVersions[0]).primaryConnectionString"

        ArmExpression.create (expr, this.ResourceId)

type SignalRBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            Sku = Free
            Capacity = None
            AllowedOrigins = []
            ServiceMode = Default
            Tags = Map.empty
            UpstreamTemplates = []
        }

    member _.Run(state: SignalRConfig) =
        { state with
            Name = state.Name |> sanitiseSignalR |> ResourceName
        }

    /// Sets the name of the Azure SignalR instance.
    [<CustomOperation("name")>]
    member _.Name(state: SignalRConfig, name) = { state with Name = name }

    member this.Name(state: SignalRConfig, name) = this.Name(state, ResourceName name)

    /// Sets the SKU of the Azure SignalR instance.
    [<CustomOperation("sku")>]
    member _.Sku(state: SignalRConfig, sku) = { state with Sku = sku }

    /// Sets the capacity of the Azure SignalR instance.
    [<CustomOperation("capacity")>]
    member _.Capacity(state: SignalRConfig, capacity) = { state with Capacity = Some capacity }

    /// Sets the allowed origins of the Azure SignalR instance.
    [<CustomOperation("allowed_origins")>]
    member _.AllowedOrigins(state: SignalRConfig, allowedOrigins) =
        { state with
            AllowedOrigins = allowedOrigins
        }

    /// Sets the service mode of the Azure SignalR instance.
    [<CustomOperation("service_mode")>]
    member _.ServiceMode(state: SignalRConfig, serviceMode) =
        { state with ServiceMode = serviceMode }

    /// Add an upstream setting to the Azure SignalR instance
    [<CustomOperation("add_upstream")>]
    member _.UpstreamConfigs(state: SignalRConfig, urlTemplate, hubPattern, categoryPattern, eventPattern) =
        { state with UpstreamTemplates = { UrlTemplate = urlTemplate; HubPattern = hubPattern; CategoryPattern = categoryPattern; EventPattern = eventPattern } :: state.UpstreamTemplates }

    /// Add an upstream setting to the Azure SignalR instance
    [<CustomOperation("add_upstream")>]
    member this.UpstreamConfigs(state: SignalRConfig, urlTemplate) = this.UpstreamConfigs(state, urlTemplate, Any, Any, Any)

    interface ITaggable<SignalRConfig> with
        member _.Add state tags =
            { state with
                Tags = state.Tags |> Map.merge tags
            }

let signalR = SignalRBuilder()
