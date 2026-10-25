# Product Management Application - Clean Architecture Learning

This application demonstrates the **Presentation Layer** of Clean Architecture with MediatR. 
The structure follows the principles from the Clean Architecture guide you shared.

## Current Structure (Presentation Layer)

- **Models/** - Contains Product model (Domain layer representation)
- **Core/** - Contains RelayCommand implementation
- **ViewModels/** - Contains ProductViewModel (Presentation layer)
- **MainWindow.xaml/.cs** - The main view that binds to the ViewModel

## What You'll Learn to Implement Next

Following the Clean Architecture pattern from the guide:

### 1. Application Layer (Next Step)
Create these folders and implement:
- `Application/Interfaces/IProductRepository.cs` - Define repository contracts
- `Application/UseCases/CreateProductUseCase.cs` - Business logic orchestrators
- `Application/Commands/CreateProductCommand.cs` - MediatR command
- `Application/Queries/GetProductsQuery.cs` - MediatR query

### 2. Infrastructure Layer (After Application)
Create these folders and implement:
- `Infrastructure/Data/ProductRepository.cs` - Concrete repository implementations
- `Infrastructure/Data/DatabaseContext.cs` - Database context (if using EF Core)

### 3. Enhanced DI Setup (Final Step)
Update `App.xaml.cs` to register all services following the dependency rule.

## Key Clean Architecture Principles Demonstrated

1. **Dependency Rule**: Inner circles (Domain) don't know about outer circles (Presentation)
2. **Separation of Concerns**: Each layer has distinct responsibilities
3. **Testability**: Business logic separated from UI concerns
4. **UI Independence**: Business rules don't depend on WPF

## Current Flow
```
MainWindow (View) -> ProductViewModel (Presentation) -> [Will connect to] UseCases (Application) -> Repository Interface (Application) -> Repository Implementation (Infrastructure)
```

## Learning Path
1. Study the current Presentation layer structure
2. Implement the Application layer with MediatR commands/queries
3. Connect the ViewModel to MediatR instead of using hardcoded logic
4. Implement Infrastructure layer with actual data persistence
5. Complete the DI registration in App.xaml.cs

## MediatR Integration Points
The ViewModel is prepared to work with MediatR - you'll replace the hardcoded logic with:
- Commands: `await _mediator.Send(new CreateProductCommand(name, quantity))`
- Queries: `var products = await _mediator.Send(new GetProductsQuery())`

Start with the Application layer to continue your learning journey!