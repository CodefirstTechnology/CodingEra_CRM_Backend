# See https://aka.ms/customizecontainer to learn how to customize your debug container

# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY . .

RUN dotnet restore Backend_ERP.sln

RUN dotnet publish Backend_ERP/Backend_ERP.API/Backend_ERP.API.csproj -c Release -o /app/publish

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Backend_ERP.API.dll"]
