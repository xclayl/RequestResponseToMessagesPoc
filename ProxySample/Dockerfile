FROM mcr.microsoft.com/dotnet/sdk:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ProxySample/ProxySample.csproj", "ProxySample/"]
RUN dotnet restore "ProxySample/ProxySample.csproj"
COPY . .
WORKDIR "/src/ProxySample"
RUN dotnet build "ProxySample.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProxySample.csproj" -c Release -o /app/publish

FROM base AS final

RUN mkdir "/root/.dotnet/tools"
RUN PATH="/root/.dotnet/tools:${PATH}"
RUN dotnet tool install -g dotnet-counters

WORKDIR /app
COPY --from=publish /app/publish .
ENV DOTNET_gcConcurrent=1
ENTRYPOINT ["dotnet", "ProxySample.dll", "--urls", "http://0.0.0.0:80"]
