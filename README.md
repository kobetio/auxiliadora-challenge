# Rental Pipeline API

🌍 Language
- 🇺🇸 English (current)
- 🇧🇷 [Português](README.pt-BR.md)

---

A REST API built with **.NET 10** to manage the complete lifecycle of residential rental proposals.

This project was developed as a technical challenge with the objective of demonstrating software engineering best practices, domain modeling, clean architecture, data consistency, and automated testing.

---

# Features

- Property Management (full CRUD)
- Customer Management (full CRUD)
- Rental Proposal Management
- Proposal State Machine
- Proposal Status History
- Event Publishing Simulation
- Concurrency Protection (Serializable transactions + Optimistic Concurrency)
- Swagger / OpenAPI Documentation
- Docker Support (auto-applies database migrations on startup)
- Unit Tests
- Integration Tests (real PostgreSQL via Testcontainers)

---

# Tech Stack

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

# Project Structure

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

# Prerequisites

Before running the project, make sure the following tools are installed:

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (only needed to run the API directly on the host, or to run the tests)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Docker Engine + Docker Compose)

---

# Running the Project

## Clone the repository

```bash
git clone https://github.com/kobetio/auxiliadora-challenge.git

cd auxiliadora-challenge
```

## Option A — Fully Dockerized (recommended)

Builds the API image and starts both the API and PostgreSQL containers. Database migrations are applied automatically on startup — no extra steps needed.

```bash
docker compose up --build
```

Once both containers are healthy, the API is available at:

```
http://localhost:8080
```

Swagger UI:

```
http://localhost:8080/swagger
```

To stop the containers:

```bash
docker compose down
```

## Option B — API on the host, PostgreSQL in Docker

Start only PostgreSQL (exposed on the host at port `5433`):

```bash
docker compose up -d postgres
```

Run the API directly with the .NET SDK. `appsettings.json` is already configured to connect to `localhost:5433`, and migrations are applied automatically on startup, just like in Option A:

```bash
dotnet run --project src/RentalPipeline.Api
```

The API is available at:

```
http://localhost:5023
```

Swagger UI:

```
http://localhost:5023/swagger
```

---

# Running the Tests

Run all tests (unit + integration):

```bash
dotnet test
```

The project contains:

- **Unit Tests** (`RentalPipeline.UnitTests`) — Domain and Application layers, using mocked dependencies. No external services required.
- **Integration Tests** (`RentalPipeline.IntegrationTests`) — full HTTP request pipeline against a real, ephemeral PostgreSQL instance started automatically with **Testcontainers**. **Docker must be running** for these to execute.

---

# API Documentation

After running the application (see above), Swagger is available at `/swagger` on whichever port you started the API on.

---

# Business Rules

The most important business rules are:

- Every property starts as **Available**.
- A proposal can only be created for **Available** properties.
- Creating a proposal changes the property status to **InNegotiation**.
- Proposal status transitions must follow the defined State Machine.
- Invalid transitions are rejected.
- When a proposal becomes **Active**, the property becomes **Rented**.
- Properties with status **Rented** are permanently removed from the rental market and are not returned by **GET /properties**.
- Rejected or Cancelled proposals return the property to **Available**.
- Every proposal transition — including its initial creation — generates a history record.
- Activating a proposal simulates publishing an integration event.

---

# Architecture

This project follows **Clean Architecture** with a lightweight **DDD (DDD Lite)** approach.

Additional information about the architectural decisions can be found in:

- **ARCHITECTURE_DECISIONS.md**

---

# Future Improvements

Some features were intentionally left outside the scope of this challenge and are documented in:

- RabbitMQ Integration
- JWT Authentication
- Authorization
- Redis Distributed Lock
- Outbox Pattern
- OpenTelemetry
- Kubernetes

See **ARCHITECTURE_DECISIONS.md** for more details.
