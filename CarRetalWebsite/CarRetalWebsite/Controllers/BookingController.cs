using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarRetalWebsite.Models;

namespace CarRetalWebsite.Controllers
{
    public class BookingController : Controller
    {
        private readonly CarRentalDbContext _context;
        private const int CurrentCustomerId = 1; // Khách hàng Vũ Minh Khang cố định trong DB

        public BookingController(CarRentalDbContext context)
        {
            _context = context;
        }

        // Hiển thị danh sách xe có sẵn (Available) để khách hàng chọn đặt
        public async Task<IActionResult> Index(string keyword, int? typeId, decimal? maxPrice)
        {
            var query = _context.Cars
                .Include(c => c.CarImages)
                .Include(c => c.Type)
                .Where(c => c.Status == "Available");

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(c => c.CarName.Contains(keyword) || c.Brand.Contains(keyword));
            }

            if (typeId.HasValue)
            {
                query = query.Where(c => c.TypeId == typeId.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(c => c.PricePerDay <= maxPrice.Value);
            }

            var cars = await query.ToListAsync();
            ViewBag.CarTypes = await _context.CarTypes.ToListAsync();
            ViewBag.Keyword = keyword;
            ViewBag.TypeId = typeId;
            ViewBag.MaxPrice = maxPrice;

            return View(cars);
        }

        // Xem danh sách đặt xe của Vũ Minh Khang
        public async Task<IActionResult> MyBookings(string keyword, DateTime? startDate, DateTime? endDate, string status)
        {
            var query = _context.Bookings
                .Include(b => b.Car)
                .ThenInclude(c => c.CarImages)
                .Include(b => b.Feedback)
                .Where(b => b.CustomerId == CurrentCustomerId);

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(b => b.Car.CarName.Contains(keyword) || b.Car.Brand.Contains(keyword));
            }

            if (startDate.HasValue)
            {
                query = query.Where(b => b.StartDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(b => b.EndDate <= endDate.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(b => b.Status == status);
            }

            var bookings = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();

            ViewBag.Keyword = keyword;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.Status = status;

            return View(bookings);
        }

        // GET: Đặt xe
        public async Task<IActionResult> Book(int carId)
        {
            var car = await _context.Cars
                .Include(c => c.CarImages)
                .Include(c => c.Type)
                .FirstOrDefaultAsync(c => c.CarId == carId);

            if (car == null || car.Status != "Available")
            {
                TempData["ErrorMessage"] = "Xe không tồn tại hoặc hiện không sẵn sàng.";
                return RedirectToAction("Index");
            }

            return View(car);
        }

        // POST: Xử lý đặt xe
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBook(int carId, DateTime startDate, DateTime endDate)
        {
            var car = await _context.Cars.FindAsync(carId);
            if (car == null || car.Status != "Available")
            {
                TempData["ErrorMessage"] = "Xe không tồn tại hoặc hiện không sẵn sàng.";
                return RedirectToAction("Index");
            }

            if (startDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("", "Ngày bắt đầu thuê không được nhỏ hơn ngày hiện tại.");
                return View("Book", car);
            }

            if (endDate.Date < startDate.Date)
            {
                ModelState.AddModelError("", "Ngày kết thúc thuê phải lớn hơn hoặc bằng ngày bắt đầu.");
                return View("Book", car);
            }

            // Kiểm tra xe có bị trùng lịch thuê khác không (đơn chưa hủy/từ chối)
            var isOverlapping = await _context.Bookings.AnyAsync(b =>
                b.CarId == carId &&
                b.Status != "Cancelled" && b.Status != "Rejected" &&
                ((startDate.Date >= b.StartDate.Date && startDate.Date <= b.EndDate.Date) ||
                 (endDate.Date >= b.StartDate.Date && endDate.Date <= b.EndDate.Date) ||
                 (b.StartDate.Date >= startDate.Date && b.StartDate.Date <= endDate.Date))
            );

            if (isOverlapping)
            {
                ModelState.AddModelError("", "Xe đã được đặt trong khoảng thời gian này. Vui lòng chọn ngày thuê khác.");
                return View("Book", car);
            }

            // Áp dụng công thức tính toán phí thuê xe
            int totalDays = (endDate.Date - startDate.Date).Days + 1;
            decimal subtotalFee = totalDays * car.PricePerDay;
            decimal platformCommission = subtotalFee * 0.10m; // 10% phí nền tảng
            decimal ownerPayout = subtotalFee - platformCommission;

            // Tạo đơn đặt xe mới
            var booking = new Booking
            {
                CustomerId = CurrentCustomerId,
                CarId = carId,
                StartDate = startDate,
                EndDate = endDate,
                TotalDays = totalDays,
                SubtotalFee = subtotalFee,
                PlatformCommission = platformCommission,
                OwnerPayout = ownerPayout,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // Tạo lịch sử trạng thái đặt xe
            var history = new BookingHistory
            {
                BookingId = booking.BookingId,
                ChangedBy = CurrentCustomerId,
                OldStatus = null,
                NewStatus = "Pending",
                Note = "Khách hàng Vũ Minh Khang tạo đơn đặt xe.",
                ChangedAt = DateTime.Now
            };

            _context.BookingHistories.Add(history);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đặt xe {car.CarName} thành công! Đơn hàng của bạn đang chờ duyệt.";
            return RedirectToAction("MyBookings");
        }

        // POST: Hủy đơn đặt xe của bản thân (chỉ được hủy nếu ở trạng thái Pending)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == id && b.CustomerId == CurrentCustomerId);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng đặt xe này.";
                return RedirectToAction("MyBookings");
            }

            if (booking.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Chỉ được phép hủy các đơn đặt xe ở trạng thái đang chờ duyệt (Pending).";
                return RedirectToAction("MyBookings");
            }

            string oldStatus = booking.Status;
            booking.Status = "Cancelled";
            _context.Bookings.Update(booking);

            // Ghi nhận lịch sử thay đổi trạng thái
            var history = new BookingHistory
            {
                BookingId = booking.BookingId,
                ChangedBy = CurrentCustomerId,
                OldStatus = oldStatus,
                NewStatus = "Cancelled",
                Note = "Khách hàng tự hủy đơn.",
                ChangedAt = DateTime.Now
            };

            _context.BookingHistories.Add(history);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã hủy đơn đặt xe thành công.";
            return RedirectToAction("MyBookings");
        }
    }
}
