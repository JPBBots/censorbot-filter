using System.Text.Json.Serialization;

using CensorBotFilter.Utilities;
using CensorBotFilter.Filter.Files;

namespace CensorBotFilter.Filter
{
    public class FilterSettings
    {
        [JsonPropertyName("base")]
        public List<string> Base { get; set; } = null!;

        [JsonPropertyName("server")]
        public List<string> Server { get; set; } = null!;

        [JsonPropertyName("phrases")]
        public List<string> Phrases { get; set; } = null!;

        [JsonPropertyName("words")]
        public List<string> Words { get; set; } = null!;

        [JsonPropertyName("uncensor")]
        public List<string> Uncensor { get; set; } = null!;

    }

    public class FilterResult
    {
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

    public class TestContext
    {
        public string Content { get; set; } = null!;
        public FilterSettings Settings { get; set; } = null!;
        public StringResolved Resolved { get; set; } = null!;
        public FilterResult Result { get; set; } = new();
    }

    public static class Tester
    {

        private readonly static Language[] Languages = new[] {
            FilterJsonLoader.LoadLanguage("en"),
            FilterJsonLoader.LoadLanguage("off"),
            FilterJsonLoader.LoadLanguage("es"),
            FilterJsonLoader.LoadLanguage("de"),
            FilterJsonLoader.LoadLanguage("ru")
        };

        public static FilterResult Test(string content, FilterSettings settings)
        {
            TestContext ctx = new()
            {
                Content = content,
                Settings = settings,
                Resolved = Resolver.Resolve(content)
            };

            TestPhrases(ctx);
            TestWords(ctx);
            TestFilters(ctx);

            CollapseRanges(ctx);

            ctx.Result.Ranges.SortByStart();

            return ctx.Result;
        }

        private static void TestFilters(TestContext ctx)
        {
            List<Word> words = Languages
                .Where((lang) => ctx.Settings.Base.Contains(lang.Name))
                .SelectMany((lang) => lang.Words)
                .ToList();

            foreach (var word in ctx.Settings.Server)
            {
                words.Add(new(word));
            }

            foreach (var spot in ctx.Resolved.Spots)
            {
                bool added = false;

                foreach (var word in words)
                {
                    if (!word.Test(spot.Text, ctx.Settings.Uncensor)) continue;

                    ctx.Result.Censored = true;

                    if (!added) ctx.Result.Ranges.Add(spot.Range);
                    added = true;

                    ctx.Result.Places.Add(word.Name);
                    // TODO: filters & places
                }
            }
        }

        private static void TestPhrases(TestContext ctx)
        {
            foreach (var phrase in ctx.Settings.Phrases)
            {
                var indexes = FindAllPhraseIndexes(ctx.Content, phrase);

                foreach (var index in indexes)
                {
                    var range = InclusiveRange.FromStringIndexes(ctx.Content, index, index + phrase.Length);

                    ctx.Result.Censored = true;
                    ctx.Result.Ranges.Add(range);
                    ctx.Result.Places.Add(phrase);
                }
            }
        }

        private static List<int> FindAllPhraseIndexes(string text, string phrase)
        {
            List<int> indexes = new();

            while (true)
            {
                int current = text.IndexOf(phrase);
                if (current == -1) break;

                indexes.Add(current);
                text = text[(current + phrase.Length)..];
            }

            return indexes;
        }

        private static void TestWords(TestContext ctx)
        {
            string[] words = ctx.Content.Split(" ");

            foreach (var (word, index) in words.WithIndex())
            {
                InclusiveRange range = new(index, index);

                if (ctx.Settings.Words.Contains(word.ToLower()))
                {
                    ctx.Result.Censored = true;
                    ctx.Result.Ranges.Add(range);
                    ctx.Result.Places.Add(word);
                }
            }
        }

        private static void CollapseRanges(TestContext ctx)
        {
            // Expand ranges next to eachother to cover eachothers range
            foreach (var (range, nextRange) in ctx.Result.Ranges.WithNext())
            {
                if (nextRange == null) continue;

                if (range.End == nextRange.Start)
                {
                    nextRange.Start = range.Start;
                    range.Invalidate();
                }
            }

            ctx.Result.Ranges.SortByStart();

            int added = 1;
            while (added > 0)
            {
                added = 0;
                ctx.Result.Ranges.Clean();

                foreach (var (range, nextRange) in ctx.Result.Ranges.WithNext())
                {
                    if (nextRange == null || range.Invalid) continue;

                    if (range.Contains(nextRange))
                    {
                        range.UpdateIndexes(nextRange);
                        nextRange.Invalidate();
                        added++;
                    }
                }
            }
        }
    }
}
