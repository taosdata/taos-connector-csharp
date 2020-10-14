#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /src
#COPY ["src/Example/TDengine-client-2.0.5.1-Linux-x64.tar.gz", "src/Example/TDengine-client-2.0.5.1-Linux-x64.tar.gz"]
RUN curl -o TDengine-client.tar.gz https://www.taosdata.com/download/download-gettingStarted.php?pkg=tdengine_linux && \
   tar -xzvf TDengine-client.tar.gz && rm TDengine-client.tar.gz -f  && cd  $(ls TDengine-client*  -d) && ./install_client.sh && \
    rm $(pwd) -rf
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["src/Example/Example.csproj", "src/Example/"]
COPY ["src/Maikebing.Data.Taos/Maikebing.Data.Taos.csproj", "src/Maikebing.Data.Taos/"]
COPY ["src/EFCore.Taos.Core/Maikebing.EntityFrameworkCore.Taos.csproj", "src/EFCore.Taos.Core/"]
COPY ["src/LICENSE", "LICENSE"]
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