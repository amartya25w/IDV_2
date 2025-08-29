using IDV_2.Models;
using Microsoft.EntityFrameworkCore;
using UserAuthAPI.Models;

namespace IDV_2.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            Console.WriteLine(Database.ProviderName);
        }

        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<FormTemplate> FormTemplates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.CreatedAt)
                        .HasColumnType("datetime(6)")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.Property(e => e.UpdatedAt)
                      .HasColumnType("datetime(6)")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                      .ValueGeneratedOnAddOrUpdate();

                // One-to-many relationship with RefreshTokens
                entity.HasMany(u => u.RefreshTokens)
                      .WithOne(rt => rt.User)
                      .HasForeignKey(rt => rt.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasAlternateKey(u => u.PublicId)
                      .HasName("Ak_Users_PublicId");
            });

            // RefreshToken configuration
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ExpiresAt).IsRequired();
                entity.Property(e => e.CreatedAt)
                  .HasColumnType("datetime(6)")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            });

            var ft = modelBuilder.Entity<FormTemplate>();
            ft.ToTable("form_templates");

            ft.HasKey(x => x.Id);
            ft.Property(x => x.Id).HasColumnName("id").HasColumnType("char(36)");

            ft.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            ft.Property(x => x.Description).HasColumnName("description");

            ft.Property(x => x.CreatedBy)
              .HasColumnName("created_by")
              .HasColumnType("char(36)")    // or use a ValueConverter for BINARY(16)
              .IsRequired();

            ft.Property(x => x.TemplateRulesJson).HasColumnName("template_rules").HasColumnType("json");
            ft.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            ft.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc")
                                            .HasColumnType("datetime(6)")
                                            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

            ft.HasIndex(x => x.IsActive).HasDatabaseName("idx_templates_active");
            ft.HasIndex(x => x.CreatedBy).HasDatabaseName("idx_templates_creator");

            // 3) Link FK to the User’s alternate key (Guid)
            ft.HasOne(x => x.CreatedByUser)
              .WithMany(u => u.FormTemplatesCreated)
              .HasForeignKey(x => x.CreatedBy)
              .HasPrincipalKey(u => u.PublicId)
              .OnDelete(DeleteBehavior.Restrict);
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is User && e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                ((User)entry.Entity).UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}