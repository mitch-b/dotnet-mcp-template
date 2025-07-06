# .NET MCP Server Template

* [Streamable HTTP](https://modelcontextprotocol.io/docs/concepts/transports) from Aspire host runtime.
* .NET 9 application host
* Docker container build
* OAuth support for protecting your ToolServer and authenticating Console clients

## Get Started

Take this template repository and create your repo from it. 

1. Rename "`McpTemplate`" to your app's name using [renamer](https://github.com/mitch-b/renamer) tool.

    ```bash
    # cd project root
    docker run --rm -it -v "$PWD:/data" ghcr.io/mitch-b/renamer McpTemplate YourMcp
    ```

## What's Inside?

* [McpClient Registration](./McpTemplate.Application/Extensions/ServiceCollectionExtensions.cs) from appsettings configuration
* [Chat Completion in Console](./McpTemplate.Console/ChatRuntime.cs#L49) 
* ToolServer [McpServer Setup](./McpTemplate.ToolServer/Program.cs)
* ToolServer [DateTime](./McpTemplate.ToolServer/Tools/DateTimeTool.cs)


## OAuth Configuration

To enable OAuth protection for your ToolServer and Console client, add an `OAuth` section to your `appsettings.json` (or use user-secrets for sensitive values):

```json
"OAuth": {
  "Authority": "https://login.microsoftonline.com/{tenantId}/v2.0",
  "Audience": "your-api-audience",
  "ClientId": "your-client-id",
  "ClientSecret": "set-in-user-secrets",
  "RedirectUri": "http://localhost:5000/callback",
  "Scopes": [ "api://your-api-audience/.default" ]
}
```

**Recommended:** Store `ClientSecret` and any other sensitive values using [user-secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) for local development:

```bash
dotnet user-secrets set OAuth:ClientSecret <your-client-secret>
dotnet user-secrets set OAuth:ClientId <your-client-id>
dotnet user-secrets set OAuth:Audience <your-api-audience>
dotnet user-secrets set OAuth:Authority https://login.microsoftonline.com/<tenantId>/v2.0
dotnet user-secrets set OAuth:RedirectUri http://localhost:5000/callback
dotnet user-secrets set OAuth:Scopes:0 api://<your-api-audience>/.default
```

If you do **not** set the `OAuth` section, the ToolServer and Console will run in unauthenticated mode for quick local usage.

---

| Secret | |
|--|--|
| McpTemplateOptions__Endpoint | https://{name}.openai.azure.com |
| McpTemplateOptions__ApiKey | Get from your Azure OpenAI Service |
| McpTemplateOptions__Model | Name of your deployed model, example: `gpt-4o-mini` |


Add these in Visual Studio by right-clicking your project and selecting "Manage User Secrets". This will open a `secrets.json` file. Add the above secrets in the following format:

```json
{
  "McpTemplateOptions": {
    "Endpoint": "https://{name}.openai.azure.com",
    "ApiKey": "<your_api_key>",
    "Model": "gpt-4o-mini"
  },
  "OAuth": {
    "Authority": "https://login.microsoftonline.com/{tenantId}/v2.0",
    "Audience": "your-api-audience",
    "ClientId": "your-client-id",
    "ClientSecret": "<your-client-secret>",
    "RedirectUri": "http://localhost:5000/callback",
    "Scopes": [ "api://your-api-audience/.default" ]
  }
}
```

Or, you can use the command line to set them:

```bash
dotnet user-secrets set McpTemplateOptions:Endpoint https://{name}.openai.azure.com
dotnet user-secrets set McpTemplateOptions:ApiKey <your_api_key>
dotnet user-secrets set McpTemplateOptions:Model gpt-4o-mini
# OAuth secrets
dotnet user-secrets set OAuth:Authority https://login.microsoftonline.com/<tenantId>/v2.0
dotnet user-secrets set OAuth:Audience <your-api-audience>
dotnet user-secrets set OAuth:ClientId <your-client-id>
dotnet user-secrets set OAuth:ClientSecret <your-client-secret>
dotnet user-secrets set OAuth:RedirectUri http://localhost:5000/callback
dotnet user-secrets set OAuth:Scopes:0 api://<your-api-audience>/.default
```

## Docker Image

To build the Docker image, run the following command within the `src` folder:

```bash
docker build \
  -f ./McpTemplate.ToolServer/Dockerfile \
  -t template-mcp:dev \
  .

docker run \
  --rm \
  -p 8080:8080 \
  -e McpTemplateOptions__Endpoint="https://{name}.openai.azure.com" \
  -e McpTemplateOptions__ApiKey="your-api-key" \
  -e McpTemplateOptions__Model="gpt-4o-mini" \
  template-mcp:dev
```


## Solution Design

- **McpTemplate.Application**: Core business logic and service registration; extend here for new features/services.
- **McpTemplate.Common**: Shared models and options; update for cross-project types and configuration.
- **McpTemplate.Console**: Console app for testing and running chat completions; useful for local development.
- **McpTemplate.ToolServer**: Implements MCP ToolServer and tools; add new tools in `Tools/` and configure in `Program.cs`.

### Aspire Components
- **McpTemplate.AppHost**: Main entrypoint for hosting the application; configures and runs the Aspire Dashboard server for local development.
- **McpTemplate.ServiceDefaults**: Common service extensions and defaults; use for shared service setup.
