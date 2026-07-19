using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarRetalWebsite.Models;

namespace CarRetalWebsite.Controllers
{
    public class CarController : Controller
    {
        private readonly CarRentalDbContext _context;

        public CarController(CarRentalDbContext context)
        {
            _context = context;
        }

        // ====================================================================
        // UC-10: SEARCH RENTAL CARS (Guest, Customer)
        // ====================================================================
        [HttpGet]
        public async Task<IActionResult> Index(CarSearchViewModel search)
        {
            // Sanitize & set defaults
            search.Page = search.Page < 1 ? 1 : search.Page;
            search.PageSize = 9;

            // Validate date range
            if (search.StartDate.HasValue && search.StartDate.Value.Date < DateTime.Today)
            {
                search.StartDate = DateTime.Today;
            }
            if (search.StartDate.HasValue && (!search.EndDate.HasValue || search.EndDate.Value.Date < search.StartDate.Value.Date))
            {
                search.EndDate = search.StartDate.Value.AddDays(2);
            }

            // Validate price range
            if (search.MinPrice.HasValue && search.MinPrice.Value < 0)
            {
                search.MinPrice = null;
            }
            if (search.MaxPrice.HasValue && search.MaxPrice.Value < 0)
            {
                search.MaxPrice = null;
            }
            if (search.MinPrice.HasValue && search.MaxPrice.HasValue && search.MaxPrice.Value < search.MinPrice.Value)
            {
                var temp = search.MinPrice;
                search.MinPrice = search.MaxPrice;
                search.MaxPrice = temp;
            }

            // Query only available cars
            var query = _context.Cars
                .Include(c => c.CarImages)
                .Include(c => c.Type)
                .Where(c => c.Status == "Available");

            // Filter Keyword
            if (!string.IsNullOrWhiteSpace(search.Keyword))
            {
                var kw = search.Keyword.Trim().ToLower();
                query = query.Where(c => 
                    c.CarName.ToLower().Contains(kw) || 
                    c.Brand.ToLower().Contains(kw) || 
                    c.Model.ToLower().Contains(kw) ||
                    c.Type.TypeName.ToLower().Contains(kw));
            }

            // Filter CarType
            if (search.TypeId.HasValue && search.TypeId.Value > 0)
            {
                query = query.Where(c => c.TypeId == search.TypeId.Value);
            }

            // Filter Brand
            if (!string.IsNullOrWhiteSpace(search.Brand))
            {
                query = query.Where(c => c.Brand.ToLower() == search.Brand.Trim().ToLower());
            }

            // Filter Price Range
            if (search.MinPrice.HasValue)
            {
                query = query.Where(c => c.PricePerDay >= search.MinPrice.Value);
            }
            if (search.MaxPrice.HasValue)
            {
                query = query.Where(c => c.PricePerDay <= search.MaxPrice.Value);
            }

            // Filter Transmission inside SpecsJson
            if (!string.IsNullOrWhiteSpace(search.Transmission))
            {
                var trans = search.Transmission.Trim();
                query = query.Where(c => c.SpecsJson != null && c.SpecsJson.Contains(trans));
            }

            // Filter Fuel inside SpecsJson
            if (!string.IsNullOrWhiteSpace(search.Fuel))
            {
                var fuel = search.Fuel.Trim();
                query = query.Where(c => c.SpecsJson != null && c.SpecsJson.Contains(fuel));
            }

            // Filter by Minimum Rating if requested
            if (search.MinRating.HasValue && search.MinRating.Value >= 1 && search.MinRating.Value <= 5)
            {
                var minR = search.MinRating.Value;
                query = query.Where(c => _context.Feedbacks
                    .Where(f => f.Booking.CarId == c.CarId)
                    .Average(f => (double?)f.Rating) >= minR);
            }

            // Filter by Date Availability
            if (search.StartDate.HasValue && search.EndDate.HasValue)
            {
                var start = search.StartDate.Value.Date;
                var end = search.EndDate.Value.Date;

                // Find car IDs that have conflicting active bookings
                var bookedCarIds = await _context.Bookings
                    .Where(b => (b.Status == "Pending" || b.Status == "Approved" || b.Status == "Active") &&
                                b.StartDate.Date <= end && b.EndDate.Date >= start)
                    .Select(b => b.CarId)
                    .Distinct()
                    .ToListAsync();

                query = query.Where(c => !bookedCarIds.Contains(c.CarId));
            }

            // Sorting
            switch (search.SortBy?.ToLower())
            {
                case "price_asc":
                    query = query.OrderBy(c => c.PricePerDay);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(c => c.PricePerDay);
                    break;
                case "rating_desc":
                    query = query.OrderByDescending(c => _context.Feedbacks
                        .Where(f => f.Booking.CarId == c.CarId)
                        .Average(f => (double?)f.Rating) ?? 5.0);
                    break;
                case "newest":
                default:
                    query = query.OrderByDescending(c => c.CarId);
                    search.SortBy = "newest";
                    break;
            }

            // Total count for pagination
            search.TotalItems = await query.CountAsync();

            // Sanitize page upper bound
            if (search.TotalPages > 0 && search.Page > search.TotalPages)
            {
                search.Page = search.TotalPages;
            }

            // Execute paginated query
            search.Cars = await query
                .Skip((search.Page - 1) * search.PageSize)
                .Take(search.PageSize)
                .ToListAsync();

            // Load rating statistics for cars on current page
            var pageCarIds = search.Cars.Select(c => c.CarId).ToList();
            if (pageCarIds.Any())
            {
                var ratingData = await _context.Feedbacks
                    .Where(f => pageCarIds.Contains(f.Booking.CarId))
                    .GroupBy(f => f.Booking.CarId)
                    .Select(g => new
                    {
                        CarId = g.Key,
                        Avg = Math.Round(g.Average(f => f.Rating), 1),
                        Count = g.Count()
                    })
                    .ToListAsync();

                foreach (var r in ratingData)
                {
                    search.CarRatings[r.CarId] = new CarRatingStats
                    {
                        AverageRating = r.Avg,
                        ReviewCount = r.Count
                    };
                }
            }

            // Load CarTypes and distinct Brands for filter options
            search.CarTypes = await _context.CarTypes.ToListAsync();
            search.Brands = await _context.Cars
                .Where(c => c.Status == "Available")
                .Select(c => c.Brand)
                .Distinct()
                .OrderBy(b => b)
                .ToListAsync();

            return View(search);
        }

