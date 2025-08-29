using System;
using System.Security.Cryptography.X509Certificates;
using UserAuthAPI.Models;

namespace IDV_2.Models;

public class FormTemplate
{
    /// <summary>Primary key (GUID). Stored as CHAR(36) for readability.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Visible name of the template.</summary>
    public string Name { get; set; } = default!;

    /// <summary>Optional human description.</summary>
    public string? Description { get; set; }

    /// <summary>User id (GUID) of the creator/owner.</summary>
    public Guid CreatedBy { get; set; }

    public User? CreatedByUser { get; set; } = null!;

    /// <summary>JSON document holding global rules (timeouts, draft, etc.).</summary>
    /// <remarks>We store JSON as string and mark the column type as JSON in MySQL.</remarks>
    public string? TemplateRulesJson { get; set; }

    /// <summary>Whether the template is active/visible.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Creationd timestamp (UTC).</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
