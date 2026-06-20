using System;
using System.Collections.Generic;

namespace CarRetalWebsite.Models;

public partial class BookingHistory
{
    public int HistoryId { get; set; }

    public int BookingId { get; set; }

    public int ChangedBy { get; set; }

    public string? OldStatus { get; set; }

    public string NewStatus { get; set; } = null!;

    public string? Note { get; set; }

    public DateTime ChangedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual User ChangedByNavigation { get; set; } = null!;
}
