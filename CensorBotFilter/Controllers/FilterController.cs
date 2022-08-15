using Microsoft.AspNetCore.Mvc;
using CensorBotFilter.Filter;

using System.Text.Json.Serialization;

namespace CensorBotFilter.Controllers
{
    public class FilterPost
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = default!;
    }
    [ApiController]
    [Route("[controller]")]
    public class FilterController : ControllerBase
    {
        [HttpPost]
        [Route("/filter/resolve")]
        public async Task<StringResolved> Resolve([FromBody] FilterPost post)
        {
            return Resolver.Resolve(post.Content);
        }

        [HttpPost]
        [Route("/filter/test")]
        public async Task<FilterResult> Test([FromBody] FilterPost post)
        {
            return Tester.Test(post.Content);
        }
    }
}