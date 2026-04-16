using Microsoft.AspNetCore.Mvc;
using HomeAssignmentPFTC.DataAccess;
using HomeAssignmentPFTC.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using System.Text;

namespace HomeAssignmentPFTC.Controllers;

public class CatalogController : Controller
{
    private readonly FirestoreRepository _firestoreRepository;
    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;

    public CatalogController(FirestoreRepository firestoreRepository, IMemoryCache cache, IHttpClientFactory httpClientFactory)
    {
        _firestoreRepository = firestoreRepository;
        _cache = cache;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index(string searchString, string sortOrder)
    {
        ViewData["CurrentFilter"] = searchString;
        ViewData["PriceSortParam"] = sortOrder == "price_asc" ? "price_desc" : "price_asc";

        var items = await _firestoreRepository.GetReadyCatalogItemsAsync();

        if (!string.IsNullOrEmpty(searchString))
        {
            items = items.Where(i => i.ItemName.Contains(searchString, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        items = sortOrder switch
        {
            "price_asc" => items.OrderBy(i => i.NumericPrice).ToList(),
            "price_desc" => items.OrderByDescending(i => i.NumericPrice).ToList(),
            _ => items.OrderBy(i => i.ItemName).ToList(),
        };

        return View(items);
    }

    [HttpPost]
    public async Task<IActionResult> TranslateItem([FromBody] TranslationRequestDto request)
    {
        if (string.IsNullOrEmpty(request.Text) || string.IsNullOrEmpty(request.TargetLanguage))
        {
            return BadRequest("Invalid request.");
        }

        string cacheKey = $"{request.Text}_{request.TargetLanguage}";

        if (_cache.TryGetValue(cacheKey, out string cachedTranslation))
        {
            return Json(new { translatedText = cachedTranslation, source = "cache" });
        }

        string cloudFunctionUrl = "https://europe-west1-pftc-home-493205.cloudfunctions.net/menu-translator"; 

        var client = _httpClientFactory.CreateClient();
        var jsonContent = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        var response = await client.PostAsync(cloudFunctionUrl, jsonContent);

        if (response.IsSuccessStatusCode)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseString);
            string translatedText = result.GetProperty("translatedText").GetString();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(24)); // Keep in cache for 24 hours
            
            _cache.Set(cacheKey, translatedText, cacheOptions);

            return Json(new { translatedText = translatedText, source = "api" });
        }

        return StatusCode(500, "Translation failed.");
    }
}

public class TranslationRequestDto
{
    public string Text { get; set; } = "";
    public string TargetLanguage { get; set; } = "";
}