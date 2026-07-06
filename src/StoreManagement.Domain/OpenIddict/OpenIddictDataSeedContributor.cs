using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenIddict.Abstractions;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.OpenIddict;
using Volo.Abp.OpenIddict.Applications;
using Volo.Abp.OpenIddict.Scopes;
using Volo.Abp.Uow;

namespace StoreManagement.OpenIddict;

/*
 * Creates initial data that is needed to properly run the application
 * and make client-to-server communication possible.
 */
public class OpenIddictDataSeedContributor :
    OpenIddictDataSeedContributorBase,
    IDataSeedContributor,
    ITransientDependency
{
    public OpenIddictDataSeedContributor(
        IConfiguration configuration,
        IOpenIddictApplicationRepository openIddictApplicationRepository,
        IAbpApplicationManager applicationManager,
        IOpenIddictScopeRepository openIddictScopeRepository,
        IOpenIddictScopeManager scopeManager)
        : base(
            configuration,
            openIddictApplicationRepository,
            applicationManager,
            openIddictScopeRepository,
            scopeManager)
    {
    }

    [UnitOfWork]
    public virtual async Task SeedAsync(DataSeedContext context)
    {
        await CreateScopesAsync();
        await CreateApplicationsAsync();
    }

    private async Task CreateScopesAsync()
    {
        await CreateScopesAsync(new OpenIddictScopeDescriptor
        {
            Name = "StoreManagement",
            DisplayName = "StoreManagement API",
            Resources = { "StoreManagement" }
        });
    }

    private async Task CreateApplicationsAsync()
    {
        var commonScopes = new List<string>
        {
            OpenIddictConstants.Permissions.Scopes.Address,
            OpenIddictConstants.Permissions.Scopes.Email,
            OpenIddictConstants.Permissions.Scopes.Phone,
            OpenIddictConstants.Permissions.Scopes.Profile,
            OpenIddictConstants.Permissions.Scopes.Roles,
            "StoreManagement"
        };

        var configurationSection = Configuration.GetSection("OpenIddict:Applications");

        // Mobile Client
        var mobileClientId = configurationSection["StoreManagement_Mobile:ClientId"];

        if (!mobileClientId.IsNullOrWhiteSpace())
        {
            var mobileRedirectUri = configurationSection["StoreManagement_Mobile:RedirectUri"];
            var mobilePostLogoutRedirectUri = configurationSection["StoreManagement_Mobile:PostLogoutRedirectUri"];

            if (mobileRedirectUri.IsNullOrWhiteSpace())
            {
                throw new AbpException("StoreManagement_Mobile:RedirectUri is missing in OpenIddict applications configuration.");
            }

            if (mobilePostLogoutRedirectUri.IsNullOrWhiteSpace())
            {
                throw new AbpException("StoreManagement_Mobile:PostLogoutRedirectUri is missing in OpenIddict applications configuration.");
            }

            await CreateOrUpdateApplicationAsync(
                applicationType: OpenIddictConstants.ApplicationTypes.Native,
                name: mobileClientId!,
                type: OpenIddictConstants.ClientTypes.Public,
                consentType: OpenIddictConstants.ConsentTypes.Implicit,
                displayName: "StoreManagement Mobile Application",
                secret: null,
                grantTypes: new List<string>
                {
                    OpenIddictConstants.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.GrantTypes.RefreshToken,
                    OpenIddictConstants.GrantTypes.Password
                },
                scopes: commonScopes,
                redirectUris: new List<string>
                {
                    mobileRedirectUri!
                },
                postLogoutRedirectUris: new List<string>
                {
                    mobilePostLogoutRedirectUri!
                },
                clientUri: null,
                logoUri: null
            );
        }

        // Swagger Client
        var swaggerClientId = configurationSection["StoreManagement_Swagger:ClientId"];

        if (!swaggerClientId.IsNullOrWhiteSpace())
        {
            var swaggerRootUrl = configurationSection["StoreManagement_Swagger:RootUrl"]?.TrimEnd('/');

            if (swaggerRootUrl.IsNullOrWhiteSpace())
            {
                throw new AbpException("StoreManagement_Swagger:RootUrl is missing in OpenIddict applications configuration.");
            }

            await CreateOrUpdateApplicationAsync(
                applicationType: OpenIddictConstants.ApplicationTypes.Web,
                name: swaggerClientId!,
                type: OpenIddictConstants.ClientTypes.Public,
                consentType: OpenIddictConstants.ConsentTypes.Implicit,
                displayName: "Swagger Application",
                secret: null,
                grantTypes: new List<string>
                {
                    OpenIddictConstants.GrantTypes.AuthorizationCode
                },
                scopes: commonScopes,
                redirectUris: new List<string>
                {
                    $"{swaggerRootUrl}/swagger/oauth2-redirect.html"
                },
                clientUri: swaggerRootUrl.EnsureEndsWith('/') + "swagger",
                logoUri: "/images/clients/swagger.svg"
            );
        }
    }
}