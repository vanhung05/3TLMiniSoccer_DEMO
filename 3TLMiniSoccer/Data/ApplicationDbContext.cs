using Microsoft.EntityFrameworkCore;
using _3TLMiniSoccer.Models;
using System.Linq;

namespace _3TLMiniSoccer.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            
            // Configure SQL Server to handle triggers properly
            optionsBuilder.UseSqlServer(options => 
            {
                options.CommandTimeout(30);
                // Enable retry on failure for better handling of trigger conflicts
                options.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
            });
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await base.SaveChangesAsync(cancellationToken);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && 
                sqlEx.Message.Contains("The target table") && sqlEx.Message.Contains("cannot have any enabled triggers"))
            {
                // Handle trigger conflict by using ExecuteSqlRaw for updates
                var entries = ChangeTracker.Entries()
                    .Where(e => e.State == EntityState.Modified)
                    .ToList();

                foreach (var entry in entries)
                {
                    if (entry.Entity is Field field)
                    {
                        await Database.ExecuteSqlRawAsync(
                            "UPDATE Fields SET FieldName = {0}, FieldTypeId = {1}, Location = {2}, Status = {3}, Description = {4}, ImageUrl = {5}, UpdatedAt = {6} WHERE FieldId = {7}",
                            field.FieldName, field.FieldTypeId, field.Location, field.Status, field.Description, field.ImageUrl, DateTime.UtcNow, field.FieldId);
                    }
                }

                // Clear the change tracker to avoid conflicts
                ChangeTracker.Clear();
                return entries.Count;
            }
        }

        // User Management
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<SocialLogin> SocialLogins { get; set; }

        // Field Management
        public DbSet<FieldType> FieldTypes { get; set; }
        public DbSet<Field> Fields { get; set; }
        public DbSet<PricingRule> PricingRules { get; set; }
        public DbSet<FieldSchedule> FieldSchedules { get; set; }

        // Booking Management
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingSession> BookingSessions { get; set; }
        public DbSet<SessionOrder> SessionOrders { get; set; }
        public DbSet<SessionOrderItem> SessionOrderItems { get; set; }

        // Payment Management
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<PaymentConfig> PaymentConfigs { get; set; }
        public DbSet<PaymentOrder> PaymentOrders { get; set; }
        
        // System Configuration (replaces PaymentConfigs and SystemSettings)
        public DbSet<SystemConfig> SystemConfigs { get; set; }
        
        // Shopping Cart
        public DbSet<CartItem> CartItems { get; set; }

        // Discount & Loyalty
        public DbSet<DiscountCode> DiscountCodes { get; set; }
        public DbSet<DiscountCodeUsage> DiscountCodeUsages { get; set; }
        public DbSet<LoyaltyPoint> LoyaltyPoints { get; set; }


        // Product Management
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductType> ProductTypes { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        // Communication
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Contact> Contacts { get; set; }

        // System
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity to work with triggers
            modelBuilder.Entity<User>()
                .ToTable("Users", t => t.HasTrigger("tr_Users_Audit"));

            // Configure Booking entity to work with triggers  
            modelBuilder.Entity<Booking>()
                .ToTable("Bookings", t => t.HasTrigger("tr_Bookings_UpdateSchedule"))
                .ToTable("Bookings", t => t.HasTrigger("tr_Bookings_UpdatedAt"));

            // Configure relationships and constraints

            // User-Role relationship (đã gộp vào User)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(u => u.AssignedByUser)
                .WithMany(u => u.AssignedUsers)
                .HasForeignKey(u => u.AssignedBy)
                .OnDelete(DeleteBehavior.NoAction);

            // Field relationships (đã bỏ FieldCluster)
            modelBuilder.Entity<Field>()
                .HasOne(f => f.FieldType)
                .WithMany(ft => ft.Fields)
                .HasForeignKey(f => f.FieldTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Booking relationships
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Field)
                .WithMany(f => f.Bookings)
                .HasForeignKey(b => b.FieldId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<Booking>()
                .HasOne(b => b.ConfirmedByUser)
                .WithMany()
                .HasForeignKey(b => b.ConfirmedBy)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.CancelledByUser)
                .WithMany()
                .HasForeignKey(b => b.CancelledBy)
                .OnDelete(DeleteBehavior.NoAction);

            // FieldSchedule relationships
            modelBuilder.Entity<FieldSchedule>()
                .HasOne(fs => fs.Field)
                .WithMany(f => f.FieldSchedules)
                .HasForeignKey(fs => fs.FieldId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<FieldSchedule>()
                .HasOne(fs => fs.Booking)
                .WithMany(b => b.FieldSchedules)
                .HasForeignKey(fs => fs.BookingId)
                .OnDelete(DeleteBehavior.SetNull);

            // Transaction relationships
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Booking)
                .WithMany(b => b.Transactions)
                .HasForeignKey(t => t.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.PaymentMethod)
                .WithMany(pm => pm.Transactions)
                .HasForeignKey(t => t.PaymentMethodId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order relationships
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Booking)
                .WithMany(b => b.Orders)
                .HasForeignKey(o => o.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);


            // Product relationships
            modelBuilder.Entity<Product>()
                .HasOne(p => p.CreatedByUser)
                .WithMany()
                .HasForeignKey(p => p.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.ProductType)
                .WithMany(pt => pt.Products)
                .HasForeignKey(p => p.ProductTypeId)
                .OnDelete(DeleteBehavior.SetNull);


            // Discount Code relationships
            modelBuilder.Entity<DiscountCode>()
                .HasOne(dc => dc.CreatedByUser)
                .WithMany()
                .HasForeignKey(dc => dc.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DiscountCodeUsage>()
                .HasOne(dcu => dcu.DiscountCode)
                .WithMany(dc => dc.DiscountCodeUsages)
                .HasForeignKey(dcu => dcu.DiscountCodeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DiscountCodeUsage>()
                .HasOne(dcu => dcu.Booking)
                .WithMany()
                .HasForeignKey(dcu => dcu.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DiscountCodeUsage>()
                .HasOne(dcu => dcu.User)
                .WithMany()
                .HasForeignKey(dcu => dcu.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Loyalty Point relationships
            modelBuilder.Entity<LoyaltyPoint>()
                .HasOne(lp => lp.User)
                .WithMany(u => u.LoyaltyPoints)
                .HasForeignKey(lp => lp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LoyaltyPoint>()
                .HasOne(lp => lp.Booking)
                .WithMany()
                .HasForeignKey(lp => lp.BookingId)
                .OnDelete(DeleteBehavior.SetNull);

            // Notification relationships
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Contact relationships
            modelBuilder.Entity<Contact>()
                .HasOne(c => c.RespondedByUser)
                .WithMany()
                .HasForeignKey(c => c.RespondedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Audit Log relationships
            modelBuilder.Entity<AuditLog>()
                .HasOne(al => al.User)
                .WithMany()
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // System Config relationships
            modelBuilder.Entity<SystemConfig>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(sc => sc.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // PaymentOrder relationships
            modelBuilder.Entity<PaymentOrder>()
                .HasOne(po => po.Booking)
                .WithMany()
                .HasForeignKey(po => po.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PaymentOrder>()
                .HasOne(po => po.User)
                .WithMany()
                .HasForeignKey(po => po.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Social Login relationships
            modelBuilder.Entity<SocialLogin>()
                .HasOne(sl => sl.User)
                .WithMany(u => u.SocialLogins)
                .HasForeignKey(sl => sl.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Booking Session relationships
            modelBuilder.Entity<BookingSession>()
                .HasOne(bs => bs.Booking)
                .WithMany(b => b.BookingSessions)
                .HasForeignKey(bs => bs.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Role>()
                .HasIndex(r => r.RoleName)
                .IsUnique();

            modelBuilder.Entity<Booking>()
                .HasIndex(b => b.BookingCode)
                .IsUnique();

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderCode)
                .IsUnique();

            modelBuilder.Entity<DiscountCode>()
                .HasIndex(dc => dc.Code)
                .IsUnique();

            modelBuilder.Entity<SystemConfig>()
                .HasIndex(sc => sc.ConfigKey)
                .IsUnique();

            modelBuilder.Entity<ProductType>()
                .HasIndex(pt => pt.TypeName)
                .IsUnique();

            modelBuilder.Entity<SocialLogin>()
                .HasIndex(sl => new { sl.Provider, sl.ProviderKey })
                .IsUnique();

            // Configure field schedules unique constraint
            modelBuilder.Entity<FieldSchedule>()
                .HasIndex(fs => new { fs.FieldId, fs.Date, fs.StartTime, fs.EndTime })
                .IsUnique();

            // Configure decimal precision
            modelBuilder.Entity<FieldType>()
                .Property(ft => ft.BasePrice)
                .HasPrecision(10, 2);

            modelBuilder.Entity<PricingRule>()
                .Property(pr => pr.Price)
                .HasPrecision(10, 2);

            modelBuilder.Entity<PricingRule>()
                .Property(pr => pr.PeakMultiplier)
                .HasPrecision(3, 2);

            modelBuilder.Entity<Booking>()
                .Property(b => b.TotalPrice)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Transaction>()
                .Property(t => t.Amount)
                .HasPrecision(10, 2);

            modelBuilder.Entity<DiscountCode>()
                .Property(dc => dc.DiscountValue)
                .HasPrecision(10, 2);

            modelBuilder.Entity<DiscountCode>()
                .Property(dc => dc.MinOrderAmount)
                .HasPrecision(10, 2);

            modelBuilder.Entity<DiscountCode>()
                .Property(dc => dc.MaxDiscountAmount)
                .HasPrecision(10, 2);

            modelBuilder.Entity<DiscountCodeUsage>()
                .Property(dcu => dcu.DiscountAmount)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(10, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasPrecision(10, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.TotalPrice)
                .HasPrecision(10, 2);
        }
    }
}
