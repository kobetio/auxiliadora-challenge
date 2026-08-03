# Architecture Decisions

🌍 Language
- 🇺🇸 English (current)
- 🇧🇷 [Português](ARCHITECTURE_DECISIONS.pt-BR.md)

---

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

# Full CRUD for Property and Customer

The original specification only documents Create and Get operations for Property and Customer.

Full Update and Delete operations were added for both entities on explicit request, to provide a complete and consistent CRUD experience across the API.

Delete operations include a safe-delete guard: a Property or Customer with at least one associated rental proposal cannot be deleted. This prevents orphaned foreign keys and preserves the integrity of the proposal pipeline's history.

---

# Scope Priority: Proposal Flow and Entity States Over Financial Details

The challenge's core requirement is a correct rental proposal pipeline — status transitions, property reservation/release, concurrency safety, history, and event simulation.

Financial aspects of a proposal (rent amount, deposits, fees, payment terms, etc.) were intentionally left out of scope. The priority was the flow itself and the correct functioning of proposal and property state changes, not a complete commercial model of a rental contract.

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

# Optimistic Concurrency: RowVersion Mapped to PostgreSQL xmin

Concurrent updates to the same Property or RentalProposal must be detected and rejected safely, instead of silently overwriting each other.

Rather than maintaining a separate, manually-managed concurrency column, the optimistic concurrency token is mapped directly to PostgreSQL's native `xmin` system column, using the Npgsql EF Core provider's `IsRowVersion()` configuration.

This requires the concurrency token to be typed as `uint` (not `byte[]`, which is the more common convention for SQL Server) on the Domain entities, since that is how the Npgsql provider represents `xmin`.

Benefits:

- No extra column, trigger, or manual increment logic needed
- Native database support, updated automatically by PostgreSQL on every row change
- Conflicts surface as a `DbUpdateConcurrencyException`, mapped to `409 Conflict`

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

# Domain Exceptions: One Generic Type

The Domain layer defines a single `DomainException` type instead of a hierarchy of specific exception subclasses (e.g. one exception per invalid state).

Reasoning:

- These exceptions only exist as defense-in-depth safety nets, for states that should never occur if the Application layer performed its expected validation beforehand
- None of them are meant to be caught or handled differently from one another — they all represent the same category of "this should never happen"
- A hierarchy of subclasses would add ceremony without any real behavioral benefit

Expected business failures (an invalid proposal status transition, a property that is not available, etc.) are never represented as exceptions — they always flow through `Result<T>` instead.

---

# Why Manual Mapping Instead of AutoMapper?

Mapping between Domain entities and DTOs is done through small, explicit extension methods (e.g. `ToDto()`) instead of a mapping library like AutoMapper.

Reasons:

- The mappings involved are simple, with no complex flattening, nested collections, or conditional logic that would justify a mapping library
- Explicit mapping code is easier to read, debug and refactor safely, and gives full compiler-checked safety
- AutoMapper moved to a paid licensing model for commercial use, which is an unnecessary dependency and cost for this project's straightforward mapping needs

This keeps the Application layer free of a reflection-based library while remaining just as maintainable.

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

# Cross-Aggregate Coordination

`RentalProposal` and `Property` are separate Aggregate Roots. A single aggregate must never directly mutate another aggregate's state.

Because of this, side effects that span both aggregates — such as reserving or releasing a Property when a proposal's status changes — are coordinated by the Application Service layer, not by the `RentalProposal` entity itself.

Each aggregate stays responsible only for enforcing its own invariants:

- `RentalProposal` owns its own status transitions and status history
- `Property` owns its own status guard clauses

The Application Service loads both aggregates, applies the proposal's transition, applies the resulting property transition, and persists both changes through a single Unit of Work.

---

# Proposal History Includes Its Own Creation

The full lifecycle of a rental proposal must be visible through its history — including the moment it was created, not only its later status transitions.

For this reason, the "previous status" recorded in history is nullable: the entry created together with the proposal has no real previous status, and `null` communicates that more honestly than reusing the initial status as a fake "previous" value.

As a result, every rental proposal always has at least one history entry from the moment it exists, and its history endpoint always reflects the proposal's complete story, from creation through to its current state.

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

# Concurrency Implementation Details

