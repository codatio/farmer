module Redis

open System
open Expecto
open Farmer
open Farmer.Arm.Cache
open Farmer.Builders
open Newtonsoft.Json.Linq

let verifySku (redisCache: RedisConfig, expectedSku: string, expectedCapacity: int, expectedFamily: string) =
    let deployment = arm { add_resource redisCache }
    let jobj = deployment.Template |> Writer.toJson |> JObject.Parse

    let sku = string (jobj.SelectToken "resources[0].properties.sku.name")
    let capacity = int (jobj.SelectToken "resources[0].properties.sku.capacity")
    let family = string (jobj.SelectToken "resources[0].properties.sku.family")

    Expect.equal expectedSku sku $"Unexpected sku: {sku}"
    Expect.equal expectedCapacity capacity $"Unexpected capacity: {capacity}"
    Expect.equal expectedFamily family $"Unexpected family: {family}"

let tests =
    testList
        "Redis Tests"
        [
            test "Standard 250 MB" {
                let redisCache =
                    redis {
                        name "some-redis-cache"
                        sku Redis.Standard_WithCapacity.``250 MB``
                    }

                verifySku (redisCache, "Standard", 0, "C")
            }
            test "Standard 1 GB" {
                let redisCache =
                    redis {
                        name "some-redis-cache"
                        sku Redis.Standard_WithCapacity.``1 GB``
                    }

                verifySku (redisCache, "Standard", 1, "C")
            }
            test "Standard 2.5 GB" {
                let redisCache =
                    redis {
                        name "some-redis-cache"
                        sku Redis.Standard_WithCapacity.``2.5 GB``
                    }

                verifySku (redisCache, "Standard", 2, "C")
            }
            test "Premium 6 GB" {
                let redisCache =
                    redis {
                        name "some-redis-cache"
                        sku Redis.Premium_WithCapacity.``6 GB``
                    }

                verifySku (redisCache, "Premium", 1, "P")
            }
            test "Premium 13 GB" {
                let redisCache =
                    redis {
                        name "some-redis-cache"
                        sku Redis.Premium_WithCapacity.``13 GB``
                    }

                verifySku (redisCache, "Premium", 2, "P")
            }
            test "Public network access is enabled by default" {
                let redisCache = redis {
                    name "some-redis-cache"
                    sku Redis.Standard_WithCapacity.``1 GB``
                }

                let deployment = arm { add_resource redisCache }
                let jobj = deployment.Template |> Writer.toJson |> JObject.Parse

                let publicNetworkAccess = string (jobj.SelectToken "resources[0].properties.publicNetworkAccess")

                Expect.equal "Enabled" publicNetworkAccess "Public network access should be enabled by default"
            }
            test "Public network access can be disabled" {
                let redisCache = redis {
                    name "some-redis-cache"
                    sku Redis.Standard_WithCapacity.``1 GB``
                    disable_public_network_access
                }

                let deployment = arm { add_resource redisCache }
                let jobj = deployment.Template |> Writer.toJson |> JObject.Parse

                let publicNetworkAccess = string (jobj.SelectToken "resources[0].properties.publicNetworkAccess")

                Expect.equal "Disabled" publicNetworkAccess "Public network access should be disabled"
            }
            test "Public network access can be toggled" {
                let redisCache = redis {
                    name "some-redis-cache"
                    sku Redis.Standard_WithCapacity.``1 GB``
                    disable_public_network_access
                    disable_public_network_access FeatureFlag.Disabled
                }

                let deployment = arm { add_resource redisCache }
                let jobj = deployment.Template |> Writer.toJson |> JObject.Parse

                let publicNetworkAccess = string (jobj.SelectToken "resources[0].properties.publicNetworkAccess")

                Expect.equal "Enabled" publicNetworkAccess "Public network access should be enabled"
            }
        ]
