using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CensorBotFilter.Filter.Files;
using CensorBotFilter.Utilities;

namespace CensorBotFilter.Filter
{
    public class StringResolved
    {
        [JsonPropertyName("content")]
        public string Content { get; private set; } = null!;

        [JsonPropertyName("spots")]
        public List<Spot> Spots { get; set; } = null!;

        public StringResolved(string content)
        {
            Content = content;
        }
        public void CleanSpots()
        {
            Spots = Spots.Where((spot) => !spot.Removing && !string.IsNullOrEmpty(spot.Text)).ToList();
        }

        public StringResolved ToLower()
        {
            SetContent(Content.ToLower());

            return this;
        }

        public StringResolved TrimStart()
        {
            SetContent(Content.TrimStart());

            return this;
        }

        public StringResolved SetContent(string content)
        {
            Content = content;

            return this;
        }

        public StringResolved Replace(Regex regex, string replaceWith)
        {
            return SetContent(regex.Replace(Content, replaceWith));
        }

        public StringResolved Replace(Regex regex, MatchEvaluator eval)
        {
            return SetContent(regex.Replace(Content, eval));
        }
    }

    public static class SpotsListExtension
    {
        public static List<Spot> WithoutNoEdits (this List<Spot> list)
        {
            return list.Where((spot) => !spot.NoEdits).ToList();
        }
    }

    public class Spot
    {
        [JsonPropertyName("t")]
        public string Text { get; set; } = default!;

        [JsonPropertyName("i")]
        public InclusiveRange Range { get; set; } = default!;

        [JsonPropertyName("n")]
        public bool NoEdits { get; set; } = false;

        [JsonIgnore]
        public bool Removing { get; set; } = false;

        public Spot (string text, InclusiveRange range)
        {
            Text = text;
            Range = range;
        }

        public void UpdateIndexes (int index)
        {
            if (index < Range.Start) Range.Start = index;
            if (index > Range.End) Range.End = index;
        }

        public void UpdateIndexes (InclusiveRange range)
        {
            UpdateIndexes(range.Start);
            UpdateIndexes(range.End);
        }

        public void Remove ()
        {
            Removing = true;
        }
    } 

    public class Resolver
    {
        private static readonly Chars CharacterConversions = FilterJsonLoader.GetChars();

        private static readonly string[] ShortWords = new[] { "an", "as", "us", "be", "it", "at", "xd" };

        private static class CommonRegex
        {
            public static readonly Regex Mention = new(@"<#?@?!?&?(\d+)>", RegexOptions.Compiled);
            public static readonly Regex Emoji = new(@"<a?:(?'name'\w+):(?'id'\d+)>", RegexOptions.Compiled);
            public static readonly Regex Email = new(@"(?'name'[a-zA-Z0-9_\-.]+)@(?'domain'(\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9-]+\.)+))(?'tld'[a-zA-Z]{2,4}|[0-9]{1,3})", RegexOptions.Compiled);
            public static readonly Regex Link = new(@"https?:\/\/(www\.)?(?'domain'[-a-zA-Z0-9@:%._+~#=]{1,256})\.(?'tld'[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_+.~#?&//=]*))", RegexOptions.Compiled);
            public static readonly Regex MultipleOfSameCharacter = new(@"(\w)\1{2,}", RegexOptions.Compiled);
            public static readonly Regex SingleCharacter = new(@".", RegexOptions.Compiled);
        }

