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
                .Include(b => b.Customer)
                    .ThenInclude(u => u.Profile)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();

                query = query.Where(b =>
                    b.BookingId.ToString().Contains(search) ||
                    b.Car.CarName.ToLower().Contains(search) ||
                    b.Car.LicensePlate.ToLower().Contains(search) ||
                    (b.Customer.Profile != null &&
                     b.Customer.Profile.FullName.ToLower().Contains(search)));
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
                .Include(b => b.Customer)
                    .ThenInclude(u => u.Profile)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
                return NotFound();

            return View("~/Views/Staff/Booking/Details.cshtml", booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus)
        {
            var booking = await _context.Bookings.FindAsync(id);

            if (booking == null)
                return NotFound();

            booking.Status = newStatus;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}