using System;

using Newtonsoft.Json;

using SKBKontur.Catalogue.Objects.TimeBasedUuid;

namespace SKBKontur.Catalogue.Objects.Json
{
    public class TimeGuidConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteValue((value as TimeGuid).ToGuid().ToString());
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                var readAsString = (string)reader.Value;
                return TimeGuid.Parse(readAsString);
            }
            return null;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TimeGuid);
        }
    }
}