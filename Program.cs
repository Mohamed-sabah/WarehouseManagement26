using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Data;
using WarehouseManagement.Models;
using WarehouseManagement.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Add Entity Framework
builder.Services.AddDbContext<WarehouseContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add custom services
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IDataMigrationService, DataMigrationService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
// Add Memory Cache
builder.Services.AddMemoryCache();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Route configuration
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<WarehouseContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var created = context.Database.EnsureCreated();
        
        if (created)
        {
            logger.LogInformation("Database created successfully");
            await SeedInitialDataAsync(context, logger);
        }
        else
        {
            logger.LogInformation("Database already exists");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database");
    }
}

app.Run();

// دالة إضافة البيانات الأولية
static async Task SeedInitialDataAsync(WarehouseContext context, ILogger logger)
{
    if (await context.Materials.AnyAsync())
    {
        logger.LogInformation("Database already contains data, skipping seed");
        return;
    }

    logger.LogInformation("Seeding initial data...");

    // إضافة مواد تجريبية
    var materials = new List<Material>
    {
        new Material
        {
            Name = "حاسوب لاب توب",
            Code = "1/1/1",
            Description = "جهاز حاسوب محمول",
            Unit = "قطعة",
            CategoryId = 1,
            MinimumStock = 5,
            Ownership = "قسم الهندسة الكهروميكانيكية",
            CreatedDate = DateTime.Now,
            LastUpdated = DateTime.Now
        },
        new Material
        {
            Name = "حاسوب مكتبي",
            Code = "1/1/2",
            Description = "حاسوب مكتبي كامل",
            Unit = "قطعة",
            CategoryId = 1,
            MinimumStock = 3,
            Ownership = "قسم الهندسة الكهروميكانيكية",
            CreatedDate = DateTime.Now,
            LastUpdated = DateTime.Now
        },
        new Material
        {
            Name = "منضدة مكتب خشب",
            Code = "6/1/1",
            Description = "منضدة مكتب خشبية مع أدراج",
            Unit = "قطعة",
            CategoryId = 2,
            MinimumStock = 2,
            Ownership = "قسم الهندسة الكهروميكانيكية",
            CreatedDate = DateTime.Now,
            LastUpdated = DateTime.Now
        }
    };

    context.Materials.AddRange(materials);
    await context.SaveChangesAsync();

    // إضافة مخزون أولي
    foreach (var material in materials)
    {
        var stock = new MaterialStock
        {
            MaterialId = material.Id,
            LocationId = 3,
            Quantity = 10,
            Condition = "جيدة",
            CreatedDate = DateTime.Now,
            LastUpdated = DateTime.Now
        };
        context.MaterialStocks.Add(stock);

        var purchase = new Purchase
        {
            MaterialId = material.Id,
            LocationId = 3,
            Quantity = 10,
            UnitPrice = 500000,
            StoredTotalPrice = 5000000,
            PurchaseDate = DateTime.Now.AddMonths(-6),
            Method = AcquisitionMethod.OpeningBalance,
            IsAddedToStock = true,
            AddedToStockDate = DateTime.Now,
            Notes = "رصيد افتتاحي",
            CreatedDate = DateTime.Now
        };
        context.Purchases.Add(purchase);
    }

    await context.SaveChangesAsync();
    logger.LogInformation("Initial data seeded successfully");
}
