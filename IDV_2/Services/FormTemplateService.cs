using IDV_2.Data;
using IDV_2.DTOs;
using IDV_2.DTOs.FormTemplate;
using IDV_2.Models;
using IDV_2.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Json;

namespace IDV_2.Services;

public class FormTemplateService : IFormTemplateService
{
    private readonly ApplicationDbContext _db;

    public FormTemplateService(ApplicationDbContext db) => _db = db;

    public async Task<FormTemplateResponse> CreateAsync(CreateFormTemplateRequest dto, Guid currentUserId, CancellationToken ct)
    {
        var entity = new FormTemplate
        {
            Name = dto.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description!.Trim(),
            CreatedBy = currentUserId,
            TemplateRulesJson = dto.TemplateRules is null ? null : JsonSerializer.Serialize(dto.TemplateRules.Value)
        };

        _db.FormTemplates.Add(entity);
        await _db.SaveChangesAsync(ct);

        return ToResponse(entity);
    }

    public async Task<FormTemplateResponse?> GetAsync(Guid id, CancellationToken ct)
    {
        var entity = await _db.FormTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return entity is null ? null : ToResponse(entity);
    }

    public async Task<PagedResult<FormTemplateResponse>> ListAsync(FormTemplateQuery q, CancellationToken ct)
    {
        var query = _db.FormTemplates.AsNoTracking().AsQueryable();

        if (q.IsActive is not null)
            query = query.Where(t => t.IsActive == q.IsActive);

        if (q.CreatedBy is not null)
            query = query.Where(t => t.CreatedBy == q.CreatedBy);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(t => t.Name.Contains(s) || (t.Description != null && t.Description.Contains(s)));
        }

        var total = await query.LongCountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync(ct);

        return new PagedResult<FormTemplateResponse>
        {
            Page = q.Page,
            PageSize = q.PageSize,
            Total = total,
            Items = items.Select(ToResponse).ToList()
        };
    }

    public async Task<FormTemplateResponse?> UpdateAsync(Guid id, UpdateFormTemplateRequest dto, CancellationToken ct)
    {
        var entity = await _db.FormTemplates.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return null;

        if (dto.Name is not null) entity.Name = dto.Name.Trim();
        if (dto.Description is not null) entity.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        if (dto.IsActive is not null) entity.IsActive = dto.IsActive.Value;
        if (dto.TemplateRules is not null) entity.TemplateRulesJson = JsonSerializer.Serialize(dto.TemplateRules.Value);

        await _db.SaveChangesAsync(ct);
        return ToResponse(entity);
    }

    public async Task<bool> ArchiveAsync(Guid id, CancellationToken ct)
    {
        var entity = await _db.FormTemplates.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return false;
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static FormTemplateResponse ToResponse(FormTemplate e)
    {
        JsonElement? rules = null;
        if (!string.IsNullOrWhiteSpace(e.TemplateRulesJson))
        {
            using var doc = JsonDocument.Parse(e.TemplateRulesJson!);
            rules = doc.RootElement.Clone();
        }

        return new FormTemplateResponse
        {
            Id = e.Id,
            Name = e.Name,
            Description = e.Description,
            CreatedBy = e.CreatedBy,
            TemplateRules = rules,
            IsActive = e.IsActive,
            CreatedAtUtc = e.CreatedAtUtc
        };
    }
}
