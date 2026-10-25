using MyInvoicingApp.Application.DTOs;

namespace MyInvoicingApp.Application.Interfaces;

// Production abstraction - solves Case Study 3: Untestable File System & Printing
public interface IPrintService
{
    bool CanPrint { get; }
    Task PrintInvoiceAsync(InvoiceDto invoice);
}

public interface IPdfExportService
{
    Task<string> ExportSingleInvoiceToPdfAsync(InvoiceDto invoice, string filePath);
    Task<string> ExportInvoicesListToPdfAsync(List<InvoiceDto> invoices, string filePath);
    Task<string> ExportSingleInvoiceToPdfAsync(InvoiceDto invoice); // auto-picks file path
}
