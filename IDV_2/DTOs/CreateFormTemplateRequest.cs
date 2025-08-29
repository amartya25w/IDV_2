using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace IDV_2.DTOs.FormTemplate;

public class CreateFormTemplateRequest
{
    [Required, MaxLength(255)]
    public string Name { get; set; } = default!;

    [MaxLength(4000)]
    public string? Description { get; set; }

    /// <summary>Optional JSON rules; supply any object which will be serialized to JSON.</summary>
    public JsonElement? TemplateRules { get; set; }
}
