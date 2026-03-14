# Stage 1: Build using .NET 10 SDK on Debian 13 (Trixie)
FROM dhi.io/dotnet:10-sdk-debian13 AS build
WORKDIR /src

# Copy the entire solution
COPY . .

# Forcefully delete any appsettings files that might have slipped through
RUN find . -name "appsettings*.json" -type f -delete

# Restore specifically for the Frontend project
RUN dotnet restore "ProjectIqraFrontend/ProjectIqraFrontend.csproj"

# Publish the application
WORKDIR "/src/ProjectIqraFrontend"
RUN dotnet publish "ProjectIqraFrontend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Run using .NET 10 ASP.NET Runtime on Debian 13 (Trixie-slim)
FROM dhi.io/dotnet:10-debian13 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "ProjectIqraFrontend.dll"]