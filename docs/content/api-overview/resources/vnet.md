---
title: "Virtual Network"
date: 2021-06-09T10:31:36-05:00
chapter: false
weight: 21
---

#### Overview

The virtual network builder is used to deploy virtual networks and their subnets.

- Virtual Network (`Microsoft.Network/virtualNetworks`)
- Subnets (`Microsoft.Network/virtualNetworks/subnets`)

The Virtual Network module contains five builders

- The `vnet` builder is used to create Azure Virtual Network instances.
- The `subnet` builder is used within the `vnet` builder to define subnets.
- The `addressSpace` builder can be used to automatically generate subnets based on the sizes of networks needed within the address space.
- The `subnetSpec` builder is used to define the automatically generated subnets, with the primary different from the `subnet` builder being that you define the `size` for the prefix, and not the address.
- The `privateEndpoint` builder is used to create Azure Private Endpoints.

#### Builder Keywords

##### Virtual Network: `vnet`

| Keyword                                 | Purpose                                                                |
| --------------------------------------- | ---------------------------------------------------------------------- |
| name                                    | Sets the name of the virtual network.                                  |
| add_address_spaces                      | Adds address spaces to the virtual network.                            |
| add_subnets                             | Adds subnets to the virtual network.                                   |
| build_address_spaces                    | Automatically builds address spaces for subnet names and sizes.        |
| add_tags                                | Adds a set of tags to the resource                                     |
| add_tag                                 | Adds a tag to the resource                                             |
| add_peer                                | Adds VNet peering between this and another VNet (one/two-way)          |
| add_peers                               | Adds VNet peering between this and other VNets (one/two-way)           |

##### Subnet: `subnet`

| Keyword                                 | Purpose                                                                                        |
| --------------------------------------- | ---------------------------------------------------------------------------------------------- |
| name                                    | Name of the subnet resource                                                                    |
| prefix                                  | Subnet prefix in CIDR notation (e.g. 192.168.100.0/24)                                         |
| network_security_group                  | Specify the network security group from the same deployment.                                   |
| link_to_network_security_group          | Specify an existing network security group for this subnet.                                    |
| link_to_vnet                            | Link a standalone subnet to a vnet in the same template.                                       |
| link_to_unmanaged_vnet                  | Link a standalone subnet to an existing vnet that is already deployed.                         |
| add_delegations                         | Adds one or more delegations to this subnet.                                                   |
| add_service_endpoints                   | Adds one or more service endpoints to this subnet.                                             |
| associate_service_endpoint_policies     | Associates a subnet with an existing service policy.                                           |
| allow_private_endpoints                 | Enable or disable support for private endpoints, default is `Disabled`                         |
| private_link_service_network_policies   | Enable or disable support for private link service network polices, default is `Disabled`      |
| link_to_route_table                     | Associates this subnet with a network route table included in the same deployment              |
| link_to_unmanaged_route_table           | Associates this subnet with a network route table which is not included in the same deployment |

##### Private Endpoint: `privateEndpoint`

| Keyword                                 | Purpose                                                                                            |
| --------------------------------------- | ---------------------------------------------------------------------------------------------------|
| name                                    | Name of the private endpoint                                                                       |
| subnet                                  | The subnet the private endpoint belongs too                                                        |
| link_to_resource                        | The resource the private endpoint will be attached too                                             |
| link_to_private_dns_zone                | Optionally link the private endpoint to a private dns zone, to automatically register DNS records. |

##### Automatically build out an address space: `addressSpace`

| Keyword                                 | Purpose                                                                |
| --------------------------------------- | ---------------------------------------------------------------------- |
| space                                   | When using `build_address_space` this specifies the address space.     |
| subnets                                 | Specifies the subnets to build automatically.                          |


##### Specify subnets in automatic address space: `subnetSpec`

| Keyword                                 | Purpose                                                                                   |
| --------------------------------------- | ----------------------------------------------------------------------------------------- |
| name                                    | Specifies the name of the subnet to build.                                                |
| size                                    | Specifies the size of the subnet to build, default is /24.                                |
| network_security_group                  | Specify the network security group from the same deployment.                              |
| link_to_network_security_group          | Specify an existing network security group for the subnet.                                |
| add_delegations                         | Adds service delegations for the subnet.                                                  |
| add_service_endpoints                   | Adds service endpoints for the subnet.                                                    |
| add_service_endpoint_policies           | Associates the service endpoint policies with the subnet.                                 |
| allow_private_endpoints                 | Enable or disable support for private endpoints, default is `Disabled`                    |
| private_link_service_network_policies   | Enable or disable support for private link service network polices, default is `Disabled` |

