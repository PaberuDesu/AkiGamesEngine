using System.Text.Json;
using System.Text.Json.Serialization;

namespace AkiGames.Core
{
    public class ColorConverter : JsonConverter<Color>
    {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            uint packed = reader.GetUInt32();
            return Color.FromPackedValue(packed);
        }
    
        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.PackedValue);
        }
    }
}