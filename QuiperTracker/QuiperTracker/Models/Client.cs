using System;
using System.Collections.Generic;

namespace QuiperTracker.Models;

public partial class Client
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Status { get; set; } = null!;
}
