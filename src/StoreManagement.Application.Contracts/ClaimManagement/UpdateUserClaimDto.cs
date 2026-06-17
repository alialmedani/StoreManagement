using System;
using System.ComponentModel.DataAnnotations;

namespace StoreManagement.ClaimManagement;

public class UpdateUserClaimDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [StringLength(256)]
    public string OldClaimType { get; set; } = string.Empty;

    [Required]
    [StringLength(512)]
    public string OldClaimValue { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string NewClaimType { get; set; } = string.Empty;

    [Required]
    [StringLength(512)]
    public string NewClaimValue { get; set; } = string.Empty;
}