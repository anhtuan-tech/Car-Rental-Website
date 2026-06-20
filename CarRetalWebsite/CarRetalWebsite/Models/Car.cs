using System;
using System.Collections.Generic;

namespace CarRetalWebsite.Models;

public partial class Car
{
    public int CarId { get; set; }

    public int OwnerId { get; set; }

    public int TypeId { get; set; }

    public string CarName { get; set; } = null!;

    public string Brand { get; set; } = null!;

    public string Model { get; set; } = null!;

    public string LicensePlate { get; set; } = null!;

    public string? SpecsJson { get; set; }

    public decimal PricePerDay { get; set; }

    public string Status { get; set; } = null!;

    public string? DocumentUrl { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<CarImage> CarImages { get; set; } = new List<CarImage>();

    public virtual User Owner { get; set; } = null!;

    public virtual CarType Type { get; set; } = null!;
}
