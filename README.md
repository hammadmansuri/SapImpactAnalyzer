# SAP Impact Analyzer

Analyze SAP transport requests and their potential technical and business impact. The project includes an ASP.NET Core Web API with a static web interface, a separate Azure Functions host, and shared services for SAP, Azure OpenAI, and Azure AI Search.

## Capabilities

- Retrieve SAP transport objects and analyze transport requests.
- Enrich analysis with AI-generated insights and SAP knowledge-base search.
- Ingest knowledge into Azure AI Search.
- Run the analysis API as an ASP.NET Core application or as Azure Functions.

## Solution layout

| Project | Description | Runtime |
| --- | --- | --- |
| `SapImpactAnalyzer.csproj` | ASP.NET Core API and static web app | .NET 9 |
| `SapImpactAnalyzer.AzureFunctions/` | Azure Functions isolated worker | .NET 8, Functions v4 |
| `SapImpactAnalyzer.Shared/` | Shared models, integrations, and analysis services | .NET 8 |

## Prerequisites

- .NET SDKs 8 and 9
- Azure OpenAI resource with a chat deployment; an embeddings deployment is needed for vector search
- Azure AI Search service and an index compatible with the knowledge document model
- SAP OData endpoint and credentials for live transport data
- Azure Functions Core Tools v4 to run the Functions host locally

## Configuration

Configuration files containing credentials are intentionally excluded from this repository. Supply settings locally through .NET User Secrets or environment variables for the API, and through `local.settings.json` for Azure Functions. Do not commit credentials or local settings files.

The API reads the following configuration keys:

| Key | Purpose |
| --- | --- |
| `AzureOpenAI:Endpoint` | Azure OpenAI resource endpoint |
| `AzureOpenAI:ApiKey` | Azure OpenAI key |
| `AzureOpenAI:DeploymentName` | Chat model deployment name |
| `AzureOpenAI:EmbeddingDeploymentName` | Embeddings deployment name used for vector search |
| `AzureSearch:Endpoint` | Azure AI Search endpoint |
| `AzureSearch:ApiKey` | Azure AI Search key |
| `AzureSearch:IndexName` | Existing search index name |
| `SapApi:BaseUrl` | SAP OData service base URL |
| `SapApi:TransportAPIUrl` | Transport endpoint path |
| `SapApi:Username` | SAP account name |
| `SapApi:Password` | SAP account password |

For the ASP.NET Core API, initialize User Secrets and set the values without putting them in a tracked file:

```powershell
dotnet user-secrets init --project SapImpactAnalyzer.csproj
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<resource>.openai.azure.com/" --project SapImpactAnalyzer.csproj
dotnet user-secrets set "AzureOpenAI:ApiKey" "<key>" --project SapImpactAnalyzer.csproj
dotnet user-secrets set "AzureOpenAI:DeploymentName" "<chat-deployment>" --project SapImpactAnalyzer.csproj
dotnet user-secrets set "AzureOpenAI:EmbeddingDeploymentName" "<embedding-deployment>" --project SapImpactAnalyzer.csproj
dotnet user-secrets set "AzureSearch:Endpoint" "https://<service>.search.windows.net/" --project SapImpactAnalyzer.csproj
dotnet user-secrets set "AzureSearch:ApiKey" "<key>" --project SapImpactAnalyzer.csproj
dotnet user-secrets set "AzureSearch:IndexName" "<index-name>" --project SapImpactAnalyzer.csproj
dotnet user-secrets set "SapApi:BaseUrl" "https://<sap-host>/sap/opu/odata/sap" --project SapImpactAnalyzer.csproj
dotnet user-secrets set "SapApi:TransportAPIUrl" "/<service>/Transports" --project SapImpactAnalyzer.csproj
dotnet user-secrets set "SapApi:Username" "<username>" --project SapImpactAnalyzer.csproj
dotnet user-secrets set "SapApi:Password" "<password>" --project SapImpactAnalyzer.csproj
```

The Functions host reads its local values from `SapImpactAnalyzer.AzureFunctions/local.settings.json`; create that file locally with the required `AzureOpenAI:*`, `AzureSearch:*`, and `SapApi:*` values. For deployment, configure the same values as Function App application settings.

## Build and run

Build the solution:

```powershell
dotnet build SapImpactAnalyzer.sln
```

Run the ASP.NET Core API and static web app:

```powershell
dotnet run --project SapImpactAnalyzer.csproj
```

Run the Azure Functions host from the repository root:

```powershell
func start --script-root SapImpactAnalyzer.AzureFunctions
```

The API exposes OpenAPI metadata at `/openapi/v1.json` in the Development environment.

## API endpoints

### ASP.NET Core API

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/api/transport-analysis/health` | Health check |
| `GET` | `/api/transport-analysis/{transportId}` | Analyze one transport |
| `POST` | `/api/transport-analysis/analyze` | Analyze a list of transports |
| `POST` | `/api/transport-analysis/analyze-using-sk` | Run Semantic Kernel-powered analysis |
| `GET` | `/api/impact-analysis/analyze-tr/{transportNumber}` | Get transport impact details |
| `POST` | `/api/impact-analysis/ingest-knowledge` | Ingest sample knowledge |

Example request for batch analysis:

```json
{
  "transportRequestIds": ["DEVK900001"]
}
```

### Azure Functions

With the default Functions route prefix (`api`), the host exposes `POST /api/analyze`, `GET /api/analyze/{transportId}`, and `GET /api/health`.

## Security

Never commit API keys, passwords, `appsettings*.json`, or `local.settings.json`. If credentials have been exposed, revoke and replace them, then check repository history and logs as appropriate.
