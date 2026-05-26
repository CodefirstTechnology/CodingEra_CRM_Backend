GNU nano 7.2                           Dockerfile
# See https://aka.ms/customizecontainer to learn how to customize your debug container>

# This stage is used when running from VS in fast mode (Default for Debug configuratio>
# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY . .

RUN dotnet restore

RUN dotnet publish -c Release -o /app/publish

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Backend_CRM.dll"]