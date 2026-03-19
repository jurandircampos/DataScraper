FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
# Directory for the shared sqlite db
RUN mkdir -p /app/data

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy projects
COPY ["ScraperApi/ScraperApi.csproj", "ScraperApi/"]
COPY ["ScraperCore/ScraperCore.csproj", "ScraperCore/"]

# Restore dependencies
RUN dotnet restore "ScraperApi/ScraperApi.csproj"

# Copy everything else
COPY . .
WORKDIR "/src/ScraperApi"
RUN dotnet build "ScraperApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ScraperApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ScraperApi.dll"]
