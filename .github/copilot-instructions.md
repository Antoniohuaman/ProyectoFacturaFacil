# Copilot Instructions for ProyectoFacturaFacil

## Big Picture Architecture
- The solution is organized by business capability (BC) folders under `src/`, each with its own subdomains: `Adapters`, `Application`, `Domain`, and `Infrastructure`.
- Each BC (e.g., `CatalogoArticulosBC`, `ComprobantesElectronicosBC`, etc.) follows DDD (Domain-Driven Design) principles: `Aggregates`, `Entities`, `ValueObjects`, `Repositories`, and `Specifications`.
- Shared concepts are in `SharedKernel`, especially reusable `ValueObjects`.
- Integration points are in `src/Integration`.
- Tests are organized in parallel to BCs under `tests/`, with `UnitTests` and sometimes `EndToEnd`.

## Developer Workflows
- **Build:** Run `dotnet build` from the workspace root to build all projects.
- **Test:** Run `dotnet test` from the workspace root to execute all tests. For a specific test project, run `dotnet test <path-to-csproj>`.
- **Debug:** Use Visual Studio or VS Code's C# extension for step debugging. Test projects use NUnit.
- **Project files:** Each BC and test project has its own `.csproj` file.

## Project-Specific Conventions
- **Value Objects:** Located in `Domain/ValueObjects` or `SharedKernel/ValueObjects`. Always immutable and validated on creation.
- **Specifications:** Encapsulate business rules, found in `Domain/Specifications`.
- **Repositories:** Interface-based, with in-memory implementations for tests in `Adapters/Output/Persistence/InMemory`.
- **DTOs:** Application layer uses DTOs for input/output, found in `Application/DTOs`.
- **Use Cases:** Application logic is in `Application/UseCases`, each as a class named `*UseCase`.
- **Unit of Work:** Interface in `Application/Interfaces/IUnitOfWork.cs` and in-memory/test implementations.
- **Testing:** Test classes mirror domain and use case structure. Use `[TestFixture]` and `[Test]` attributes (NUnit).

## Integration & External Dependencies
- No direct external service calls found; integration is likely handled via adapters and the `Integration` project.
- In-memory repositories are used for testing and prototyping.
- PDF in `docs/FacturaFacil_DDD.pdf` describes domain model and architecture (recommended for deep dives).

## Examples
- To add a new business rule, create a specification in `Domain/Specifications` and use it in the relevant aggregate or use case.
- To test a new value object, add a test class in the corresponding `tests/*/UnitTests/ValueObjects/` folder.
- To extend a use case, update the class in `Application/UseCases` and its DTOs in `Application/DTOs`.

## Key Files & Directories
- `src/SharedKernel/ValueObjects/`: Common value objects (e.g., `Dinero.cs`, `Moneda.cs`, `TipoCambio.cs`).
- `src/*/Domain/Specifications/`: Business rules and validation logic.
- `src/*/Application/UseCases/`: Application service classes.
- `tests/*/UnitTests/`: NUnit test classes for domain and use cases.
- `docs/FacturaFacil_DDD.pdf`: Domain model documentation.

---

If any section is unclear or missing details, please specify which part to improve or expand.
