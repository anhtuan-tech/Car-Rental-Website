using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarRetalWebsite.Models;

namespace CarRetalWebsite.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly CarRentalDbContext _context;

        public AdminController(CarRentalDbContext context)
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

        // ====================================================================
        // UC-20: DASHBOARD OVERVIEW
        // ====================================================================
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalUsers = await _context.Users.CountAsync(u => u.RoleId == 3 || u.RoleId == 4);
            ViewBag.TotalStaff = await _context.Users.CountAsync(u => u.RoleId == 2);
            ViewBag.TotalCars = await _context.Cars.CountAsync();
            ViewBag.TotalBookings = await _context.Bookings.CountAsync();
            ViewBag.TotalRevenue = await _context.Bookings.SumAsync(b => b.SubtotalFee);

            // Lấy 5 tài khoản đăng ký mới nhất
            var recentUsers = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Profile)
                .OrderByDescending(u => u.CreatedAt)
                .Take(50)
                .ToListAsync();

            return View(recentUsers);
        }

        // ====================================================================
        // UC-20.1 & UC-20.2: VIEW & SEARCH STAFF ACCOUNTS
        // ====================================================================
        [HttpGet]
        public async Task<IActionResult> Staffs(string search)
        {
            var query = _context.Users
                .Include(u => u.Profile)
                .Where(u => u.RoleId == 2 && u.Status != "Deleted"); // Staff

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                if (search.Length > 100) search = search.Substring(0, 100);
                var searchLower = search.ToLower();
                query = query.Where(u => 
                    u.Email.ToLower().Contains(searchLower) || 
                    u.PhoneNumber.ToLower().Contains(searchLower) || 
                    (u.Profile != null && u.Profile.FullName.ToLower().Contains(searchLower))
                );
            }

            var staffs = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
            ViewBag.SearchTerm = search;
            return View(staffs);
        }

        // ====================================================================
        // UC-20.3: CREATE STAFF ACCOUNT
        // ====================================================================
        private async Task<string?> SaveAvatarFileAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return "/uploads/avatars/" + uniqueFileName;
        }

        private void DeleteOldAvatarFile(string? avatarUrl)
        {
            if (string.IsNullOrEmpty(avatarUrl) || !avatarUrl.StartsWith("/uploads/avatars/")) return;

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", avatarUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    System.IO.File.Delete(filePath);
                }
                catch
                {
                    // Ignore exception on delete
                }
            }
        }

        // ====================================================================
        // UC-20.3: CREATE STAFF ACCOUNT
        // ====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStaff(string fullName, string email, string phoneNumber, string password, string confirmPassword, IFormFile? avatarFile)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || 
                string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(password) || 
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ tất cả các trường." });
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[\w\.-]+@[\w\.-]+\.\w+$"))
            {
                return Json(new { success = false, message = "Địa chỉ Email không đúng định dạng." });
            }

            if (password.Length < 6)
            {
                return Json(new { success = false, message = "Mật khẩu phải chứa ít nhất 6 ký tự." });
            }

            if (password != confirmPassword)
            {
                return Json(new { success = false, message = "Mật khẩu xác nhận không khớp." });
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, @"^(03|05|07|08|09)\d{8}$"))
            {
                return Json(new { success = false, message = "Số điện thoại phải gồm đúng 10 chữ số hợp lệ của nhà mạng Việt Nam (bắt đầu bằng 03, 05, 07, 08, 09)." });
            }

            var duplicateEmail = await _context.Users.AnyAsync(u => u.Email == email);
            if (duplicateEmail)
            {
                return Json(new { success = false, message = "Địa chỉ Email đã được sử dụng." });
            }

            var duplicatePhone = await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber);
            if (duplicatePhone)
            {
                return Json(new { success = false, message = "Số điện thoại đã được đăng ký." });
            }

            var newUser = new User
            {
                RoleId = 2, // Staff
                Email = email,
                Password = HashPasswordSha256(password),
                PhoneNumber = phoneNumber,
                Status = "Active",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            string? avatarUrl = null;
            if (avatarFile != null)
            {
                avatarUrl = await SaveAvatarFileAsync(avatarFile);
            }


            var newProfile = new Profile
            {
                FullName = fullName,
                AvatarUrl = avatarUrl
            };

            newUser.Profile = newProfile;

            try
            {
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Tạo tài khoản Nhân viên thành công!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi khi lưu dữ liệu. Vui lòng thử lại." });
            }
        }

        // ====================================================================
        // UC-20.4: VIEW STAFF DETAILS (API JSON)
        // ====================================================================
        [HttpGet]
        public async Task<IActionResult> GetStaffDetails(int id)
        {
            if (id <= 0)
            {
                return BadRequest("ID không hợp lệ.");
            }
            var user = await _context.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.UserId == id && u.RoleId == 2);

            if (user == null)
            {
                return NotFound();
            }

            return Json(new
            {
                userId = user.UserId,
                fullName = user.Profile?.FullName ?? "N/A",
                email = user.Email,
                phoneNumber = user.PhoneNumber,
                status = user.Status,
                createdAt = user.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                avatarUrl = user.Profile?.AvatarUrl ?? ""
            });
        }

        // ====================================================================
        // UC-20.5: UPDATE STAFF ACCOUNT
        // ====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStaff(int userId, string fullName, string phoneNumber, string status, IFormFile? avatarFile)
        {
            if (userId <= 0)
            {
                return Json(new { success = false, message = "ID nhân viên không hợp lệ." });
            }

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(status))
            {
                return Json(new { success = false, message = "Vui lòng điền đầy đủ các thông tin." });
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, @"^(03|05|07|08|09)\d{8}$"))
            {
                return Json(new { success = false, message = "Số điện thoại phải gồm đúng 10 chữ số hợp lệ của nhà mạng Việt Nam (bắt đầu bằng 03, 05, 07, 08, 09)." });
            }

            if (status != "Active" && status != "Inactive" && status != "Banned" && status != "Deleted")
            {
                return Json(new { success = false, message = "Trạng thái tài khoản không hợp lệ." });
            }

            var user = await _context.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.UserId == userId && u.RoleId == 2);

            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tài khoản nhân viên." });
            }

            if (user.PhoneNumber != phoneNumber)
            {
                var duplicatePhone = await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber && u.UserId != userId);
                if (duplicatePhone)
                {
                    return Json(new { success = false, message = "Số điện thoại đã thuộc về một tài khoản khác." });
                }
            }

            user.PhoneNumber = phoneNumber;
            user.Status = status;
            user.UpdatedAt = DateTime.Now;

            if (user.Profile != null)
            {
                user.Profile.FullName = fullName;
                if (avatarFile != null)
                {
                    var newAvatarUrl = await SaveAvatarFileAsync(avatarFile);
                    if (!string.IsNullOrEmpty(newAvatarUrl))
                    {
                        DeleteOldAvatarFile(user.Profile.AvatarUrl);
                        user.Profile.AvatarUrl = newAvatarUrl;
                    }
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cập nhật tài khoản nhân viên thành công!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi khi cập nhật. Vui lòng thử lại." });
            }
        }

        // ====================================================================
        // UC-20.6: DELETE STAFF ACCOUNT
        // ====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStaff(int id)
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = "ID nhân viên không hợp lệ." });
            }

            var adminEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (!string.IsNullOrEmpty(adminEmail))
            {
                var admin = await _context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
                if (admin != null && admin.UserId == id)
                {
                    return Json(new { success = false, message = "Bạn không thể tự xóa tài khoản của chính mình." });
                }
            }

            var user = await _context.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.UserId == id && u.RoleId == 2);

            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tài khoản nhân viên cần xóa." });
            }

            try
            {
                user.Status = "Deleted";
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã xóa tài khoản nhân viên thành công!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi khi xóa nhân viên này. Vui lòng thử lại." });
            }
        }

        // ====================================================================
        // UC-21.1 & UC-21.2: VIEW & SEARCH USERS (CUSTOMERS & OWNERS)
        // ====================================================================
        [HttpGet]
        public async Task<IActionResult> Users(string search, string role)
        {
            var query = _context.Users
                .Include(u => u.Role)
                .Include(u => u.Profile)
                .Where(u => (u.RoleId == 3 || u.RoleId == 4) && u.Status != "Deleted"); // Customer or Owner

            if (!string.IsNullOrWhiteSpace(role))
            {
                if (role == "Customer") query = query.Where(u => u.RoleId == 3);
                else if (role == "Owner") query = query.Where(u => u.RoleId == 4);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                if (search.Length > 100) search = search.Substring(0, 100);
                var searchLower = search.ToLower();
                query = query.Where(u => 
                    u.Email.ToLower().Contains(searchLower) || 
                    u.PhoneNumber.ToLower().Contains(searchLower) || 
                    (u.Profile != null && u.Profile.FullName.ToLower().Contains(searchLower))
                );
            }

            var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
            ViewBag.SearchTerm = search;
            ViewBag.SelectedRole = role;
            return View(users);
        }

        // ====================================================================
        // UC-21.3: VIEW USER DETAILS (API JSON)
        // ====================================================================
        [HttpGet]
        public async Task<IActionResult> GetUserDetails(int id)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.UserId == id && (u.RoleId == 3 || u.RoleId == 4));

            if (user == null)
            {
                return NotFound();
            }

            return Json(new
            {
                userId = user.UserId,
                fullName = user.Profile?.FullName ?? "N/A",
                email = user.Email,
                phoneNumber = user.PhoneNumber,
                roleName = user.Role?.RoleName ?? "Customer",
                status = user.Status,
                createdAt = user.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                avatarUrl = user.Profile?.AvatarUrl ?? "",
                driverLicenseNo = user.Profile?.DriverLicenseNo ?? "Không có",
                idCardNo = user.Profile?.IdCardNo ?? "Không có",
                address = user.Profile?.Address ?? "Không có"
            });
        }

        // ====================================================================
        // UC-21.4: UPDATE USER STATUS (API JSON)
        // ====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserStatus(int userId, string status)
        {
            if (userId <= 0)
            {
                return Json(new { success = false, message = "ID người dùng không hợp lệ." });
            }

            if (string.IsNullOrWhiteSpace(status))
            {
                return Json(new { success = false, message = "Trạng thái không hợp lệ." });
            }

            if (status != "Active" && status != "Inactive" && status != "Banned" && status != "Deleted")
            {
                return Json(new { success = false, message = "Trạng thái tài khoản không hợp lệ." });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && (u.RoleId == 3 || u.RoleId == 4));

            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tài khoản người dùng." });
            }

            user.Status = status;
            user.UpdatedAt = DateTime.Now;

            // Ràng buộc: Nếu Owner bị chuyển sang trạng thái khác Active (Deleted, Inactive, Banned), tự động tắt hoạt động xe của họ
            if (user.RoleId == 4 && status != "Active")
            {
                var cars = await _context.Cars.Where(c => c.OwnerId == userId).ToListAsync();
                foreach (var car in cars)
                {
                    car.Status = "Unavailable";
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cập nhật trạng thái người dùng thành công!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi khi cập nhật trạng thái. Vui lòng thử lại." });
            }
        }

        // ====================================================================
        // UC-21.5: DELETE USER
        // ====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = "ID người dùng không hợp lệ." });
            }

            var user = await _context.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.UserId == id && (u.RoleId == 3 || u.RoleId == 4));

            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tài khoản người dùng cần xóa." });
            }

            // Kiểm tra ràng buộc: Nếu khách hàng đang có đơn đặt xe chưa hoàn thành, chặn xóa
            if (user.RoleId == 3)
            {
                var activeBooking = await _context.Bookings.AnyAsync(b => b.CustomerId == id && b.Status != "Completed" && b.Status != "Cancelled");
                if (activeBooking)
                {
                    return Json(new { success = false, message = "Không thể xóa khách hàng này do đang có chuyến đi chưa hoàn thành." });
                }
            }

            // Kiểm tra ràng buộc: Nếu chủ xe đang có xe có đơn đặt chưa hoàn thành, chặn xóa
            if (user.RoleId == 4)
            {
                var activeOwnerBooking = await _context.Bookings.AnyAsync(b => b.Car.OwnerId == id && b.Status != "Completed" && b.Status != "Cancelled");
                if (activeOwnerBooking)
                {
                    return Json(new { success = false, message = "Không thể xóa chủ xe này do đang có đơn đặt xe chưa hoàn thành liên kết với xe của họ." });
                }

                // Tự động chuyển toàn bộ xe của người đó sang trạng thái Unavailable
                var cars = await _context.Cars.Where(c => c.OwnerId == id).ToListAsync();
                foreach (var car in cars)
                {
                    car.Status = "Unavailable";
                }
            }

            try
            {
                user.Status = "Deleted";
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã xóa tài khoản người dùng thành công!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi khi xóa thành viên này. Vui lòng thử lại." });
            }
        }

        // ====================================================================
        // UC-22: REVENUE REPORT & CSV EXPORT
        // ====================================================================
        [HttpGet]
        public async Task<IActionResult> RevenueReport(DateTime? startDate, DateTime? endDate)
        {
            if (startDate.HasValue && endDate.HasValue)
            {
                if (startDate.Value > endDate.Value)
                {
                    TempData["RevenueError"] = "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.";
                    startDate = null;
                    endDate = null;
                }
                else if ((endDate.Value - startDate.Value).TotalDays > 366)
                {
                    TempData["RevenueError"] = "Khoảng thời gian báo cáo không được vượt quá 1 năm.";
                    startDate = null;
                    endDate = null;
                }
            }

            var end = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);
            var start = startDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            var query = _context.Bookings
                .Include(b => b.Customer)
                .ThenInclude(c => c.Profile)
                .Include(b => b.Car)
                .ThenInclude(c => c.Owner)
                .ThenInclude(o => o.Profile)
                .Where(b => b.CreatedAt >= start && b.CreatedAt <= end);

            // Chỉ tính doanh thu dựa trên các đơn đặt xe có trạng thái thanh toán là Paid và Booking đã hoàn thành hoặc được duyệt
            var revenueQuery = query.Where(b => (b.Status == "Completed" || b.Status == "Approved") 
                                                && b.Payments.Any(p => p.PaymentStatus == "Paid"));

            var totalBookings = await query.CountAsync();
            var totalRevenue = await revenueQuery.SumAsync(b => b.SubtotalFee);
            var totalCommission = await revenueQuery.SumAsync(b => b.PlatformCommission);
            var totalPayout = await revenueQuery.SumAsync(b => b.OwnerPayout);

            // ---- KPI bổ sung ----
            var validBookingsCount = await revenueQuery.CountAsync();
            var avgRevenue = validBookingsCount > 0 ? totalRevenue / validBookingsCount : 0;
            var cancelledCount = await query.CountAsync(b => b.Status == "Cancelled");
            var cancellationRate = totalBookings > 0 ? Math.Round((double)cancelledCount / totalBookings * 100, 1) : 0;

            var daysDifference = (end - start).TotalDays;
            var chartData = new List<object>();

            if (daysDifference <= 60)
            {
                var grouped = await revenueQuery
                    .GroupBy(b => b.CreatedAt.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Revenue = g.Sum(b => b.SubtotalFee),
                        Commission = g.Sum(b => b.PlatformCommission),
                        Bookings = g.Count()
                    })
                    .ToListAsync();

                var allDates = Enumerable.Range(0, (int)daysDifference + 1)
                    .Select(offset => start.AddDays(offset).Date)
                    .ToList();

                foreach (var date in allDates)
                {
                    var match = grouped.FirstOrDefault(g => g.Date == date);
                    chartData.Add(new
                    {
                        Label = date.ToString("dd/MM"),
                        Revenue = match?.Revenue ?? 0,
                        Commission = match?.Commission ?? 0,
                        Bookings = match?.Bookings ?? 0
                    });
                }
            }
            else
            {
                var grouped = await revenueQuery
                    .GroupBy(b => new { b.CreatedAt.Year, b.CreatedAt.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Revenue = g.Sum(b => b.SubtotalFee),
                        Commission = g.Sum(b => b.PlatformCommission),
                        Bookings = g.Count()
                    })
                    .ToListAsync();

                var temp = start;
                while (temp <= end)
                {
                    var y = temp.Year;
                    var m = temp.Month;
                    var match = grouped.FirstOrDefault(g => g.Year == y && g.Month == m);
                    chartData.Add(new
                    {
                        Label = $"{m}/{y}",
                        Revenue = match?.Revenue ?? 0,
                        Commission = match?.Commission ?? 0,
                        Bookings = match?.Bookings ?? 0
                    });
                    temp = temp.AddMonths(1);
                }
            }

            var statusData = await query
                .GroupBy(b => b.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            // ---- Doanh thu từng Owner ----
            var bookingsForOwner = await query
                .Select(b => new
                {
                    OwnerId = b.Car.OwnerId,
                    OwnerName = b.Car.Owner.Profile != null ? b.Car.Owner.Profile.FullName : b.Car.Owner.Email,
                    OwnerEmail = b.Car.Owner.Email,
                    CarId = b.CarId,
                    b.SubtotalFee,
                    b.PlatformCommission,
                    b.OwnerPayout,
                    b.Status,
                    IsPaid = b.Payments.Any(p => p.PaymentStatus == "Paid")
                })
                .ToListAsync();

            var ownerStatsData = bookingsForOwner
                .GroupBy(b => b.OwnerId)
                .Select(g => {
                    var validList = g.Where(b => (b.Status == "Completed" || b.Status == "Approved") && b.IsPaid).ToList();
                    return new
                    {
                        OwnerId = g.Key,
                        OwnerName = g.First().OwnerName,
                        OwnerEmail = g.First().OwnerEmail,
                        TotalCars = g.Select(b => b.CarId).Distinct().Count(),
                        TotalBookings = g.Count(),
                        TotalRevenue = validList.Sum(b => b.SubtotalFee),
                        TotalCommission = validList.Sum(b => b.PlatformCommission),
                        TotalPayout = validList.Sum(b => b.OwnerPayout),
                        AvgRevenue = validList.Count > 0 ? validList.Sum(b => b.SubtotalFee) / validList.Count : 0
                    };
                })
                .OrderByDescending(o => o.TotalRevenue)
                .ToList<object>();

            // ---- Top 5 xe doanh thu cao ----
            var topCarsData = bookingsForOwner
                .Where(b => (b.Status == "Completed" || b.Status == "Approved") && b.IsPaid)
                .GroupBy(b => b.CarId)
                .Select(g => new
                {
                    CarId = g.Key,
                    TotalRevenue = g.Sum(b => b.SubtotalFee),
                    TotalBookings = g.Count()
                })
                .OrderByDescending(c => c.TotalRevenue)
                .Take(5)
                .ToList();

            // Lấy tên xe
            var topCarIds = topCarsData.Select(c => c.CarId).ToList();
            var topCarNames = await _context.Cars
                .Where(c => topCarIds.Contains(c.CarId))
                .Select(c => new { c.CarId, c.CarName, c.LicensePlate })
                .ToListAsync();

            var topCarsResult = topCarsData.Select(tc =>
            {
                var carInfo = topCarNames.FirstOrDefault(c => c.CarId == tc.CarId);
                return new
                {
                    CarName = carInfo != null ? $"{carInfo.CarName} ({carInfo.LicensePlate})" : $"Xe #{tc.CarId}",
                    tc.TotalRevenue,
                    tc.TotalBookings
                };
            }).ToList<object>();

            var bookingsList = await query
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    totalBookings,
                    totalRevenue,
                    totalCommission,
                    totalPayout,
                    avgRevenue,
                    cancellationRate,
                    chartData,
                    statusData,
                    ownerStatsData,
                    topCarsData = topCarsResult,
                    bookings = bookingsList.Select(b => new
                    {
                        bookingId = b.BookingId,
                        customerName = b.Customer?.Profile?.FullName ?? "N/A",
                        ownerName = b.Car?.Owner?.Profile?.FullName ?? b.Car?.Owner?.Email ?? "N/A",
                        carName = b.Car?.CarName ?? "N/A",
                        licensePlate = b.Car?.LicensePlate ?? "N/A",
                        startDate = b.StartDate.ToString("dd/MM/yyyy HH:mm"),
                        endDate = b.EndDate.ToString("dd/MM/yyyy HH:mm"),
                        totalDays = b.TotalDays,
                        subtotalFee = b.SubtotalFee,
                        platformCommission = b.PlatformCommission,
                        ownerPayout = b.OwnerPayout,
                        status = b.Status,
                        createdAt = b.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                    })
                });
            }

            ViewBag.StartDate = start.ToString("yyyy-MM-dd");
            ViewBag.EndDate = end.ToString("yyyy-MM-dd");
            ViewBag.TotalBookings = totalBookings;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalCommission = totalCommission;
            ViewBag.TotalPayout = totalPayout;
            ViewBag.AvgRevenue = avgRevenue;
            ViewBag.CancellationRate = cancellationRate;
            ViewBag.ChartDataJson = System.Text.Json.JsonSerializer.Serialize(chartData);
            ViewBag.StatusDataJson = System.Text.Json.JsonSerializer.Serialize(statusData);
            ViewBag.OwnerStatsJson = System.Text.Json.JsonSerializer.Serialize(ownerStatsData);
            ViewBag.TopCarsJson = System.Text.Json.JsonSerializer.Serialize(topCarsResult);

            return View(bookingsList);
        }

        // ====================================================================
        // UC-22.5: OWNER REVENUE REPORT (Doanh thu từng Chủ xe)
        // ====================================================================
        [HttpGet]
        public async Task<IActionResult> OwnerRevenue(DateTime? startDate, DateTime? endDate)
        {
            if (startDate.HasValue && endDate.HasValue)
            {
                if (startDate.Value > endDate.Value)
                {
                    TempData["RevenueError"] = "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.";
                    startDate = null;
                    endDate = null;
                }
                else if ((endDate.Value - startDate.Value).TotalDays > 366)
                {
                    TempData["RevenueError"] = "Khoảng thời gian báo cáo không được vượt quá 1 năm.";
                    startDate = null;
                    endDate = null;
                }
            }

            var end   = endDate   ?? DateTime.Today.AddDays(1).AddSeconds(-1);
            var start = startDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            var query = _context.Bookings
                .Include(b => b.Car)
                .ThenInclude(c => c.Owner)
                .ThenInclude(o => o.Profile)
                .Where(b => b.CreatedAt >= start && b.CreatedAt <= end);

            // Flatten for in-memory grouping (EF Core limitation with navigation props in GroupBy)
            var bookingsFlat = await query
                .Select(b => new
                {
                    OwnerId    = b.Car.OwnerId,
                    OwnerName  = b.Car.Owner.Profile != null ? b.Car.Owner.Profile.FullName : b.Car.Owner.Email,
                    OwnerEmail = b.Car.Owner.Email,
                    OwnerAvatar = b.Car.Owner.Profile != null ? b.Car.Owner.Profile.AvatarUrl : null,
                    CarId      = b.CarId,
                    b.SubtotalFee,
                    b.PlatformCommission,
                    b.OwnerPayout,
                    b.Status,
                    b.CreatedAt,
                    IsPaid = b.Payments.Any(p => p.PaymentStatus == "Paid")
                })
                .ToListAsync();

            var ownerStats = bookingsFlat
                .GroupBy(b => b.OwnerId)
                .Select(g => {
                    var validList = g.Where(b => (b.Status == "Completed" || b.Status == "Approved") && b.IsPaid).ToList();
                    return new
                    {
                        OwnerId         = g.Key,
                        OwnerName       = g.First().OwnerName,
                        OwnerEmail      = g.First().OwnerEmail,
                        OwnerAvatar     = g.First().OwnerAvatar,
                        TotalCars       = g.Select(b => b.CarId).Distinct().Count(),
                        TotalBookings   = g.Count(),
                        CompletedCount  = g.Count(b => b.Status == "Completed" || b.Status == "Approved"),
                        CancelledCount  = g.Count(b => b.Status == "Cancelled"),
                        TotalRevenue    = validList.Sum(b => b.SubtotalFee),
                        TotalCommission = validList.Sum(b => b.PlatformCommission),
                        TotalPayout     = validList.Sum(b => b.OwnerPayout),
                        AvgRevenue      = validList.Count > 0 ? validList.Sum(b => b.SubtotalFee) / validList.Count : 0
                    };
                })
                .OrderByDescending(o => o.TotalRevenue)
                .ToList();

            // Monthly trend per owner (for chart) - only completed/approved & paid
            var monthlyTrend = bookingsFlat
                .Where(b => (b.Status == "Completed" || b.Status == "Approved") && b.IsPaid)
                .GroupBy(b => new { b.OwnerId, b.CreatedAt.Year, b.CreatedAt.Month })
                .Select(g => new
                {
                    g.Key.OwnerId,
                    Label   = $"{g.Key.Month:D2}/{g.Key.Year}",
                    Revenue = g.Sum(b => b.SubtotalFee),
                    Payout  = g.Sum(b => b.OwnerPayout)
                })
                .OrderBy(x => x.Label)
                .ToList();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    ownerStats,
                    totalOwners      = ownerStats.Count,
                    totalRevenue     = ownerStats.Sum(o => o.TotalRevenue),
                    totalPayout      = ownerStats.Sum(o => o.TotalPayout),
                    totalBookings    = ownerStats.Sum(o => o.TotalBookings),
                    monthlyTrend
                });
            }

            ViewBag.StartDate     = start.ToString("yyyy-MM-dd");
            ViewBag.EndDate       = end.ToString("yyyy-MM-dd");
            ViewBag.OwnerStatsJson = System.Text.Json.JsonSerializer.Serialize(ownerStats);
            ViewBag.TotalOwners   = ownerStats.Count;
            ViewBag.TotalRevenue  = ownerStats.Sum(o => o.TotalRevenue);
            ViewBag.TotalPayout   = ownerStats.Sum(o => o.TotalPayout);
            ViewBag.TotalBookings = ownerStats.Sum(o => o.TotalBookings);

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ExportRevenueCsv(DateTime? startDate, DateTime? endDate)
        {
            if (startDate.HasValue && endDate.HasValue)
            {
                if (startDate.Value > endDate.Value)
                {
                    return BadRequest("Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.");
                }
                else if ((endDate.Value - startDate.Value).TotalDays > 366)
                {
                    return BadRequest("Khoảng thời gian báo cáo không được vượt quá 1 năm.");
                }
            }

            var end = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);
            var start = startDate ?? DateTime.Today.AddDays(-30);

            var bookings = await _context.Bookings
                .Include(b => b.Customer)
                .ThenInclude(c => c.Profile)
                .Include(b => b.Car)
                .Where(b => b.CreatedAt >= start && b.CreatedAt <= end)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("Mã Đơn,Khách Hàng,Tên Xe,Biển Số,Ngày Bắt Đầu,Ngày Kết Thúc,Số Ngày Thuê,Doanh Thu (đ),Hoa Hồng (đ),Thanh Toán Chủ Xe (đ),Trạng Thái,Ngày Tạo");

            foreach (var b in bookings)
            {
                var customerName = b.Customer?.Profile?.FullName ?? "N/A";
                var carName = b.Car?.CarName ?? "N/A";
                var licensePlate = b.Car?.LicensePlate ?? "N/A";
                
                customerName = $"\"{customerName.Replace("\"", "\"\"")}\"";
                carName = $"\"{carName.Replace("\"", "\"\"")}\"";
                licensePlate = $"\"{licensePlate.Replace("\"", "\"\"")}\"";

                csvBuilder.AppendLine($"{b.BookingId},{customerName},{carName},{licensePlate},{b.StartDate:dd/MM/yyyy HH:mm},{b.EndDate:dd/MM/yyyy HH:mm},{b.TotalDays},{b.SubtotalFee},{b.PlatformCommission},{b.OwnerPayout},{b.Status},{b.CreatedAt:dd/MM/yyyy HH:mm}");
            }

            var bom = new byte[] { 0xEF, 0xBB, 0xBF };
            var fileContentBytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
            var combinedBytes = bom.Concat(fileContentBytes).ToArray();

            var fileName = $"BaoCaoDoanhThu_{start:yyyyMMdd}_{end:yyyyMMdd}.csv";
            return File(combinedBytes, "text/csv; charset=utf-8", fileName);
        }
    }
}
