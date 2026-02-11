using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Models;

namespace WarehouseManagement.Data
{
    public class WarehouseContext : DbContext
    {
        public WarehouseContext(DbContextOptions<WarehouseContext> options) : base(options)
        {
        }

        // الجداول الأساسية
        public DbSet<Material> Materials { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<MaterialStock> MaterialStocks { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<Transfer> Transfers { get; set; }
        public DbSet<InventoryRecord> InventoryRecords { get; set; }
        public DbSet<ConsumptionRecord> ConsumptionRecords { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<AppUser> AppUsers { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureMaterial(modelBuilder);
            ConfigureCategory(modelBuilder);
            ConfigureLocation(modelBuilder);
            ConfigureMaterialStock(modelBuilder);
            ConfigurePurchase(modelBuilder);
            ConfigureTransfer(modelBuilder);
            ConfigureInventoryRecord(modelBuilder);
            ConfigureConsumptionRecord(modelBuilder);
            
            SeedData(modelBuilder);
            base.OnModelCreating(modelBuilder);
        }

        private void ConfigureMaterial(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Material>(entity =>
            {
                entity.HasIndex(m => m.Code).IsUnique();
                entity.HasOne(m => m.Category)
                    .WithMany(c => c.Materials)
                    .HasForeignKey(m => m.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureCategory(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasOne(c => c.ParentCategory)
                    .WithMany(c => c.SubCategories)
                    .HasForeignKey(c => c.ParentCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureLocation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Location>(entity =>
            {
                entity.HasIndex(l => l.Code).IsUnique();
            });
        }

        private void ConfigureMaterialStock(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MaterialStock>(entity =>
            {
                entity.HasIndex(s => new { s.MaterialId, s.LocationId }).IsUnique();
                entity.HasOne(s => s.Material)
                    .WithMany(m => m.Stocks)
                    .HasForeignKey(s => s.MaterialId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(s => s.Location)
                    .WithMany(l => l.Stocks)
                    .HasForeignKey(s => s.LocationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigurePurchase(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Purchase>(entity =>
            {
                entity.Property(p => p.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(p => p.StoredTotalPrice).HasColumnType("decimal(18,2)");
                entity.Property(p => p.ExchangeRate).HasColumnType("decimal(18,4)");
                entity.HasIndex(p => p.PurchaseDate);
                entity.HasOne(p => p.Material)
                    .WithMany(m => m.Purchases)
                    .HasForeignKey(p => p.MaterialId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(p => p.Location)
                    .WithMany(l => l.Purchases)
                    .HasForeignKey(p => p.LocationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureTransfer(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Transfer>(entity =>
            {
                entity.HasOne(t => t.Material)
                    .WithMany(m => m.Transfers)
                    .HasForeignKey(t => t.MaterialId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(t => t.FromLocation)
                    .WithMany()
                    .HasForeignKey(t => t.FromLocationId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(t => t.ToLocation)
                    .WithMany()
                    .HasForeignKey(t => t.ToLocationId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasCheckConstraint("CK_Transfer_DifferentLocations", 
                    "[FromLocationId] <> [ToLocationId]");
            });
        }

        private void ConfigureInventoryRecord(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InventoryRecord>(entity =>
            {
                entity.HasIndex(i => new { i.MaterialId, i.LocationId, i.Year }).IsUnique();
                entity.HasIndex(i => i.Year);
                entity.HasOne(i => i.Material)
                    .WithMany(m => m.InventoryRecords)
                    .HasForeignKey(i => i.MaterialId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(i => i.Location)
                    .WithMany(l => l.InventoryRecords)
                    .HasForeignKey(i => i.LocationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureConsumptionRecord(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ConsumptionRecord>(entity =>
            {
                entity.Property(c => c.DamagePercentage).HasColumnType("decimal(5,2)");
                entity.Property(c => c.OriginalUnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(c => c.StoredOriginalValue).HasColumnType("decimal(18,2)");
                entity.Property(c => c.StoredResidualValue).HasColumnType("decimal(18,2)");
                entity.Property(c => c.SaleValue).HasColumnType("decimal(18,2)");
                entity.HasIndex(c => c.ReportDate);
                entity.HasOne(c => c.InventoryRecord)
                    .WithMany(i => i.Consumptions)
                    .HasForeignKey(c => c.InventoryRecordId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            var seedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "أجهزة كمبيوتر", Description = "أجهزة كمبيوتر وملحقاتها", CreatedDate = seedDate },
                new Category { Id = 2, Name = "أثاث مكتبي", Description = "كراسي ومكاتب وخزائن", CreatedDate = seedDate },
                new Category { Id = 3, Name = "قرطاسية", Description = "أوراق وأقلام ومستلزمات مكتبية", CreatedDate = seedDate },
                new Category { Id = 4, Name = "أجهزة كهربائية", Description = "أجهزة كهربائية متنوعة", CreatedDate = seedDate },
                new Category { Id = 5, Name = "مواد تنظيف", Description = "مواد ومستلزمات التنظيف", CreatedDate = seedDate },
                new Category { Id = 6, Name = "أدوات ومعدات", Description = "أدوات ومعدات متنوعة", CreatedDate = seedDate }
            );

            // Seed Locations
            modelBuilder.Entity<Location>().HasData(
                new Location { Id = 1, Name = "المخزن الرئيسي", Code = "WH-01", Type = LocationType.Warehouse, CreatedDate = seedDate },
                new Location { Id = 2, Name = "مكتب الإدارة", Code = "OF-01", Type = LocationType.Office, CreatedDate = seedDate },
                new Location { Id = 3, Name = "قسم الهندسة الكهروميكانيكية", Code = "ELEC-01", Type = LocationType.Department, CreatedDate = seedDate },
                new Location { Id = 4, Name = "ورشة الصيانة", Code = "WS-01", Type = LocationType.Workshop, CreatedDate = seedDate },
                new Location { Id = 5, Name = "المختبر", Code = "LB-01", Type = LocationType.Laboratory, CreatedDate = seedDate }
            );
        }
    }
}
