using Microsoft.Extensions.Logging.Abstractions;
using MyInvoicingApp.Application.UseCases;

namespace MyInvoicingApp.Tests.Application;

public class CreateInvoiceUseCaseTests
{
    [Fact]
    public async Task Execute_Should_Save_When_Valid()
    {
        // Arrange - No DI container, pure unit test (Domain + Application only)
        var fakeRepo = new FakeInvoiceRepository();
        var useCase = new CreateInvoiceUseCase(fakeRepo, NullLogger<CreateInvoiceUseCase>.Instance);

        // Act
        var invoice = await useCase.ExecuteAsync("Test Customer", 1000, "Test invoice");

        // Assert
        Assert.Single(fakeRepo.Saved);
        Assert.Equal("Test Customer", invoice.CustomerName);
        Assert.True(invoice.IsValid());
    }

    [Fact]
    public async Task Execute_Should_Throw_When_Invalid()
    {
        var fakeRepo = new FakeInvoiceRepository();
        var useCase = new CreateInvoiceUseCase(fakeRepo, NullLogger<CreateInvoiceUseCase>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(async () => await useCase.ExecuteAsync("", 0));
        Assert.Empty(fakeRepo.Saved);
    }

    [Fact]
    public async Task GetAll_Should_Return_DTOs_With_Tax_Calculated()
    {
        var fakeRepo = new FakeInvoiceRepository();
        var createUseCase = new CreateInvoiceUseCase(fakeRepo, NullLogger<CreateInvoiceUseCase>.Instance);
        await createUseCase.ExecuteAsync("Customer A", 100);
        await createUseCase.ExecuteAsync("Customer B", 200);

        var getAll = new GetAllInvoicesUseCase(fakeRepo);
        var dtos = await getAll.ExecuteAsync();

        Assert.Equal(2, dtos.Count);
        Assert.All(dtos, d => Assert.True(d.Total > d.Amount)); // Total includes tax
    }
}
