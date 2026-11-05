# 10 — Coding Conventions (source of truth: `pal-suite`)

The architecture follows the team's reference project **pal-suite** (`../pal-suite`):
Clean Architecture, CQRS with MediatR, FluentValidation, LanguageExt `Either`/`Option`,
strongly-typed IDs, repositories + query interfaces, controllers + controller services,
error factories, Testcontainers integration tests. When this document and pal-suite
disagree, **this document wins** (it records deliberate deviations, marked ⚠).

---

## 1. Backend layers

```
Domain          entities, strongly-typed ids, value objects, enums, Guard     → no deps (LanguageExt only)
Application     commands (MediatR) + validators, exceptions, interfaces       → Domain, Solver
                (repositories, queries, services), behaviours, settings
Infrastructure  EF Core (persistence, repositories), import/export, solve     → Application
                worker, caching, time provider
Api             controllers, DTOs, controller services (read side),           → Application, Infrastructure
                error factories, modules, SignalR hubs
Solver          pure CP-SAT engine (input/output records only)               → no deps (OR-Tools only)
```

### CQRS split (as in pal-suite)

| Side | Path | Mechanism |
|---|---|---|
| **Write** | Controller → `ISender.Send(command)` → `IRequestHandler` → `I<Entity>Repository` | MediatR, validated by `ValidationBehaviour`, returns `Either<<Feature>Exception, <Entity>>` |
| **Read** | Controller → `I<Feature>ControllerService` → `I<Entity>Queries` | No MediatR; services map entities to DTOs; return `IReadOnlyList<Dto>` or `Option<Dto>` |

Repository classes implement **both** interfaces and are registered three times
(concrete + repository interface + queries interface resolving the same scoped instance).

---

## 2. Domain

```csharp
// Domain/Centers/TestCenterId.cs
public record TestCenterId(Guid Value)
{
    public static TestCenterId Empty() => new(Guid.Empty);
    public static TestCenterId New() => new(Guid.CreateVersion7());   // ⚠ v7 instead of NewGuid()
    public override string ToString() => Value.ToString();
}

// Domain/Centers/TestCenter.cs
public class TestCenter : IEntity<TestCenterId>
{
    public TestCenterId Id { get; private set; }
    public ExamSessionId SessionId { get; private set; }
    public string Name { get; private set; }
    public GeoPoint Location { get; private set; }          // value object
    public bool IsActive { get; private set; }
    public ICollection<Room> Rooms { get; private set; } = [];

    private TestCenter(TestCenterId id, ExamSessionId sessionId, string name, GeoPoint location, bool isActive)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(name, nameof(name));
        Guard.LessThanOrEqual(name, TestCenterConstraints.NameMaxLength, nameof(name));
        (Id, SessionId, Name, Location, IsActive) = (id, sessionId, name, location, isActive);
    }

    public static TestCenter New(TestCenterId id, ExamSessionId sessionId, string name, GeoPoint location, bool isActive)
        => new(id, sessionId, name, location, isActive);

    public void UpdateDetails(string name, GeoPoint location, bool isActive) { /* Guard … then assign */ }
}
```

Rules:
- Private setters, private constructor, `static New(...)`, behaviour methods (`UpdateDetails`,
  `ChangeLayout`, …). No public setters, no anemic DTO-entities.
- Invariants via `Domain/Common/Guard`. Length limits come from a per-entity
  `static class <Entity>Constraints { public const int NameMaxLength = 200; }` — the **same
  constants** are used by EF configurations and FluentValidation validators.
- `Domain/Common/ValueObject` base for value objects (`GeoPoint`, `RoomLayout`, `SeatKey`,
  `PersonName`, `AttributeKey`).
- Enums live next to their aggregate (`Domain/Rules/RuleType.cs`), explicit values
  (`Unknown = 0` first where a default makes sense).

---

## 3. Application

Folder per feature: `Application/<Feature>/{Commands,Exceptions,Services}`.

