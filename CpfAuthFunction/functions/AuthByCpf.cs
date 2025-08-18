using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CpfAuthFunction.functions;

public class AuthByCpf
{
    private readonly ILogger<AuthByCpf> _logger;

    public AuthByCpf(ILogger<AuthByCpf> logger)
    {
        _logger = logger;
    }

    [Function("AuthByCpf")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}
