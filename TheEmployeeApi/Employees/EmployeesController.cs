using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TheEmployeeApi.Employees;

public class EmployeesController(
    ApplicationDBContext context,
    ILogger<EmployeesController> logger)
    : BaseController
{
    /// <summary>
    /// Get all employees.
    /// </summary>
    /// <returns>An array of all employees.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GetEmployeeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllEmployees([FromQuery] GetAllEmployeesRequest? request)
    {
        int page = request?.Page ?? 1;
        int numberOfRecords = request?.RecordsPerPage ?? 100;

        IQueryable<Employee> query = context.Employees
            .Include(e => e.Benefits)
            .Skip((page - 1) * numberOfRecords)
            .Take(numberOfRecords);

        if (request != null)
        {
            if (!string.IsNullOrWhiteSpace(request.FirstNameContains))
            {
                query = query.Where(e => e.FirstName.Contains(request.FirstNameContains));
            }

            if (!string.IsNullOrWhiteSpace(request.LastNameContains))
            {
                query = query.Where(e => e.LastName.Contains(request.LastNameContains));
            }
        }

        var employees = await query.ToArrayAsync();

        return Ok(employees.Select(EmployeeToGetEmployeeResponse));
    }

    /// <summary>
    /// Gets an employee by ID.
    /// </summary>
    /// <param name="id">The ID of the employee.</param>
    /// <returns>The single employee record.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(GetEmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEmployeeById(int id)
    {
        var employee = await context.Employees.Include(e => e.Benefits).SingleOrDefaultAsync(e => e.Id == id);
        if (employee == null)
        {
            return NotFound();
        }
        var employeeResponse = EmployeeToGetEmployeeResponse(employee);
        return Ok(employeeResponse);
    }

    /// <summary>
    /// Creates a new employee.
    /// </summary>
    /// <param name="employeeRequest">The employee to be created.</param>
    /// <returns>A link to the employee that was created.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(GetEmployeeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest employeeRequest)
    {
        var newEmployee = new Employee
        {
            FirstName = employeeRequest.FirstName!,
            LastName = employeeRequest.LastName!,
            SocialSecurityNumber = employeeRequest.SocialSecurityNumber,
            Address1 = employeeRequest.Address1,
            Address2 = employeeRequest.Address2,
            City = employeeRequest.City,
            State = employeeRequest.State,
            ZipCode = employeeRequest.ZipCode,
            PhoneNumber = employeeRequest.PhoneNumber,
            Email = employeeRequest.Email
        };

        context.Employees.Add(newEmployee);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetEmployeeById), new { id = newEmployee.Id }, newEmployee);
    }

    /// <summary>
    /// Updates an employee.
    /// </summary>
    /// <param name="id">The ID of the employee to update.</param>
    /// <param name="employeeRequest">The employee data to update.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(GetEmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeRequest employeeRequest)
    {
        logger.LogInformation("Updating employee with ID: {EmployeeId}", id);
        var existingEmployee = await context.Employees.FindAsync(id);
        if (existingEmployee == null)
        {
            logger.LogWarning("Employee with ID: {EmployeeId} not found", id);
            return NotFound();
        }

        logger.LogDebug("Updating employee details for ID: {EmployeeId}", id);
        existingEmployee.Address1 = employeeRequest.Address1;
        existingEmployee.Address2 = employeeRequest.Address2;
        existingEmployee.City = employeeRequest.City;
        existingEmployee.State = employeeRequest.State;
        existingEmployee.ZipCode = employeeRequest.ZipCode;
        existingEmployee.PhoneNumber = employeeRequest.PhoneNumber;
        existingEmployee.Email = employeeRequest.Email;

        try
        {
            context.Entry(existingEmployee).State = EntityState.Modified;
            await context.SaveChangesAsync();
            logger.LogInformation("Employee with ID: {EmployeeId} successfully updated", id);
            return Ok(existingEmployee);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while updating employee with ID: {EmployeeId}", id);
            return StatusCode(500, "An error occurred while updating the employee");
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var employee = await context.Employees.FindAsync(id);

        if (employee == null)
        {
            return NotFound();
        }

        context.Employees.Remove(employee);
        await context.SaveChangesAsync();

        return NoContent();
    }
    
  /// <summary>
    /// Gets the benefits for an employee.
    /// </summary>
    /// <param name="employeeId">The ID to get the benefits for.</param>
    /// <returns>The benefits for that employee.</returns>
    [HttpGet("{employeeId}/benefits")]
    [ProducesResponseType(typeof(IEnumerable<GetEmployeeResponseEmployeeBenefit>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetBenefitsForEmployee(int employeeId)
    {
        var employee = await context.Employees
            .Include(e => e.Benefits)
            .ThenInclude(e => e.Benefit)
            .SingleOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null)
        {
            return NotFound();
        }

        var benefits = employee.Benefits.Select(b => new GetEmployeeResponseEmployeeBenefit
        {
            Id = b.Id,
            Name = b.Benefit.Name,
            Description = b.Benefit.Description,
            Cost = b.CostToEmployee ?? b.Benefit.BaseCost
        });

        return Ok(benefits);
    }

    private GetEmployeeResponse EmployeeToGetEmployeeResponse(Employee employee)
    {
        return new GetEmployeeResponse
        {
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Address1 = employee.Address1,
            Address2 = employee.Address2,
            City = employee.City,
            State = employee.State,
            ZipCode = employee.ZipCode,
            PhoneNumber = employee.PhoneNumber,
            Email = employee.Email,
        };
    }
}