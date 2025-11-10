FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar arquivos de projeto
COPY ["src/PagueVeloz.TransactionProcessor.Api/PagueVeloz.TransactionProcessor.Api.csproj", "src/PagueVeloz.TransactionProcessor.Api/"]
COPY ["src/PagueVeloz.TransactionProcessor.Application/PagueVeloz.TransactionProcessor.Application.csproj", "src/PagueVeloz.TransactionProcessor.Application/"]
COPY ["src/PagueVeloz.TransactionProcessor.Domain/PagueVeloz.TransactionProcessor.Domain.csproj", "src/PagueVeloz.TransactionProcessor.Domain/"]
COPY ["src/PagueVeloz.TransactionProcessor.Infrastructure/PagueVeloz.TransactionProcessor.Infrastructure.csproj", "src/PagueVeloz.TransactionProcessor.Infrastructure/"]

# Restaurar dependências
RUN dotnet restore "src/PagueVeloz.TransactionProcessor.Api/PagueVeloz.TransactionProcessor.Api.csproj"

# Copiar todo o código
COPY . .

# Build
WORKDIR /src
RUN dotnet build "src/PagueVeloz.TransactionProcessor.Api/PagueVeloz.TransactionProcessor.Api.csproj" -c Release -o /app/build

FROM build AS publish
WORKDIR /src
RUN dotnet publish "src/PagueVeloz.TransactionProcessor.Api/PagueVeloz.TransactionProcessor.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Criar diretório para logs
RUN mkdir -p /app/logs

ENTRYPOINT ["dotnet", "PagueVeloz.TransactionProcessor.Api.dll"]

