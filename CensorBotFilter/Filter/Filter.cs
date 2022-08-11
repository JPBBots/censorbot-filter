using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace CensorBotFilter.Filter
{
    public class StringResolved
    {
        [JsonPropertyName("content")]
        public string Resolved { get; set; } = null!;
    }

    public class Filter
    {
        private static class CommonRegex
        {
            public static Regex Mention = new(@"<#?@?!?&?(\d+)>", RegexOptions.Compiled);
            public static Regex Emoji = new(@"<a?:(?'name'\w+):(?'id'\d+)>", RegexOptions.Compiled);
            public static Regex Email = new(@"(?'name'[a-zA-Z0-9_\-.]+)@(?'domain'(\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9-]+\.)+))(?'tld'[a-zA-Z]{2,4}|[0-9]{1,3})", RegexOptions.Compiled);
            public static Regex Link = new(@"https?:\/\/(www\.)?(?'domain'[-a-zA-Z0-9@:%._+~#=]{1,256})\.(?'tld'[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_+.~#?&//=]*))", RegexOptions.Compiled);
            public static Regex ExtendedSpaces = new(@"\s|_|\/|\\|\.|\n|&|-|\^|\+|=|:|~|,|\?|\(|\)", RegexOptions.Compiled);
            public static Regex MultipleOfSameCharacter = new(@"(\w)\1{2,}", RegexOptions.Compiled);
        }

        public static StringResolved Resolve (string content)
        {
            content = RemoveIgnoredPatterns(content);
            content = content.TrimStart();

            return new StringResolved() {
                Resolved = content
            };
        }

        private static string RemoveIgnoredPatterns (string content) {
            content = CommonRegex.Mention.Replace(content, "");
            content = CommonRegex.Emoji.Replace(content, "${name}");
            content = CommonRegex.Email.Replace(content, (match) =>
                CommonRegex.ExtendedSpaces.Replace(
                    $"{match.Groups["name"]}{match.Groups["domain"]}{match.Groups["tld"]}",
                    ""
                )
            );
            content = CommonRegex.Link.Replace(content, (match) =>
                CommonRegex.ExtendedSpaces.Replace(
                    $"{match.Groups["domain"]}{match.Groups["tld"]}",
                    ""
                )
            );
            content = CommonRegex.MultipleOfSameCharacter.Replace(content, "$1$1");

            return content;
        }
    }
}
