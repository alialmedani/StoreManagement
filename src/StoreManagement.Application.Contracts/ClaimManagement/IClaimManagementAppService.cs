using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace StoreManagement.ClaimManagement;

public interface IClaimManagementAppService : IApplicationService
{
    Task<List<UserClaimDto>> GetUserClaimsAsync(
        Guid userId);

    Task<UserClaimDto> AddUserClaimAsync(
        AddUserClaimDto input);

    Task<UserClaimDto> UpdateUserClaimAsync(
        UpdateUserClaimDto input);

    Task DeleteUserClaimAsync(
        DeleteUserClaimDto input);
}