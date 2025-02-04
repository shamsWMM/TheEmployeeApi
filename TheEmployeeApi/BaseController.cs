using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace TheEmployeeApi;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public abstract class BaseController : Controller
{
}