```csharp
// Application/Centers/Commands/CreateTestCenterCommand.cs  (command + handler in one file)
public record CreateTestCenterCommand : IRequest<Either<TestCenterException, TestCenter>>
{
    public Guid SessionId { get; init; }
    public string? Name { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public bool IsActive { get; init; }
}

public class CreateTestCenterCommandHandler(
    IExamSessionRepository sessionRepository,
    ITestCenterRepository centerRepository)
    : IRequestHandler<CreateTestCenterCommand, Either<TestCenterException, TestCenter>>
{
    public Task<Either<TestCenterException, TestCenter>> Handle(CreateTestCenterCommand request, CancellationToken ct)
    {
        var sessionId = new ExamSessionId(request.SessionId);
        return EnsureSessionExists(sessionId, ct)
            .BindAsync(_ => EnsureNameIsUnique(sessionId, request.Name!, ct))
            .BindAsync(_ => Create(sessionId, request, ct));
    }
    // private steps return Either<TestCenterException, …>; unexpected exceptions → TestCenterUnknownException
}

// Application/Centers/Commands/CreateTestCenterCommandValidator.cs
public class CreateTestCenterCommandValidator : AbstractValidator<CreateTestCenterCommand>
{
    public CreateTestCenterCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(TestCenterConstraints.NameMaxLength);
        RuleFor(x => x.Latitude).InclusiveBetween(GeoPoint.MinLatitude, GeoPoint.MaxLatitude);
        RuleFor(x => x.Longitude).InclusiveBetween(GeoPoint.MinLongitude, GeoPoint.MaxLongitude);
    }
}

// Application/Centers/Exceptions/TestCenterExceptions.cs
public abstract class TestCenterException(TestCenterId id, ErrorCode code, string message, Exception? inner = null)
    : Exception(message, inner)
{
    public TestCenterId TestCenterId { get; } = id;
    public ErrorCode Code { get; } = code;                     // ⚠ added: machine-readable code
}
public class TestCenterSessionNotFoundException(TestCenterId id, ExamSessionId sessionId)
    : TestCenterException(id, ErrorCode.SessionNotFound, $"Session with id {sessionId} does not exist");
public class TestCenterAlreadyExistsException(TestCenterId id, string name)
    : TestCenterException(id, ErrorCode.TestCenterAlreadyExists, $"Test center with name {name} already exists");
public class TestCenterNotFoundException(TestCenterId id) : TestCenterException(id, ErrorCode.TestCenterNotFound, $"Test center with id {id} does not exist");
public class TestCenterUnknownException(TestCenterId id, Exception inner) : TestCenterException(id, ErrorCode.Unknown, $"Unknown exception for the test center {id}", inner);
```

- `Application/ConfigureServices.cs`: `AddValidatorsFromAssembly`, `AddMediatR`, pipeline
  behaviours `UnhandledExceptionBehaviour<,>` then `ValidationBehaviour<,>`.
- `Application/Common/Interfaces/{Repositories,Queries}/I<Entity>Repository|Queries.cs`.
- `Application/Common/Settings/ApplicationSettings.cs` — strongly-typed settings bound once
  and registered as singleton (pal-suite style); section names via `nameof`.
- `ErrorCode` enum in `Application/Common/Errors/ErrorCode.cs` — every exception has one.

---

## 4. Infrastructure

- `Persistence/ApplicationDbContext.cs` — `DbSet` per aggregate, `ApplyConfigurationsFromAssembly`.
- `Persistence/Configurations/<Entity>Configuration.cs` — id conversions
  (`HasConversion(x => x.Value, x => new TestCenterId(x))`), lengths from `<Entity>Constraints`,
  FK names, jsonb columns, **enums stored as strings** (`HasConversion<string>()`).
- `UseSnakeCaseNamingConvention()` (EFCore.NamingConventions), `NpgsqlDataSourceBuilder.EnableDynamicJson()`.
- `Persistence/ApplicationDbContextInitialiser.cs` — migrate + seed on startup
  (called from `Api/Modules/DbModule`).
- `Persistence/Repositories/RepositoryBase<T,TKey>` — Add/Update/Remove(+Range)/SaveChanges;
  `<Entity>Repository : RepositoryBase<…>, I<Entity>Repository, I<Entity>Queries`.
  Reads use `AsNoTracking()` and return `Option<T>` for single lookups.
- `ConfigureServices.cs` → `AddInfrastructureServices(configuration)` → `AddPersistenceServices`,
  `AddImportExportServices`, `AddSolvingServices`.

---

## 5. Api

```csharp
[ApiController]
[Route(ApiRoutes.Sessions.Centers)]                 // "sessions/{sessionId:guid}/centers"
public class TestCentersController(ISender sender, ITestCentersControllerService controllerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TestCenterDto>>> GetAll([FromRoute] Guid sessionId, CancellationToken ct)
        => (await controllerService.GetAll(sessionId, ct)).ToList();

    [HttpPost(ApiRoutes.Actions.Create)]             // "create"
    public async Task<ActionResult<TestCenterDto>> Create([FromRoute] Guid sessionId, [FromBody] TestCenterDto dto, CancellationToken ct)
    {
        var result = await sender.Send(dto.ToCreateCommand(sessionId), ct);
        return result.Match<ActionResult<TestCenterDto>>(c => TestCenterDto.FromDomain(c), e => e.ToObjectResult());
    }
}
```