The strategy above is implemented as two independent, complementary layers — either one alone would close the race in practice, but Architecture.md explicitly asks for both, and each protects against a slightly different anomaly:

**Layer 1 — Serializable transaction around the critical section.** `IUnitOfWork.ExecuteInSerializableTransactionAsync<TResult>` wraps a delegate in a `Database.BeginTransactionAsync(IsolationLevel.Serializable)` transaction (committed only if the delegate completes without throwing; any exception rolls it back via disposal). `RentalProposalService.CreateAsync` wraps exactly the read-check-reserve-create sequence from Architecture.md's "Transaction Flow" diagram (reading the Property, checking Rule 2, reserving it, and creating the Proposal) in this transaction — not the whole request, since the Customer existence check plays no part in the race. Under PostgreSQL's Serializable Snapshot Isolation, if two transactions' reads genuinely overlap before either commits, PostgreSQL detects the anomaly at commit time and aborts one side with a `40001 serialization_failure`.

**Layer 2 — Optimistic concurrency (`xmin`/`RowVersion`).** Even without any overlap in the transactions' read phases, both `Property` and `RentalProposal` carry a `RowVersion` mapped to PostgreSQL's `xmin`. Every `UPDATE` EF Core generates includes `WHERE Id = @id AND xmin = @originalXmin`; if another transaction already committed a change to that row, the second `UPDATE` affects zero rows and EF Core throws `DbUpdateConcurrencyException`.

**Both exceptions are mapped to `409 Conflict` in one place**: `ExceptionHandlingMiddleware` specifically catches `DbUpdateConcurrencyException` and `DbUpdateException` wrapping a `PostgresException` with `SqlState == PostgresErrorCodes.SerializationFailure`, logs each as a `Warning` (not `Error` — these are expected, retryable outcomes of a legitimate race, not bugs), and returns `409` for both. Everything else remains an unexpected `500`.

In practice, for `POST /proposals` this means: if the second request's read happens *after* the first already committed, Rule 2's explicit `property.Status != Available` check already returns a `409 ConflictError` — no exception involved. Only in the narrow window where both requests' reads genuinely overlap does either the Serializable-isolation abort or the `xmin` mismatch kick in. Externally, all three paths are indistinguishable: exactly one request ever succeeds, and the loser always receives `409`. The `ConcurrencyTests` integration test class fires two truly parallel requests (via `Task.WhenAll` and two separate `HttpClient`s hitting the same real PostgreSQL container) to verify this end to end, for both `POST /proposals` (two proposals racing for one Property) and `PATCH /proposals/{id}/status` (two updates racing for one Proposal).

---

# Integration Testing with Testcontainers

Architecture.md calls for Integration Tests focused on "REST Endpoints, Database, Transactions, Concurrency, History, Event Simulation" — none of which can be meaningfully verified against mocked repositories the way the Unit Tests do. `RentalPipeline.IntegrationTests` boots the real API in-memory via `WebApplicationFactory<Program>`, backed by a real, ephemeral PostgreSQL instance started with **Testcontainers** rather than a hand-maintained "dedicated test DB" — this avoids any shared, stateful test database that could drift or be left dirty between runs, requires no manual setup beyond having Docker available, and runs identically on every machine and in CI.

Key design points:

- **One container per test run, not per test class.** `RentalPipelineApiFactory` (`IAsyncLifetime`) starts a single `postgres:16` container and applies EF Core migrations once in `InitializeAsync`. All test classes share it via a single `[CollectionDefinition]`/`ICollectionFixture`, since starting a fresh container per class would be prohibitively slow. Because xUnit never parallelizes tests within one collection, sharing the database is safe as long as every test creates its own randomly-named Property/Customer/Proposal (see `TestDataFactory`) instead of assuming a pristine database — the one deliberate exception being `ConcurrencyTests`, which intentionally fires genuinely parallel requests against data it just created.
- **Event simulation is asserted, not inferred from logs.** `FakeEventPublisher` (the real `IEventPublisher` implementation) only logs, which integration tests can't easily assert on. `RentalPipelineApiFactory` swaps it for a `RecordingEventPublisher` test double via `ConfigureTestServices`, so tests can assert a `ContractActivatedEvent` was actually published for a specific proposal/property pair when a proposal reaches `Active`.
- **Enum JSON shape must match the real API.** The API serializes enums as strings via a `JsonStringEnumConverter` registered only in the MVC pipeline's `AddJsonOptions` — this does not apply to a plain `HttpClient`'s own `PostAsJsonAsync`/`ReadFromJsonAsync` calls. `TestJsonOptions.Default` mirrors that configuration so test code speaks the exact same JSON shape as the real API.
- **Setup goes through HTTP, never through the `DbContext` directly.** `TestDataFactory` creates Properties/Customers/Proposals by calling the real endpoints, so every test — including its own setup — exercises the full request pipeline (validation, mapping, persistence) rather than seeding data through a backdoor.

