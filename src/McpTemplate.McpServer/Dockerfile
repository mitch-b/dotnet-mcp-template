# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build

ARG TARGETPLATFORM
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY . .

RUN dotnet publish McpTemplate.McpServer/McpTemplate.McpServer.csproj \
  -c ${BUILD_CONFIGURATION} -o /app/publish \
  -p:PublishTrimmed=true -p:PublishReadyToRun=true \
  -p:PublishSingleFile=true --arch ${TARGETARCH} \
  -p:IncludeNativeLibrariesForSelfExtract=true

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final

WORKDIR /app
COPY --from=build /app/publish .

# Use a non-root user for security (optional, but recommended)
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["/app/McpTemplate.McpServer"]
