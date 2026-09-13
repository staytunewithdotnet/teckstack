using MyInvoicingApp.Application.Abstractions;

namespace MyInvoicingApp.Infrastructure.System;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}