FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Services/Warehouse/Warehouse.API/Warehouse.API.csproj", "src/Services/Warehouse/Warehouse.API/"]
COPY ["src/BuildingBlocks/Core.Messaging/Core.Messaging.csproj", "src/BuildingBlocks/Core.Messaging/"]
COPY ["src/BuildingBlocks/Core.Domain/Core.Domain.csproj", "src/BuildingBlocks/Core.Domain/"]
RUN dotnet restore "./src/Services/Warehouse/Warehouse.API/Warehouse.API.csproj"
COPY . .
WORKDIR "/src/src/Services/Warehouse/Warehouse.API"
RUN dotnet build "./Warehouse.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Warehouse.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Warehouse.API.dll"]