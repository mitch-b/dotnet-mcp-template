# .NET MCP Server Template

* [Streamable HTTP](https://modelcontextprotocol.io/docs/concepts/transports) from Aspire host runtime.
* .NET 9 application host
* Docker container build

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

## User Secrets

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
  }
}
```

or, you can use the command line to set them:

```bash
dotnet user-secrets set McpTemplateOptions:Endpoint https://{name}.openai.azure.com
dotnet user-secrets set McpTemplateOptions:ApiKey <your_api_key>
dotnet user-secrets set McpTemplateOptions:Model gpt-4o-mini
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
