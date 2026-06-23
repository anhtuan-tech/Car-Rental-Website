using CarRetalWebsite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRetalWebsite.Controllers
{
    [Authorize(Roles = "Staff")]
    public class StaffCarController : Controller
    {
        private readonly CarRentalDbContext _context;

        public StaffCarController(CarRentalDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string search, string status)
        {
            ViewBag.Search = search;
            ViewBag.Status = status;

            var query = _context.Cars
                .Include(c => c.Owner)
                    .ThenInclude(o => o.Profile)
                .Include(c => c.Type)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();

                query = query.Where(c =>
                    c.CarName.ToLower().Contains(search) ||
                    c.Brand.ToLower().Contains(search) ||
                    c.LicensePlate.ToLower().Contains(search));
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(c => c.Status == status);
            }

            var data = await query
                .OrderByDescending(c => c.CarId)
                .ToListAsync();

            return View("~/Views/Staff/Car/Index.cshtml", data);
        }

        public async Task<IActionResult> Details(int id)
        {
            var car = await _context.Cars
                .Include(c => c.Owner)
                    .ThenInclude(o => o.Profile)
                .Include(c => c.Type)
                .Include(c => c.CarImages)
                .FirstOrDefaultAsync(c => c.CarId == id);

            if (car == null)
                return NotFound();

            return View("~/Views/Staff/Car/Details.cshtml", car);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus)
        {
            var car = await _context.Cars.FindAsync(id);

            if (car == null)
                return NotFound();

            car.Status = newStatus;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}