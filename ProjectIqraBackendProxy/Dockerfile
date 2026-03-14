FROM dhi.io/dotnet:10-sdk-debian13 AS build
WORKDIR /src

COPY . .

RUN find . -name "appsettings*.json" -type f -delete

RUN dotnet restore "ProjectIqraBackendProxy/ProjectIqraBackendProxy.csproj"

WORKDIR "/src/ProjectIqraBackendProxy"
RUN dotnet publish "ProjectIqraBackendProxy.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM dhi.io/dotnet:10-debian13 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "ProjectIqraBackendProxy.dll"]