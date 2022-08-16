using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CensorBotFilter.Utilities
{
    [JsonConverter(typeof(InclusiveRangeFormatter))]
    public class InclusiveRange
    {
        public int Start { get; set; } = default!;
        public int End { get; set; } = default!;

        public Range Range => new(Start, End + 1);

        public InclusiveRange(int start, int end)
        {
            Start = start;
            End = end;
        }

        public void Invalidate()
        {
            Start = -1;
            End = -1;
        }
        public bool Invalid => Start == -1;

        public void UpdateIndexes(int index)
        {
            if (index < Start) Start = index;
            if (index > End) End = index;
        }

        public void UpdateIndexes(InclusiveRange range)
        {
            UpdateIndexes(range.Start);
            UpdateIndexes(range.End);
        }

        public bool Contains(int i)
        {
            return ((i - Start) * (i - End)) <= 0;
        }

        public bool Contains(InclusiveRange range)
        {
            return Contains(range.Start) || Contains(range.End);
        }

        public static InclusiveRange FromStringIndexes(string str, int startIndex, int endIndex)
        {
            InclusiveRange range = new(0, 0);
            string[] split = str.Split(' ');

            int search = 0;

            for (int i = 0; i < split.Length; i++)
            {
                search += split[i].Length + (i != 0 ? 1 : 0);

                if (search >= startIndex)
                {
                    range.Start = i;

                    break;
                }
            }

            search = startIndex;

            for (int i = range.Start; i < split.Length; i++)
            {
                search += split[i].Length + 1;

                if (search >= endIndex)
                {
                    range.End = i;

                    break;
                }
            }

            return range;
        }
    }

    public static class ListOfInclusiveRangeExtension
    {
        public static bool ContainsRange(this IList<InclusiveRange> ranges, InclusiveRange test)
        {
            return ranges.Any((range) =>
                test.Contains(range)
            );
        }

        public static void SortByStart(this List<InclusiveRange> ranges)
        {
            ranges.Sort((range1, range2) => range1.Start - range2.Start);
        }

        public static void Clean(this List<InclusiveRange> ranges)
        {
            ranges.RemoveAll((range) => range.Invalid);
        }
    }

    class InclusiveRangeFormatter : JsonConverter<InclusiveRange>
    {
        public override InclusiveRange Read(
                ref Utf8JsonReader reader,
                Type typeToConvert,
                JsonSerializerOptions options) =>
                    throw new NotImplementedException("No");

        public override void Write(
            Utf8JsonWriter writer,
            InclusiveRange range,
            JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(range.Start);
            writer.WriteNumberValue(range.End);
            writer.WriteEndArray();
        }
    }
}
// [Start, End]