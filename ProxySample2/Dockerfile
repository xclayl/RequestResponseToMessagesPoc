#  docker build -t http2.azurecr.io/sample/psample:1.0 -f ProxySample2/Dockerfile . 
#  docker push http2.azurecr.io/sample/psample:1.0 
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["ProxySample2/ProxySample2.csproj", "ProxySample2/"]
RUN dotnet restore "ProxySample2/ProxySample2.csproj"
COPY . .
WORKDIR "/src/ProxySample2"
RUN dotnet build "ProxySample2.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProxySample2.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProxySample2.dll"]
