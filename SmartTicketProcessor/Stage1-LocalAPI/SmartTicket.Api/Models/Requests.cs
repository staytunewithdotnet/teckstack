using System.ComponentModel.DataAnnotations;

namespace SmartTicket.Api.Models;

public sealed record SubmitTicketRequest(
    [Required, MinLength(3)] string CustomerId,
    [Required, MinLength(5), MaxLength(200)] string Title,
    [Required, MinLength(10), MaxLength(2000)] string Description);
