using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Data;
using WarehouseManagement.Models;
using WarehouseManagement.Services;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Add Entity Framework
builder.Services.AddDbContext<WarehouseContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== إضافة المصادقة =====
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.Name = "WarehouseAuth";
    });

builder.Services.AddAuthorization();

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

// ===== ترتيب المصادقة مهم =====
app.UseAuthentication();
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

        // إنشاء مستخدم admin افتراضي إذا لم يوجد
        await EnsureAdminUserAsync(context, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database");
    }
}

app.Run();

// ===== إنشاء مستخدم admin افتراضي =====
static async Task EnsureAdminUserAsync(WarehouseContext context, ILogger logger)
{
    if (await context.AppUsers.AnyAsync(u => u.Role == UserRole.Admin))
        return;

    logger.LogInformation("Creating default admin user...");

    var admin = new AppUser
    {
        Username = "admin",
        FullName = "مدير النظام",
        PasswordHash = HashPassword("admin123"),
        Role = UserRole.Admin,
        Department = "الإدارة",
        CreatedDate = DateTime.Now
    };

    context.AppUsers.Add(admin);
    await context.SaveChangesAsync();
    logger.LogInformation("Default admin user created (username: admin, password: admin123)");
}

static string HashPassword(string password)
{
    using var sha256 = SHA256.Create();
    var salt = Guid.NewGuid().ToString("N")[..16];
    var hash = Convert.ToBase64String(
        sha256.ComputeHash(Encoding.UTF8.GetBytes(salt + password)));
    return $"{salt}:{hash}";
}

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


//using Microsoft.EntityFrameworkCore;
//using WarehouseManagement.Data;
//using WarehouseManagement.Models;
//using WarehouseManagement.Services;

//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container
//builder.Services.AddControllersWithViews();

//// Add Entity Framework
//builder.Services.AddDbContext<WarehouseContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//// Add custom services
//builder.Services.AddScoped<IStockService, StockService>();
//builder.Services.AddScoped<IReportService, ReportService>();
//builder.Services.AddScoped<IDataMigrationService, DataMigrationService>();
//builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
//// Add Memory Cache
//builder.Services.AddMemoryCache();

//// Add logging
//builder.Services.AddLogging(logging =>
//{
//    logging.AddConsole();
//    logging.AddDebug();
//});

//var app = builder.Build();

//// Configure the HTTP request pipeline
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Home/Error");
//    app.UseHsts();
//}
//else
//{
//    app.UseDeveloperExceptionPage();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();
//app.UseRouting();
//app.UseAuthorization();

//// Route configuration
//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

//// Ensure database is created and seeded
//using (var scope = app.Services.CreateScope())
//{
//    var context = scope.ServiceProvider.GetRequiredService<WarehouseContext>();
//    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

//    try
//    {
//        var created = context.Database.EnsureCreated();

//        if (created)
//        {
//            logger.LogInformation("Database created successfully");
//            await SeedInitialDataAsync(context, logger);
//        }
//        else
//        {
//            logger.LogInformation("Database already exists");
//        }
//    }
//    catch (Exception ex)
//    {
//        logger.LogError(ex, "An error occurred while initializing the database");
//    }
//}

//app.Run();

//// دالة إضافة البيانات الأولية
//static async Task SeedInitialDataAsync(WarehouseContext context, ILogger logger)
//{
//    if (await context.Materials.AnyAsync())
//    {
//        logger.LogInformation("Database already contains data, skipping seed");
//        return;
//    }

//    logger.LogInformation("Seeding initial data...");

//    // إضافة مواد تجريبية
//    var materials = new List<Material>
//    {
//        new Material
//        {
//            Name = "حاسوب لاب توب",
//            Code = "1/1/1",
//            Description = "جهاز حاسوب محمول",
//            Unit = "قطعة",
//            CategoryId = 1,
//            MinimumStock = 5,
//            Ownership = "قسم الهندسة الكهروميكانيكية",
//            CreatedDate = DateTime.Now,
//            LastUpdated = DateTime.Now
//        },
//        new Material
//        {
//            Name = "حاسوب مكتبي",
//            Code = "1/1/2",
//            Description = "حاسوب مكتبي كامل",
//            Unit = "قطعة",
//            CategoryId = 1,
//            MinimumStock = 3,
//            Ownership = "قسم الهندسة الكهروميكانيكية",
//            CreatedDate = DateTime.Now,
//            LastUpdated = DateTime.Now
//        },
//        new Material
//        {
//            Name = "منضدة مكتب خشب",
//            Code = "6/1/1",
//            Description = "منضدة مكتب خشبية مع أدراج",
//            Unit = "قطعة",
//            CategoryId = 2,
//            MinimumStock = 2,
//            Ownership = "قسم الهندسة الكهروميكانيكية",
//            CreatedDate = DateTime.Now,
//            LastUpdated = DateTime.Now
//        }
//    };

//    context.Materials.AddRange(materials);
//    await context.SaveChangesAsync();

//    // إضافة مخزون أولي
//    foreach (var material in materials)
//    {
//        var stock = new MaterialStock
//        {
//            MaterialId = material.Id,
//            LocationId = 3,
//            Quantity = 10,
//            Condition = "جيدة",
//            CreatedDate = DateTime.Now,
//            LastUpdated = DateTime.Now
//        };
//        context.MaterialStocks.Add(stock);

//        var purchase = new Purchase
//        {
//            MaterialId = material.Id,
//            LocationId = 3,
//            Quantity = 10,
//            UnitPrice = 500000,
//            StoredTotalPrice = 5000000,
//            PurchaseDate = DateTime.Now.AddMonths(-6),
//            Method = AcquisitionMethod.OpeningBalance,
//            IsAddedToStock = true,
//            AddedToStockDate = DateTime.Now,
//            Notes = "رصيد افتتاحي",
//            CreatedDate = DateTime.Now
//        };
//        context.Purchases.Add(purchase);
//    }

//    await context.SaveChangesAsync();
//    logger.LogInformation("Initial data seeded successfully");
//}
