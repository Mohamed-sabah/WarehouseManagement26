using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WarehouseManagement.Data;
using WarehouseManagement.Models;
using WarehouseManagement.Models.ViewModels;

namespace WarehouseManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly WarehouseContext _context;

        public AccountController(WarehouseContext context)
        {
            _context = context;
        }

        // GET: Account/Login
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Username == model.Username && u.IsActive);

            if (user == null || !VerifyPassword(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "اسم المستخدم أو كلمة المرور غير صحيحة");
                return View(model);
            }

            // تحديث آخر دخول
            user.LastLoginDate = DateTime.Now;
            await _context.SaveChangesAsync();

            // إنشاء Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("FullName", user.FullName),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("Department", user.Department ?? "")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe
                        ? DateTimeOffset.UtcNow.AddDays(30)
                        : DateTimeOffset.UtcNow.AddHours(8)
                });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // GET: Account/Register (Admin only)
        [Authorize(Roles = "Admin")]
        public IActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (await _context.AppUsers.AnyAsync(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "اسم المستخدم موجود مسبقاً");
                return View(model);
            }

            var user = new AppUser
            {
                Username = model.Username,
                FullName = model.FullName,
                Email = model.Email,
                PasswordHash = HashPassword(model.Password),
                Role = model.Role,
                Department = model.Department,
                CreatedDate = DateTime.Now
            };

            _context.AppUsers.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"تم إنشاء حساب المستخدم '{model.FullName}' بنجاح";
            return RedirectToAction(nameof(Users));
        }

        // GET: Account/Users (Admin only)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Users()
        {
            var users = await _context.AppUsers
                .OrderByDescending(u => u.CreatedDate)
                .ToListAsync();
            return View(users);
        }

        // GET: Account/ChangePassword
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = await _context.AppUsers.FindAsync(userId);

            if (user == null || !VerifyPassword(model.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError("CurrentPassword", "كلمة المرور الحالية غير صحيحة");
                return View(model);
            }

            user.PasswordHash = HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تغيير كلمة المرور بنجاح";
            return RedirectToAction("Index", "Dashboard");
        }

        // POST: Account/ToggleUser/5 (Admin only)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUser(int id)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = user.IsActive
                ? $"تم تفعيل حساب '{user.FullName}'"
                : $"تم تعطيل حساب '{user.FullName}'";

            return RedirectToAction(nameof(Users));
        }

        // GET: Account/AccessDenied
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        #region Password Helpers

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var salt = Guid.NewGuid().ToString("N")[..16];
            var hash = Convert.ToBase64String(
                sha256.ComputeHash(Encoding.UTF8.GetBytes(salt + password)));
            return $"{salt}:{hash}";
        }

        private static bool VerifyPassword(string password, string storedHash)
        {
            var parts = storedHash.Split(':');
            if (parts.Length != 2) return false;

            using var sha256 = SHA256.Create();
            var hash = Convert.ToBase64String(
                sha256.ComputeHash(Encoding.UTF8.GetBytes(parts[0] + password)));
            return hash == parts[1];
        }

        #endregion
    }
}
