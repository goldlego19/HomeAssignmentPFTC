using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Google.Cloud.Functions.Framework;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MenuDataCleaner;

public class Function : IHttpFunction
{
    private readonly ILogger _logger;
    private readonly FirestoreDb _firestoreDb;

    public Function(ILogger<Function> logger)
    {
        _logger = logger;
        // Connect to your specific project
        string projectId = "pftc-home-493205"; 
        _firestoreDb = FirestoreDb.Create(projectId);
    }

    public async Task HandleAsync(HttpContext context)
    {
        try
        {
            _logger.LogInformation("Data Cleaner Started: Scanning for 'Pending' menus...");

            // 1. Find all menus across the database marked as Pending
            Query pendingMenusQuery = _firestoreDb.CollectionGroup("menus").WhereEqualTo("status", "Pending");
            QuerySnapshot pendingSnapshot = await pendingMenusQuery.GetSnapshotAsync();

            if (pendingSnapshot.Count == 0)
            {
                _logger.LogInformation("No pending menus found.");
                await context.Response.WriteAsync("No pending menus found.");
                return;
            }

            int processedCount = 0;

            // 2. Iterate through them and clean the data
            foreach (DocumentSnapshot document in pendingSnapshot.Documents)
            {
                if (document.TryGetValue("ocrText", out string ocrText))
                {
                    // Clean and structure the text
                    List<Dictionary<string, object>> structuredItems = ParseMenuText(ocrText);

                    // 3. Update the Firestore document to "ready"
                    Dictionary<string, object> updates = new Dictionary<string, object>
                    {
                        { "status", "ready" },
                        { "items", structuredItems },
                        { "cleanedAt", Timestamp.GetCurrentTimestamp() }
                    };

                    await document.Reference.UpdateAsync(updates);
                    processedCount++;
                    _logger.LogInformation($"Successfully structured menu: {document.Id}");
                }
            }

            _logger.LogInformation($"Cleaner Finished: Processed {processedCount} menus.");
            await context.Response.WriteAsync($"Success. Cleaned {processedCount} menus.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing the data cleaning task.");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Internal Server Error");
        }
    }

    // A helper method to separate text and prices into structured data
    private List<Dictionary<string, object>> ParseMenuText(string ocrText)
    {
        var items = new List<Dictionary<string, object>>();
        if (string.IsNullOrWhiteSpace(ocrText)) return items;

        // The regex to find prices
        Regex itemRegex = new Regex(@"(?<name>.*?)(?<price>[€£$]\s*\d+[\.,]\d{2})", RegexOptions.Singleline);
        MatchCollection matches = itemRegex.Matches(ocrText);

        // A list of common section headers we want to ignore/remove
        string[] sectionHeaders = {
            "Ristorante Italiano", "Maltese Menu", "American Menu", "Menu",
            "Antipasti", "Primi Piatti", "Secondi Piatti", "Dolci",
            "Starters", "Burgers & Sandwiches", "Entrees", "Desserts",
            "Main Courses", "Main Course", "Dessert", "Appetizers", "Sides", "Beverages", "Drinks", "&"
        };

        foreach (Match match in matches)
        {
            string rawName = match.Groups["name"].Value;
            string price = match.Groups["price"].Value.Trim();

            string cleanName = rawName.Replace("\n", " ").Replace("\r", "").Trim();
            cleanName = Regex.Replace(cleanName, @"^[^\w]+", "").Trim(); // Remove leading punctuation

            foreach (string header in sectionHeaders)
            {
                cleanName = Regex.Replace(cleanName, header, "", RegexOptions.IgnoreCase).Trim();
            }

            cleanName = Regex.Replace(cleanName, @"^[^\w]+|[^\w]+$", "").Trim();

            if (!string.IsNullOrWhiteSpace(cleanName) && cleanName.Length > 2)
            {
                items.Add(new Dictionary<string, object>
                {
                    { "itemName", cleanName },
                    { "price", price }
                });
            }
        }

        return items;
    }
}