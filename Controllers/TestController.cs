using Microsoft.AspNetCore.Mvc;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace keyvaultdemo.Controllers;

[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<TestController> _logger;
    private readonly IConfiguration _configuration;

    public TestController(ILogger<TestController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    // [HttpGet(Name = "GetWeatherForecast")]
    // public IEnumerable<WeatherForecast> Get()
    // {

    //     return Enumerable.Range(1, 5).Select(index => new WeatherForecast
    //     {
    //         Date = DateTime.Now.AddDays(index),
    //         TemperatureC = Random.Shared.Next(-20, 55),
    //         Summary = Summaries[Random.Shared.Next(Summaries.Length)]
    //     })
    //     .ToArray();
    // }

    [NonAction]
    public string getSecret(string key)
    {
        var UserManagedClientId = "763cd1ab-f703-459b-b1b0-38e988f1ccb3";
        DefaultAzureCredential defaultAzureCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = UserManagedClientId });
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
        var client = new SecretClient(new Uri("https://deepukeyvault.vault.azure.net/"),
                                        defaultAzureCredential,
                                        options);

        KeyVaultSecret keyvaultsecret = client.GetSecret(key);

        string secretValue = keyvaultsecret.Value;
        return secretValue;
    }
    [HttpGet("secret/{secret}")]
    public IActionResult Get(string secret)
    {
        string secretValue = getSecret(secret);
        return Ok(secretValue);
    }

    [HttpGet]
    [Route("token")]
    public ActionResult GetToken()
    {
        // Build request to acquire managed identities for Azure resources token
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource=https://management.azure.com/");
        request.Headers["Metadata"] = "true";
        request.Method = "GET";
        string accessToken = null;

        try
        {
            // Call /token endpoint
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            // Pipe response Stream to a StreamReader, and extract access token
            StreamReader streamResponse = new StreamReader(response.GetResponseStream());
            string stringResponse = streamResponse.ReadToEnd();
            Dictionary<string, string> list = (Dictionary<string, string>)JsonConvert.DeserializeObject(stringResponse, typeof(Dictionary<string, string>));
            // JavaScriptSerializer j = new JavaScriptSerializer();
            // Dictionary<string, string> list = (Dictionary<string, string>)j.Deserialize(stringResponse, typeof(Dictionary<string, string>));
            accessToken = list["access_token"];
        }
        catch (Exception e)
        {
            string errorText = String.Format("{0} \n\n{1}", e.Message, e.InnerException != null ? e.InnerException.Message : "Acquire token failed");
        }
        return Ok(accessToken);
    }

    [HttpGet("appconfig")]
    public ActionResult GetAppConfiguration(string key)
    {
        var builder = new ConfigurationBuilder();
        string connectionString = getSecret("AppConfigConnection");
        builder.AddAzureAppConfiguration(connectionString);
        var config = builder.Build();
        bool flag = config[".appconfig.featureflag/isTrue"].Contains("\"enabled\":true");
        if(Convert.ToBoolean(flag)){
            return Ok(config[key+"True"]);
        }
        else{
            return Ok(config[key+"False"]);
        }

        
    }
}
