FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["CsClassToTsConverter/CsClassToTsConverter.csproj", "CsClassToTsConverter/"]
RUN dotnet restore "CsClassToTsConverter/CsClassToTsConverter.csproj"
COPY . .
WORKDIR "/src/CsClassToTsConverter"
RUN dotnet build "CsClassToTsConverter.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CsClassToTsConverter.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CsClassToTsConverter.dll"]
