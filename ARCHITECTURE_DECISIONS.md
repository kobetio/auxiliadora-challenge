# Architecture Decisions

This document describes the main architectural and technical decisions made during the implementation of the Rental Pipeline API.

---

# Why .NET 10?

.NET 10 was chosen because it provides:

- Excellent performance
- Native Dependency Injection
- Modern hosting model
- Long-term maintainability
- Strong ecosystem

---

# Why Clean Architecture?

The challenge contains several business rules that should remain independent from infrastructure concerns.

Clean Architecture provides:

- Separation of concerns
- Better testability
- Low coupling
- High maintainability
- Easier future evolution

Business rules remain isolated from frameworks and external dependencies.

---

# Why DDD Lite?

The project models the business domain using selected Domain-Driven Design concepts without introducing unnecessary complexity.

Implemented concepts:

- Entities
- Aggregate Root
- Domain Services
- Repository Interfaces
- State Machine

Intentionally omitted:

- CQRS
- MediatR
- Event Sourcing
- Specifications
- Factories
- Domain Events

This approach keeps the solution simple while still providing a rich domain model.

---

# Why PostgreSQL?

PostgreSQL was selected because it offers:

- ACID transactions
- Excellent concurrency support
- Serializable transaction isolation
- Row locking
- Reliability
- Great integration with Entity Framework Core

These features are particularly important for protecting the proposal creation process against concurrent requests.

---

# Why Entity Framework Core?

Entity Framework Core was selected because it provides:

- Productivity
- Strong typing
- LINQ support
- Code First Migrations
- Optimistic Concurrency support
- Excellent .NET integration

---

# Why FluentValidation?

Request validation should remain separated from business logic.

FluentValidation allows validation rules to remain:

- Reusable
- Readable
- Easily testable

Business rules remain inside the Domain/Application layers.

---

# Why FluentResults?

Application Services return Result<T> instead of throwing exceptions for expected business scenarios.

Benefits:

- Explicit execution flow
- Easier testing
- Better readability
- Predictable API responses

Exceptions are reserved for unexpected failures.

---

# Why a Dedicated Proposal State Machine?

Proposal status transitions are centralized inside a dedicated ProposalStateMachine.

Instead of spreading transition rules across multiple services or using complex conditional statements, a transition map is used.

Benefits:

- Single source of truth
- Easier maintenance
- Easier testing
- Easy to extend
- Eliminates duplicated business rules

---

# Event Publishing Strategy

The challenge requires simulating an asynchronous event when a proposal becomes Active.

Instead of directly integrating RabbitMQ, the application introduces an abstraction:

```
IEventPublisher
```

The current implementation:

```
FakeEventPublisher
```

Responsibilities:

- Structured logging
- Event simulation
- Infrastructure abstraction

Future RabbitMQ integration will require replacing only the infrastructure implementation.

No changes should be necessary in the Domain or Application layers.

---

# Concurrency Strategy

One of the challenge requirements is preventing race conditions during proposal creation.

The chosen strategy combines PostgreSQL native mechanisms with Entity Framework Core.

Implemented approach:

- Database Transactions
- Serializable Isolation Level
- Optimistic Concurrency

This guarantees that two simultaneous requests cannot create proposals for the same property.

Consistency was intentionally prioritized over performance.

---

# Why Not Redis?

Redis Distributed Lock was intentionally not implemented.

Reasons:

- PostgreSQL already provides the required consistency guarantees.
- Introducing Redis would increase architectural complexity.
- The expected workload of this challenge does not justify an additional infrastructure component.

Redis is documented as a possible future improvement for high-scale distributed environments.

---

# Future Improvements

The current architecture was designed to support future evolution with minimal changes.

Possible future enhancements include:

- RabbitMQ
- Outbox Pattern
- JWT Authentication
- Authorization
- Redis Cache
- Redis Distributed Lock
- OpenTelemetry
- Health Checks
- Rate Limiting
- API Versioning
- Background Jobs
- CI/CD Pipeline
- GitHub Actions
- Kubernetes
- Horizontal Scaling

The architectural decisions adopted in this project aim to keep these future improvements isolated from the core business logic.