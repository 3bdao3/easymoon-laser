using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Finance.Application.Journals;
using ErpClink.Modules.Finance.Application.Journals.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/finance/journals")]
public sealed class FinanceJournalsController : ControllerBase
{
    private readonly IJournalService _journals;

    public FinanceJournalsController(IJournalService journals) => _journals = journals;

    [HttpPost]
    [HasPermission(PermissionCodes.FinanceJournalsCreate)]
    public async Task<ActionResult<JournalEntryDto>> CreateDraft([FromBody] CreateJournalDraftRequest request, CancellationToken cancellationToken)
    {
        var dto = await _journals.CreateDraftAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.FinanceJournalsView)]
    public async Task<ActionResult<JournalEntryDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _journals.GetByIdAsync(id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet]
    [HasPermission(PermissionCodes.FinanceJournalsView)]
    public async Task<ActionResult<PagedJournalsResult>> Search([FromQuery] SearchJournalsRequest request, CancellationToken cancellationToken) =>
        Ok(await _journals.SearchAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.FinanceJournalsUpdate)]
    public async Task<ActionResult<JournalEntryDto>> UpdateDraft(Guid id, [FromBody] UpdateJournalDraftRequest request, CancellationToken cancellationToken) =>
        Ok(await _journals.UpdateDraftAsync(id, request, cancellationToken));

    [HttpPut("{id:guid}/lines")]
    [HasPermission(PermissionCodes.FinanceJournalsUpdate)]
    public async Task<ActionResult<JournalEntryDto>> ReplaceLines(Guid id, [FromBody] ReplaceJournalLinesRequest request, CancellationToken cancellationToken) =>
        Ok(await _journals.ReplaceLinesAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/post")]
    [HasPermission(PermissionCodes.FinanceJournalsPost)]
    public async Task<ActionResult<JournalEntryDto>> Post(Guid id, [FromBody] PostJournalRequest request, CancellationToken cancellationToken) =>
        Ok(await _journals.PostAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/reverse")]
    [HasPermission(PermissionCodes.FinanceJournalsReverse)]
    public async Task<ActionResult<JournalEntryDto>> Reverse(Guid id, [FromBody] ReverseJournalRequest request, CancellationToken cancellationToken) =>
        Ok(await _journals.ReverseAsync(id, request, cancellationToken));

    [HttpGet("{id:guid}/history")]
    [HasPermission(PermissionCodes.FinanceJournalsView)]
    public async Task<ActionResult<IReadOnlyList<JournalHistoryEntryDto>>> History(Guid id, CancellationToken cancellationToken) =>
        Ok(await _journals.GetHistoryAsync(id, cancellationToken));
}
