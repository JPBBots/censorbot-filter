using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace CensorBotFilter.Filter
{
    public class StringResolved
    {
        [JsonPropertyName("content")]
        public string Content { get; private set; } = null!;

        public StringResolved (string content)
        {
            Content = content;
        }

        public StringResolved ToLower ()
        {
            SetContent(Content.ToLower());

            return this;
        }

        public StringResolved TrimStart ()
        {
            SetContent(Content.TrimStart());

            return this;
        }

        public StringResolved SetContent (string content)
        {
            Content = content;

            return this;
        }

        public StringResolved Replace (Regex regex, string replaceWith)
        {
            return SetContent(regex.Replace(Content, replaceWith));
        }

        public StringResolved Replace (Regex regex, MatchEvaluator eval)
        {
            return SetContent(regex.Replace(Content, eval));
        }
    }

    public class Filter
    {
        private static readonly Chars CharacterConversions = FilterJsonLoader.GetChars();

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
            public static readonly Regex ExtendedNothing = new(@"""|\*|'|\||\`|<|>|#|!|\[|\]|\{|\}|;|%|\u200D|\u200F|\u200E|\u200C|\u200B");
        }

        public static StringResolved Resolve (string content)
        {
            StringResolved resolved = new(content);

            resolved.ToLower();

            RemoveIgnoredPatterns(resolved);
            ConvertAlternativeCharacters(resolved);

            resolved.TrimStart();

            return resolved;
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
