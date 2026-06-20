using System;
using System.Collections.Generic;

namespace CarRetalWebsite.Models;

public partial class CarType
{
    public int TypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Car> Cars { get; set; } = new List<Car>();
}
