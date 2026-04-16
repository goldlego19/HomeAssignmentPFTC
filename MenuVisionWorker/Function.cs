using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using CloudNative.CloudEvents;
using Google.Cloud.Functions.Framework;
using Google.Events.Protobuf.Cloud.PubSub.V1;
using Microsoft.Extensions.Logging;
using Google.Cloud.Vision.V1;
using Google.Cloud.Firestore;

namespace MenuVisionWorker;

public class Function : ICloudEventFunction<MessagePublishedData>
{
    private readonly ILogger _logger;
    private readonly FirestoreDb _firestoreDb;
    private readonly ImageAnnotatorClient _visionClient;

    public Function(ILogger<Function> logger)
    {
        _logger = logger;
        
        string projectId = "pftc-home-493205"; // Hardcoded for your specific project
        
        _firestoreDb = FirestoreDb.Create(projectId);
        _visionClient = ImageAnnotatorClient.Create();
    }

    public async Task HandleAsync(CloudEvent cloudEvent, MessagePublishedData data, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Read the Pub/Sub message sent by your web app
            string jsonMessage = data.Message?.TextData;
            if (string.IsNullOrEmpty(jsonMessage))
            {
                _logger.LogWarning("Received empty message.");
                return;
            }

            _logger.LogInformation($"Received message: {jsonMessage}");
            
            // Parse the JSON payload
            var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonMessage);
            string restaurantId = payload["RestaurantId"];
            string menuId = payload["MenuId"];
            string httpsUrl = payload["ImageUrl"];

            // 2. Prepare the Image for the Vision API
            string gsUri = httpsUrl.Replace("https://storage.googleapis.com/", "gs://");
            var image = Image.FromUri(gsUri);

            // 3. Call the Cloud Vision API to extract text (OCR)
            _logger.LogInformation($"Analysing image with Vision API: {gsUri}");
            var response = await _visionClient.DetectTextAsync(image);
            string extractedText = response.Count > 0 ? response[0].Description : "No text detected";

            _logger.LogInformation($"Successfully extracted {extractedText.Length} characters of text.");

            // 4. Update the Firestore Database
            var menuRef = _firestoreDb
                .Collection("restaurants").Document(restaurantId)
                .Collection("menus").Document(menuId);

            var updateData = new Dictionary<string, object>
            {
                { "ocrText", extractedText },
                { "status", "Pending" }, // Crucial for the next cron job step!
                { "processedAt", Timestamp.GetCurrentTimestamp() }
            };

            await menuRef.SetAsync(updateData, SetOptions.MergeAll);
            
            _logger.LogInformation($"Successfully updated Firestore for Menu: {menuId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst processing the menu image.");
            throw; // Rethrowing ensures Pub/Sub knows the message failed and will retry it
        }
    }
}