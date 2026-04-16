using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Google.Cloud.Translation.V2;

namespace MenuTranslator;

public class Function : IHttpFunction
{
    private readonly ILogger _logger;
    private readonly TranslationClient _translationClient;

    public Function(ILogger<Function> logger)
    {
        _logger = logger;
        // Initialise the Google Translation Client
        _translationClient = TranslationClient.Create();
    }

    public async Task HandleAsync(HttpContext context)
    {
        // 1. Enable CORS so your web app can call this function from the browser
        context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        if (context.Request.Method == HttpMethods.Options)
        {
            context.Response.Headers.Append("Access-Control-Allow-Methods", "POST");
            context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type");
            context.Response.StatusCode = 204;
            return;
        }

        try
        {
            // 2. Read the incoming request
            using TextReader reader = new StreamReader(context.Request.Body);
            string json = await reader.ReadToEndAsync();
            
            if (string.IsNullOrEmpty(json))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Request body cannot be empty.");
                return;
            }

            // Parse the JSON payload (Expecting { "text": "Apple Pie", "targetLanguage": "it" })
            var payload = JsonSerializer.Deserialize<TranslationRequest>(json);
            
            if (payload == null || string.IsNullOrEmpty(payload.Text) || string.IsNullOrEmpty(payload.TargetLanguage))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Missing text or targetLanguage in request.");
                return;
            }

            _logger.LogInformation($"Translating '{payload.Text}' into '{payload.TargetLanguage}'...");

            // 3. Call the Google Cloud Translation API
            var response = await _translationClient.TranslateTextAsync(
                text: payload.Text, 
                targetLanguage: payload.TargetLanguage
            );

            // 4. Return the translated text
            var result = new { translatedText = response.TranslatedText };
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to translate text.");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Internal Server Error");
        }
    }
}

// A simple class to represent the incoming JSON data
public class TranslationRequest
{
    public string? Text { get; set; }
    public string? TargetLanguage { get; set; }
}