- DTOs: `Api/Dtos/<Feature>Dtos.cs`, `record`s with `static FromDomain(...)` and
  `To<Command>(...)` mapping methods (no AutoMapper — manual mapping like pal-suite).
- Read side: `Api/Services/Abstract/I<Feature>ControllerService.cs` +
  `Api/Services/Implementation/<Feature>ControllerService.cs`.
- Errors: `Api/Modules/Errors/<Feature>ErrorFactory.cs` with `ToObjectResult()` switch
  exception → status code. ⚠ The body is `ProblemDetails { Title = message, Status,
  Extensions["code"] = ErrorCode }` instead of a bare string, so the client can switch on
  `ErrorCode` instead of parsing text.
- ⚠ Validation: `ValidationBehaviour` throws `FluentValidation.ValidationException`; pal-suite
  leaves it unhandled (→ 500). Here `Api/Modules/ExceptionHandlingModule.cs` registers an
  `IExceptionHandler` mapping it to `400 ValidationProblemDetails` with
  `Extensions["code"] = ErrorCode.ValidationFailed`.
- Modules: `SetupModule` (controllers, CORS, settings, controller services, SignalR,
  JSON enum-as-string), `DbModule`, `ApiEndpointsModule` (`/health`), `ExceptionHandlingModule`.
- Route strings only in `Api/Constants/ApiRoutes.cs` (nested static classes of `const string`).
- Swagger via Swashbuckle (pal-suite); JSON enums serialized as strings
  (`JsonStringEnumConverter`) so generated client enums are string enums.
- Auth: none in MVP. When added, follow pal-suite (`[Authorize(Roles = …)]`,
  `PermissionsService`, `Role` constants, claims transformation).

---

## 6. No magic strings / numbers — backend

| Kind | Where |
|---|---|
| Route templates & segments | `Api/Constants/ApiRoutes.cs` |
| Hub routes & method names | `Api/Constants/HubRoutes.cs`, `SolveProgressHubMethods` |
| Configuration sections | `nameof(ApplicationSettings.Solver)` etc. |
| Length/range limits | `<Entity>Constraints` (Domain), `GeoPoint.MinLatitude` … |
| Error / diagnostic / import error codes | enums `ErrorCode`, `DiagnosticCode`, `ImportErrorCode` |
| Attribute keys used by rules | `AttributeKeys.Group`, `AttributeKeys.School` (const) + `AttributeKey` value object |
| Seat keys | `SeatKey` value object (`SeatKey.From(row, column, seat)`, `SeatKey.Parse`) — never string concatenation |
| Excel headers, sheet names, XML element names | `Infrastructure/ImportExport/Constants/{ImportHeaders,ExportSheets,XmlNames}.cs` |
| Solver tuning defaults | `SolveOptions` defaults + `SolverDefaults` constants |
| Cache keys | `CacheKeys` static methods |

Analyzer settings: copy pal-suite's `api/.editorconfig` (StyleCop rules at error level) into
`api/.editorconfig`; `StyleCop.Analyzers` referenced by every project.

---

## 7. Client (pal-suite layout + stricter typing)

```
client/src/
├─ index.tsx, Routes.tsx
├─ components/
│  ├─ App/                  providers: QueryClient, SnackbarProvider, Theme, Router, axios setup
│  ├─ Common/               CustomStepper, ReadOnlyBlock, CustomBackdrop, ConfirmDialog (own replacements for blum components)
│  ├─ Layout/               shell, menu
│  └─ Global/
│     ├─ Queries/           <feature>.ts — useQuery hooks
│     └─ <Feature>/
│        ├─ Common/         index.ts (state interface, ValidationErrors, reducer + <Feature>ReducerAction enum), components/{Steps,StepDetails,StepReview}
│        ├─ Create/         index.tsx + mutations.ts
│        ├─ Edit/           index.tsx + mutations.ts
│        ├─ List/           index.tsx (MUI X DataGrid)
│        └─ View/           index.tsx
├─ views/<Feature>/{Create,Edit,List,View}/index.tsx    thin pages: read route params, render Global component, navigate on finish
├─ api/                     axiosInstance.ts, generated/schema.ts (openapi-typescript --enum), dtos.ts (named re-exports)
├─ constants/               routes.ts, apiRoutes.ts, queryKeys.ts, mutationKeys.ts, queryParams.ts, storageKeys.ts, ui.ts
├─ enums/                   UI-only enums (SeatState, ExportFormat, WizardStep, …); API enums re-exported from api/dtos.ts
├─ helpers/                 processError (switches on ErrorCode), formatters
├─ hooks/, layouts/, i18n/uk/<feature>.json
```

