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

        public bool NumberInRange (int i)
        {
            return ((i - Start) * (i - End)) <= 0;
        }

        public static bool InRanges(InclusiveRange[] ranges, InclusiveRange test)
        {
            return ranges.Any((range) =>
                test.NumberInRange(range.Start) || test.NumberInRange(range.End)
            );
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