using System;

using Newtonsoft.Json;

namespace SKBKontur.Catalogue.Objects.Json
{
    public class StringTimestampConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
                writer.WriteNull();
            else
                writer.WriteValue(((Timestamp)value).ToDateTime());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
            if (reader.TokenType == JsonToken.Date)
                return new Timestamp((DateTime)reader.Value);
            throw new JsonSerializationException(string.Format("Unexpected token when parsing timestamp. Expected Date, got {0}", reader.TokenType));
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Timestamp);
        }
    }
}