using CarRetalWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRetalWebsite.Controllers
{
    public class HomeController : Controller
    {
        private readonly CarRentalDbContext _context;

        public HomeController(CarRentalDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                else if (User.IsInRole("Owner") || User.IsInRole("Staff"))
                {
                    return RedirectToAction("Index", "StaffCar");
                }
            }

            // Truy vấn lấy danh sách tối đa 4 xe nổi bật có trạng thái Available
            var featuredCars = await _context.Cars
                .Include(c => c.CarImages)
                .Include(c => c.Type)
                .Where(c => c.Status == "Available")
                .Take(4)
                .ToListAsync();

            return View(featuredCars);
        }

        public IActionResult About()
        {
            return View();
        }
    }
}
