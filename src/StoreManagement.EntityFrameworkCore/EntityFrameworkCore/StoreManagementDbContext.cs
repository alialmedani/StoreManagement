using Microsoft.EntityFrameworkCore;
using StoreManagement.Categories;
using StoreManagement.Inventory;
using StoreManagement.Media;
using StoreManagement.Orders;
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

public DbSet<StockMovement> StockMovements { get; set; }

public DbSet<Order> Orders { get; set; }

public DbSet<OrderItem> OrderItems { get; set; }

public DbSet<OrderPayment> OrderPayments { get; set; }

public DbSet<OrderNumberSequence> OrderNumberSequences { get; set; }

public DbSet<Media.Media> Media { get; set; }

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

public StoreManagementDbContext(
    DbContextOptions<StoreManagementDbContext> options)
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

    builder.Entity<Media.Media>(b =>
    {
        b.ToTable(
            StoreManagementConsts.DbTablePrefix + "Media",
            StoreManagementConsts.DbSchema
        );

        b.ConfigureByConvention();

        b.Property(media => media.EntityId)
            .IsRequired()
            .HasMaxLength(MediaConsts.MaxEntityIdLength);

        b.Property(media => media.EntityType)
            .IsRequired();

        b.Property(media => media.FilePlacement)
            .IsRequired()
            .HasMaxLength(MediaConsts.MaxFilePlacementLength);

        b.Property(media => media.FileName)
            .IsRequired()
            .HasMaxLength(MediaConsts.MaxFileNameLength);

        b.Property(media => media.OriginalFileName)
            .IsRequired()
            .HasMaxLength(MediaConsts.MaxFileNameLength);

        b.Property(media => media.BlobName)
            .IsRequired()
            .HasMaxLength(MediaConsts.MaxFileNameLength);

        b.Property(media => media.ContentType)
            .IsRequired()
            .HasMaxLength(MediaConsts.MaxContentTypeLength);

        b.Property(media => media.Size)
            .IsRequired();

        b.HasIndex(media => new
            {
                media.EntityType,
                media.EntityId
            })
            .HasDatabaseName(
                "IX_StoreManagement_Media_EntityType_EntityId"
            );

        b.HasIndex(media => media.FileName)
            .HasDatabaseName(
                "IX_StoreManagement_Media_FileName"
            );

        b.HasIndex(media => media.BlobName)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName(
                "UX_StoreManagement_Media_BlobName_Active"
            );

        b.HasIndex(media => media.CreationTime)
            .HasDatabaseName(
                "IX_StoreManagement_Media_CreationTime"
            );
    });

    builder.Entity<OrderNumberSequence>(b =>
    {
        b.ToTable(
            StoreManagementConsts.DbTablePrefix + "OrderNumberSequences",
            StoreManagementConsts.DbSchema
        );

        b.ConfigureByConvention();

        b.Property(sequence => sequence.Prefix)
            .IsRequired()
            .HasMaxLength(OrderConsts.MaxOrderNumberPrefixLength);

        b.Property(sequence => sequence.Year)
            .IsRequired();

        b.Property(sequence => sequence.NextNumber)
            .IsRequired();

        b.HasIndex(sequence => new
            {
                sequence.Prefix,
                sequence.Year
            })
            .IsUnique()
            .HasDatabaseName(
                "UX_StoreManagement_OrderNumberSequences_Prefix_Year"
            );
    });

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

        b.Property(category => category.ImageUrl)
            .HasMaxLength(500);

        b.Property(category => category.IsActive)
            .IsRequired();

        b.HasIndex(category => category.NormalizedName)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName(
                "UX_StoreManagement_Categories_NormalizedName_Active"
            );
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

        b.Property(product => product.ImageUrl)
            .HasMaxLength(500);

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
            .HasDatabaseName(
                "UX_StoreManagement_Products_CategoryId_NormalizedName_Active"
            );
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

        b.Property(variant => variant.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

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
            .HasDatabaseName(
                "UX_StoreManagement_ProductVariants_ProductId_Color_Size_Active"
            );

        b.HasIndex(variant => variant.ProductId)
            .HasDatabaseName(
                "IX_StoreManagement_ProductVariants_ProductId"
            );
    });

    builder.Entity<StockMovement>(b =>
    {
        b.ToTable(
            StoreManagementConsts.DbTablePrefix + "StockMovements",
            StoreManagementConsts.DbSchema
        );

        b.ConfigureByConvention();

        b.Property(movement => movement.ProductVariantId)
            .IsRequired();

        b.Property(movement => movement.MovementType)
            .IsRequired();

        b.Property(movement => movement.QuantityChange)
            .IsRequired();

        b.Property(movement => movement.OldQuantity)
            .IsRequired();

        b.Property(movement => movement.NewQuantity)
            .IsRequired();

        b.Property(movement => movement.SourceType)
            .IsRequired()
            .HasDefaultValue(StockMovementSourceType.Manual);

        b.Property(movement => movement.ReferenceId);

        b.Property(movement => movement.Note)
            .HasMaxLength(InventoryConsts.MaxNoteLength);

        b.HasOne(movement => movement.ProductVariant)
            .WithMany()
            .HasForeignKey(movement => movement.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(movement => movement.ProductVariantId)
            .HasDatabaseName(
                "IX_StoreManagement_StockMovements_ProductVariantId"
            );

        b.HasIndex(movement => movement.MovementType)
            .HasDatabaseName(
                "IX_StoreManagement_StockMovements_MovementType"
            );

        b.HasIndex(movement => movement.SourceType)
            .HasDatabaseName(
                "IX_StoreManagement_StockMovements_SourceType"
            );

        b.HasIndex(movement => movement.ReferenceId)
            .HasDatabaseName(
                "IX_StoreManagement_StockMovements_ReferenceId"
            );

        b.HasIndex(movement => movement.CreationTime)
            .HasDatabaseName(
                "IX_StoreManagement_StockMovements_CreationTime"
            );
    });

    builder.Entity<Order>(b =>
    {
        b.ToTable(
            StoreManagementConsts.DbTablePrefix + "Orders",
            StoreManagementConsts.DbSchema
        );

        b.ConfigureByConvention();

        b.Property(order => order.OrderNumber)
            .IsRequired()
            .HasMaxLength(OrderConsts.MaxOrderNumberLength);

        b.Property(order => order.CustomerName)
            .IsRequired()
            .HasMaxLength(OrderConsts.MaxCustomerNameLength);
        b.Property(order => order.CustomerAddress)
            .IsRequired()
            .HasMaxLength(OrderConsts.MaxCustomerAddressLength);
        b.Property(order => order.CustomerPhone)
            .IsRequired()
            .HasMaxLength(OrderConsts.MaxCustomerPhoneLength);

        b.Property(order => order.Note)
            .HasMaxLength(OrderConsts.MaxNoteLength);

        b.Property(order => order.CancellationReason)
            .HasMaxLength(OrderConsts.MaxNoteLength);

        b.Property(order => order.CancellationTime);

        b.Property(order => order.Status)
            .IsRequired();

        b.Property(order => order.TotalAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        b.Property(order => order.PaymentStatus)
            .IsRequired()
            .HasDefaultValue(OrderPaymentStatus.Unpaid);

        b.Property(order => order.PaidAmount)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        b.HasMany(order => order.Items)
            .WithOne(item => item.Order)
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Navigation(order => order.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        b.HasMany(order => order.Payments)
            .WithOne(payment => payment.Order)
            .HasForeignKey(payment => payment.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Navigation(order => order.Payments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        b.HasIndex(order => order.OrderNumber)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName(
                "UX_StoreManagement_Orders_OrderNumber_Active"
            );

        b.HasIndex(order => order.Status)
            .HasDatabaseName(
                "IX_StoreManagement_Orders_Status"
            );

        b.HasIndex(order => order.PaymentStatus)
            .HasDatabaseName(
                "IX_StoreManagement_Orders_PaymentStatus"
            );

        b.HasIndex(order => order.CancellationTime)
            .HasDatabaseName(
                "IX_StoreManagement_Orders_CancellationTime"
            );

        b.HasIndex(order => order.CreationTime)
            .HasDatabaseName(
                "IX_StoreManagement_Orders_CreationTime"
            );
    });

    builder.Entity<OrderPayment>(b =>
    {
        b.ToTable(
            StoreManagementConsts.DbTablePrefix + "OrderPayments",
            StoreManagementConsts.DbSchema
        );

        b.ConfigureByConvention();

        b.Property(payment => payment.OrderId)
            .IsRequired();

        b.Property(payment => payment.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        b.Property(payment => payment.PaymentMethod)
            .IsRequired();

        b.Property(payment => payment.PaymentDate)
            .IsRequired();

        b.Property(payment => payment.ReferenceNumber)
            .HasMaxLength(
                OrderConsts.MaxPaymentReferenceNumberLength
            );

        b.Property(payment => payment.Note)
            .HasMaxLength(
                OrderConsts.MaxPaymentNoteLength
            );

        b.HasIndex(payment => new
            {
                payment.OrderId,
                payment.PaymentDate
            })
            .HasDatabaseName(
                "IX_StoreManagement_OrderPayments_OrderId_PaymentDate"
            );

        b.HasIndex(payment => payment.PaymentMethod)
            .HasDatabaseName(
                "IX_StoreManagement_OrderPayments_PaymentMethod"
            );

        b.HasIndex(payment => payment.CreationTime)
            .HasDatabaseName(
                "IX_StoreManagement_OrderPayments_CreationTime"
            );
    });

    builder.Entity<OrderItem>(b =>
    {
        b.ToTable(
            StoreManagementConsts.DbTablePrefix + "OrderItems",
            StoreManagementConsts.DbSchema
        );

        b.ConfigureByConvention();

        b.Property(item => item.OrderId)
            .IsRequired();

        b.Property(item => item.ProductVariantId)
            .IsRequired();

        b.Property(item => item.ProductName)
            .IsRequired()
            .HasMaxLength(ProductConsts.MaxNameLength);

        b.Property(item => item.Color)
            .IsRequired()
            .HasMaxLength(ProductVariantConsts.MaxColorLength);

        b.Property(item => item.Size)
            .IsRequired()
            .HasMaxLength(ProductVariantConsts.MaxSizeLength);

        b.Property(item => item.Quantity)
            .IsRequired();

        b.Property(item => item.UnitPrice)
            .IsRequired()
            .HasPrecision(18, 2);

        b.Property(item => item.LineTotal)
            .IsRequired()
            .HasPrecision(18, 2);

        b.HasOne(item => item.Order)
            .WithMany(order => order.Items)
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(item => item.ProductVariant)
            .WithMany()
            .HasForeignKey(item => item.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(item => item.OrderId)
            .HasDatabaseName(
                "IX_StoreManagement_OrderItems_OrderId"
            );

        b.HasIndex(item => item.ProductVariantId)
            .HasDatabaseName(
                "IX_StoreManagement_OrderItems_ProductVariantId"
            );
    });
}
 
}