        // ====================================================================
        // UC-11: VIEW CAR REVIEWS (Guest, Customer)
        // UC-09: VIEW RENTAL CAR DETAILS (Guest, Customer)
        // ====================================================================
        [HttpGet]
        public async Task<IActionResult> Details(int id, DateTime? startDate, DateTime? endDate, int? ratingFilter)
        {
            var car = await _context.Cars
                .Include(c => c.CarImages)
                .Include(c => c.Type)
                .Include(c => c.Owner)
                .ThenInclude(o => o.Profile)
                .FirstOrDefaultAsync(c => c.CarId == id);

            if (car == null)
            {
                return NotFound("Không tìm thấy thông tin xe yêu cầu.");
            }

            // Check dates logic
            var start = startDate.HasValue && startDate.Value.Date >= DateTime.Today 
                ? startDate.Value.Date 
                : DateTime.Today;
            var end = endDate.HasValue && endDate.Value.Date >= start 
                ? endDate.Value.Date 
                : start.AddDays(2);

            // Parse specs_json
            var specsDetail = ParseSpecsJson(car.SpecsJson);

            // Fetch Feedbacks for this car via Bookings
            var allFeedbacks = await _context.Feedbacks
                .Include(f => f.Customer)
                .ThenInclude(c => c.Profile)
                .Include(f => f.Booking)
                .Where(f => f.Booking.CarId == id)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            // Calculate overall rating stats
            double avgRating = 5.0;
            int totalReviews = allFeedbacks.Count;
            var ratingBreakdown = new Dictionary<int, int>
            {
                { 5, 0 }, { 4, 0 }, { 3, 0 }, { 2, 0 }, { 1, 0 }
            };

            if (totalReviews > 0)
            {
                avgRating = Math.Round(allFeedbacks.Average(f => f.Rating), 1);
                foreach (var fb in allFeedbacks)
                {
                    if (ratingBreakdown.ContainsKey(fb.Rating))
                    {
                        ratingBreakdown[fb.Rating]++;
                    }
                }
            }

            // Filter feedbacks list if ratingFilter is specified
            var displayedFeedbacks = allFeedbacks;
            if (ratingFilter.HasValue && ratingFilter.Value >= 1 && ratingFilter.Value <= 5)
            {
                displayedFeedbacks = allFeedbacks.Where(f => f.Rating == ratingFilter.Value).ToList();
            }

            // Fetch booked date ranges for calendar widget
            var activeBookings = await _context.Bookings
                .Where(b => b.CarId == id && (b.Status == "Pending" || b.Status == "Approved" || b.Status == "Active"))
                .Select(b => new BookedDateRange
                {
                    StartDate = b.StartDate,
                    EndDate = b.EndDate
                })
                .ToListAsync();

            // Fetch Related Cars (same type or brand)
            var relatedCars = await _context.Cars
                .Include(c => c.CarImages)
                .Include(c => c.Type)
                .Where(c => c.Status == "Available" && c.CarId != id && (c.TypeId == car.TypeId || c.Brand == car.Brand))
                .Take(3)
                .ToListAsync();

            var viewModel = new CarDetailsViewModel
            {
                Car = car,
                Specs = specsDetail,
                StartDate = start,
                EndDate = end,
                AverageRating = avgRating,
                TotalReviews = totalReviews,
                SelectedRatingFilter = ratingFilter,
                RatingBreakdown = ratingBreakdown,
                Feedbacks = displayedFeedbacks,
                RelatedCars = relatedCars,
                BookedRanges = activeBookings
            };

            return View(viewModel);
        }

