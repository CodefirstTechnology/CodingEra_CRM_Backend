# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Backend_CRM/Backend_CRM.csproj", "Backend_CRM/"]
COPY ["CRM.Business/CRM.Business.csproj", "CRM.Business/"]
COPY ["CRM.Data/CRM.Data.csproj", "CRM.Data/"]
RUN dotnet restore "./Backend_CRM/Backend_CRM.csproj"
COPY . .
WORKDIR "/src/Backend_CRM"
RUN dotnet build "./Backend_CRM.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Backend_CRM.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Backend_CRM.dll"]
