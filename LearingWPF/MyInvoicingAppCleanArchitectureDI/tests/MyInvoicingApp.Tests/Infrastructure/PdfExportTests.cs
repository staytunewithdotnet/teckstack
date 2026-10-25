using MyInvoicingApp.Application.DTOs;
using MyInvoicingApp.Infrastructure.Services;

namespace MyInvoicingApp.Tests.Infrastructure;

// Integration test - real PDF file generation
public class PdfExportTests : IDisposable
{
    private readonly List<string> _tempFiles = new();

    [Fact]
    public async Task ExportSingleInvoice_Should_Create_Real_Pdf_File()
    {
        var service = new PdfInvoiceExportService();
        var invoice = new InvoiceDto
        {
            Id = Guid.NewGuid(),
            CustomerName = "Acme Corp",
            Description = "Test Invoice for PDF",
            Amount = 1000,
            Tax = 180,
            Total = 1180,
            CreatedAt = DateTime.Now,
            IsPaid = false
        };

        var tempPath = Path.Combine(Path.GetTempPath(), $"test_invoice_{Guid.NewGuid()}.pdf");
        _tempFiles.Add(tempPath);

        var resultPath = await service.ExportSingleInvoiceToPdfAsync(invoice, tempPath);

        Assert.True(File.Exists(resultPath), "PDF file should exist on disk");
        var fileInfo = new FileInfo(resultPath);
        Assert.True(fileInfo.Length > 1000, "PDF should have content > 1KB");
    }

    [Fact]
    public async Task ExportAllInvoices_Should_Create_Pdf_With_Multiple_Rows()
    {
        var service = new PdfInvoiceExportService();
        var invoices = new List<InvoiceDto>
        {
            new() { Id = Guid.NewGuid(), CustomerName = "Customer A", Amount = 100, Tax = 18, Total = 118, CreatedAt = DateTime.Now },
            new() { Id = Guid.NewGuid(), CustomerName = "Customer B", Amount = 200, Tax = 36, Total = 236, CreatedAt = DateTime.Now },
        };

        var tempPath = Path.Combine(Path.GetTempPath(), $"test_all_{Guid.NewGuid()}.pdf");
        _tempFiles.Add(tempPath);

        var resultPath = await service.ExportInvoicesListToPdfAsync(invoices, tempPath);

        Assert.True(File.Exists(resultPath));
        Assert.True(new FileInfo(resultPath).Length > 1000);
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            try { if (File.Exists(file)) File.Delete(file); } catch {}
        }
    }
}
