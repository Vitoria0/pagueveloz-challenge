# PagueVeloz Transaction Processor

Sistema backend em C# .NET 9 para processamento de transações financeiras, desenvolvido seguindo Clean Architecture e princípios DDD.

## 📋 Índice

- [Visão Geral](#visão-geral)
- [Arquitetura](#arquitetura)
- [Tecnologias](#tecnologias)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Requisitos](#requisitos)
- [Instalação e Execução](#instalação-e-execução)
- [API Endpoints](#api-endpoints)
- [Regras de Negócio](#regras-de-negócio)
- [Testes](#testes)
- [Docker](#docker)
- [Decisões Arquiteturais](#decisões-arquiteturais)

## 🎯 Visão Geral

Este sistema simula um processador de transações financeiras com suporte a:

- Operações financeiras (crédito, débito, reserva, captura, reversão, transferência)
- Controle de concorrência (lock otimista)
- Idempotência via `reference_id`
- Retry com backoff exponencial
- Event sourcing e domain events
- Health checks e métricas Prometheus
- Logs estruturados com Serilog

## 🏗️ Arquitetura

O projeto segue **Clean Architecture** (Onion/DDD) com as seguintes camadas:

```
PagueVeloz.TransactionProcessor/
├── src/
│   ├── PagueVeloz.TransactionProcessor.Api/          # Camada de apresentação (Controllers, Middleware)
│   ├── PagueVeloz.TransactionProcessor.Application/  # Casos de uso (CQRS, Handlers, DTOs)
│   ├── PagueVeloz.TransactionProcessor.Domain/        # Entidades, Value Objects, Domain Events
│   └── PagueVeloz.TransactionProcessor.Infrastructure/# Implementações (EF Core, Repositórios)
└── tests/
    └── PagueVeloz.TransactionProcessor.Tests/          # Testes unitários e de integração
```

### Princípios Aplicados

- **SOLID**: Separação de responsabilidades, inversão de dependências
- **DDD**: Entidades ricas, value objects, domain events
- **CQRS**: Separação de comandos e consultas
- **Event Sourcing**: Eventos de domínio assíncronos via Channel

## 🛠️ Tecnologias

- **.NET 9.0**
- **ASP.NET Core 9** (Minimal API)
- **Entity Framework Core 9** com PostgreSQL
- **MediatR** (CQRS e Domain Events)
- **Polly** (Retry, Backoff, Circuit Breaker)
- **FluentValidation**
- **Serilog** (Logging estruturado)
- **Swagger/OpenAPI**
- **xUnit** e **Moq** (Testes)
- **Docker** e **Docker Compose**

## 📁 Estrutura do Projeto

### Domain Layer

- **Entities**: `Account`, `Client`, `Transaction`
- **Enums**: `AccountStatus`, `TransactionOperation`, `TransactionStatus`
- **Events**: `TransactionProcessedEvent`, `AccountBlockedEvent`, etc.
- **Value Objects**: `Money`
- **Repositories**: Interfaces dos repositórios

### Application Layer

- **Commands**: `CreateAccountCommand`, `CreateTransactionCommand`
- **Queries**: `GetAccountQuery`, `GetAccountTransactionsQuery`
- **Handlers**: Implementação dos handlers CQRS
- **DTOs**: Objetos de transferência de dados
- **Validators**: Validações com FluentValidation
- **Behaviors**: Pipeline behaviors (Validation, Logging)

### Infrastructure Layer

- **Data**: `ApplicationDbContext`, configurações EF Core
- **Repositories**: Implementações dos repositórios
- **Services**: `DomainEventDispatcher` (processamento assíncrono de eventos)

### API Layer

- **Controllers**: `AccountsController`, `TransactionsController`
- **Program.cs**: Configuração da aplicação, middleware, DI

## 📦 Requisitos

- .NET 9 SDK
- Docker e Docker Compose (opcional, para PostgreSQL)
- PostgreSQL 16+ (se não usar Docker)

## 🚀 Instalação e Execução

### Opção 1: Docker Compose (Recomendado)

```bash
# Clonar o repositório
git clone <repository-url>
cd PagueVeloz.TransactionProcessor

# Executar com Docker Compose
docker-compose up -d

# A API estará disponível em:
# - HTTP: http://localhost:5000
# - HTTPS: https://localhost:5001
# - Swagger: http://localhost:5000/swagger
```

### Opção 2: Execução Local

```bash
# Restaurar dependências
dotnet restore

# Aplicar migrações (se necessário)
cd src/PagueVeloz.TransactionProcessor.Api
dotnet ef database update

# Executar a aplicação
dotnet run

# A API estará disponível em:
# - HTTP: http://localhost:5000
# - HTTPS: https://localhost:5001
```

### Configuração do Banco de Dados

Edite `appsettings.json` ou `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=pagueveloz_db;Username=postgres;Password=postgres"
  }
}
```

## 🌐 API Endpoints

### Contas

#### POST /api/accounts

Cria uma nova conta.

**Request:**

```json
{
  "client_id": "CLI-001",
  "initial_balance": 0,
  "credit_limit": 50000
}
```

**Response:**

```json
{
  "accountId": "ACC-1234567890abcdef",
  "clientId": "CLI-001",
  "balance": 0,
  "reservedBalance": 0,
  "creditLimit": 50000,
  "availableBalance": 50000,
  "status": 1
}
```

#### GET /api/accounts/{id}

Obtém uma conta por ID.

#### GET /api/accounts/{id}/transactions

Obtém todas as transações de uma conta.

### Transações

#### POST /api/transactions

Cria uma nova transação financeira.

**Request (Crédito):**

```json
{
  "operation": 1,
  "account_id": "ACC-001",
  "amount": 100000,
  "currency": "BRL",
  "reference_id": "TXN-001",
  "metadata": {
    "description": "Depósito inicial"
  }
}
```

**Request (Débito):**

```json
{
  "operation": 2,
  "account_id": "ACC-001",
  "amount": 50000,
  "currency": "BRL",
  "reference_id": "TXN-002"
}
```

**Request (Reserva):**

```json
{
  "operation": 3,
  "account_id": "ACC-001",
  "amount": 30000,
  "currency": "BRL",
  "reference_id": "TXN-003"
}
```

**Request (Captura):**

```json
{
  "operation": 4,
  "account_id": "ACC-001",
  "amount": 30000,
  "currency": "BRL",
  "reference_id": "TXN-004"
}
```

**Request (Reversão):**

```json
{
  "operation": 5,
  "account_id": "ACC-001",
  "amount": 0,
  "currency": "BRL",
  "reference_id": "TXN-005",
  "original_reference_id": "TXN-001"
}
```

**Request (Transferência):**

```json
{
  "operation": 6,
  "account_id": "ACC-001",
  "destination_account_id": "ACC-002",
  "amount": 20000,
  "currency": "BRL",
  "reference_id": "TXN-006"
}
```

**Response:**

```json
{
  "transactionId": "TXN-001-PROCESSED",
  "status": "success",
  "balance": 100000,
  "reservedBalance": 0,
  "availableBalance": 100000,
  "timestamp": "2025-01-07T20:05:00Z",
  "errorMessage": null
}
```

### Health Checks

- **GET /health**: Health check básico
- **GET /health-ui**: Interface de health checks
- **GET /metrics**: Métricas Prometheus

## 💰 Regras de Negócio

### Operações Financeiras

1. **Credit**: Adiciona valor ao saldo da conta
2. **Debit**: Subtrai valor do saldo disponível (saldo + limite de crédito)
3. **Reserve**: Move valor do saldo disponível para saldo reservado
4. **Capture**: Confirma reserva, removendo do saldo reservado
5. **Reversal**: Reverte uma operação anterior
6. **Transfer**: Move valor entre duas contas

### Validações

- Saldo disponível nunca pode ficar negativo
- Respeitar limite de crédito
- Reservas só com saldo disponível suficiente
- Capturas só se houver saldo reservado suficiente
- Operações na mesma conta são atômicas e thread-safe
- Idempotência via `reference_id` (transações duplicadas retornam o mesmo resultado)

### Concorrência

- **Lock Otimista**: Usando `IsConcurrencyToken()` no EF Core
- Retry automático em caso de conflito de concorrência
- Backoff exponencial com Polly (3 tentativas)

## 🧪 Testes

### Executar Testes

```bash
# Todos os testes
dotnet test

# Com cobertura
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Testes específicos
dotnet test --filter "FullyQualifiedName~AccountTests"
```

### Cobertura de Testes

- Testes unitários para entidades de domínio
- Testes de integração para repositórios
- Testes de handlers CQRS
- Testes de concorrência
- Testes de idempotência

**Cobertura mínima esperada: 80%**

## 🐳 Docker

### Dockerfile

O Dockerfile está configurado para build multi-stage, otimizando o tamanho da imagem final.

### Docker Compose

O `docker-compose.yml` inclui:

- **PostgreSQL 16**: Banco de dados
- **API**: Aplicação .NET 9

### Comandos Úteis

```bash
# Iniciar serviços
docker-compose up -d

# Ver logs
docker-compose logs -f api

# Parar serviços
docker-compose down

# Rebuild
docker-compose up -d --build

# Limpar volumes
docker-compose down -v
```

## 🎓 Decisões Arquiteturais

### 1. Clean Architecture

- Separação clara de responsabilidades
- Independência de frameworks
- Testabilidade

### 2. CQRS com MediatR

- Separação de comandos e consultas
- Facilita escalabilidade e manutenção
- Pipeline behaviors para cross-cutting concerns

### 3. Event Sourcing (Opcional)

- Domain events via Channel
- Processamento assíncrono
- Facilita auditoria e rastreabilidade

### 4. Concorrência

- Lock otimista no EF Core
- Retry com backoff exponencial
- Tratamento de conflitos

### 5. Idempotência

- Verificação de `reference_id` antes de processar
- Retorno do resultado existente para transações duplicadas

### 6. Resiliência

- Polly para retry e circuit breaker
- Health checks para monitoramento
- Logs estruturados com Serilog

### 7. Observabilidade

- Métricas Prometheus
- Health checks
- Logs estruturados (JSON)

## 📝 Exemplos de Requisições cURL

### Criar Conta

```bash
curl -X POST http://localhost:5000/api/accounts \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": "CLI-001",
    "initialBalance": 0,
    "creditLimit": 50000
  }'
```

### Criar Transação (Crédito)

```bash
curl -X POST http://localhost:5000/api/transactions \
  -H "Content-Type: application/json" \
  -d '{
    "operation": 1,
    "accountId": "ACC-001",
    "amount": 100000,
    "currency": "BRL",
    "referenceId": "TXN-001"
  }'
```

### Obter Conta

```bash
curl -X GET http://localhost:5000/api/accounts/ACC-001
```

### Obter Transações da Conta

```bash
curl -X GET http://localhost:5000/api/accounts/ACC-001/transactions
```

## 🔒 Segurança

- Validação de entrada com FluentValidation
- Tratamento de erros padronizado
- Logs sem informações sensíveis

## 📊 Monitoramento

- **Health Checks**: `/health` e `/health-ui`
- **Métricas Prometheus**: `/metrics`
- **Logs Estruturados**: Arquivos em `logs/` e console (JSON)

## 🤝 Contribuindo

1. Fork o projeto
2. Crie uma branch para sua feature (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

## 📄 Licença

Este projeto foi desenvolvido como parte de um desafio técnico.

---

**Nota**: Este é um sistema de demonstração. Para uso em produção, considere adicionar autenticação, autorização, rate limiting e outras medidas de segurança.
