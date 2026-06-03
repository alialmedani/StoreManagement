using Microsoft.EntityFrameworkCore;
using StoreManagement.Categories;
using StoreManagement.Products;
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

    public DbSet<Product> Products { get; set; }

    public DbSet<ProductVariant> ProductVariants { get; set; }

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

        builder.Entity<Product>(b =>
        {
            b.ToTable(
                StoreManagementConsts.DbTablePrefix + "Products",
                StoreManagementConsts.DbSchema
            );

            b.ConfigureByConvention();

            b.Property(product => product.Name)
                .IsRequired()
                .HasMaxLength(ProductConsts.MaxNameLength);

            b.Property(product => product.NormalizedName)
                .IsRequired()
                .HasMaxLength(ProductConsts.MaxNameLength);

            b.Property(product => product.Description)
                .HasMaxLength(ProductConsts.MaxDescriptionLength);

            b.Property(product => product.Price)
                .IsRequired()
                .HasPrecision(18, 2);

            b.Property(product => product.IsActive)
                .IsRequired();

            b.Property(product => product.TargetAudience)
                .IsRequired();

            b.Property(product => product.CategoryId)
                .IsRequired();

            b.HasOne(product => product.Category)
                .WithMany()
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasMany(product => product.Variants)
                .WithOne(variant => variant.Product)
                .HasForeignKey(variant => variant.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(product => new
                {
                    product.CategoryId,
                    product.NormalizedName
                })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0")
                .HasDatabaseName("UX_StoreManagement_Products_CategoryId_NormalizedName_Active");
        });

        builder.Entity<ProductVariant>(b =>
        {
            b.ToTable(
                StoreManagementConsts.DbTablePrefix + "ProductVariants",
                StoreManagementConsts.DbSchema
            );

            b.ConfigureByConvention();

            b.Property(variant => variant.ProductId)
                .IsRequired();

            b.Property(variant => variant.Color)
                .IsRequired()
                .HasMaxLength(ProductVariantConsts.MaxColorLength);

            b.Property(variant => variant.NormalizedColor)
                .IsRequired()
                .HasMaxLength(ProductVariantConsts.MaxColorLength);

            b.Property(variant => variant.Size)
                .IsRequired()
                .HasMaxLength(ProductVariantConsts.MaxSizeLength);

            b.Property(variant => variant.NormalizedSize)
                .IsRequired()
                .HasMaxLength(ProductVariantConsts.MaxSizeLength);

            b.Property(variant => variant.StockQuantity)
                .IsRequired();

            b.HasOne(variant => variant.Product)
                .WithMany(product => product.Variants)
                .HasForeignKey(variant => variant.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(variant => new
                {
                    variant.ProductId,
                    variant.NormalizedColor,
                    variant.NormalizedSize
                })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0")
                .HasDatabaseName("UX_StoreManagement_ProductVariants_ProductId_Color_Size_Active");

            b.HasIndex(variant => variant.ProductId)
                .HasDatabaseName("IX_StoreManagement_ProductVariants_ProductId");
        });
    }
}