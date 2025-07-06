# OAuth-Protected MCP Server

This server demonstrates how to protect MCP endpoints using OAuth 2.0 and JWT Bearer authentication, following the Model Context Protocol (MCP) sample patterns.

## Features
- OAuth 2.0 protection for all MCP endpoints
- JWT Bearer authentication and ASP.NET Core authorization
- OAuth resource metadata at `/.well-known/oauth-protected-resource`
- Example weather tools (see `Tools/WeatherTools.cs`)

## Configuration

See `appsettings.json` for OAuth and MCP options. Example:

```json
"OAuth": {
  "Authority": "https://localhost:7029",
  "Audience": "demo-client",
  "ClientId": "demo-client",
  "ClientSecret": "demo-secret",
  "RedirectUri": "http://localhost:1179/callback",
  "Scopes": [ "mcp:tools" ]
}
```

## Running
1. Start the Test OAuth Server (see MCP SDK samples)
2. Run this server: `dotnet run`
3. Access the MCP endpoint (requires a valid access token)

## Endpoints
- `/` MCP endpoint (protected)
- `/.well-known/oauth-protected-resource` OAuth resource metadata

## References
- [ProtectedMCPServer sample](https://github.com/modelcontextprotocol/csharp-sdk/tree/main/samples/ProtectedMCPServer)
- [ProtectedMCPClient sample](https://github.com/modelcontextprotocol/csharp-sdk/tree/main/samples/ProtectedMCPClient)
