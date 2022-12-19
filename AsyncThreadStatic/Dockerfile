# FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["AsyncThreadStatic/AsyncThreadStatic.csproj", "AsyncThreadStatic/"]
RUN dotnet restore "AsyncThreadStatic/AsyncThreadStatic.csproj"
COPY . .
WORKDIR "/src/AsyncThreadStatic"
RUN dotnet build "AsyncThreadStatic.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AsyncThreadStatic.csproj" -c Release -o /app/publish

FROM base AS final

RUN mkdir "/root/.dotnet/tools"
RUN PATH="/root/.dotnet/tools:${PATH}"
RUN dotnet tool install -g dotnet-counters

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AsyncThreadStatic.dll", "--urls", "http://0.0.0.0:80"]
