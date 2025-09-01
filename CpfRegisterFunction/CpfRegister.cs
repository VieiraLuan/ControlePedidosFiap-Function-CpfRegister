using CpfRegisterFunction.DTOs.Input;
using CpfRegisterFunction.DTOs.Output;
using CpfRegisterFunction.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CpfRegisterFunction;

public class CpfRegister
{
    private readonly ILogger<CpfRegister> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public CpfRegister(ILogger<CpfRegister> logger, HttpClient httpClient, IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClient;
        _config = configuration;
    }

    [Function("CpfRegister")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        var response = req.CreateResponse();
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");

        try
        {
            _logger.LogInformation("Starting CPF registration process...");

            var accessToken = await GetAccessTokenAsync();
            var userAccount = await ParseRequestPayloadAsync(req.Body);
            var apiResponse = await AddNewCustomerAsync(userAccount, accessToken);
            var genericResponse = await BuildGenericResponse(apiResponse);

            await response.WriteStringAsync(JsonSerializer.Serialize(genericResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during CPF registration process");

            var errorResponse = new GenericResponseDTO
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Message = "An error occurred while processing your request.",
                Details = ex.Message
            };

            await response.WriteStringAsync(JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })).ConfigureAwait(false);

            return response;
        }
    }


    private static async Task<GenericResponseDTO> BuildGenericResponse(HttpResponseMessage result)
    {
        var isSuccess = result.IsSuccessStatusCode;
        var content = await result.Content.ReadAsStringAsync();

        var createdUserId = "";


        if (!string.IsNullOrWhiteSpace(content))
        {
            try
            {
                using var doc = JsonDocument.Parse(content);

                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("id", out var idProp))
                {
                    createdUserId = idProp.GetString();
                }
            }
            catch (JsonException ex)
            {
                createdUserId = $"Error parsing ID: {ex.Message}";
            }
        }


        return new GenericResponseDTO
        {
            StatusCode = result.StatusCode,
            Message = isSuccess ? "CPF registered successfully." : "Failed to register CPF.",
            Details = isSuccess ? "User Account Created" : content,
            UserId = isSuccess ? createdUserId : "Empty"

        };
    }



    private async Task<UserAccount> ParseRequestPayloadAsync(Stream body)
    {
        using var reader = new StreamReader(body);
        var bodyString = await reader.ReadToEndAsync().ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(bodyString))
            throw new ArgumentException("Request body cannot be empty.");

        _logger.LogInformation("Received payload: {Payload}", bodyString);

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var customer = JsonSerializer.Deserialize<CustomerRequestDTO>(bodyString, options)
            ?? throw new ArgumentException("Invalid payload: Could not deserialize CustomerRequestDTO.");

        if (string.IsNullOrWhiteSpace(customer.Name))
            throw new ArgumentException("Invalid value for Customer Name.");

        if (string.IsNullOrWhiteSpace(customer.Cpf))
            throw new ArgumentException("Invalid value for Customer CPF.");

        return new UserAccount
        {
            DisplayName = customer.Name,
            MailNickname = customer.Cpf
        }.WithUserPrincipalName(customer.Name);
    }

    private async Task<HttpResponseMessage> AddNewCustomerAsync(UserAccount customer, string token)
    {
        if (customer == null)
            throw new ArgumentNullException(nameof(customer), "Customer account cannot be null.");

        var apiUrl = _config["ADD_USER_URL"];
        if (string.IsNullOrWhiteSpace(apiUrl))
            throw new Exception("Invalid configuration: ADD_USER_URL is not set.");

        var json = JsonSerializer.Serialize(customer, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        _logger.LogInformation($"New customer: {json}");

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, apiUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        _logger.LogInformation("Sending request to {ApiUrl}", apiUrl);

        var response = await _httpClient.SendAsync(requestMessage).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        _logger.LogInformation("API Response: {Response}", content);

        return response;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var tenantId = _config["TENANT_ID"];
        var clientId = _config["CLIENT_ID"];
        var clientSecret = _config["CLIENT_SECRET"];
        var scope = _config["SCOPE"];
        var grantType = _config["GRANT_TYPE"];

        if (string.IsNullOrWhiteSpace(tenantId) ||
            string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecret) ||
            string.IsNullOrWhiteSpace(scope) ||
            string.IsNullOrWhiteSpace(grantType))
        {
            throw new Exception("Invalid configuration: One or more required values are missing (TENANT_ID, CLIENT_ID, CLIENT_SECRET, SCOPE, GRANT_TYPE).");
        }

        var url = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("scope", scope),
            new KeyValuePair<string, string>("grant_type", grantType)
        });

        var response = await _httpClient.PostAsync(url, content).ConfigureAwait(false);
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to get access token. Status: {response.StatusCode}, Response: {json}");
        }

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString()
               ?? throw new Exception("Access token not found in response.");
    }
}