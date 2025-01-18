using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TheEmployeeAPI.Tests;

public class BasicTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetAllEmployees_ReturnsOkResult()
    {
        // Arrange
        var client = factory.CreateClient();
        // Act
        var response = await client.GetAsync("/employees");
        // Assert
        response.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task GetEmployeeById_ReturnsOkResult()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/employees/1");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateEmployee_ReturnsCreatedResult()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/employees", new Employee { FirstName = "John", LastName = "Doe" });

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateEmployee_ReturnsBadRequestResult()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/employees", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
