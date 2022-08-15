using System.Text.RegularExpressions;

namespace CensorBotFilter.Filter
{
    public class Word
    {
        private readonly List<Regex> Uncensor = new();
        private Regex Matcher { get; set; } = null!;

        public string Name { get; private set; } = null!;

        public Word (string word, string[] uncensorList)
        {
            Name = word;
            Matcher = new Regex(word, RegexOptions.Compiled);

            foreach (var uncensor in uncensorList)
            {
                Uncensor.Add(new Regex(uncensor, RegexOptions.Compiled));
            }
        }

        public bool Test (string str, string[] customUncensorList)
        {
            if (!Matcher.Match(str).Success) return false;

            foreach (var uncensor in Uncensor)
            {
                if (uncensor.Match(str).Success) return false;
            }
            foreach(var uncensorWord in customUncensorList)
            {
                if (new Regex(uncensorWord).Match(str).Success) return false;
            }

            return true;
        }
    }
}
