<div align="center">
<h1>Microsoft RESTier - OData Made Simple</h1>

[Releases](https://github.com/OData/RESTier/releases)&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;Documentation&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;[OData v4.01 Documentation](https://www.odata.org/documentation/)&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;[License](license.md)

</div>

## What is RESTier?

RESTier is an API development framework for building standardized, OData V4 based RESTful services on .NET.

RESTier is the spiritual successor to [WCF Data Services](https://en.wikipedia.org/wiki/WCF_Data_Services). Instead of
generating endless boilerplate code with the current Web API + OData toolchain, RESTier helps you bootstrap a standardized,
queryable HTTP-based REST interface in literally minutes. And that's just the beginning.

Like WCF Data Services before it, RESTier provides simple and straightforward ways to shape queries and intercept submissions
_before_ and _after_ they hit the database. And like Web API + OData, you still have the flexibility to add your own
custom queries and actions with techniques you're already familiar with.

## What is OData?

OData stands for the Open Data Protocol. OData enables the creation and consumption of RESTful APIs, which allow
resources, defined in a data model and identified by using URLs, to be published and edited by Web clients using
simple HTTP requests.

OData was originally designed by Microsoft to be a framework for exposing Entity Framework objects over REST services.
The first concepts shipped as "Project Astoria" in 2007. By 2009, the concept had evolved enough for Microsoft to
announce OData, along with a [larger effort](https://blogs.msdn.microsoft.com/odatateam/2009/11/17/breaking-down-data-silos-the-open-data-protocol-odata/)
to push the format as an industry standard.

Work on the current version of the protocol (V4) began in April 2012, and was ratified by OASIS as an industry standard in Feb 2014.

## Getting Started

To get started with RESTier, see the [Getting Started guide](getting-started.md). Reference the
`Microsoft.Restier.AspNetCore` and `Microsoft.Restier.EntityFrameworkCore` NuGet packages in your project
and RESTier will take care of the rest.

## Supported Platforms

RESTier currently supports the following platforms:

- .NET 8.0
- .NET 9.0
- .NET 10.0

Entity Framework 6.x support is available for .NET Framework 4.8 via the `Microsoft.Restier.EntityFramework` package.

## RESTier Components

RESTier is made up of the following packages:

| Package | Description |
|---------|-------------|
| **Microsoft.Restier.AspNetCore** | ASP.NET Core integration, routing, and OData controller |
| **Microsoft.Restier.Core** | Core convention-based interception framework and pipeline |
| **Microsoft.Restier.EntityFrameworkCore** | Entity Framework Core data provider |
| **Microsoft.Restier.EntityFramework** | Entity Framework 6.x data provider (.NET Framework) |
| **Microsoft.Restier.AspNetCore.Swagger** | OpenAPI/Swagger document generation |
| **Microsoft.Restier.Breakdance** | In-memory integration testing framework |

## Ecosystem

There is a growing set of tools to support RESTier-based development:

- [Breakdance](https://github.com/cloudnimble/breakdance): Convention-based name troubleshooting and integration test support.

## Community

### Contributing

If you'd like to help out with the project, please see our [Contribution Guidelines](contribution-guidelines.md).

## Contributors

Special thanks to everyone involved in making RESTier the best API development platform for .NET. The following people
have made various contributions to the codebase:

| Microsoft     | External       |
|---------------|----------------|
| Lewis Cheng   | Cengiz Ilerler |
| Challenh      | Kemal M        |
| Eric Erhardt  | Robert McLaws  |
| Vincent He    |                |
| Dong Liu      |                |
| Layla Liu     |                |
| Fan Ouyang    |                |
| Congyong S    |                |
| Mark Stafford |                |
| Ray Yao       |                |

## 

<!--
Link References
-->

[devops-build]:https://dev.azure.com/cloudnimble/Restier/_build?definitionId=8
[devops-release]:https://dev.azure.com/cloudnimble/Restier/_release?view=all&definitionId=1
[nightly-feed]:https://www.myget.org/F/restier-nightly/api/v3/index.json
[twitter-intent]:https://twitter.com/intent/tweet?url=https%3A%2F%2Fgithub.com%2FOData%2FRESTier&via=robertmclaws&text=Check%20out%20Restier%21%20It%27s%20the%20simple%2C%20queryable%20framework%20for%20building%20data-driven%20APIs%20in%20.NET%21&hashtags=odata
[code-of-conduct]:https://opensource.microsoft.com/codeofconduct/

[devops-build-img]:https://img.shields.io/azure-devops/build/cloudnimble/restier/8.svg?style=for-the-badge&logo=azuredevops
[devops-release-img]:https://img.shields.io/azure-devops/release/cloudnimble/d3aaa016-9aea-4903-b6a6-abda1d4c84f0/1/1.svg?style=for-the-badge&logo=azuredevops
[nightly-feed-img]:https://img.shields.io/badge/continuous%20integration-feed-0495dc.svg?style=for-the-badge&logo=nuget&logoColor=fff
[github-version-img]:https://img.shields.io/github/release/ryanoasis/nerd-fonts.svg?style=for-the-badge
[gitter-img]:https://img.shields.io/gitter/room/nwjs/nw.js.svg?style=for-the-badge
[code-climate-img]:https://img.shields.io/codeclimate/issues/github/ryanoasis/nerd-fonts.svg?style=for-the-badge
[code-of-conduct-img]: https://img.shields.io/badge/code%20of-conduct-00a1f1.svg?style=for-the-badge&logo=windows
[twitter-img]:https://img.shields.io/badge/share-on%20twitter-55acee.svg?style=for-the-badge&logo=twitter
