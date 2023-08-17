#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
RUN apt-get update && apt-get install -y libgdiplus
RUN apt-get install -y openssl 


WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["TcpMultiplexer.Server/TcpMultiplexer.Server.csproj", "TcpMultiplexer.Server/"]
COPY ["ModelingEvolution.IO/ModelingEvolution.IO.csproj", "ModelingEvolution.IO/"]
RUN dotnet restore "TcpMultiplexer.Server/TcpMultiplexer.Server.csproj"
RUN dotnet restore "ModelingEvolution.IO/ModelingEvolution.IO.csproj"
COPY . .
WORKDIR "/src/TcpMultiplexer.Server"
RUN dotnet build "TcpMultiplexer.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TcpMultiplexer.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TcpMultiplexer.Server.dll"]
