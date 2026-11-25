FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/Services/ApiYarnGateway/ApiYarnGateway.csproj src/Services/ApiYarnGateway/
RUN dotnet restore src/Services/ApiYarnGateway/ApiYarnGateway.csproj
COPY src/Services/ApiYarnGateway/ src/Services/ApiYarnGateway/
RUN dotnet publish src/Services/ApiYarnGateway/ApiYarnGateway.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ApiYarnGateway.dll"]
