using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.LaserClinic.Application.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/customers")]
public sealed class CustomersController : ControllerBase
{
    private readonly ICustomerAppService _customers;
    private readonly ICurrentUser _currentUser;

    public CustomersController(ICustomerAppService customers, ICurrentUser currentUser)
    {
        _customers = customers;
        _currentUser = currentUser;
    }

    [HttpGet]
    [HasPermission(PermissionCodes.LaserCustomersView)]
    public async Task<ActionResult<IReadOnlyList<CustomerListItemDto>>> Search(
        [FromQuery] string? q,
        [FromQuery] bool? isActive,
        [FromQuery] CustomerListSort sort = CustomerListSort.Name,
        CancellationToken cancellationToken = default)
        => Ok(await _customers.SearchAsync(q, isActive ?? true, sort, cancellationToken));

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.LaserCustomersView)]
    public async Task<ActionResult<CustomerDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _customers.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [HasPermission(PermissionCodes.LaserCustomersCreate)]
    public async Task<ActionResult<CustomerDto>> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _customers.CreateAsync(request, _currentUser.UserId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.LaserCustomersUpdate)]
    public async Task<ActionResult<CustomerDto>> Update(
        Guid id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken)
        => Ok(await _customers.UpdateAsync(id, request, _currentUser.UserId, cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [HasPermission(PermissionCodes.LaserCustomersUpdate)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await _customers.SetActiveAsync(id, true, _currentUser.UserId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(PermissionCodes.LaserCustomersUpdate)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _customers.SetActiveAsync(id, false, _currentUser.UserId, cancellationToken);
        return NoContent();
    }
}
