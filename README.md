# Rental Pipeline API

A REST API built with **.NET 10** to manage the complete lifecycle of residential rental proposals.

This project was developed as a technical challenge with the objective of demonstrating software engineering best practices, domain modeling, clean architecture, data consistency, and automated testing.

---

# Features

- Property Management
- Customer Management
- Rental Proposal Management
- Proposal State Machine
- Proposal Status History
- Event Publishing Simulation
- Concurrency Protection
- Swagger Documentation
- Docker Support
- Unit Tests
- Integration Tests

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

- .NET 10 SDK
- Docker
- Docker Compose

---

# Running the Project

## Clone the repository

```bash
git clone <repository-url>

cd RentalPipeline
```

## Start PostgreSQL

```bash
docker compose up -d
```

## Apply database migrations

```bash
dotnet ef database update
```

## Run the application

```bash
dotnet run --project src/RentalPipeline.Api
```

---

# Running the Tests

Run all tests:

```bash
dotnet test
```

The project contains:

- Unit Tests
- Integration Tests

---

# API Documentation

After running the application, Swagger will be available at:

```
https://localhost:<port>/swagger
```

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
- Every proposal transition generates a history record.
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