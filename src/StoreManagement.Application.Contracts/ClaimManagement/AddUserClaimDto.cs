using System;
using System.ComponentModel.DataAnnotations;

namespace StoreManagement.ClaimManagement;

public class AddUserClaimDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [StringLength(256)]
    public string ClaimType { get; set; } = string.Empty;

    [Required]
    [StringLength(512)]
    public string ClaimValue { get; set; } = string.Empty;
}