# Rental Pipeline API

🌍 Idioma
- 🇧🇷 Português (atual)
- 🇺🇸 [English](README.md)

---

API REST construída com **.NET 10** para gerenciar o ciclo de vida completo da Esteira de Contratos de Aluguel.

Este projeto foi desenvolvido como um desafio técnico com o objetivo de demonstrar boas práticas de engenharia de software, modelagem de domínio, clean architecture, consistência de dados e testes automatizados.

---

# Funcionalidades

- Gestão de Imóveis (CRUD completo)
- Gestão de Clientes (CRUD completo)
- Gestão de Propostas de Locação
- Máquina de Estados da Proposta
- Histórico de Status da Proposta
- Simulação de Publicação de Eventos
- Proteção contra Concorrência (transações Serializable + Concorrência Otimista)
- Documentação Swagger / OpenAPI
- Suporte a Docker (aplica as migrations do banco de dados automaticamente na inicialização)
- Testes Unitários
- Testes de Integração (PostgreSQL real via Testcontainers)

---

# Stack Tecnológica

- .NET 10
- ASP.NET Core Web API
- PostgreSQL
- Entity Framework Core
- FluentValidation
- FluentResults
- Swagger / OpenAPI
- xUnit
- NSubstitute
- Testcontainers
- Docker

---

# Estrutura do Projeto

```
src/
    RentalPipeline.Api
    RentalPipeline.Application
    RentalPipeline.Domain
    RentalPipeline.Infrastructure

tests/
    RentalPipeline.UnitTests
    RentalPipeline.IntegrationTests
```

---

# Pré-requisitos

Antes de executar o projeto, certifique-se de ter as seguintes ferramentas instaladas:

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (necessário apenas para rodar a API diretamente na máquina host, ou para rodar os testes)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Docker Engine + Docker Compose)

---

# Executando o Projeto

## Clonar o repositório

```bash
git clone https://github.com/kobetio/auxiliadora-challenge.git

cd auxiliadora-challenge
```

## Opção A — Totalmente via Docker (recomendado)

Constrói a imagem da API e sobe os containers da API e do PostgreSQL. As migrações do banco de dados são aplicadas automaticamente na inicialização — nenhum passo extra é necessário.

```bash
docker compose up --build
```

Assim que os dois containers estiverem saudáveis, a API estará disponível em:

```
http://localhost:8080
```

Swagger UI:

```
http://localhost:8080/swagger
```

Para parar os containers:

```bash
docker compose down
```

## Opção B — API na máquina host, PostgreSQL no Docker

Suba apenas o PostgreSQL (exposto na máquina host na porta `5433`):

```bash
docker compose up -d postgres
```

Execute a API diretamente com o .NET SDK. O `appsettings.json` já está configurado para conectar em `localhost:5433`, e as migrations são aplicadas automaticamente na inicialização, assim como na Opção A:

```bash
dotnet run --project src/RentalPipeline.Api
```

A API estará disponível em:

```
http://localhost:5023
```

Swagger UI:

```
http://localhost:5023/swagger
```

---

# Executando os Testes

Executar todos os testes (unitários + integração):

```bash
dotnet test
```

O projeto contém:

- **Testes Unitários** (`RentalPipeline.UnitTests`) — camadas de Domain e Application, usando dependências mockadas. Nenhum serviço externo é necessário.
- **Testes de Integração** (`RentalPipeline.IntegrationTests`) — pipeline HTTP completo contra uma instância real e efêmera de PostgreSQL, iniciada automaticamente com **Testcontainers**. **O Docker precisa estar em execução** para que esses testes rodem.

---

# Documentação da API

Após executar a aplicação (veja acima), o Swagger fica disponível em `/swagger`, na porta em que a API foi iniciada.

---

# Regras de Negócio

As regras de negócio mais importantes são:

- Todo imóvel novo começa com status **Available**.
- Uma proposta só pode ser criada para imóveis com status **Available**.
- Ao criar uma proposta, o status do imóvel muda para **InNegotiation**.
- As transições de status da proposta devem seguir a Máquina de Estados definida.
- Transições inválidas são rejeitadas.
- Quando uma proposta se torna **Active**, o imóvel se torna **Rented**.
- Imóveis com status **Rented** são removidos permanentemente do mercado de locação e não são retornados por **GET /properties**.
- Propostas com status **Rejected** ou **Cancelled** fazem o imóvel retornar para **Available**.
- Toda transição de proposta — incluindo sua criação inicial — gera um registro de histórico.
- A ativação de uma proposta simula a publicação de um evento de integração.

---

# Arquitetura

Este projeto segue **Clean Architecture** com abordagem de **DDD (DDD Lite)**.

Informações adicionais sobre as decisões arquiteturais podem ser encontradas em:

- **[ARCHITECTURE_DECISIONS.pt-BR.md](ARCHITECTURE_DECISIONS.pt-BR.md)**

---

# Melhorias Futuras

Algumas funcionalidades foram intencionalmente deixadas fora do escopo deste desafio e estão documentadas em:

- Integração com RabbitMQ
- Autenticação JWT
- Autorização
- Redis Distributed Lock
- Outbox Pattern
- OpenTelemetry
- Kubernetes

Veja **[ARCHITECTURE_DECISIONS.pt-BR.md](ARCHITECTURE_DECISIONS.pt-BR.md)** para mais detalhes.
