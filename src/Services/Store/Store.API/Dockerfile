FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Services/Store/Store.API/Store.API.csproj", "src/Services/Store/Store.API/"]
COPY ["src/BuildingBlocks/Core.Messaging/Core.Messaging.csproj", "src/BuildingBlocks/Core.Messaging/"]
COPY ["src/BuildingBlocks/Core.Domain/Core.Domain.csproj", "src/BuildingBlocks/Core.Domain/"]
RUN dotnet restore "./src/Services/Store/Store.API/Store.API.csproj"
COPY . .
WORKDIR "/src/src/Services/Store/Store.API"
RUN dotnet build "./Store.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Store.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Store.API.dll"]