using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarRetalWebsite.Models;

namespace CarRetalWebsite.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly CarRentalDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProfileController(CarRentalDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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

        private async Task<User?> GetCurrentUserAsync()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
            {
                return null;
            }

            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Profile)
                .Include(u => u.Bookings)
                .Include(u => u.Cars)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var profile = user.Profile ?? new Profile();

            // Stats for Customer
            var totalBookings = user.Bookings.Count;
            var totalSpent = user.Bookings
                .Where(b => b.Status != "Cancelled" && b.Status != "Rejected")
                .Sum(b => (decimal?)b.SubtotalFee) ?? 0m;
            var activeBookings = user.Bookings
                .Count(b => b.Status == "Pending" || b.Status == "Approved" || b.Status == "Active");

            // Stats for Owner
            var totalCars = user.Cars.Count;
            var ownerBookingsQuery = _context.Bookings.Where(b => b.Car.OwnerId == user.UserId);
            var totalOwnerBookings = await ownerBookingsQuery.CountAsync();
            var totalOwnerRevenue = await ownerBookingsQuery
                .Where(b => b.Status == "Completed")
                .SumAsync(b => (decimal?)b.OwnerPayout) ?? 0m;

            var viewModel = new UserProfileViewModel
            {
                UserId = user.UserId,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                RoleName = user.Role?.RoleName ?? "Khách hàng",
                Status = user.Status,
                CreatedAt = user.CreatedAt,
                FullName = profile.FullName ?? "",
                AvatarUrl = profile.AvatarUrl,
                DriverLicenseNo = profile.DriverLicenseNo,
                IdCardNo = profile.IdCardNo,
                Address = profile.Address,
                TotalBookings = totalBookings,
                TotalSpent = totalSpent,
                ActiveBookings = activeBookings,
                TotalCars = totalCars,
                TotalOwnerBookings = totalOwnerBookings,
                TotalOwnerRevenue = totalOwnerRevenue
            };

            ViewBag.EditModel = new EditProfileViewModel
            {
                FullName = profile.FullName ?? "",
                PhoneNumber = user.PhoneNumber,
                DriverLicenseNo = profile.DriverLicenseNo,
                IdCardNo = profile.IdCardNo,
                Address = profile.Address,
                CurrentAvatarUrl = profile.AvatarUrl
            };

            ViewBag.ChangePasswordModel = new ChangePasswordViewModel();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault();
                TempData["EditProfileError"] = firstError ?? "Vui lòng kiểm tra lại các thông tin nhập.";
                return RedirectToAction("Index");
            }

            // Check duplicate phone number
            var duplicatePhone = await _context.Users
                .AnyAsync(u => u.PhoneNumber == model.PhoneNumber && u.UserId != user.UserId);
            if (duplicatePhone)
            {
                TempData["EditProfileError"] = "Số điện thoại này đã được sử dụng bởi tài khoản khác.";
                return RedirectToAction("Index");
            }

            // Check duplicate DriverLicenseNo if provided
            if (!string.IsNullOrWhiteSpace(model.DriverLicenseNo))
            {
                var duplicateLicense = await _context.Profiles
                    .AnyAsync(p => p.DriverLicenseNo == model.DriverLicenseNo && p.UserId != user.UserId);
                if (duplicateLicense)
                {
                    TempData["EditProfileError"] = "Số giấy phép lái xe đã tồn tại trên hệ thống.";
                    return RedirectToAction("Index");
                }
            }

            // Check duplicate IdCardNo if provided
            if (!string.IsNullOrWhiteSpace(model.IdCardNo))
            {
                var duplicateIdCard = await _context.Profiles
                    .AnyAsync(p => p.IdCardNo == model.IdCardNo && p.UserId != user.UserId);
                if (duplicateIdCard)
                {
                    TempData["EditProfileError"] = "Số CCCD/CMND đã tồn tại trên hệ thống.";
                    return RedirectToAction("Index");
                }
            }

            // Ensure Profile entity exists
            if (user.Profile == null)
            {
                user.Profile = new Profile { UserId = user.UserId };
                _context.Profiles.Add(user.Profile);
            }

            // Handle Avatar Upload
            if (model.AvatarFile != null && model.AvatarFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var extension = Path.GetExtension(model.AvatarFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    TempData["EditProfileError"] = "Ảnh đại diện phải thuộc định dạng: JPG, JPEG, PNG, WEBP.";
                    return RedirectToAction("Index");
                }

                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"avatar_{user.UserId}_{DateTime.Now.Ticks}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.AvatarFile.CopyToAsync(fileStream);
                }

                user.Profile.AvatarUrl = $"/uploads/avatars/{uniqueFileName}";
            }

            // Update user & profile attributes
            user.PhoneNumber = model.PhoneNumber;
            user.UpdatedAt = DateTime.Now;
            user.Profile.FullName = model.FullName;
            user.Profile.DriverLicenseNo = string.IsNullOrWhiteSpace(model.DriverLicenseNo) ? null : model.DriverLicenseNo.Trim();
            user.Profile.IdCardNo = string.IsNullOrWhiteSpace(model.IdCardNo) ? null : model.IdCardNo.Trim();
            user.Profile.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();

            try
            {
                await _context.SaveChangesAsync();

                // Re-issue login cookie to update Name and AvatarUrl claims in real-time
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Profile.FullName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "Customer"),
                    new Claim("AvatarUrl", user.Profile.AvatarUrl ?? "")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity)
                );

                TempData["EditProfileSuccess"] = "Cập nhật hồ sơ cá nhân thành công!";
            }
            catch (Exception)
            {
                TempData["EditProfileError"] = "Đã xảy ra lỗi khi lưu thông tin. Vui lòng thử lại.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                TempData["ChangePasswordError"] = firstError ?? "Vui lòng điền đầy đủ các trường thông tin đổi mật khẩu.";
                return RedirectToAction("Index");
            }

            var currentHashed = HashPasswordSha256(model.CurrentPassword);
            if (user.Password != currentHashed)
            {
                TempData["ChangePasswordError"] = "Mật khẩu hiện tại không chính xác.";
                return RedirectToAction("Index");
            }

            var newHashed = HashPasswordSha256(model.NewPassword);
            if (user.Password == newHashed)
            {
                TempData["ChangePasswordError"] = "Mật khẩu mới không được trùng với mật khẩu hiện tại.";
                return RedirectToAction("Index");
            }

            user.Password = newHashed;
            user.UpdatedAt = DateTime.Now;

            try
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                TempData["ChangePasswordSuccess"] = "Đổi mật khẩu thành công!";
            }
            catch (Exception)
            {
                TempData["ChangePasswordError"] = "Đã xảy ra lỗi trong quá trình cập nhật mật khẩu. Vui lòng thử lại.";
            }

            return RedirectToAction("Index");
        }
    }
}
