using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AbpIdentityUser = Volo.Abp.Identity.IdentityUser;
using AspNetIdentityResult = Microsoft.AspNetCore.Identity.IdentityResult;
using Microsoft.AspNetCore.Authorization;
using StoreManagement.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Identity;

namespace StoreManagement.ClaimManagement;

public class ClaimManagementAppService :
    ApplicationService,
    IClaimManagementAppService
{
    private readonly IIdentityUserRepository _userRepository;
    private readonly IdentityUserManager _userManager;

    public ClaimManagementAppService(
        IIdentityUserRepository userRepository,
        IdentityUserManager userManager)
    {
        _userRepository = userRepository;
        _userManager = userManager;
    }

    [Authorize(StoreManagementPermissions.ClaimManagement.View)]
    public async Task<List<UserClaimDto>> GetUserClaimsAsync(
        Guid userId)
    {
        var user =
            await GetUserAsync(userId);

        var claims =
            await _userManager.GetClaimsAsync(user);

        return claims
            .OrderBy(claim => claim.Type)
            .ThenBy(claim => claim.Value)
            .Select(claim => new UserClaimDto
            {
                ClaimType = claim.Type,
                ClaimValue = claim.Value
            })
            .ToList();
    }

    [Authorize(StoreManagementPermissions.ClaimManagement.Manage)]
    public async Task<UserClaimDto> AddUserClaimAsync(
        AddUserClaimDto input)
    {
        var user =
            await GetUserAsync(input.UserId);

        var claimType =
            NormalizeRequiredValue(
                input.ClaimType,
                "StoreManagement:ClaimTypeRequired"
            );

        var claimValue =
            NormalizeRequiredValue(
                input.ClaimValue,
                "StoreManagement:ClaimValueRequired"
            );

        var existingClaims =
            await _userManager.GetClaimsAsync(user);

        if (existingClaims.Any(claim =>
                claim.Type == claimType &&
                claim.Value == claimValue))
        {
            throw new BusinessException(
                "StoreManagement:UserClaimAlreadyExists"
            );
        }

        var result =
            await _userManager.AddClaimAsync(
                user,
                new Claim(claimType, claimValue)
            );

        CheckIdentityResult(result);

        return new UserClaimDto
        {
            ClaimType = claimType,
            ClaimValue = claimValue
        };
    }

    [Authorize(StoreManagementPermissions.ClaimManagement.Manage)]
    public async Task<UserClaimDto> UpdateUserClaimAsync(
        UpdateUserClaimDto input)
    {
        var user =
            await GetUserAsync(input.UserId);

        var oldClaimType =
            NormalizeRequiredValue(
                input.OldClaimType,
                "StoreManagement:OldClaimTypeRequired"
            );

        var oldClaimValue =
            NormalizeRequiredValue(
                input.OldClaimValue,
                "StoreManagement:OldClaimValueRequired"
            );

        var newClaimType =
            NormalizeRequiredValue(
                input.NewClaimType,
                "StoreManagement:NewClaimTypeRequired"
            );

        var newClaimValue =
            NormalizeRequiredValue(
                input.NewClaimValue,
                "StoreManagement:NewClaimValueRequired"
            );

        var existingClaims =
            await _userManager.GetClaimsAsync(user);

        var oldClaim =
            existingClaims.FirstOrDefault(claim =>
                claim.Type == oldClaimType &&
                claim.Value == oldClaimValue
            );

        if (oldClaim == null)
        {
            throw new BusinessException(
                "StoreManagement:UserClaimNotFound"
            );
        }

        if (oldClaimType == newClaimType &&
            oldClaimValue == newClaimValue)
        {
            return new UserClaimDto
            {
                ClaimType = newClaimType,
                ClaimValue = newClaimValue
            };
        }

        if (existingClaims.Any(claim =>
                claim.Type == newClaimType &&
                claim.Value == newClaimValue))
        {
            throw new BusinessException(
                "StoreManagement:UserClaimAlreadyExists"
            );
        }

        var removeResult =
            await _userManager.RemoveClaimAsync(
                user,
                oldClaim
            );

        CheckIdentityResult(removeResult);

        var addResult =
            await _userManager.AddClaimAsync(
                user,
                new Claim(newClaimType, newClaimValue)
            );

        CheckIdentityResult(addResult);

        return new UserClaimDto
        {
            ClaimType = newClaimType,
            ClaimValue = newClaimValue
        };
    }

    [Authorize(StoreManagementPermissions.ClaimManagement.Manage)]
    public async Task DeleteUserClaimAsync(
        DeleteUserClaimDto input)
    {
        var user =
            await GetUserAsync(input.UserId);

        var claimType =
            NormalizeRequiredValue(
                input.ClaimType,
                "StoreManagement:ClaimTypeRequired"
            );

        var claimValue =
            NormalizeRequiredValue(
                input.ClaimValue,
                "StoreManagement:ClaimValueRequired"
            );

        var existingClaims =
            await _userManager.GetClaimsAsync(user);

        var claimToDelete =
            existingClaims.FirstOrDefault(claim =>
                claim.Type == claimType &&
                claim.Value == claimValue
            );

        if (claimToDelete == null)
        {
            throw new BusinessException(
                "StoreManagement:UserClaimNotFound"
            );
        }

        var result =
            await _userManager.RemoveClaimAsync(
                user,
                claimToDelete
            );

        CheckIdentityResult(result);
    }

    private async Task<AbpIdentityUser> GetUserAsync(
        Guid userId)
    {
        var user =
            await _userRepository.FindAsync(userId);

        if (user == null)
        {
            throw new EntityNotFoundException(
                typeof(AbpIdentityUser),
                userId
            );
        }

        return user;
    }

    private static string NormalizeRequiredValue(
        string value,
        string errorCode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessException(errorCode);
        }

        return value.Trim();
    }

    private static void CheckIdentityResult(
        AspNetIdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors =
            string.Join(
                "; ",
                result.Errors.Select(error =>
                    error.Description)
            );

        throw new BusinessException(
                "StoreManagement:IdentityOperationFailed"
            )
            .WithData(
                "Errors",
                errors
            );
    }
}