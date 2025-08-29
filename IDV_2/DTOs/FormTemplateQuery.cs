using System;

namespace IDV_2.DTOs.FormTemplate;
public class FormTemplateQuery
{
    public bool? IsActive { get; set; }
    public Guid? CreatedBy { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