        private CarSpecsDetail ParseSpecsJson(string? specsJson)
        {
            var specs = new CarSpecsDetail();
            if (string.IsNullOrWhiteSpace(specsJson))
            {
                specs.Features = new List<string> { "Bản đồ GPS", "Bluetooth", "Camera lùi", "Túi khí an toàn", "Phanh ABS" };
                return specs;
            }

            try
            {
                // Try parsing JSON if formatted properly
                using var doc = JsonDocument.Parse(specsJson);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("seats", out var seatsProp) && seatsProp.TryGetInt32(out var seats)) specs.Seats = seats;
                    if (root.TryGetProperty("transmission", out var transProp)) specs.Transmission = transProp.GetString() ?? specs.Transmission;
                    if (root.TryGetProperty("fuel", out var fuelProp)) specs.Fuel = fuelProp.GetString() ?? specs.Fuel;
                    if (root.TryGetProperty("consumption", out var consProp)) specs.Consumption = consProp.GetString() ?? specs.Consumption;
                    if (root.TryGetProperty("engine", out var engProp)) specs.Engine = engProp.GetString() ?? specs.Engine;
                    if (root.TryGetProperty("year", out var yearProp) && yearProp.TryGetInt32(out var yr)) specs.Year = yr;
                    if (root.TryGetProperty("features", out var featProp) && featProp.ValueKind == JsonValueKind.Array)
                    {
                        specs.Features = featProp.EnumerateArray().Select(f => f.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
                    }
                }
            }
            catch
            {
                // Simple text string parsing fallback if specsJson is plain text string
                if (specsJson.Contains("Số sàn")) specs.Transmission = "Số sàn";
                if (specsJson.Contains("Dầu") || specsJson.Contains("Diesel")) specs.Fuel = "Dầu";
                if (specsJson.Contains("Điện")) specs.Fuel = "Điện";
                
                specs.Features = new List<string> { "Bản đồ GPS", "Bluetooth", "Camera lùi", "Túi khí an toàn", "Cảm biến đỗ xe" };
            }

            if (specs.Features.Count == 0)
            {
                specs.Features = new List<string> { "Bản đồ GPS", "Bluetooth", "Camera lùi", "Túi khí an toàn" };
            }

            return specs;
        }
    }
}
