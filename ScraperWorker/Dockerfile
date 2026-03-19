FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app
# Directory for the shared sqlite db
RUN mkdir -p /app/data

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy projects
COPY ["ScraperWorker/ScraperWorker.csproj", "ScraperWorker/"]
COPY ["ScraperCore/ScraperCore.csproj", "ScraperCore/"]

# Restore dependencies
RUN dotnet restore "ScraperWorker/ScraperWorker.csproj"

# Copy everything else
COPY . .
WORKDIR "/src/ScraperWorker"
RUN dotnet build "ScraperWorker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ScraperWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ScraperWorker.dll"]
