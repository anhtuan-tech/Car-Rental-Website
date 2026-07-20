using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarRetalWebsite.Models;

namespace CarRetalWebsite.Controllers
{
    [Authorize(Roles = "Owner")]
    public class OwnerController : Controller
    {
        private readonly CarRentalDbContext _context;
        private readonly CarRetalWebsite.Services.IImageService _imageService;

        public OwnerController(CarRentalDbContext context, CarRetalWebsite.Services.IImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        private int? GetCurrentOwnerId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int ownerId))
            {
                return ownerId;
            }
            return null;
        }

        public async Task<IActionResult> Index()
        {
            var ownerId = GetCurrentOwnerId();
            if (ownerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View("OwnerView");
        }

        [HttpGet]
        public async Task<IActionResult> GetCars()
        {
            var ownerId = GetCurrentOwnerId();
            if (ownerId == null) return Unauthorized();

            var cars = await _context.Cars
                .Include(c => c.Type)
                .Include(c => c.CarImages)
                .Where(c => c.OwnerId == ownerId.Value && c.Status != "Deleted")
                .Select(c => new
                {
                    id = c.CarId,
                    name = c.CarName,
                    brand = c.Brand,
                    model = c.Model,
                    carType = c.Type.TypeName,
                    pricePerDay = c.PricePerDay,
                    description = c.SpecsJson,
                    images = c.CarImages.Select(ci => new { ci.ImageUrl, ci.IsPrimary }).ToList()
                })
                .ToListAsync();

            return Json(cars);
        }

        [HttpGet]
        public async Task<IActionResult> SearchCars(string query)
        {
            var ownerId = GetCurrentOwnerId();
            if (ownerId == null) return Unauthorized();

            var q = (query ?? "").ToLower();

            var cars = await _context.Cars
                .Include(c => c.Type)
                .Include(c => c.CarImages)
                .Where(c => c.OwnerId == ownerId.Value && c.Status != "Deleted")
                .Where(c => c.CarName.ToLower().Contains(q) ||
                            c.Brand.ToLower().Contains(q) ||
                            c.Model.ToLower().Contains(q) ||
                            c.Type.TypeName.ToLower().Contains(q))
                .Select(c => new
                {
                    id = c.CarId,
                    name = c.CarName,
                    brand = c.Brand,
                    model = c.Model,
                    carType = c.Type.TypeName,
                    pricePerDay = c.PricePerDay,
                    description = c.SpecsJson,
                    images = c.CarImages.Select(ci => new { ci.ImageUrl, ci.IsPrimary }).ToList()
                })
                .ToListAsync();

            return Json(cars);
        }

        [HttpPost]
        public async Task<IActionResult> AddCar([FromForm] CarDto carDto, List<IFormFile>? imageFiles)
        {
            var ownerId = GetCurrentOwnerId();
            if (ownerId == null) return Unauthorized();

            if (carDto == null || string.IsNullOrWhiteSpace(carDto.Name) || string.IsNullOrWhiteSpace(carDto.Brand))
            {
                return Json(new { success = false, message = "Thông tin xe không hợp lệ." });
            }

            var type = await _context.CarTypes
                .FirstOrDefaultAsync(t => t.TypeName.ToLower() == carDto.CarType.ToLower());
            if (type == null)
            {
                type = new CarType { TypeName = carDto.CarType };
                _context.CarTypes.Add(type);
                await _context.SaveChangesAsync();
            }

            // Generate a random unique license plate
            string licensePlate;
            var random = new Random();
            do
            {
                licensePlate = $"LP-{random.Next(100, 999)}-{random.Next(10, 99)}";
            } while (await _context.Cars.AnyAsync(c => c.LicensePlate == licensePlate));

            var car = new Car
            {
                OwnerId = ownerId.Value,
                TypeId = type.TypeId,
                CarName = carDto.Name,
                Brand = carDto.Brand,
                Model = carDto.Model ?? "2023",
                LicensePlate = licensePlate,
                PricePerDay = carDto.PricePerDay,
                SpecsJson = carDto.Description,
                Status = "Pending_Approval"
            };

            _context.Cars.Add(car);
            await _context.SaveChangesAsync();

            // Save uploaded images
            if (imageFiles != null && imageFiles.Count > 0)
            {
                bool isFirst = true;
                foreach (var file in imageFiles)
                {
                    if (file.Length > 0)
                    {
                        var savedPath = await _imageService.SaveImageAsync(file, "Car");
                        var carImage = new CarImage
                        {
                            CarId = car.CarId,
                            ImageUrl = savedPath,
                            IsPrimary = isFirst
                        };
                        _context.CarImages.Add(carImage);
                        isFirst = false;
                    }
                }
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCar([FromForm] CarDto carDto, List<IFormFile>? imageFiles)
        {
            var ownerId = GetCurrentOwnerId();
            if (ownerId == null) return Unauthorized();

            if (carDto == null || carDto.Id == null)
            {
                return Json(new { success = false, message = "Thông tin xe không hợp lệ." });
            }

            var car = await _context.Cars
                .FirstOrDefaultAsync(c => c.CarId == carDto.Id && c.OwnerId == ownerId.Value);

            if (car == null)
            {
                return Json(new { success = false, message = "Không tìm thấy xe." });
            }

            var type = await _context.CarTypes
                .FirstOrDefaultAsync(t => t.TypeName.ToLower() == carDto.CarType.ToLower());
            if (type == null)
            {
                type = new CarType { TypeName = carDto.CarType };
                _context.CarTypes.Add(type);
                await _context.SaveChangesAsync();
            }

            car.CarName = carDto.Name;
            car.Brand = carDto.Brand;
            car.Model = carDto.Model ?? car.Model;
            car.TypeId = type.TypeId;
            car.PricePerDay = carDto.PricePerDay;
            car.SpecsJson = carDto.Description;

            _context.Cars.Update(car);
            await _context.SaveChangesAsync();

            // If new images are uploaded, remove existing ones and save new ones
            if (imageFiles != null && imageFiles.Count > 0)
            {
                var oldImages = await _context.CarImages.Where(ci => ci.CarId == car.CarId).ToListAsync();
                foreach (var oldImg in oldImages)
                {
                    _imageService.DeleteImage(oldImg.ImageUrl);
                    _context.CarImages.Remove(oldImg);
                }
                await _context.SaveChangesAsync();

                bool isFirst = true;
                foreach (var file in imageFiles)
                {
                    if (file.Length > 0)
                    {
                        var savedPath = await _imageService.SaveImageAsync(file, "Car");
                        var carImage = new CarImage
                        {
                            CarId = car.CarId,
                            ImageUrl = savedPath,
                            IsPrimary = isFirst
                        };
                        _context.CarImages.Add(carImage);
                        isFirst = false;
                    }
                }
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> RequestCarRemoval(int id)
        {
            var ownerId = GetCurrentOwnerId();
            if (ownerId == null) return Unauthorized();

            var car = await _context.Cars
                .FirstOrDefaultAsync(c => c.CarId == id && c.OwnerId == ownerId.Value);

            if (car == null)
            {
                return Json(new { success = false, message = "Không tìm thấy xe hoặc bạn không phải là chủ sở hữu xe này." });
            }

            car.Status = "Pending_Removal";
            _context.Cars.Update(car);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> ViewEarning()
        {
            var ownerId = GetCurrentOwnerId();
            if (ownerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var earningHistory = await _context.BookingHistories
                .Include(bh => bh.Booking)
                    .ThenInclude(b => b.Car)
                .Include(bh => bh.Booking)
                    .ThenInclude(b => b.Customer)
                        .ThenInclude(c => c.Profile)
                .Where(bh => bh.Booking.Car.OwnerId == ownerId.Value)
                .ToListAsync();

            return View(earningHistory);
        }

        [HttpGet]
        public async Task<IActionResult> ViewRentalOrder()
        {
            var ownerId = GetCurrentOwnerId();
            if (ownerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var bookings = await _context.Bookings
                .Include(b => b.Car)
                .Include(b => b.Customer)
                    .ThenInclude(c => c.Profile)
                .Where(b => b.Car.OwnerId == ownerId.Value)
                .OrderByDescending(b => b.BookingId)
                .Select(b => new RentalOrderViewModel
                {
                    Id = b.BookingId,
                    CarName = b.Car.CarName,
                    RenterName = (b.Customer.Profile != null ? b.Customer.Profile.FullName : null) ?? b.Customer.Email,
                    StartDate = b.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = b.EndDate.ToString("yyyy-MM-dd"),
                    Status = b.Status,
                    Message = ""
                })
                .ToListAsync();

            return View(bookings);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Car car)
        {
            var ownerId = GetCurrentOwnerId();
            if (ownerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                car.OwnerId = ownerId.Value;
                _context.Cars.Add(car);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(car);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ownerId = GetCurrentOwnerId();
            if (ownerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var car = await _context.Cars
                .FirstOrDefaultAsync(c => c.CarId == id && c.OwnerId == ownerId.Value);

            if (car == null)
            {
                return NotFound();
            }

            return View(car);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ownerId = GetCurrentOwnerId();
            if (ownerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var car = await _context.Cars
                .FirstOrDefaultAsync(c => c.CarId == id && c.OwnerId == ownerId.Value);

            if (car == null)
            {
                return NotFound();
            }

            return View(car);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Car car)
        {
            var ownerId = GetCurrentOwnerId();
            if (ownerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id != car.CarId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existingCar = await _context.Cars
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CarId == id && c.OwnerId == ownerId.Value);

                if (existingCar == null)
                {
                    return NotFound();
                }

                car.OwnerId = ownerId.Value;
                _context.Cars.Update(car);

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CarExists(id, ownerId.Value))
                    {
                        return NotFound();
                    }
                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(car);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ownerId = GetCurrentOwnerId();
            if (ownerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var car = await _context.Cars
                .FirstOrDefaultAsync(c => c.CarId == id && c.OwnerId == ownerId.Value);

            if (car == null)
            {
                return NotFound();
            }

            return View(car);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ownerId = GetCurrentOwnerId();
            if (ownerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var car = await _context.Cars
                .FirstOrDefaultAsync(c => c.CarId == id && c.OwnerId == ownerId.Value);

            if (car == null)
            {
                return NotFound();
            }

            _context.Cars.Remove(car);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CarExists(int id, int ownerId)
        {
            return _context.Cars.Any(c => c.CarId == id && c.OwnerId == ownerId);
        }
    }

    public class CarDto
    {
        public int? Id { get; set; }
        public string Name { get; set; } = null!;
        public string Brand { get; set; } = null!;
        public string Model { get; set; } = null!;
        public string CarType { get; set; } = null!;
        public decimal PricePerDay { get; set; }
        public string? Description { get; set; }
    }

    public class RentalOrderViewModel
    {
        public int Id { get; set; }
        public string CarName { get; set; } = null!;
        public string RenterName { get; set; } = null!;
        public string StartDate { get; set; } = null!;
        public string EndDate { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? Message { get; set; }
    }
}
