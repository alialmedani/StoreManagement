using Volo.Abp.Application.Dtos;

namespace StoreManagement.ClaimManagement;

public class UserClaimDto : EntityDto
{
    public string ClaimType { get; set; } = string.Empty;

    public string ClaimValue { get; set; } = string.Empty;
}