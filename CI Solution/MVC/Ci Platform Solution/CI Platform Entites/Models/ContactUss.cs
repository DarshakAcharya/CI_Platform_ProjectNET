using System;
using System.Collections.Generic;

namespace CI_Platform_Entites.Models;

public partial class ContactUss
{
    public long? UserId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    public string? Subject { get; set; }

    public string? Message { get; set; }

    public DateTime? QcreatedAt { get; set; }

    public long ContactUsId { get; set; }
}
