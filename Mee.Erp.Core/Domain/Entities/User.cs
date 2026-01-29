using Mee.Erp.Shared.Kernel.Domain.Entities;

namespace Mee.Erp.Core.Domain.Entities;

// A simplified user entity for audit purposes.
public class User : BaseEntity
{
    public required string Username { get; set; }
    public required string Email { get; set; }
    public string? FullName { get; set; }
}