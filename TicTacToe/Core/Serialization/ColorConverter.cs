using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace AkiGames.Core.Serialization
{
    public class ColorConverter : JsonConverter<Color>
    {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new(){ PackedValue = reader.GetUInt32() };

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value.PackedValue);
    }
}