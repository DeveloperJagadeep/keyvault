using Microsoft.AspNetCore.Mvc;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Core;

namespace keyvaultdemo.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {

        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }

    [HttpGet("secret/{secret}")]
    public IActionResult Get(string secret)
    {
        var UserManagedClientId = "<PUT CLIENTID HERE>";
        SecretClientOptions options = new SecretClientOptions()
        {
            Retry =
        {
            Delay= TimeSpan.FromSeconds(2),
            MaxDelay = TimeSpan.FromSeconds(16),
            MaxRetries = 5,
            Mode = RetryMode.Exponential
         }
        };
        var client = new SecretClient(new Uri("https://deepurgkeyvault.vault.azure.net/"),
         new DefaultAzureCredential(new DefaultAzureCredentialOptions {ManagedIdentityClientId = UserManagedClientId}), options);

        KeyVaultSecret keyvaultsecret = client.GetSecret(secret);

        string secretValue = keyvaultsecret.Value;
        return Ok(secretValue);
    }
}
