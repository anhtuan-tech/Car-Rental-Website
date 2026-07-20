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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [Route("Home/Error")]
        public IActionResult Error(int? statusCode = null)
        {
            if (statusCode == 404)
            {
                ViewData["ErrorCode"] = 404;
                ViewData["ErrorTitle"] = "Không tìm thấy trang";
                ViewData["ErrorMessage"] = "Đường dẫn bạn truy cập không tồn tại hoặc đã bị di chuyển.";
                return View("Error");
            }
            else if (statusCode == 403)
            {
                ViewData["ErrorCode"] = 403;
                ViewData["ErrorTitle"] = "Truy cập bị từ chối";
                ViewData["ErrorMessage"] = "Bạn không có quyền truy cập vào trang này.";
                return View("Error");
            }

            ViewData["ErrorCode"] = statusCode ?? 500;
            ViewData["ErrorTitle"] = "Đã xảy ra lỗi hệ thống";
            ViewData["ErrorMessage"] = "Chúng tôi xin lỗi vì sự bất tiện này. Vui lòng quay lại sau.";
            return View("Error");
        }
    }
}
