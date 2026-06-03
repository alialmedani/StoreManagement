using Microsoft.EntityFrameworkCore;
using StoreManagement.Categories;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.BlobStoring.Database.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;

namespace StoreManagement.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ReplaceDbContext(typeof(ITenantManagementDbContext))]
[ConnectionStringName("Default")]
public class StoreManagementDbContext :
    AbpDbContext<StoreManagementDbContext>,
    ITenantManagementDbContext,
    IIdentityDbContext
{
    /*
     * Add DbSet properties for your Aggregate Roots / Entities here.
     */

    public DbSet<Category> Categories { get; set; }

    #region Entities from the modules

    /*
     * Notice: We only implemented IIdentityDbContext and ITenantManagementDbContext
     * and replaced them for this DbContext. This allows you to perform JOIN
     * queries for the entities of these modules over the repositories easily.
     */

    // Identity
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }

    // Tenant Management
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }

    #endregion

    public StoreManagementDbContext(DbContextOptions<StoreManagementDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /*
         * Include modules to your migration db context.
         */

        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureFeatureManagement();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureTenantManagement();
        builder.ConfigureBlobStoring();

        /*
         * Configure your own tables/entities inside here.
         */

        builder.Entity<Category>(b =>
        {
            b.ToTable(
                StoreManagementConsts.DbTablePrefix + "Categories",
                StoreManagementConsts.DbSchema
            );

            b.ConfigureByConvention();

            b.Property(category => category.Name)
                .IsRequired()
                .HasMaxLength(CategoryConsts.MaxNameLength);

            b.Property(category => category.NormalizedName)
                .IsRequired()
                .HasMaxLength(CategoryConsts.MaxNameLength);

            b.Property(category => category.Description)
                .HasMaxLength(CategoryConsts.MaxDescriptionLength);

            b.Property(category => category.SizeType)
                .IsRequired();

            b.Property(category => category.IsActive)
                .IsRequired();

            b.HasIndex(category => category.NormalizedName)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0")
                .HasDatabaseName("UX_StoreManagement_Categories_NormalizedName_Active");
        });
    }
}