#### Configuration Members

| Member                                  | Purpose                                                                |
| --------------------------------------- | ---------------------------------------------------------------------- |
| SubnetIds                               | Gets a map of subnet ResourceIds by subnet name                        |

#### Example - Manual Subnets

A virtual network is defined with the `vnet` builder. Address spaces and
subnets should be added, taking care to ensure the subnets are contained
within an address space on the virtual network.

```fsharp
open Farmer
open Farmer.Builders

let myVnet = vnet {
    name "my-vnet"
    add_address_spaces [ "192.168.200.0/22" ]
    add_subnets [
        subnet {
            name "vms"
            prefix "192.168.200.0/24"
        }
        subnet {
            name "containers"
            prefix "192.168.201.0/24"
            add_delegations [
                SubnetDelegationService.ContainerGroups
            ]
            add_service_endpoints [
                EndpointServiceType.Storage, [Location.NorthEurope; Location.WestEurope]
            ]
        }
        subnet {
            name "databases"
            prefix "192.168.202.0/24"
            add_delegations [
                SubnetDelegationService.SqlManagedInstances
            ]
        }
    ]
}

let deployment = arm {
    location Location.NorthEurope
    add_resource myVnet
}
```

#### Example - Automatically Build Subnets

Address spaces and subnets can be built out automatically to ensure the subnets
are contained within the address spaces. This reduces the need for "IP math"
to determine the start addresses for contiguous networks of different sizes.

```fsharp
open Farmer
open Farmer.Builders

let myVnet = vnet {
    name "my-vnet"
    build_address_spaces [
        addressSpace {
            space "10.28.0.0/16"
            subnets [
                subnetSpec {
                    name "vms"
                    size 26
                }
                subnetSpec {
                    name "services"
                    size 24
                }
                subnetSpec {
                    name "corporate-west"
                    size 18
                }
                subnetSpec {
                    name "corporate-east"
                    size 18
                }
                subnetSpec {
                    name "corporate-east"
                    size 18
                }
                subnetSpec {
                    name "GatewaySubnet"
                    size 28
                }
                subnetSpec {
                    name "containers"
                    size 27
                    add_delegations [SubnetDelegationService.ContainerGroups]
                    add_service_endpoints [
                        EndpointServiceType.Storage, [
                            Location.NorthEurope
                            Location.WestEurope
                        ]
                    ]
                }
            ]
        }
        addressSpace {
            space "10.30.0.0/16"
            subnets [
                subnetSpec {
                    name "remote-office"
                    size 23
                }
                subnetSpec {
                    name "applications"
                    size 24
                    add_service_endpoints [
                        EndpointServiceType.Storage, [
                            Location.NorthEurope
                            Location.WestEurope
                        ]
                    ]
                }
                subnetSpec {
                    name "reserved"
                    size 24
                }
                subnetSpec {
                    name "GatewaySubnet"
                    size 28
                }
            ]
        }
    ]
}

let deployment = arm {
    location Location.NorthEurope
    add_resource myVnet
}
```

#### Example - Create a VNET, private DNS zone, and a web app with a private endpoint and automatically register the DNS

```fsharp
let myDnsZone = dnsZone {
  name "privatelink.azurewebsites.net"
  zone_type Dns.Private
}

let myVnet = vnet {
  name "my-vnet"
  add_address_spaces [ "192.168.200.0/22" ]
}

let webAppSubnet = subnet {
  name "webapps"
  prefix "192.168.201.0/24"
  add_delegations [
      SubnetDelegationService "Microsoft.Web/serverFarms"
  ]
  link_to_vnet myVnet.ResourceId
}

let privateEndpointSubnet = subnet {
  name "privateendpoints"
  prefix "192.168.202.0/24"
  allow_private_endpoints Enabled
  link_to_vnet myVnet.ResourceId
  depends_on webAppSubnet // Can't deploy multiple subnets in parallel
}

let myWebApp = webApp env {
  name "codatfarmertest-my-webapp"
  link_to_vnet webAppSubnet
  sku Sku.P1V3
}

let myPrivateEndpoint = privateEndpoint {
  name "mywebapp-privateendpoint"
  subnet privateEndpointSubnet
  link_to_resource (Managed myWebApp.ResourceId)
  link_to_private_dns_zone myDnsZone
}

let deployment = arm {
  add_resources [
      myDnsZone
      myVnet
      webAppSubnet
      privateEndpointSubnet
      myWebApp
      myPrivateEndpoint
  ]
}
```