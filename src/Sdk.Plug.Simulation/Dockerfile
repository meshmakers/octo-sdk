FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Sdk.Plug.Simulation/Sdk.Plug.Simulation.csproj", "src/Sdk.Plug.Simulation/"]
RUN dotnet restore "src/Sdk.Plug.Simulation/Sdk.Plug.Simulation.csproj"
COPY . .
WORKDIR "/src/src/Sdk.Plug.Simulation"
RUN dotnet build "Sdk.Plug.Simulation.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Sdk.Plug.Simulation.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sdk.Plug.Simulation.dll"]