        public static class CoreRegex
        {
            public static readonly Regex ExtendedSpaces = new(@"\s|_|\/|\\|\.|\n|&|-|\^|\+|=|:|~|,|\?|\(|\)", RegexOptions.Compiled);
            public static readonly Regex ExtendedNothing = new(@"""|\*|'|\||\`|<|>|#|!|\[|\]|\{|\}|;|%|\u200D|\u200F|\u200E|\u200C|\u200B", RegexOptions.Compiled);
        }

        public static StringResolved Resolve(string content)
        {
            StringResolved resolved = new(content);

            resolved.ToLower();

            RemoveIgnoredPatterns(resolved);
            ConvertAlternativeCharacters(resolved);

            resolved.TrimStart();

            resolved.Spots = StringToSpots(resolved);

            TraverseShortCharactersForwards(resolved);
            TraverseShortCharactersBackwards(resolved);

            CombineLeadingCharacters(resolved);

            return resolved;
        }

        private static void CombineLeadingCharacters (StringResolved content)
        {
            var spots = content.Spots.WithoutNoEdits();

            foreach (var (item, nextSpot, index) in spots.WithIndexAndNext())
            {
                if (nextSpot == null) continue;

                if (ShortWords.Any((word) => item.Text.EndsWith(word))) continue;

                var endingCharacter = item.Text[^1];
                var startingCharacter = nextSpot.Text[0];

                if (endingCharacter == startingCharacter)
                {
                    nextSpot.Text = item.Text + nextSpot.Text;
                    nextSpot.UpdateIndexes(item.Range);

                    item.Remove();
                }
            }

            content.CleanSpots();
        }

        private static void TraverseShortCharactersForwards(StringResolved content)
        {
            foreach (var (spot, combiningInto) in content.Spots.WithoutNoEdits().WithNext())
            {
                if (combiningInto == null || ShortWords.Contains(spot.Text)) continue;

                if (spot.Text.Length < 3)
                {
                    combiningInto.UpdateIndexes(spot.Range);
                    combiningInto.Text = spot.Text + combiningInto.Text;

                    spot.Remove();
                }
            }

            content.CleanSpots();
        }

        private static void TraverseShortCharactersBackwards(StringResolved content)
        {
            content.Spots.Reverse();
            foreach (var (spot, combiningInto) in content.Spots.WithoutNoEdits().WithNext())
            {
                if (combiningInto == null || ShortWords.Contains(spot.Text)) continue;

                if (spot.Text.Length < 3)
                {
                    combiningInto.UpdateIndexes(spot.Range);
                    combiningInto.Text += spot.Text;

                    spot.Remove();
                }
            }
            content.Spots.Reverse();

            content.CleanSpots();
        }

        private static List<Spot> StringToSpots (StringResolved content)
        {
            List<Spot> spots = new();

            string[] splitContent = CoreRegex.ExtendedSpaces.Split(content.Content);
            
            foreach (var (piece, index) in splitContent.WithIndex())
            {
                string resolvedPiece = CoreRegex.ExtendedNothing.Replace(piece, "");

                spots.Add(new Spot(resolvedPiece, new InclusiveRange(index, index)));
                spots.Add(new Spot(resolvedPiece, new InclusiveRange(index, index))
                {
                    NoEdits = true
                });
            }

            return spots;
        }

        private static void ConvertAlternativeCharacters (StringResolved resolved)
        {
            resolved.Replace(CommonRegex.SingleCharacter, (match) =>
            {
                if (CoreRegex.ExtendedNothing.IsMatch(match.Value) || CoreRegex.ExtendedSpaces.IsMatch(match.Value)) return match.Value;

                return CharacterConversions.GetValueOrDefault(match.Value, match.Value);
            });
        }

        private static void RemoveIgnoredPatterns (StringResolved resolved) {
            resolved
                .Replace(CommonRegex.Mention, "")
                .Replace(CommonRegex.Emoji, "${name}")
                .Replace(CommonRegex.Email, (match) =>
                    CoreRegex.ExtendedSpaces.Replace(
                        $"{match.Groups["name"]}{match.Groups["domain"]}{match.Groups["tld"]}",
                        ""
                    )
                )
                .Replace(CommonRegex.Link, (match) =>
                    CoreRegex.ExtendedSpaces.Replace(
                        $"{match.Groups["domain"]}{match.Groups["tld"]}",
                        ""
                    )
                )
                .Replace(CommonRegex.MultipleOfSameCharacter, "$1$1");
        }
    }

}
