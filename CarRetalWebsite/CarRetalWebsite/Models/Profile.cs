using System;
using System.Collections.Generic;

namespace CarRetalWebsite.Models;

public partial class Profile
{
    public int ProfileId { get; set; }

    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public string? DriverLicenseNo { get; set; }

    public string? IdCardNo { get; set; }

    public string? Address { get; set; }

    public virtual User User { get; set; } = null!;
}
