using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace IDV_2.DTOs.FormTemplate;
public class UpdateFormTemplateRequest
{
    [MaxLength(255)]
    public string? Name { get; set; }

    [MaxLength(4000)]
    public string? Description { get; set; }

    public bool? IsActive { get; set; }

    public JsonElement? TemplateRules { get; set; }
}

