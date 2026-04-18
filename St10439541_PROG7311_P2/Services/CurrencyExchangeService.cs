using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace St10439541_PROG7311_P2.Services
{
    public class CurrencyExchangeService : ICurrencyExchangeService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CurrencyExchangeService> _logger;
        private const string CacheKey = "USD_ZAR_Rate";
        private const int CacheDurationMinutes = 60;

        public CurrencyExchangeService(
            HttpClient httpClient,
            IMemoryCache cache,
            ILogger<CurrencyExchangeService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
        }

        public async Task<decimal> GetUsdToZarRateAsync()
        {
            // Try to get from cache first
            if (_cache.TryGetValue(CacheKey, out decimal cachedRate))
            {
                return cachedRate;
            }

            try
            {
                // Using a free API - ExchangeRate-API (requires API key for production)
                // For demo, we'll use a mock endpoint or fallback rate
                // In production, use: https://api.exchangerate-api.com/v4/latest/USD

                // For demo purposes with a working free API
                var response = await _httpClient.GetAsync("https://api.exchangerate-api.com/v4/latest/USD");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<ExchangeRateResponse>(json);

                    if (data != null && data.Rates.TryGetValue("ZAR", out decimal rate))
                    {
                        // Cache the rate
                        _cache.Set(CacheKey, rate, TimeSpan.FromMinutes(CacheDurationMinutes));
                        return rate;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching exchange rate");
            }

            // Fallback rate if API fails
            return 19.50m;
        }
    }

    public class ExchangeRateResponse
    {
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }
}
