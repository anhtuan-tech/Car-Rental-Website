using System;
using System.Collections.Generic;

namespace CarRetalWebsite.Models;

public partial class Feedback
{
    public int FeedbackId { get; set; }

    public int BookingId { get; set; }

    public int CustomerId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public string? OwnerReply { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual User Customer { get; set; } = null!;
}
