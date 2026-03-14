FROM dhi.io/dotnet:10-sdk-debian13 AS build
WORKDIR /src

COPY . .

RUN find . -name "appsettings*.json" -type f -delete

RUN dotnet restore "ProjectIqraBackendApp/ProjectIqraBackendApp.csproj"

WORKDIR "/src/ProjectIqraBackendApp"
RUN dotnet publish "ProjectIqraBackendApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM dhi.io/dotnet:10-debian13 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "ProjectIqraBackendApp.dll"]