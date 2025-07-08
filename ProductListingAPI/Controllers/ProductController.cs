using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ProductListingAPI.Models;

namespace ProductListingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly HttpClient _httpClient;

        public ProductsController(IWebHostEnvironment env)
        {
            _env = env;
            _httpClient = new HttpClient();
        }

        [HttpGet]
        public async Task<IActionResult> Get(
       [FromQuery] double? minPrice,
       [FromQuery] double? maxPrice,
       [FromQuery] double? minScore)
        {
            string jsonPath = Path.Combine(_env.ContentRootPath, "Data", "products.json");
            var jsonData = await System.IO.File.ReadAllTextAsync(jsonPath);
            var products = JsonConvert.DeserializeObject<List<Product>>(jsonData);

            double goldPrice = await GetGoldPrice();

            var result = products
                .Select(p => new
                {
                    p.Name,
                    p.PopularityScore,
                    p.Weight,
                    p.Images,
                    Price = Math.Round((p.PopularityScore + 1) * p.Weight * goldPrice, 2)
                })
                .Where(p =>
                    (!minPrice.HasValue || p.Price >= minPrice.Value) &&
                    (!maxPrice.HasValue || p.Price <= maxPrice.Value) &&
                    (!minScore.HasValue || p.PopularityScore >= minScore.Value)
                );

            return Ok(result);
        }

        private async Task<double> GetGoldPrice()
        {
            try
            {
                var response = await _httpClient.GetStringAsync("https://api.metals.live/v1/spot");
                var data = JsonConvert.DeserializeObject<List<List<object>>>(response);

                foreach (var item in data)
                {
                    if (item[0].ToString().ToLower() == "gold")
                    {
                        return Convert.ToDouble(item[1]);
                    }
                }

                return 70.0; 
            }
            catch
            {
                return 70.0;
            }
        }

    }
}
