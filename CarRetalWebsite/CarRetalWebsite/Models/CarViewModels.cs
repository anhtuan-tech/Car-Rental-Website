using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CarRetalWebsite.Models
{
    public class CarSearchViewModel
    {
        public string? Keyword { get; set; }
        public int? TypeId { get; set; }
        public string? Brand { get; set; }
        public string? Transmission { get; set; } // "Số tự động", "Số sàn"
        public string? Fuel { get; set; } // "Xăng", "Dầu", "Điện", "Hybrid"
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinRating { get; set; } // Lọc theo số sao tối thiểu (vd: >= 4 sao)

        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        public string SortBy { get; set; } = "newest"; // "newest", "price_asc", "price_desc", "rating_desc"
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 9;

        // Results
        public List<Car> Cars { get; set; } = new List<Car>();
        public List<CarType> CarTypes { get; set; } = new List<CarType>();
        public List<string> Brands { get; set; } = new List<string>();
        
        // Dictionary mapping CarId -> Rating Stats
        public Dictionary<int, CarRatingStats> CarRatings { get; set; } = new Dictionary<int, CarRatingStats>();

        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    }

    public class CarRatingStats
    {
        public double AverageRating { get; set; } = 5.0;
        public int ReviewCount { get; set; } = 0;
    }

    public class CarSpecsDetail
    {
        public int Seats { get; set; } = 5;
        public string Transmission { get; set; } = "Số tự động";
        public string Fuel { get; set; } = "Xăng";
        public string Consumption { get; set; } = "7 L/100km";
        public string Engine { get; set; } = "2.0L";
        public int Year { get; set; } = 2022;
        public List<string> Features { get; set; } = new List<string>();
    }

    public class CarDetailsViewModel
    {
        public Car Car { get; set; } = null!;
        public CarSpecsDetail Specs { get; set; } = new CarSpecsDetail();
        
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(2);
        
        public int TotalDays => Math.Max(1, (int)(EndDate - StartDate).TotalDays);
        public decimal SubtotalFee => TotalDays * Car.PricePerDay;
        public decimal PlatformCommission => SubtotalFee * 0.10m;
        public decimal OwnerPayout => SubtotalFee * 0.90m;

        // Feedback / Rating statistics
        public double AverageRating { get; set; } = 5.0;
        public int TotalReviews { get; set; } = 0;
        public int? SelectedRatingFilter { get; set; } // null/0: Tất cả, 1..5 sao
        public Dictionary<int, int> RatingBreakdown { get; set; } = new Dictionary<int, int>();
        public List<Feedback> Feedbacks { get; set; } = new List<Feedback>();

        // Related Cars
        public List<Car> RelatedCars { get; set; } = new List<Car>();
        
        // Disabled dates for datepicker (already booked periods)
        public List<BookedDateRange> BookedRanges { get; set; } = new List<BookedDateRange>();
    }

    public class BookedDateRange
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
