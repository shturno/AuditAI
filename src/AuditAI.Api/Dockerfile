# Dockerfile for AuditAI.Api

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy solution and project files
COPY *.sln .
COPY src/AuditAI.Api/*.csproj src/AuditAI.Api/
COPY src/AuditAI.Application/*.csproj src/AuditAI.Application/
COPY src/AuditAI.Domain/*.csproj src/AuditAI.Domain/
COPY src/AuditAI.Infrastructure/*.csproj src/AuditAI.Infrastructure/
COPY tests/AuditAI.UnitTests/*.csproj tests/AuditAI.UnitTests/
COPY tests/AuditAI.IntegrationTests/*.csproj tests/AuditAI.IntegrationTests/

# Restore dependencies
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AuditAI.Api.dll"]
