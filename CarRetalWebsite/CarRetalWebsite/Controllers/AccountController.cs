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
using Microsoft.Extensions.Caching.Memory;
using CarRetalWebsite.Services;

namespace CarRetalWebsite.Controllers
{
    public class AccountController : Controller
    {
        private readonly CarRentalDbContext _context;
        private readonly IMemoryCache _memoryCache;
        private readonly EmailService _emailService;

        public AccountController(CarRentalDbContext context, IMemoryCache memoryCache, EmailService emailService)
        {
            _context = context;
            _memoryCache = memoryCache;
            _emailService = emailService;
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

            if (email.Contains("@") && !System.Text.RegularExpressions.Regex.IsMatch(email, @"^[\w\.-]+@[\w\.-]+\.\w+$"))
            {
                TempData["LoginError"] = "Địa chỉ Email không đúng định dạng.";
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
            if (user.Status == "Deleted" || user.Status == "Blocked" || user.Status == "Banned")
            {
                TempData["LoginError"] = "Tài khoản của bạn đã bị khóa hoặc xóa khỏi hệ thống.";
                return View();
            }
            else if (user.Status != "Active")
            {
                TempData["LoginError"] = "Tài khoản của bạn đang chờ phê duyệt hoặc ngừng hoạt động.";
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

            if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[\w\.-]+@[\w\.-]+\.\w+$"))
            {
                TempData["RegisterError"] = "Địa chỉ Email không đúng định dạng.";
                return View();
            }

            if (password.Length < 6)
            {
                TempData["RegisterError"] = "Mật khẩu phải chứa ít nhất 6 ký tự.";
                return View();
            }

            if (password != confirmPassword)
            {
                TempData["RegisterError"] = "Mật khẩu xác nhận không trùng khớp.";
                return View();
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, @"^\d{10,}$"))
            {
                TempData["RegisterError"] = "Số điện thoại phải chỉ gồm các chữ số và có độ dài từ 10 ký tự trở lên.";
                return View();
            }

            var duplicateEmail = await _context.Users.AnyAsync(u => u.Email == email);
            if (duplicateEmail)
            {
                TempData["RegisterError"] = "Địa chỉ Email đã được sử dụng.";
                return View();
            }

            var duplicatePhone = await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber);
            if (duplicatePhone)
            {
                TempData["RegisterError"] = "Số điện thoại đã được đăng ký.";
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

            if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[\w\.-]+@[\w\.-]+\.\w+$"))
            {
                TempData["RegisterOwnerError"] = "Địa chỉ Email không đúng định dạng.";
                return View();
            }

            if (password.Length < 6)
            {
                TempData["RegisterOwnerError"] = "Mật khẩu phải chứa ít nhất 6 ký tự.";
                return View();
            }

            if (password != confirmPassword)
            {
                TempData["RegisterOwnerError"] = "Mật khẩu xác nhận không trùng khớp.";
                return View();
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, @"^\d{10,}$"))
            {
                TempData["RegisterOwnerError"] = "Số điện thoại phải chỉ gồm các chữ số và có độ dài từ 10 ký tự trở lên.";
                return View();
            }

            var duplicateEmail = await _context.Users.AnyAsync(u => u.Email == email);
            if (duplicateEmail)
            {
                TempData["RegisterOwnerError"] = "Địa chỉ Email đã được sử dụng.";
                return View();
            }

            var duplicatePhone = await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber);
            if (duplicatePhone)
            {
                TempData["RegisterOwnerError"] = "Số điện thoại đã được đăng ký.";
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

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["ForgotPasswordError"] = "Vui lòng điền địa chỉ Email.";
                return View();
            }

            var user = await _context.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Email == email && u.Status != "Deleted");

            if (user == null)
            {
                TempData["ForgotPasswordError"] = "Địa chỉ Email không tồn tại trong hệ thống.";
                return View();
            }

