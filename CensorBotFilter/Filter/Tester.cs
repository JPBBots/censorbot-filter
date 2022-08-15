using System.Text.Json.Serialization;

using CensorBotFilter.Utilities;
using CensorBotFilter.Filter.Files;

namespace CensorBotFilter.Filter
{
    public class FilterResult {
        [JsonPropertyName("censored")]
        public bool Censored { get; set; } = false;

        [JsonPropertyName("ranges")]
        public List<InclusiveRange> Ranges { get; set; } = new();

        [JsonPropertyName("places")]
        public List<string> Places { get; } = new();
    }

    public class Language
    {
        public string Name { get; private set; } = null!;
        public Word[] Words { get; private set; } = null!;

        public Language(string name, Word[] words)
        {
            Name = name;
            Words = words;
        }
    }

    public static class Tester
    {

        private readonly static Language[] Languages = new[] { FilterJsonLoader.LoadLanguage("en") };

        public static FilterResult Test (string content)
        {
            FilterResult result = new();

            var resolved = Resolver.Resolve(content);

            Language[] scanFor = Languages; // TODO: filter settings

            resolved.Spots.Sort((spot, _) => spot.NoEdits ? -1 : 1);

            foreach (var spot in resolved.Spots)
            {
                if (InclusiveRange.InRanges(result.Ranges.ToArray(), spot.Range)) continue;

                bool added = false;

                foreach (var language in scanFor)
                {
                    foreach (var word in language.Words)
                    {
                        if (!word.Test(spot.Text, new string[] { })) continue;

                        result.Censored = true;

                        if (!added) result.Ranges.Add(spot.Range);
                        added = true;

                        result.Places.Add(word.Name);
                        // TODO: filters & places
                    }
                }
            }

            foreach (var (range, nextRange) in result.Ranges.WithNext())
            {
                if (nextRange == null) continue;

                if (range.End == nextRange.Start)
                {
                    nextRange.Start = range.Start;
                }
            }

            result.Ranges.Sort((range1, range2) => range1.Start - range2.Start);

            return result;
        }
    }
}
