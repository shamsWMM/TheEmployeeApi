using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using TheEmployeeApi.Employees;

namespace TheEmployeeApi.Tests;

public class BasicTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GetAllEmployees_ReturnsOkResult()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/employees");

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to get employees: {content}");
        }

        var employees = await response.Content.ReadFromJsonAsync<IEnumerable<GetEmployeeResponse>>();
        Assert.NotEmpty(employees);
    }

    [Fact]
    public async Task GetAllEmployees_WithFilter_ReturnsOneResult()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/employees?FirstNameContains=Jan");

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Failed to get employees: {content}");
        }

        var employees = await response.Content.ReadFromJsonAsync<IEnumerable<GetEmployeeResponse>>();
        Assert.Single(employees);
    }

    [Fact]
    public async Task GetEmployeeById_ReturnsOkResult()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/employees/2");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateEmployee_ReturnsCreatedResult()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/employees", new Employee { FirstName = "Johnathon", LastName = "Doe" });

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateEmployee_ReturnsBadRequestResult()
    {
        // Arrange
        var client = factory.CreateClient();
        var invalidEmployee = new CreateEmployeeRequest(); // Empty object to trigger validation errors

        // Act
        var response = await client.PostAsJsonAsync("/employees", invalidEmployee);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Contains("FirstName", problemDetails.Errors.Keys);
        Assert.Contains("LastName", problemDetails.Errors.Keys);
        Assert.Contains("'First Name' must not be empty.", problemDetails.Errors["FirstName"]);
        Assert.Contains("'Last Name' must not be empty.", problemDetails.Errors["LastName"]);
    }

    [Fact]
    public async Task UpdateEmployee_ReturnsOkResult()
    {
        var client = factory.CreateClient();
        var response = await client
            .PutAsJsonAsync("/employees/2", new Employee
            {
                FirstName = "John",
                LastName = "Doe",
                Address1 = "23 Main Smot"
            });

        response.EnsureSuccessStatusCode();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        var employee = await db.Employees.FindAsync(2);
        Assert.Equal("23 Main Smot", employee.Address1);
    }

    [Fact]
    public async Task UpdateEmployee_ReturnsBadRequestWhenAddress()
    {
        // Arrange
        var client = factory.CreateClient();
        var invalidEmployee = new UpdateEmployeeRequest(); // Empty object to trigger validation errors

        // Act
        var response = await client.PutAsJsonAsync($"/employees/2", invalidEmployee);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Contains("Address1", problemDetails.Errors.Keys);
    }

    [Fact]
    public async Task DeleteEmployee_ReturnsNoContentResult()
    {
        var client = factory.CreateClient();

        var newEmployee = new Employee { FirstName = "Meow", LastName = "Garita" };
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            db.Employees.Add(newEmployee);
            await db.SaveChangesAsync();
        }

        var response = await client.DeleteAsync($"/employees/{newEmployee.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEmployee_ReturnsNotFoundResult()
    {
        var client = factory.CreateClient();
        var response = await client.DeleteAsync("/employees/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBenefitsForEmployee_ReturnsOkResult()
    {
        // Act
        var client = factory.CreateClient();
        var response = await client.GetAsync("/employees/2/benefits");

        // Assert
        response.EnsureSuccessStatusCode();
        var benefits = await response.Content.ReadFromJsonAsync<IEnumerable<GetEmployeeResponseEmployeeBenefit>>();
        Assert.NotNull(benefits);
        Assert.Equal(2, benefits.Count());
    }
}
