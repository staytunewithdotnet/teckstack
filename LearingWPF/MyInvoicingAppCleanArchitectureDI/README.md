# MyInvoicingApp - Clean Architecture WPF + Real SQLite

.NET 8 WPF application implementing Clean Architecture + DI + MVVM as per the guide.

## What you get
- 4 projects following Dependency Rule (Domain -> Application -> Infrastructure -> Presentation)
- REAL file-based SQLite at %LocalAppData%/MyInvoicingApp/invoices.db (not in-memory)
- DI with Microsoft.Extensions.Hosting, Transient ViewModels, Singleton Factory
- Scope per child window to prevent memory leaks (see MainViewModel.OpenCreateWindow)
- Unit Tests (Domain + UseCase with FakeRepo) + Integration Tests (real SQLite file)
- WAL mode + new connection per operation to fix DB locked issues

## How to run
1. Requires .NET 8 SDK + Visual Studio 2022 with .NET desktop workload
2. Open MyInvoicingApp.sln
3. Set MyInvoicingApp.Presentation as Startup Project
4. Restore NuGet: dotnet restore
5. Run: dotnet run --project src/MyInvoicingApp.Presentation
   or F5 in Visual Studio

## Where is the DB file?
Check Debug output or StatusBar: C:\Users\<you>\AppData\Local\MyInvoicingApp\invoices.db
You can open it with DB Browser for SQLite.

## How to run tests
dotnet test tests/MyInvoicingApp.Tests -v n

Tests cover:
- Domain: Invoice.IsValid(), CalculateTotal(), MarkAsPaid()
- Application: CreateInvoiceUseCase with FakeRepository (no DB)
- Infrastructure: SqliteInvoiceRepository with real file in temp folder, verifies persistence across restarts

## Architecture to learn from
- App.xaml.cs is Composition Root - only place with GetRequiredService
- MainViewModel depends on UseCases, not Repositories
- SqliteConnectionFactory is Singleton, but CreateConnection() returns NEW connection each time (Transient connection)
- Child window uses App.Services.CreateScope() -> prevents leak
- IDbConnectionFactory exposes DbPath so UI can show it

## Common mistakes fixed here
- Service Locator: No GetService() inside ViewModels
- Captive Dependency: Factory Singleton does not hold connection
- ViewModel as Singleton: All ViewModels Transient
- IQueryable leak: Repository returns List<Invoice>, not IQueryable