### Enums over types — rules

1. **No string-literal union types.** `type Status = 'queued' | 'running'` is forbidden;
   use `enum SolveRunStatus { Queued = 'Queued', Running = 'Running' }`.
   Enforced by ESLint `no-restricted-syntax` on `TSUnionType > TSLiteralType`.
2. **API enums are generated** as TS `enum`s with `openapi-typescript --enum` from
   `contracts/openapi.json` and re-exported from `api/dtos.ts`. Never redeclare them by hand.
3. **Interfaces for object shapes** (`interface TestCenterDto`), re-exported from the generated
   schema (`export type TestCenterDto = components['schemas']['TestCenterDto']` is the only
   allowed `type` alias form, and only inside `api/dtos.ts`).
4. Reducer actions are enums (`TestCenterReducerAction.Name`) — pal-suite style.

### No magic strings / numbers — client

| Kind | Where | Example |
|---|---|---|
| App routes | `constants/routes.ts`: `enum RoutePath` (patterns) + `routes.*` builders | `routes.centers.list(sessionId)` |
| API endpoints | `constants/apiRoutes.ts` builders (mirror `ApiRoutes.cs`) | `apiRoutes.centers.create(sessionId)` |
| Query keys | `constants/queryKeys.ts`: `enum QueryKey` + `queryKeys.*` factories returning arrays | `queryKeys.centers.list(sessionId)` |
| Mutation keys | `enum MutationKey` | `MutationKey.CreateTestCenter` |
| Stale times, page sizes, debounce | `constants/queryParams.ts`, `constants/ui.ts` | `DEFAULT_STALE_TIME_MS` |
| Storage keys | `enum StorageKey` | |
| Form validation messages | i18n keys via typed `t()` (i18next TS augmentation) | |
| Error handling | `processError` switches on generated `ErrorCode` enum | |
| `data-qa` test ids | `enum TestId` per feature | `TestId.CenterNameField` |

ESLint (⚠ flat config — pal-suite's `react-app` base does not run on ESLint 9+): `typescript-eslint` recommended + react-hooks + `@typescript-eslint/no-unused-vars: error`,
`no-restricted-syntax` (literal unions), `@typescript-eslint/prefer-enum-initializers`,
`no-magic-numbers` (ignore 0, 1, -1, array indexes). Prettier config copied from pal-suite
(`printWidth: 180`, single quotes, trailing comma es5).

---

## 8. Tests

| Project | Contents |
|---|---|
| `tests/Api.Tests.Integration/<Feature>/` | controller tests through `HttpClient`, Arrange in `InitializeAsync`, cleanup in `DisposeAsync` (pal-suite) |
| `tests/Tests.Common/` | `IntegrationTestWebFactory` (Testcontainers PostgreSQL), `BaseIntegrationTest`, `TestsExtensions` (`RightOrThrow`, `LeftOrThrow`, `ToResponseModel<T>`) |
| `tests/Tests.Data/` | static test data factories per aggregate (`TestCentersData.MainCenter(sessionId)`) |
| `tests/Domain.Tests/` | ⚠ new: entity & value-object unit tests |
| `tests/Solver.Tests/` | ⚠ new: solver unit tests (validator as oracle) |
| `tests/Infrastructure.Tests/` | ⚠ new: import/export unit tests |

xUnit + FluentAssertions **7.x** (8.x is commercially licensed). Test names `Should<Behaviour>`.

## 9. Package versions & licensing

Pinned in `api/Directory.Packages.props` (⚠ central package management — pal-suite pins
per project; centralizing avoids conflicts between parallel agents):

- **MediatR 12.x** — last Apache-2.0 line (13+ requires a commercial license key).
- **FluentAssertions 7.x** — same reason.
- **LanguageExt.Core 4.4.x** — same API as pal-suite (v5 has breaking changes).
- FluentValidation 11.x/12.x, EFCore.NamingConventions, Npgsql EF provider for .NET 10,
  Swashbuckle.AspNetCore, StyleCop.Analyzers 1.2.0-beta, Testcontainers.PostgreSql,
  Google.OrTools, ClosedXML, CsvHelper, QuestPDF.
