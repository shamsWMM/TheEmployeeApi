using FluentValidation;

namespace TheEmployeeApi.Employees;

public class UpdateEmployeeRequest
{
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}
 public class UpdateEmployeeRequestValidator : AbstractValidator<UpdateEmployeeRequest>
{
    private readonly HttpContext _httpContext;
    private readonly ApplicationDBContext _context;

    public UpdateEmployeeRequestValidator(IHttpContextAccessor httpContextAccessor, ApplicationDBContext context)
    {
        _httpContext = httpContextAccessor.HttpContext!;
        _context = context;

        RuleFor(x => x.Address1)
            .MustAsync(NotBeEmptyIfItIsSetOnEmployeeAlreadyAsync)
            .WithMessage("Address1 must not be empty.");
    }

    private async Task<bool> NotBeEmptyIfItIsSetOnEmployeeAlreadyAsync(string? address, CancellationToken token)
    {
        var id = Convert.ToInt32(_httpContext.Request.RouteValues["id"]);
        var employee = await _context.Employees.FindAsync(id);

        if (employee!.Address1 != null && string.IsNullOrWhiteSpace(address))
        {
            return false;
        }

        return true;
    }
}