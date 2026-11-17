using AkuranaExplore.Models;
using Microsoft.EntityFrameworkCore;

namespace AkuranaExplore.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Place> Places => Set<Place>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(c => c.Name).IsUnique();
            entity.HasIndex(c => c.Slug).IsUnique();
        });

        modelBuilder.Entity<Place>(entity =>
        {
            entity.HasIndex(p => p.Name).IsUnique();
            entity.Property(p => p.EntryFeeLkr).HasPrecision(10, 2);
            entity.HasOne(p => p.Category)
                .WithMany(c => c.Places)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.HasIndex(a => a.Username).IsUnique();
        });
    }
}
