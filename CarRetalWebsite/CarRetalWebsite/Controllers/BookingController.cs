using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarRetalWebsite.Models;
using CarRetalWebsite.Services;

namespace CarRetalWebsite.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly CarRentalDbContext _context;

        private int CurrentCustomerId
        {
            get
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim, out int id))
                {
                    return id;
                }
                return 0;
            }
        }

        public BookingController(CarRentalDbContext context)
        {
            _context = context;
        }

        // Xem danh sách đặt xe của khách hàng Vũ Minh Khang
        public async Task<IActionResult> Index(string keyword, DateTime? startDate, DateTime? endDate, string status)
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

        // Redirect cũ để đảm bảo các liên kết cũ vẫn chạy về đúng trang mới
        public IActionResult MyBookings(string keyword, DateTime? startDate, DateTime? endDate, string status)
        {
            return RedirectToAction("Index", new { keyword, startDate, endDate, status });
        }

        // GET: Đặt xe
        public async Task<IActionResult> Create(int carId, DateTime startDate, DateTime endDate)
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

            ViewBag.StartDate = startDate == DateTime.MinValue ? DateTime.Today : startDate;
            ViewBag.EndDate = endDate == DateTime.MinValue ? DateTime.Today.AddDays(1) : endDate;

            return View(car);
        }

        // POST: Xử lý đặt xe
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBook(int carId, DateTime startDate, DateTime endDate)
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

            if (startDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("", "Ngày bắt đầu thuê không được nhỏ hơn ngày hiện tại.");
                ViewBag.StartDate = startDate;
                ViewBag.EndDate = endDate;
                return View("Create", car);
            }

            if (endDate.Date < startDate.Date)
            {
                ModelState.AddModelError("", "Ngày kết thúc thuê phải lớn hơn hoặc bằng ngày bắt đầu.");
                ViewBag.StartDate = startDate;
                ViewBag.EndDate = endDate;
                return View("Create", car);
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
                ViewBag.StartDate = startDate;
                ViewBag.EndDate = endDate;
                return View("Create", car);
            }

            // Áp dụng công thức tính toán phí thuê xe
            int totalDays = (endDate.Date - startDate.Date).Days;
            if (totalDays < 1) totalDays = 1;
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
                Note = $"Khách hàng {User.Identity?.Name ?? "Người dùng"} tạo đơn đặt xe.",
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

        [HttpGet]
        public async Task<IActionResult> PayWithVnPay(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Car)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.CustomerId == CurrentCustomerId);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn đặt xe.";
                return RedirectToAction("Index");
            }

            if (booking.Status != "Approved")
            {
                TempData["ErrorMessage"] = "Đơn đặt xe phải ở trạng thái Đã duyệt (Approved) mới có thể thanh toán.";
                return RedirectToAction("Index");
            }

            // VNPay config
            string vnp_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            string vnp_TmnCode = "35D4SA6W";
            string vnp_HashSecret = "FJ1QVMTG3QFH25SSJBSOY9ESG5TUGSYW";
            string vnp_ReturnUrl = Url.Action("VnPayCallback", "Booking", null, Request.Scheme) ?? "";

            var vnpay = new VnPayLibrary();
            
            // Convert subtotalFee (decimal) to long (VNPay expects amount * 100)
            long amount = (long)(booking.SubtotalFee * 100);

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", amount.ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            vnpay.AddRequestData("vnp_IpAddr", ipAddress);
            
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan don dat xe #{booking.BookingId}");
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_ReturnUrl);
            
            string txnRef = $"{booking.BookingId}_{DateTime.Now.Ticks}";
            vnpay.AddRequestData("vnp_TxnRef", txnRef);

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            return Redirect(paymentUrl);
        }

        [HttpGet]
        public async Task<IActionResult> VnPayCallback()
        {
            if (Request.Query.Count > 0)
            {
                string vnp_HashSecret = "FJ1QVMTG3QFH25SSJBSOY9ESG5TUGSYW";
                var vnpayData = Request.Query;
                var vnpay = new VnPayLibrary();

                foreach (var key in vnpayData.Keys)
                {
                    if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(key, vnpayData[key]);
                    }
                }

                string txnRef = vnpay.GetResponseData("vnp_TxnRef");
                string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                string? vnp_SecureHash = Request.Query["vnp_SecureHash"].ToString();
                string vnp_TransactionNo = vnpay.GetResponseData("vnp_TransactionNo");
                
                string amountStr = vnpay.GetResponseData("vnp_Amount");
                decimal vnp_Amount = 0;
                if (long.TryParse(amountStr, out long parsedAmount))
                {
                    vnp_Amount = (decimal)parsedAmount / 100m;
                }

                bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);
                if (checkSignature)
                {
                    string[] parts = txnRef.Split('_');
                    if (parts.Length > 0 && int.TryParse(parts[0], out int bookingId))
                    {
                        var booking = await _context.Bookings
                            .Include(b => b.Car)
                            .FirstOrDefaultAsync(b => b.BookingId == bookingId);

                        if (booking != null)
                        {
                            if (vnp_ResponseCode == "00")
                            {
                                string oldStatus = booking.Status;
                                booking.Status = "Paid";
                                _context.Bookings.Update(booking);

                                var payment = new Payment
                                {
                                    BookingId = bookingId,
                                    Amount = vnp_Amount,
                                    PaymentMethod = "VNPay",
                                    TransactionReference = vnp_TransactionNo,
                                    PaymentStatus = "Paid",
                                    PaidAt = DateTime.Now
                                };
                                _context.Payments.Add(payment);

                                var history = new BookingHistory
                                {
                                    BookingId = bookingId,
                                    ChangedBy = CurrentCustomerId > 0 ? CurrentCustomerId : booking.CustomerId,
                                    OldStatus = oldStatus,
                                    NewStatus = "Paid",
                                    Note = $"Thanh toán thành công qua VNPay. Mã GD: {vnp_TransactionNo}.",
                                    ChangedAt = DateTime.Now
                                };
                                _context.BookingHistories.Add(history);

                                await _context.SaveChangesAsync();

                                TempData["SuccessMessage"] = $"Thanh toán thành công cho đơn đặt xe #{bookingId}!";
                            }
                            else
                            {
                                TempData["ErrorMessage"] = $"Thanh toán thất bại cho đơn đặt xe #{bookingId}. Mã lỗi: {vnp_ResponseCode}";
                            }
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Không tìm thấy thông tin đơn đặt xe tương ứng với giao dịch.";
                        }
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Mã giao dịch không hợp lệ.";
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Chữ ký bảo mật không hợp lệ.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Không nhận được thông tin phản hồi từ cổng thanh toán VNPay.";
            }

            return RedirectToAction("Index");
        }
    }
}
