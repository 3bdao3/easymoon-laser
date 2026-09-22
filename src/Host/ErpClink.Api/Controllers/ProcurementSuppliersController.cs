using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Procurement.Application.Suppliers;
using ErpClink.Modules.Procurement.Application.Suppliers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/procurement/suppliers")]
public sealed class ProcurementSuppliersController : ControllerBase
{
    private readonly ISupplierService _suppliers;

    public ProcurementSuppliersController(ISupplierService suppliers) => _suppliers = suppliers;

    [HttpPost]
    [HasPermission(PermissionCodes.ProcurementSuppliersCreate)]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<SupplierDto>> Create(
        [FromBody] CreateSupplierRequest request,
        CancellationToken cancellationToken)
    {
        var supplier = await _suppliers.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, supplier);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.ProcurementSuppliersView)]
    public async Task<ActionResult<SupplierDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var supplier = await _suppliers.GetByIdAsync(id, cancellationToken);
        return supplier is null ? NotFound() : Ok(supplier);
    }

    [HttpGet("by-code/{supplierCode}")]
    [HasPermission(PermissionCodes.ProcurementSuppliersView)]
    public async Task<ActionResult<SupplierDto>> GetByCode(string supplierCode, CancellationToken cancellationToken)
    {
        var supplier = await _suppliers.GetByCodeAsync(supplierCode, cancellationToken);
        return supplier is null ? NotFound() : Ok(supplier);
    }

    [HttpGet("search")]
    [HasPermission(PermissionCodes.ProcurementSuppliersView)]
    public async Task<ActionResult<PagedSuppliersResult>> Search(
        [FromQuery] SearchSuppliersRequest request,
        CancellationToken cancellationToken)
        => Ok(await _suppliers.SearchAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.ProcurementSuppliersUpdate)]
    public async Task<ActionResult<SupplierDto>> Update(
        Guid id,
        [FromBody] UpdateSupplierRequest request,
        CancellationToken cancellationToken)
        => Ok(await _suppliers.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [HasPermission(PermissionCodes.ProcurementSuppliersActivate)]
    public async Task<IActionResult> Activate(
        Guid id,
        [FromQuery] byte[]? rowVersion,
        CancellationToken cancellationToken)
    {
        await _suppliers.ActivateAsync(id, rowVersion, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(PermissionCodes.ProcurementSuppliersDeactivate)]
    public async Task<IActionResult> Deactivate(
        Guid id,
        [FromQuery] byte[]? rowVersion,
        CancellationToken cancellationToken)
    {
        await _suppliers.DeactivateAsync(id, rowVersion, cancellationToken);
        return NoContent();
    }
}
