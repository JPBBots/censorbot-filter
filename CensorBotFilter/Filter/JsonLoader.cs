using System.Text.Json;

using CharsMedium = System.Collections.Generic.Dictionary<string, string[]>;

namespace CensorBotFilter.Filter
{
    public class Chars : Dictionary<string, string> { }
    public static class FilterJsonLoader
    {
        private static T LoadFile<T>(string name)
        {
            FileStream text = File.OpenRead("./Filter/Files/" + name + ".json");

            return JsonSerializer.Deserialize<T>(text)!;
        }


        public static Chars GetChars ()
        {
            CharsMedium charsMedium = LoadFile<CharsMedium> ("chars");

            Chars chars = new();

            foreach (var key in charsMedium.Keys)
            {
                if (key == "-" || key == "?") continue;

                foreach (var character in charsMedium[key])
                {
                    chars[character] = key;
                }
            }

            return chars;
        }
    }
}
