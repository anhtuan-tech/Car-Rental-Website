using System;
using System.Collections.Generic;

namespace CarRetalWebsite.Models;

public partial class User
{
    public int UserId { get; set; }

    public int RoleId { get; set; }

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<BookingHistory> BookingHistories { get; set; } = new List<BookingHistory>();

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<Car> Cars { get; set; } = new List<Car>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual Profile? Profile { get; set; }

    public virtual Role Role { get; set; } = null!;
}
