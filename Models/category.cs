using System;
using System.Collections.Generic;

namespace StoreAPI.Models;

public partial class category
{
    public int category_id { get; set; }

    public string category_name { get; set; } = string.Empty;

    public int category_status { get; set; }

    public virtual ICollection<product> products { get; set; } = new List<product>();
}
