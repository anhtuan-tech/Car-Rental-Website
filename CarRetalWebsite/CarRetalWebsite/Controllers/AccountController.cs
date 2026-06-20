using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarRetalWebsite.Models;

namespace CarRetalWebsite.Controllers
{
    public class AccountController : Controller
    {
        private readonly CarRentalDbContext _context;

        public AccountController(CarRentalDbContext context)
        {
            _context = context;
        }

        private string HashPasswordSha256(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hashBytes = sha256.ComputeHash(bytes);
                var builder = new StringBuilder();
                foreach (var b in hashBytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            // Nếu đã đăng nhập sẵn rồi, tự động chuyển hướng về Trang chủ
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["LoginError"] = "Vui lòng điền đầy đủ Email/Số điện thoại và Mật khẩu.";
                return View();
            }

            // Tìm kiếm tài khoản từ bảng Users dựa trên email hoặc số điện thoại
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Email == email || u.PhoneNumber == email);

            var hashedPassword = HashPasswordSha256(password);
            if (user == null || user.Password != hashedPassword)
            {
                TempData["LoginError"] = "Tài khoản hoặc mật khẩu không chính xác.";
                return View();
            }

            // Kiểm tra trạng thái tài khoản
            if (user.Status != "Active")
            {
                TempData["LoginError"] = "Tài khoản của bạn đã bị khóa hoặc đang chờ phê duyệt.";
                return View();
            }

            // Thiết lập các Claims hệ thống
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Profile?.FullName ?? "Người dùng"),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "Customer"),
                new Claim("AvatarUrl", user.Profile?.AvatarUrl ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(2)
            };

            // Tạo Cookie đăng nhập
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );

            TempData["LoginSuccess"] = true;

            if (user.Role?.RoleName == "Admin")
            {
                return RedirectToAction("Dashboard", "Admin");
            }
            else if (user.Role?.RoleName == "Owner" || user.Role?.RoleName == "Staff")
            {
                return Content("Chức năng cho Chủ xe và Nhân viên đang được phát triển.");
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            // Xóa cookie và quay về trang chủ
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["LogoutSuccess"] = true;
            return RedirectToAction("Index", "Home");
        }
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return Content("Bạn không có quyền truy cập trang này. Vui lòng đăng xuất và đăng nhập lại bằng tài khoản Customer.");
        }

        [HttpGet]
        public IActionResult Register()
        {
            // Nếu đã đăng nhập sẵn rồi, tự động chuyển hướng về Trang chủ
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            string fullName,
            string email,
            string password,
            string confirmPassword,
            string phoneNumber,
            string driverLicenseNo)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || 
                string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword) || 
                string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(driverLicenseNo))
            {
                TempData["RegisterError"] = "Vui lòng điền đầy đủ các thông tin bắt buộc.";
                return View();
            }

            if (password != confirmPassword)
            {
                TempData["RegisterError"] = "Mật khẩu xác nhận không trùng khớp.";
                return View();
            }

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email || u.PhoneNumber == phoneNumber);
            if (existingUser != null)
            {
                TempData["RegisterError"] = "Email hoặc Số điện thoại đã được đăng ký.";
                return View();
            }

            var newUser = new User
            {
                RoleId = 3, // Customer
                Email = email,
                Password = HashPasswordSha256(password),
                PhoneNumber = phoneNumber,
                Status = "Active",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var newProfile = new Profile
            {
                FullName = fullName,
                AvatarUrl = null,
                DriverLicenseNo = driverLicenseNo
            };

            newUser.Profile = newProfile;

            try
            {
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                TempData["RegisterSuccess"] = true;
                return RedirectToAction("Login");
            }
            catch (Exception)
            {
                TempData["RegisterError"] = "Đã xảy ra lỗi trong quá trình tạo tài khoản. Vui lòng thử lại.";
                return View();
            }
        }

        [HttpGet]
        public IActionResult RegisterOwner()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterOwner(
            string fullName,
            string email,
            string password,
            string confirmPassword,
            string phoneNumber,
            string idCardNo,
            string address)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || 
                string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword) || 
                string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(idCardNo) || 
                string.IsNullOrWhiteSpace(address))
            {
                TempData["RegisterOwnerError"] = "Vui lòng điền đầy đủ các thông tin bắt buộc.";
                return View();
            }

            if (password != confirmPassword)
            {
                TempData["RegisterOwnerError"] = "Mật khẩu xác nhận không trùng khớp.";
                return View();
            }

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email || u.PhoneNumber == phoneNumber);
            if (existingUser != null)
            {
                TempData["RegisterOwnerError"] = "Email hoặc Số điện thoại đã được đăng ký.";
                return View();
            }

            var newUser = new User
            {
                RoleId = 4, // Owner
                Email = email,
                Password = HashPasswordSha256(password),
                PhoneNumber = phoneNumber,
                Status = "Active",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var newProfile = new Profile
            {
                FullName = fullName,
                AvatarUrl = null,
                IdCardNo = idCardNo,
                Address = address
            };

            newUser.Profile = newProfile;

            try
            {
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                TempData["RegisterSuccess"] = true;
                return RedirectToAction("Login");
            }
            catch (Exception)
            {
                TempData["RegisterOwnerError"] = "Đã xảy ra lỗi trong quá trình tạo tài khoản. Vui lòng thử lại.";
                return View();
            }
        }
    }
}
