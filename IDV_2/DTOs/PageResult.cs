using System.Collections.Generic;

namespace IDV_2.DTOs.FormTemplate;
public class PagedResult<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public long Total { get; set; }
    public IReadOnlyList<T> Items { get; set; } = [];
}
