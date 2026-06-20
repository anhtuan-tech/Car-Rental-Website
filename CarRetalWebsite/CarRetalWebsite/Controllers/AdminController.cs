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
                .Where(u => u.RoleId == 2); // Staff

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(u => 
                    u.Email.ToLower().Contains(search) || 
                    u.PhoneNumber.ToLower().Contains(search) || 
                    (u.Profile != null && u.Profile.FullName.ToLower().Contains(search))
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

            if (password != confirmPassword)
            {
                return Json(new { success = false, message = "Mật khẩu xác nhận không khớp." });
            }

            var duplicate = await _context.Users.AnyAsync(u => u.Email == email || u.PhoneNumber == phoneNumber);
            if (duplicate)
            {
                return Json(new { success = false, message = "Email hoặc Số điện thoại đã được đăng ký." });
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
            catch (Exception ex)
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
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(status))
            {
                return Json(new { success = false, message = "Vui lòng điền đầy đủ các thông tin." });
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
            catch (Exception ex)
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
            var user = await _context.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.UserId == id && u.RoleId == 2);

            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tài khoản nhân viên cần xóa." });
            }

            try
            {
                if (user.Profile != null)
                {
                    DeleteOldAvatarFile(user.Profile.AvatarUrl);
                    _context.Profiles.Remove(user.Profile);
                }
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã xóa tài khoản nhân viên thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể xóa nhân viên này vì còn có dữ liệu ràng buộc. Vui lòng kiểm tra lại." });
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
                .Where(u => u.RoleId == 3 || u.RoleId == 4); // Customer or Owner

            if (!string.IsNullOrWhiteSpace(role))
            {
                if (role == "Customer") query = query.Where(u => u.RoleId == 3);
                else if (role == "Owner") query = query.Where(u => u.RoleId == 4);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(u => 
                    u.Email.ToLower().Contains(search) || 
                    u.PhoneNumber.ToLower().Contains(search) || 
                    (u.Profile != null && u.Profile.FullName.ToLower().Contains(search))
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
            if (string.IsNullOrWhiteSpace(status))
            {
                return Json(new { success = false, message = "Trạng thái không hợp lệ." });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && (u.RoleId == 3 || u.RoleId == 4));

            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tài khoản người dùng." });
            }

            user.Status = status;
            user.UpdatedAt = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cập nhật trạng thái người dùng thành công!" });
            }
            catch (Exception ex)
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
            var user = await _context.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.UserId == id && (u.RoleId == 3 || u.RoleId == 4));

            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tài khoản người dùng cần xóa." });
            }

            try
            {
                if (user.Profile != null)
                {
                    _context.Profiles.Remove(user.Profile);
                }
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã xóa tài khoản người dùng thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể xóa thành viên này vì đã có dữ liệu liên quan (lịch sử đặt xe, đánh giá...). Vui lòng kiểm tra lại." });
            }
        }

        // ====================================================================
        // UC-22: REVENUE REPORT & CSV EXPORT
        // ====================================================================
        [HttpGet]
        public async Task<IActionResult> RevenueReport(DateTime? startDate, DateTime? endDate)
        {
            var end = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);
            var start = startDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            var query = _context.Bookings
                .Include(b => b.Customer)
                .ThenInclude(c => c.Profile)
                .Include(b => b.Car)
                .ThenInclude(c => c.Owner)
                .ThenInclude(o => o.Profile)
                .Where(b => b.CreatedAt >= start && b.CreatedAt <= end);

            var totalBookings = await query.CountAsync();
            var totalRevenue = await query.SumAsync(b => b.SubtotalFee);
            var totalCommission = await query.SumAsync(b => b.PlatformCommission);
            var totalPayout = await query.SumAsync(b => b.OwnerPayout);

            // ---- KPI bổ sung ----
            var avgRevenue = totalBookings > 0 ? totalRevenue / totalBookings : 0;
            var cancelledCount = await query.CountAsync(b => b.Status == "Cancelled");
            var cancellationRate = totalBookings > 0 ? Math.Round((double)cancelledCount / totalBookings * 100, 1) : 0;

            var daysDifference = (end - start).TotalDays;
            var chartData = new List<object>();

            if (daysDifference <= 60)
            {
                var grouped = await query
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
                var grouped = await query
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
                    b.Status
                })
                .ToListAsync();

            var ownerStatsData = bookingsForOwner
                .GroupBy(b => b.OwnerId)
                .Select(g => new
                {
                    OwnerId = g.Key,
                    OwnerName = g.First().OwnerName,
                    OwnerEmail = g.First().OwnerEmail,
                    TotalCars = g.Select(b => b.CarId).Distinct().Count(),
                    TotalBookings = g.Count(),
                    TotalRevenue = g.Sum(b => b.SubtotalFee),
                    TotalCommission = g.Sum(b => b.PlatformCommission),
                    TotalPayout = g.Sum(b => b.OwnerPayout),
                    AvgRevenue = g.Count() > 0 ? g.Sum(b => b.SubtotalFee) / g.Count() : 0
                })
                .OrderByDescending(o => o.TotalRevenue)
                .ToList<object>();

            // ---- Top 5 xe doanh thu cao ----
            var topCarsData = bookingsForOwner
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
                    b.CreatedAt
                })
                .ToListAsync();

            var ownerStats = bookingsFlat
                .GroupBy(b => b.OwnerId)
                .Select(g => new
                {
                    OwnerId         = g.Key,
                    OwnerName       = g.First().OwnerName,
                    OwnerEmail      = g.First().OwnerEmail,
                    OwnerAvatar     = g.First().OwnerAvatar,
                    TotalCars       = g.Select(b => b.CarId).Distinct().Count(),
                    TotalBookings   = g.Count(),
                    CompletedCount  = g.Count(b => b.Status == "Completed" || b.Status == "Approved"),
                    CancelledCount  = g.Count(b => b.Status == "Cancelled"),
                    TotalRevenue    = g.Sum(b => b.SubtotalFee),
                    TotalCommission = g.Sum(b => b.PlatformCommission),
                    TotalPayout     = g.Sum(b => b.OwnerPayout),
                    AvgRevenue      = g.Count() > 0 ? g.Sum(b => b.SubtotalFee) / g.Count() : 0
                })
                .OrderByDescending(o => o.TotalRevenue)
                .ToList();

            // Monthly trend per owner (for chart)
            var monthlyTrend = bookingsFlat
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
