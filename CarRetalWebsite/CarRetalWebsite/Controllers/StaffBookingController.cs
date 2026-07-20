using CarRetalWebsite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRetalWebsite.Controllers
{
    [Authorize(Roles = "Staff")]
    public class StaffBookingController : Controller
    {
        private readonly CarRentalDbContext _context;

        public StaffBookingController(CarRentalDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string search, string status)
        {
            ViewBag.Search = search;
            ViewBag.Status = status;

            var query = _context.Bookings
                .Include(b => b.Car)
                    .ThenInclude(c => c.CarImages)
                .Include(b => b.Car)
                    .ThenInclude(c => c.Owner)
                        .ThenInclude(o => o.Profile)
                .Include(b => b.Customer)
                    .ThenInclude(u => u.Profile)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var s = search.ToLower();

                query = query.Where(b =>
                    b.BookingId.ToString().Contains(s) ||
                    (b.Car != null && ((b.Car.CarName ?? "").ToLower().Contains(s))) ||
                    (b.Car != null && ((b.Car.LicensePlate ?? "").ToLower().Contains(s))) ||
                    (b.Customer != null && b.Customer.Profile != null && ((b.Customer.Profile.FullName ?? "").ToLower().Contains(s))) ||
                    (b.Customer != null && ((b.Customer.Email ?? "").ToLower().Contains(s)))
                );
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(b => b.Status == status);
            }

            var data = await query
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View("~/Views/Staff/Booking/Index.cshtml", data);
        }

        public async Task<IActionResult> Details(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Car)
                    .ThenInclude(c => c.CarImages)
                .Include(b => b.Car)
                    .ThenInclude(c => c.Owner)
                        .ThenInclude(o => o.Profile)
                .Include(b => b.Customer)
                    .ThenInclude(u => u.Profile)
                .Include(b => b.Payments)
                .Include(b => b.BookingHistories)
                    .ThenInclude(h => h.ChangedByNavigation)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
                return NotFound();

            return View("~/Views/Staff/Booking/Details.cshtml", booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus, string? note)
        {
            var booking = await _context.Bookings.FindAsync(id);

            if (booking == null)
                return NotFound();

            var oldStatus = booking.Status;
            booking.Status = newStatus;

            // Create booking history
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var user = !string.IsNullOrEmpty(email) ? await _context.Users.FirstOrDefaultAsync(u => u.Email == email) : null;
            var changedBy = user?.UserId ?? 0;

            var history = new BookingHistory
            {
                BookingId = id,
                ChangedBy = changedBy,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                Note = note,
                ChangedAt = DateTime.Now
            };

            _context.BookingHistories.Add(history);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}