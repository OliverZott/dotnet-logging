FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["dotnet-logging-sample1/dotnet-logging-sample1.csproj", "dotnet-logging-sample1/"]
RUN dotnet restore "dotnet-logging-sample1/dotnet-logging-sample1.csproj"
COPY . .
WORKDIR "/src/dotnet-logging-sample1"
RUN dotnet build "./dotnet-logging-sample1.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./dotnet-logging-sample1.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "dotnet-logging-sample1.dll"]
