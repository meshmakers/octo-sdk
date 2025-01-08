FROM mcr.microsoft.com/dotnet/runtime:9.0-bookworm-slim AS base
USER $APP_UID
WORKDIR /app

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build
ARG BUILD_CONFIGURATION=Release
ARG OCTO_PRIVATE_NUGET_SERVICE
ARG OCTO_PRIVATE_NUGET_CERTIFICATE

COPY ${OCTO_PRIVATE_NUGET_CERTIFICATE} /usr/local/share/ca-certificates/nugetca.crt
RUN chmod 644 /usr/local/share/ca-certificates/nugetca.crt && update-ca-certificates

WORKDIR /src
COPY ["src/Sdk.Plug.Simulation/Sdk.Plug.Simulation.csproj", "src/Sdk.Plug.Simulation/"]
RUN dotnet restore "src/Sdk.Plug.Simulation/Sdk.Plug.Simulation.csproj"
COPY . .
WORKDIR "/src/src/Sdk.Plug.Simulation"
RUN dotnet build "Sdk.Plug.Simulation.csproj" -c $BUILD_CONFIGURATION -p:OctoNugetPrivateServer=${OCTO_PRIVATE_NUGET_SERVICE} -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Sdk.Plug.Simulation.csproj" -c $BUILD_CONFIGURATION -p:OctoNugetPrivateServer=${OCTO_PRIVATE_NUGET_SERVICE} -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sdk.Plug.Simulation.dll"]
