using Microsoft.AspNetCore.Mvc;
using CensorBotFilter.Filter;

using System.Text.Json.Serialization;
using System.Diagnostics;

namespace CensorBotFilter.Controllers
{
    public class FilterPost
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = null!;

        [JsonPropertyName("settings")]
        public FilterSettings Settings { get; set; } = null!;
    }

    [ApiController]
    [Route("[controller]")]
    public class FilterController : ControllerBase
    {
        [HttpPost]
        [Route("/filter/resolve")]
        public StringResolved Resolve([FromQuery(Name = "string")] string content)
        {
            return Resolver.Resolve(content);
        }

        [HttpPost]
        [Route("/filter/test")]
        public FilterResult Test([FromBody] FilterPost post)
        {
            return Tester.Test(post.Content, post.Settings);
        }

        [HttpPost]
        [Route("/filter/test/debug")]
        public FilterResult TestDebug([FromBody] FilterPost post)
        {
            var timer = new Stopwatch();
            timer.Start();

            var test = Tester.Test(post.Content, post.Settings);

            timer.Stop();

            TimeSpan timeTaken = timer.Elapsed;

            Console.WriteLine("Time taken: {0:c}", timeTaken);

            return test;
        }
    }
}