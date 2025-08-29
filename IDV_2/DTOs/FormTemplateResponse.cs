using System;
using System.Text.Json;

namespace IDV_2.DTOs.FormTemplate;

public class FormTemplateResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public Guid CreatedBy { get; set; }
    public JsonElement? TemplateRules { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

