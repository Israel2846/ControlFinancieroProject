# Copilot instructions for ControlFinancieroProject

## Build, test, and lint commands

- Build the web app with `dotnet build --nologo`.
- Run the app locally with `dotnet run --launch-profile https`.
- Apply existing EF Core migrations to the configured SQL Server database with `dotnet ef database update`.
- There is currently no automated test project in the solution and no dedicated lint/format command checked into the repository, so do not invent test or lint steps in future changes.

## High-level architecture

- This repository is a single ASP.NET Core MVC app targeting `net10.0`. `Program.cs` wires up MVC, `ApplicationDbContext`, Serilog, and the service layer; the app uses SQL Server via `UseSqlServer(...)` and writes logs to `logs/app-.log`.
- The main business flow is **controllers -> service interfaces -> EF Core DbContext -> Razor views**. `CategoriaController` and `TransaccionController` stay thin and delegate business rules to `ICategoryService`, `ITransactionService`, `ITransactionBalanceService`, and `ITransactionReportService`.
- `ApplicationDbContext` owns the persistence model for `Categoria` and `Transaccion`, including decimal precision for monetary fields and a restricted foreign key from transactions to categories. Changes to entity shape usually require both model updates and a new EF Core migration.
- Transactions are not just CRUD rows: each `Transaccion` stores `SaldoAnterior` and `SaldoNuevo`, and those balances are order-dependent. `TransactionService` calls `TransactionBalanceService.CalculateAsync(...)` before saving and `RecalculateFromAsync(...)` after create, update, delete, or full recalculation. Ordering is always by `Fecha` and then `Id`, so any feature that changes transaction dates, inserts historical rows, or edits amounts/categories must preserve that recalculation flow.
- Reporting is server-driven. `TransactionReportService` prepares monthly and comparative view models, then Razor views serialize those view-model collections into JavaScript for Chart.js charts and Bootstrap-driven UI interactions.

## Key conventions

- Keep user-facing text, labels, validation messages, and domain terminology in Spanish. The codebase consistently uses Spanish domain names such as `Categoria`, `Transaccion`, `Ingreso`, `Gasto`, and Spanish validation/error messages.
- Preserve the thin-controller pattern. Business logic belongs in services, not in controllers or Razor views. Controllers typically translate service results into MVC responses and repopulate form options on validation errors.
- Follow the current split between entities and form/report models. Category CRUD binds the EF entity directly, but transaction screens use dedicated view models such as `TransactionFormViewModel`, `TransactionIndexViewModel`, `MonthlyReportViewModel`, and `ComparativeReportViewModel`.
- For transaction operations, return `OperationResult` from the service layer when the UI needs field-level or not-found feedback. The controller pattern is to map `IsNotFound` to `NotFound()` and add `ErrorMessage` to `ModelState` for recoverable validation problems.
- When querying read-only data, services consistently use `AsNoTracking()` and explicitly `Include(...)` related `Categoria` data only where needed.
- If you change transaction or category relationships, keep the delete restriction in mind: categories are configured with `OnDelete(DeleteBehavior.Restrict)`, so deleting a category with related transactions is expected to fail unless the relationship handling is changed intentionally.
- Report views depend on pre-shaped collections from the service layer and use `System.Text.Json` serialization plus small inline scripts for Chart.js. Prefer extending the report view model and service output rather than moving aggregation logic into JavaScript.
