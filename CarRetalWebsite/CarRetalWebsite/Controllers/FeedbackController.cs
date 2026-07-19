using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarRetalWebsite.Models;

namespace CarRetalWebsite.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly CarRentalDbContext _context;
        private const int CurrentCustomerId = 1; // Khách hàng Vũ Minh Khang cố định trong DB

        public FeedbackController(CarRentalDbContext context)
        {
            _context = context;
        }

        // Xem danh sách phản hồi mà Vũ Minh Khang đã gửi
        public async Task<IActionResult> MyFeedbacks()
        {
            var feedbacks = await _context.Feedbacks
                .Include(f => f.Booking)
                .ThenInclude(b => b.Car)
                .Where(f => f.CustomerId == CurrentCustomerId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return View(feedbacks);
        }

        // Xem chi tiết một phản hồi cụ thể
        public async Task<IActionResult> FeedbackDetails(int id)
        {
            var feedback = await _context.Feedbacks
                .Include(f => f.Booking)
                .ThenInclude(b => b.Car)
                .Include(f => f.Customer)
                .FirstOrDefaultAsync(f => f.FeedbackId == id && f.CustomerId == CurrentCustomerId);

            if (feedback == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đánh giá này.";
                return RedirectToAction("MyFeedbacks");
            }

            return View(feedback);
        }

        // GET: Viết đánh giá cho đơn hàng đã hoàn thành (Completed)
        public async Task<IActionResult> CreateFeedback(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Car)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.CustomerId == CurrentCustomerId);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("MyBookings", "Booking");
            }

            if (booking.Status != "Completed")
            {
                TempData["ErrorMessage"] = "Bạn chỉ có thể viết đánh giá cho những chuyến đi đã hoàn thành (Completed).";
                return RedirectToAction("MyBookings", "Booking");
            }

            // Kiểm tra xem đơn hàng đã từng được đánh giá chưa
            var existingFeedback = await _context.Feedbacks.AnyAsync(f => f.BookingId == bookingId);
            if (existingFeedback)
            {
                TempData["ErrorMessage"] = "Đơn đặt xe này đã được đánh giá trước đó.";
                return RedirectToAction("MyBookings", "Booking");
            }

            ViewBag.Booking = booking;
            return View();
        }

        // POST: Xử lý lưu đánh giá mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCreateFeedback(int bookingId, int rating, string comment)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null || booking.CustomerId != CurrentCustomerId)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng tương ứng.";
                return RedirectToAction("MyBookings", "Booking");
            }

            if (booking.Status != "Completed")
            {
                TempData["ErrorMessage"] = "Chuyến đi chưa hoàn thành, không thể đánh giá.";
                return RedirectToAction("MyBookings", "Booking");
            }

            var existingFeedback = await _context.Feedbacks.AnyAsync(f => f.BookingId == bookingId);
            if (existingFeedback)
            {
                TempData["ErrorMessage"] = "Đơn đặt xe này đã được đánh giá rồi.";
                return RedirectToAction("MyBookings", "Booking");
            }

            if (rating < 1 || rating > 5)
            {
                ModelState.AddModelError("", "Độ hài lòng (rating) phải từ 1 đến 5 sao.");
                ViewBag.Booking = await _context.Bookings.Include(b => b.Car).FirstOrDefaultAsync(b => b.BookingId == bookingId);
                return View("CreateFeedback");
            }

            var feedback = new Feedback
            {
                BookingId = bookingId,
                CustomerId = CurrentCustomerId,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cảm ơn bạn đã gửi đánh giá cho dịch vụ!";
            return RedirectToAction("MyFeedbacks");
        }

        // GET: Sửa đánh giá đã gửi
        public async Task<IActionResult> EditFeedback(int id)
        {
            var feedback = await _context.Feedbacks
                .Include(f => f.Booking)
                .ThenInclude(b => b.Car)
                .FirstOrDefaultAsync(f => f.FeedbackId == id && f.CustomerId == CurrentCustomerId);

            if (feedback == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đánh giá cần sửa.";
                return RedirectToAction("MyFeedbacks");
            }

            return View(feedback);
        }

        // POST: Lưu thay đổi đánh giá
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmEditFeedback(int id, int rating, string comment)
        {
            var feedback = await _context.Feedbacks
                .Include(f => f.Booking)
                .ThenInclude(b => b.Car)
                .FirstOrDefaultAsync(f => f.FeedbackId == id && f.CustomerId == CurrentCustomerId);

            if (feedback == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đánh giá cần sửa.";
                return RedirectToAction("MyFeedbacks");
            }

            if (rating < 1 || rating > 5)
            {
                ModelState.AddModelError("", "Độ hài lòng (rating) phải từ 1 đến 5 sao.");
                return View("EditFeedback", feedback);
            }

            feedback.Rating = rating;
            feedback.Comment = comment;
            feedback.UpdatedAt = DateTime.Now;

            _context.Feedbacks.Update(feedback);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật đánh giá thành công.";
            return RedirectToAction("MyFeedbacks");
        }

        // POST: Xóa đánh giá
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFeedback(int id)
        {
            var feedback = await _context.Feedbacks
                .FirstOrDefaultAsync(f => f.FeedbackId == id && f.CustomerId == CurrentCustomerId);

            if (feedback == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đánh giá cần xóa.";
                return RedirectToAction("MyFeedbacks");
            }

            _context.Feedbacks.Remove(feedback);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xóa đánh giá thành công.";
            return RedirectToAction("MyFeedbacks");
        }
    }
}
