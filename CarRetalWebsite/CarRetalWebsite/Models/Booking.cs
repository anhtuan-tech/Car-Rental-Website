using System;
using System.Collections.Generic;

namespace CarRetalWebsite.Models;

public partial class Booking
{
    public int BookingId { get; set; }

    public int CustomerId { get; set; }

    public int CarId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int TotalDays { get; set; }

    public decimal SubtotalFee { get; set; }

    public decimal PlatformCommission { get; set; }

    public decimal OwnerPayout { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<BookingHistory> BookingHistories { get; set; } = new List<BookingHistory>();

    public virtual Car Car { get; set; } = null!;

    public virtual User Customer { get; set; } = null!;

    public virtual Feedback? Feedback { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
