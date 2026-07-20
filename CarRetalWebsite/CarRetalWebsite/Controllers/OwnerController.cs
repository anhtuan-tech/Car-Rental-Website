using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
// using CarRetalWebsite.Data;
using CarRetalWebsite.Models;

namespace CarRetalWebsite.Controllers
{
    [Authorize(Roles = "Owner")]
    public class OwnerController : Controller
    {
        private readonly CarRentalDbContext _context;

        public OwnerController(CarRentalDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var ownerId = User.FindFirst("OwnerId")?.Value;
            if (string.IsNullOrEmpty(ownerId))
            {
                return RedirectToAction("Login", "Account");
            }

            var cars = await _context.Cars
                .Where(c => c.OwnerId.Equals(ownerId))
                .ToListAsync();

            return View(cars);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Car car)
        {
            var ownerId = User.FindFirst("OwnerId")?.Value;
            if (string.IsNullOrEmpty(ownerId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                car.OwnerId.Equals(ownerId);
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

            var ownerId = User.FindFirst("OwnerId")?.Value;
            var car = await _context.Cars
                .FirstOrDefaultAsync(c => c.CarId== id && c.OwnerId.Equals(ownerId));

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

            var ownerId = User.FindFirst("OwnerId")?.Value;
            var car = await _context.Cars
                .FirstOrDefaultAsync(c => c.CarId == id && c.OwnerId.Equals(ownerId));

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
            var ownerId = User.FindFirst("OwnerId")?.Value;
            if (id != car.CarId || string.IsNullOrEmpty(ownerId))
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existingCar = await _context.Cars
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CarId == id && c.OwnerId.Equals(ownerId));

                if (existingCar == null)
                {
                    return NotFound();
                }

                car.OwnerId.Equals(ownerId);
                _context.Cars.Update(car);

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CarExists(id, ownerId))
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

            var ownerId = User.FindFirst("OwnerId")?.Value;
            var car = await _context.Cars
                .FirstOrDefaultAsync(c => c.CarId == id && c.OwnerId.Equals(ownerId));

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
            var ownerId = User.FindFirst("OwnerId")?.Value;
            var car = await _context.Cars
                .FirstOrDefaultAsync(c => c.CarId == id && c.OwnerId.Equals(ownerId));

            if (car == null)
            {
                return NotFound();
            }

            _context.Cars.Remove(car);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CarExists(int id, string ownerId)
        {
            return _context.Cars.Any(c => c.CarId == id && c.OwnerId.Equals(ownerId));
        }
    }
}
