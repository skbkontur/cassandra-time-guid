using System.IO;
using System.Text;

using JetBrains.Annotations;

using Newtonsoft.Json;

namespace SKBKontur.Catalogue.Objects.Json
{
    public static class JsonObjectExtensions
    {
        [NotNull]
        public static string ToJson<T>([CanBeNull] this T o, [NotNull] params JsonConverter[] converters)
        {
            return JsonConvert.SerializeObject(o, converters);
        }

        [NotNull]
        public static string ToPrettyJson<T>([CanBeNull] this T o, [NotNull] params JsonConverter[] converters)
        {
            return JsonConvert.SerializeObject(o, Formatting.Indented, converters);
        }

        [NotNull]
        public static T FromJson<T>([NotNull] this string serialized)
        {
            return JsonConvert.DeserializeObject<T>(serialized);
        }

        [NotNull]
        public static T FromJson<T>([NotNull] this Stream serialized)
        {
            using (var streamReader = new StreamReader(serialized, Encoding.UTF8))
                return streamReader.ReadToEnd().FromJson<T>();
        }

        [NotNull]
        public static T FromJson<T>([NotNull] this byte[] serialized)
        {
            using (var ms = new MemoryStream(serialized))
                return FromJson<T>(ms);
        }
    }
}