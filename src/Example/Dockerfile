#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /src
COPY ["src/Example/TDengine-client-2.0.1.1-Linux-x64.tar.gz", "src/Example/TDengine-client-2.0.1.1-Linux-x64.tar.gz"]
RUN  tar -xzvf src/Example/TDengine-client-2.0.1.1-Linux-x64.tar.gz
RUN  cd TDengine-client && ./install_client.sh
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["src/Example/Example.csproj", "src/Example/"]
COPY ["src/Maikebing.Data.Taos/Maikebing.Data.Taos.csproj", "src/Maikebing.Data.Taos/"]
COPY ["src/EFCore.Taos.Core/Maikebing.EntityFrameworkCore.Taos.csproj", "src/EFCore.Taos.Core/"]
RUN dotnet restore "src/Example/Example.csproj"
COPY . .
WORKDIR "/src/src/Example"
RUN dotnet build "Example.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Example.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Example.dll"]