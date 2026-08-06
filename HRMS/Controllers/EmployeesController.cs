using HRMS.Authorization;
using HRMS.DTOs;
using HRMS.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Controllers;

[Route("api/employees")]
[ApiController]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ICurrentUserAccessor _currentUser;

    public EmployeesController(IEmployeeService employeeService, ICurrentUserAccessor currentUser)
    {
        _employeeService = employeeService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        if (_currentUser.IsAdmin)
        {
            var employees = await _employeeService.GetAllAsync(cancellationToken);
            return Ok(employees);
        }

        if (!_currentUser.EmployeeId.HasValue)
        {
            return Forbid();
        }

        var employee = await _employeeService.GetByIdAsync(_currentUser.EmployeeId.Value, cancellationToken);
        return employee == null ? NotFound() : Ok(new[] { employee });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var accessError = this.EnsureEmployeeAccess(_currentUser, id);
        if (accessError != null)
        {
            return accessError;
        }

        var employee = await _employeeService.GetByIdAsync(id, cancellationToken);
        return employee == null ? NotFound() : Ok(employee);
    }

    [HttpPost]
    [RequirePermission(HrmsPermissions.EmployeesCreate)]
    public async Task<IActionResult> Create([FromBody] EmployeeUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var (employee, error) = await _employeeService.CreateAsync(dto, cancellationToken);
        if (error != null)
        {
            return Conflict(error);
        }

        return Ok(employee);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] EmployeeUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (_currentUser.IsAdmin)
        {
            var permissionError = this.EnsurePermission(_currentUser, HrmsPermissions.EmployeesUpdate);
            if (permissionError != null)
            {
                return permissionError;
            }
        }
        else
        {
            var accessError = this.EnsureEmployeeAccess(_currentUser, id);
            if (accessError != null)
            {
                return accessError;
            }

            var permissionError = this.EnsurePermission(_currentUser, HrmsPermissions.EmployeesUpdateOwn);
            if (permissionError != null)
            {
                return permissionError;
            }
        }

        var (employee, error) = await _employeeService.UpdateAsync(id, dto, cancellationToken);
        if (employee == null && error == null)
        {
            return NotFound();
        }

        if (error != null)
        {
            return Conflict(error);
        }

        return Ok(employee);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(HrmsPermissions.EmployeesDelete)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _employeeService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
