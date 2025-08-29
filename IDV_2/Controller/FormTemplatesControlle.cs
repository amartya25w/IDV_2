
using IDV_2.DTOs;
using IDV_2.DTOs.FormTemplate;
using IDV_2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using IDV_2.Data;
using Microsoft.EntityFrameworkCore;


namespace IDV_2.Controller;

[ApiController]
[Route("api/form-templates")]
[Authorize]
public class FormTemplatesController : ControllerBase
{
    private readonly IFormTemplateService _service;
    private readonly ApplicationDbContext _db;
    public FormTemplatesController(IFormTemplateService service, ApplicationDbContext db)
    {
        _service = service;
        _db = db;
    }

    // POST: api/form-templates
    [HttpPost]
    public async Task<ActionResult<FormTemplateResponse>> Create(
        [FromBody] CreateFormTemplateRequest dto,
        CancellationToken ct)
    {
        var currentUserPublicId = await GetPublicIdFromIntAsync(ct);  
        var result = await _service.CreateAsync(dto, currentUserPublicId, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
    
    // GET: api/form-templates/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FormTemplateResponse>> GetById(Guid id, CancellationToken ct)
    {
        var x = await _service.GetAsync(id, ct);
        return x is null ? NotFound() : Ok(x);
    }

    // GET: api/form-templates
    [HttpGet]
    public async Task<ActionResult<PagedResult<FormTemplateResponse>>> List(
        [FromQuery] FormTemplateQuery q,
        CancellationToken ct)
    {
        var page = await _service.ListAsync(q, ct);
        return Ok(page);
    }

    // PUT: api/form-templates/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FormTemplateResponse>> Update(
        Guid id,
        [FromBody] UpdateFormTemplateRequest dto,
        CancellationToken ct)
    {
        var x = await _service.UpdateAsync(id, dto, ct);
        return x is null ? NotFound() : Ok(x);
    }

    // DELETE: api/form-templates/{id}  (soft-delete / archive)
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Archive(Guid id, CancellationToken ct)
    {
        var ok = await _service.ArchiveAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }
    /// <summary>
    /// Reads the integer user id from claims, looks up that user,
    /// and returns the user's Guid PublicId for FK usage.
    /// </summary>
    private async Task<Guid> GetPublicIdFromIntAsync(CancellationToken ct)
    {
        // read an int user id from the token (adjust claim type to your setup)
        var raw = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(raw, out var userId))
            throw new UnauthorizedAccessException("No valid int user id claim.");

        // fetch the GUID PublicId from DB (User.PublicId must exist)
        var publicId = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.PublicId)
            .FirstOrDefaultAsync(ct);

        if (publicId == Guid.Empty)
            throw new UnauthorizedAccessException("User has no PublicId.");

        return publicId;
    }
    private Guid GetCurrentUserIdOrThrow()
    {
        var v = User.FindFirst("sub")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(v, out var g)) return g;

        throw new UnauthorizedAccessException("No valid GUID user id in token.");
    }
}