            // Sinh mã OTP ngẫu nhiên gồm 6 chữ số
            var random = new Random();
            var otp = random.Next(100000, 999999).ToString();

            // Lưu vào IMemoryCache dưới dạng key reset_otp_{email} với thời hạn 5 phút
            var cacheKey = $"reset_otp_{email}";
            _memoryCache.Set(cacheKey, otp, TimeSpan.FromMinutes(5));

            // Gửi qua EmailService
            var userName = user.Profile?.FullName ?? user.Email;
            var isSent = await _emailService.SendResetPasswordOtpAsync(email, userName, otp);
            if (!isSent)
            {
                TempData["ForgotPasswordError"] = "Không thể gửi email OTP. Vui lòng cấu hình SMTP chính xác trong appsettings.json hoặc kiểm tra kết nối mạng.";
                return View();
            }

            // Chuyển hướng sang trang ResetPassword kèm email trong query string
            return RedirectToAction("ResetPassword", new { email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyOtp(string email, string code)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
            {
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin." });
            }

            var cacheKey = $"reset_otp_{email}";
            if (!_memoryCache.TryGetValue(cacheKey, out string? cachedOtp))
            {
                return Json(new { success = false, message = "Mã OTP đã hết hạn hoặc không hợp lệ. Vui lòng yêu cầu lại." });
            }

            if (cachedOtp != code)
            {
                return Json(new { success = false, message = "Mã OTP không chính xác." });
            }

            return Json(new { success = true });
        }

        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string email, string code, string password, string confirmPassword)
        {
            ViewBag.Email = email;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
            {
                TempData["ResetPasswordError"] = "Vui lòng điền đầy đủ thông tin Email và mã OTP.";
                return View();
            }

            // 1. Xác thực mã OTP trong IMemoryCache trước tiên
            var cacheKey = $"reset_otp_{email}";
            if (!_memoryCache.TryGetValue(cacheKey, out string? cachedOtp))
            {
                TempData["ResetPasswordError"] = "Mã OTP đã hết hạn hoặc không hợp lệ. Vui lòng yêu cầu lại.";
                return View();
            }

            if (cachedOtp != code)
            {
                TempData["ResetPasswordError"] = "Mã OTP không chính xác.";
                return View();
            }

            // Đánh dấu OTP đã xác minh thành công để giữ hiển thị ô nhập mật khẩu nếu gặp lỗi phía dưới
            ViewBag.OtpVerified = true;
            ViewBag.Code = code;

            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                TempData["ResetPasswordError"] = "Vui lòng nhập mật khẩu mới và xác nhận mật khẩu mới.";
                return View();
            }

            if (password.Length < 6)
            {
                TempData["ResetPasswordError"] = "Mật khẩu mới phải chứa ít nhất 6 ký tự.";
                return View();
            }

            if (password != confirmPassword)
            {
                TempData["ResetPasswordError"] = "Mật khẩu xác nhận không trùng khớp.";
                return View();
            }

            // Tìm user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.Status != "Deleted");
            if (user == null)
            {
                TempData["ResetPasswordError"] = "Tài khoản không tồn tại hoặc đã bị xóa.";
                return View();
            }

            // Kiểm tra mật khẩu cũ trùng lặp
            var newHashed = HashPasswordSha256(password);
            if (user.Password == newHashed)
            {
                TempData["ResetPasswordError"] = "Mật khẩu mới không được trùng với mật khẩu cũ.";
                return View();
            }

            // Mã hóa mật khẩu mới
            user.Password = newHashed;
            user.UpdatedAt = DateTime.Now;

            try
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                // Xóa OTP khỏi cache
                _memoryCache.Remove(cacheKey);

                TempData["ResetPasswordSuccess"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập với mật khẩu mới.";
                return RedirectToAction("Login");
            }
            catch (Exception)
            {
                TempData["ResetPasswordError"] = "Đã xảy ra lỗi trong quá trình cập nhật mật khẩu. Vui lòng thử lại.";
                return View();
            }
        }
    }
}
