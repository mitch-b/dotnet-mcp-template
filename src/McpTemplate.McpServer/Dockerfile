# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG RUNTIME_IDENTIFIER=linux-musl-arm64

WORKDIR /src

COPY . .

RUN dotnet publish McpTemplate.McpServer/McpTemplate.McpServer.csproj -c Release -o /app/publish --no-restore \
    -p:PublishTrimmed=true -p:PublishReadyToRun=true -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true -r ${RUNTIME_IDENTIFIER}

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final

WORKDIR /app
COPY --from=build /app/publish .

# Use a non-root user for security (optional, but recommended)
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["/app/McpTemplate.McpServer"]
