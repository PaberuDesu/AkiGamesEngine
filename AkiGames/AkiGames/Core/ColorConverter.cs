using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

public class ColorConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            byte r = 0, g = 0, b = 0, a = 255;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propertyName = reader.GetString();
                    reader.Read();

                    switch (propertyName.ToLowerInvariant())
                    {
                        case "r":
                            r = reader.GetByte();
                            break;
                        case "g":
                            g = reader.GetByte();
                            break;
                        case "b":
                            b = reader.GetByte();
                            break;
                        case "a":
                            a = reader.GetByte();
                            break;
                            //case "packedvalue"://TODO
                            //    return new Color { PackedValue = reader.GetUInt32() };
                    }
                }
            }

            return new Color(r, g, b, a);
        }

        throw new JsonException("Invalid Color format");
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("r", value.R);
        writer.WriteNumber("g", value.G);
        writer.WriteNumber("b", value.B);
        writer.WriteNumber("a", value.A);
        writer.WriteEndObject();
    }
}