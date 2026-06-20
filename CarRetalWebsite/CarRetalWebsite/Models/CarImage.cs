using System;
using System.Collections.Generic;

namespace CarRetalWebsite.Models;

public partial class CarImage
{
    public int ImageId { get; set; }

    public int CarId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public bool IsPrimary { get; set; }

    public virtual Car Car { get; set; } = null!;
}
