using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarRetalWebsite.Models;

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
                    return Content("Chức năng cho Chủ xe và Nhân viên đang được phát triển.");
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
