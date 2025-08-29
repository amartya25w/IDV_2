using IDV_2.DTOs.FormTemplate;
using IDV_2.DTOs;
using IDV_2.Models;

namespace IDV_2.Services;

public interface IFormTemplateService
{
    Task<FormTemplateResponse> CreateAsync(CreateFormTemplateRequest dto, Guid currentUserId, CancellationToken ct);
    Task<FormTemplateResponse?> GetAsync(Guid id, CancellationToken ct);
    Task<PagedResult<FormTemplateResponse>> ListAsync(FormTemplateQuery q, CancellationToken ct);
    Task<FormTemplateResponse?> UpdateAsync(Guid id, UpdateFormTemplateRequest dto, CancellationToken ct);
    Task<bool> ArchiveAsync(Guid id, CancellationToken ct); // sets IsActive=false
}