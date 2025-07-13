# .NET MCP Server Template

* [Streamable HTTP](https://modelcontextprotocol.io/docs/concepts/transports) transport
* OAuth support for protecting your MCP server
* Docker container build
* Aspire host for local environment

## Get Started

Take this template repository and create your repo from it. 

1. Rename "`McpTemplate`" to your app's name using [renamer](https://github.com/mitch-b/renamer) tool. All files, folders, and file contents will be renamed.

    ```bash
    # cd project root
    docker run --rm -it -v "$PWD:/data" ghcr.io/mitch-b/renamer McpTemplate YourMcp
    ```

## What's Inside?

* Tool [examples](./src/McpTemplate.McpServer/Tools/)
* Prompt [examples](./src/McpTemplate.McpServer/Prompts/)
* [MCP client registration](./McpTemplate.Application/Extensions/ServiceCollectionExtensions.cs) from appsettings configuration
* [Chat completion in Console](./McpTemplate.Console/ChatRuntime.cs#L49) 
* MCP server [setup](./McpTemplate.McpServer/Program.cs)

## OAuth Configuration

This sample comes configured and tested against using Microsoft Entra ID app registration for authentication and authorization support. Replace any assumptions accordingly.

### Entra App Registration

Create a new App Registration in Entra.

* **Name**: McpTemplate Server
* **Platform**: `Web`, redirect uri: `http://localhost:5000/callback`

Create.

* In **Overview** blade: 
  * Make note of the Application (client) Id - you'll use this later. 
  * Make note of the Directory (tenant) Id - you'll use this later.
* In **Authentication** blade:
  * Ensure that `Web` platform has our redirect uri (needed if you're using a custom client, like the McpTemplate.Console)
* In **Expose an API** blade: 
  * Set Application ID at the top (will be like `api://<guid>`)
  * Add a new scope named `mcp.tools` (or whatever you prefer)


### OAuth for McpServer

To enable OAuth protection for your McpServer, add an `OAuth` section to your `appsettings.json` (or use user-secrets for sensitive values):

(represented here with demo GUIDs)

```json
"OAuth": {
  "Tenant": "eb4f98a8-4c60-4348-86a0-baea7df39d74",
  "Authority": "https://login.microsoftonline.com/eb4f98a8-4c60-4348-86a0-baea7df39d74/v2.0",

  "Audience": "api://d13cafd2-01ac-4692-a1d9-aa5611d7cbe0",
  "Scopes": [ "api://d13cafd2-01ac-4692-a1d9-aa5611d7cbe0/mcp.tools" ]
}
```

```bash
dotnet user-secrets set OAuth:Tenant eb4f98a8-4c60-4348-86a0-baea7df39d74
dotnet user-secrets set OAuth:Authority https://login.microsoftonline.com/eb4f98a8-4c60-4348-86a0-baea7df39d74/v2.0

dotnet user-secrets set OAuth:Audience api://d13cafd2-01ac-4692-a1d9-aa5611d7cbe0
dotnet user-secrets set OAuth:Scopes:0 api://d13cafd2-01ac-4692-a1d9-aa5611d7cbe0/mcp.tools
```

If you do **not** set the `OAuth` section, the McpServer and Console will run in unauthenticated mode for quick local usage.


### OAuth for Console (client)

(represented here with demo GUIDs)

```json
"OAuth": {
  "Scopes": [ "api://d13cafd2-01ac-4692-a1d9-aa5611d7cbe0/mcp.tools" ],
  "ClientId": "fb35dbf1-6916-4bbf-98ed-74821d8f7ba4",
  "RedirectUri": "http://localhost:5000/callback"
}
```

```bash
dotnet user-secrets set OAuth:Scopes:0 api://d13cafd2-01ac-4692-a1d9-aa5611d7cbe0/mcp.tools
dotnet user-secrets set OAuth:ClientId fb35dbf1-6916-4bbf-98ed-74821d8f7ba4
dotnet user-secrets set OAuth:RedirectUri http://localhost:5000/callback
```

---

| Secret | |
|--|--|
| McpTemplateOptions:Endpoint | https://{name}.openai.azure.com |
| McpTemplateOptions:ApiKey | Get from your Azure OpenAI Service |
| McpTemplateOptions:Model | Name of your deployed model, example: `gpt-4o-mini` |


Add these in Visual Studio by right-clicking your project and selecting "Manage User Secrets". This will open a `secrets.json` file. Add the above secrets in the following format:

```json
{
  "McpTemplateOptions": {
    "Endpoint": "https://{name}.openai.azure.com",
    "ApiKey": "<your_api_key>",
    "Model": "gpt-4o-mini"
  },
  "OAuth": {
    "Tenant": "eb4f98a8-4c60-4348-86a0-baea7df39d74",
    "Authority": "https://login.microsoftonline.com/eb4f98a8-4c60-4348-86a0-baea7df39d74/v2.0",

    "Audience": "api://d13cafd2-01ac-4692-a1d9-aa5611d7cbe0",
    "Scopes": [ "api://d13cafd2-01ac-4692-a1d9-aa5611d7cbe0/mcp.tools" ],

    "ClientId": "fb35dbf1-6916-4bbf-98ed-74821d8f7ba4",
    "RedirectUri": "http://localhost:5000/callback"
  }
}
```

Or, you can use the command line to set them:

```bash
dotnet user-secrets set McpTemplateOptions:Endpoint https://{name}.openai.azure.com
dotnet user-secrets set McpTemplateOptions:ApiKey <your_api_key>
dotnet user-secrets set McpTemplateOptions:Model gpt-4o-mini

dotnet user-secrets set OAuth:Tenant eb4f98a8-4c60-4348-86a0-baea7df39d74
dotnet user-secrets set OAuth:Authority https://login.microsoftonline.com/eb4f98a8-4c60-4348-86a0-baea7df39d74/v2.0

dotnet user-secrets set OAuth:Audience api://d13cafd2-01ac-4692-a1d9-aa5611d7cbe0
dotnet user-secrets set OAuth:Scopes:0 api://d13cafd2-01ac-4692-a1d9-aa5611d7cbe0/mcp.tools

dotnet user-secrets set OAuth:ClientId fb35dbf1-6916-4bbf-98ed-74821d8f7ba4
dotnet user-secrets set OAuth:RedirectUri http://localhost:5000/callback
```

## Docker Image

To build the Docker image, run the following command within the `src` folder:

```bash
docker build \
  -f ./McpTemplate.McpServer/Dockerfile \
  -t mcp-tools:dev \
  .

docker run \
  --rm \
  -p 8080:8080 \
  -e McpTemplateOptions__Endpoint="https://{name}.openai.azure.com" \
  -e McpTemplateOptions__ApiKey="your-api-key" \
  -e McpTemplateOptions__Model="gpt-4o-mini" \
  mcp-tools:dev
```


## Solution Design

- **McpTemplate.Application**: Core business logic and service registration; extend here for new features/services.
- **McpTemplate.Common**: Shared models and options; update for cross-project types and configuration.
- **McpTemplate.Console**: Console app for testing and running chat completions; useful for local development.
- **McpTemplate.McpServer**: Implements MCP server with tools and prompts; add new tools in `Tools/`, add new prompts in `Prompts/`, and configure in `Program.cs`.

### Aspire Components
- **McpTemplate.AppHost**: Main entrypoint for hosting the application; configures and runs the Aspire Dashboard server for local development.
- **McpTemplate.ServiceDefaults**: Common service extensions and defaults; use for shared service setup.