---

# Why Not Redis?

Redis Distributed Lock was intentionally not implemented.

Reasons:

- PostgreSQL already provides the required consistency guarantees.
- Introducing Redis would increase architectural complexity.
- The expected workload of this challenge does not justify an additional infrastructure component.

Redis is documented as a possible future improvement for high-scale distributed environments.

---

# Result<T> to ProblemDetails Mapping

Controllers never build HTTP responses by hand or contain try/catch blocks. A single set of `ControllerBase` extension methods (`ResultExtensions`) translates every `Result`/`Result<T>` outcome into an HTTP response:

- Success → `200 OK` / `201 Created` (with `Location` header) / `204 No Content`, depending on the extension method the controller calls.
- Failure → an RFC 7807 ProblemDetails response, built via ASP.NET Core's own `ControllerBase.Problem(...)` helper (which already fills in the `type` field with the correct RFC 9110 status-section link).

The concrete Application-layer error type drives the HTTP status and title:

- `NotFoundError` → `404`, title `"Not Found"`
- `ConflictError` → `409`, title `"Conflict"`
- `BusinessRuleViolationError` → `400`, title `"Business Rule Violation"` (matching Architecture.md's own ProblemDetails example title verbatim)
- Any other/unknown error → `400`, title `"Bad Request"`, as a safe fallback

This keeps the error-to-status mapping in exactly one place, instead of scattering `NotFound()`/`Conflict()`/`BadRequest()` calls across every controller action.

---

# Why a Custom Validation Filter Instead of FluentValidation.AspNetCore

Architecture.md asks for the FluentValidation pipeline to auto-validate incoming requests and return `400` automatically, without controllers or DTOs calling validators explicitly.

The historical way to achieve this was the `FluentValidation.AspNetCore` package, but its author deprecated and stopped maintaining it in 2021, explicitly recommending that consumers implement the equivalent behavior themselves instead of depending on it.

This project follows that recommendation: `Api/Filters/ValidationFilter` is a plain `IAsyncActionFilter`, registered once globally (`AddControllers(o => o.Filters.Add<ValidationFilter>())`), that resolves an `IValidator<T>` for each action argument (if one is registered in DI), runs it, and — on failure — short-circuits the pipeline with a `400` built via the framework's own `ProblemDetailsFactory.CreateValidationProblemDetails`, giving the exact same shape ASP.NET Core's built-in Data Annotations validation would produce. No extra, unmaintained package required.

---

# Client-Generated Guid Keys Require `ValueGeneratedNever()`

All entities generate their own primary key client-side, in the constructor (`Id = Guid.NewGuid()`), rather than relying on the database or EF Core to generate it. During manual end-to-end testing against a real PostgreSQL instance, this surfaced a subtle EF Core change-tracking bug:

Every `RentalProposal` status transition appends a new `ProposalStatusHistory` entry to the aggregate's `_statusHistory` collection. For the *first* entry — created inside the `RentalProposal` constructor, before the aggregate is ever added to the `DbSet` — this worked correctly, because EF Core cascades the `Added` state to the entire object graph when an aggregate root is explicitly added via the repository. But for every *subsequent* entry — appended after the proposal was already loaded and tracked (e.g. inside `UpdateStatusAsync`) — EF Core's change tracker discovers the new `ProposalStatusHistory` object purely through navigation/graph fixup, not through an explicit `Add()` call. In that situation, EF Core's default heuristic for deciding `Added` vs. `Modified` is "is the primary key equal to the CLR default value (`Guid.Empty`)?" — and since our Guids are always already populated by the constructor, every one of these entries was misclassified as an *existing* row being modified, producing an `UPDATE` statement against a row that didn't exist yet, and failing with `DbUpdateConcurrencyException: expected to affect 1 row(s), but actually affected 0`.

The fix: every entity configuration explicitly declares `.Property(x => x.Id).ValueGeneratedNever()`. This tells EF Core the application always owns key generation, removing the "default value" ambiguity — any untracked entity discovered in the graph, regardless of its key value, is now correctly treated as `Added`. Confirmed via `dotnet ef migrations add` that this is a pure EF Core metadata/tracking change with no actual SQL/schema impact (an empty migration was generated and then removed).

This is a good illustration of why "manually exercise every endpoint" is a mandatory step rather than an optional nice-to-have: this bug was invisible to unit tests (which mock the repositories) and only reproducible against a real database with a real change tracker.

---

# Locale-Independent API Behavior (Validation Messages, Enum Serialization)

Two small but important polish decisions, both surfaced during manual testing on a pt-BR development machine:

- **FluentValidation messages**: by default, FluentValidation localizes its built-in messages based on the running thread's culture. On a pt-BR OS, this silently produced Portuguese validation error messages in API responses — inconsistent with the rest of the (English) API and dependent on the deployment environment's locale, which is not acceptable for a production API. Fixed with `ValidatorOptions.Global.LanguageManager.Enabled = false`, which forces the default English messages everywhere, regardless of the host's OS/culture settings.
- **Enum serialization**: enums were serialized as raw integers by default (e.g. `"status": 0"`), which is technically correct but poor API ergonomics and Swagger documentation. A global `JsonStringEnumConverter` (registered via `AddJsonOptions`) makes both JSON payloads and the generated Swagger schema use the enum's name (e.g. `"status": "Available"`) instead.

---

# Database Migrations Applied Automatically on Startup

`docker compose up` (and any other deployment of the API) must bring up a fully working, migrated database with zero manual steps — no `dotnet-ef` tool, no separate `dotnet ef database update` command required on the host or in the container.

`Program.cs` calls `dbContext.Database.MigrateAsync()` once at startup, right after the host is built and before the HTTP pipeline is configured, so the API never starts accepting requests against a schema that isn't up to date. This runs identically whether the API is started via `dotnet run` on the host or via the Docker image, and it is exactly what `RentalPipelineApiFactory` (the integration test host) already did independently — the two are now consistent.

Trade-off acknowledged: for a higher-scale, multi-instance deployment, applying migrations from the application's own startup path is generally discouraged (multiple instances could race to apply the same migration, and a bad migration would block every instance from starting rather than being validated as a separate, controlled release step). For this project's single-instance deployment model, the simplicity and zero-manual-setup benefit outweighs that risk; a dedicated migration step (a one-off `dotnet ef database update` job/container, run before the API instances start) is the documented alternative for a production-grade, multi-instance evolution of this project.

---

# Docker Image Polish

Two small issues surfaced while validating `docker compose up` end to end from a clean state, both fixed in the Dockerfile/`Program.cs` rather than left as log noise:

- **Missing `libgssapi-krb5-2`**: the `mcr.microsoft.com/dotnet/aspnet:10.0` runtime image doesn't include this system library. Npgsql opportunistically probes for GSSAPI (Kerberos) support at connection time regardless of whether it's actually used, and without the library this printed `Cannot load library libgssapi_krb5.so.2` / `Error: ... cannot open shared object file` to stdout on every container start — harmless (the project only ever uses password authentication) but alarming-looking in logs. Fixed by installing `libgssapi-krb5-2` via `apt-get` in the final image stage.
- **`UseHttpsRedirection` inside the container**: the Docker image only exposes plain HTTP on port 8080 (see `docker-compose.yml`/`Dockerfile`) with no HTTPS binding at all, so `app.UseHttpsRedirection()` could never find an HTTPS port to redirect to, logging a `Failed to determine the https port for redirect` warning on *every single request*. `DOTNET_RUNNING_IN_CONTAINER` is set automatically to `true` by Microsoft's official .NET container base images, so `Program.cs` now skips `UseHttpsRedirection()` when that variable is set, while keeping it for local `dotnet run` (where Kestrel's HTTPS dev-certificate profile is available and redirection is meaningful).

---

# Future Improvements

The current architecture was designed to support future evolution with minimal changes.

Possible future enhancements include:

- Financial details on proposals (rent amount, deposits, fees, payment terms, and related validations)
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