// Plan (pseudocode):
// 1. Add a simple POCO `Department` model in the `WarehouseManagement.Models` namespace so the compiler
//    recognizes the `Department` type used in `ReportsController`.
// 2. Provide minimal properties commonly expected by controllers/views:
//    - Id (int)
//    - Name (string)
//    - Code (string?) optional
// 3. Keep the class simple so it can be picked up by EF Core via `DbContext.Set<Department>()`
//    even if the `WarehouseContext` does not expose an explicit `DbSet<Department>` property.
// 4. After adding this file, build. If runtime EF model registration is required, add a `DbSet<Department>`
//    to `WarehouseContext` or register the entity in `OnModelCreating` — only needed if EF runtime errors occur.

namespace WarehouseManagement.Models
{
    public class Department
    {
        public int Id { get; set; }

        // Display name of the department
        public string Name { get; set; } = string.Empty;

        // Optional code or short identifier
        public string? Code { get; set; }
    }
}