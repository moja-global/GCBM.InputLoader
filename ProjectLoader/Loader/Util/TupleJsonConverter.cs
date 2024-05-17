using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Recliner2GCBM.Loader.Util
{
    class TupleJsonConverter : JsonConverter
    {
        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IEnumerable<Tuple<string, string>>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var result = new ObservableCollection<Tuple<string, string>>();

            var values = serializer.Deserialize<IDictionary<string, string>>(reader);
            foreach (var val in values)
            {
                result.Add(new Tuple<string, string>(val.Key, val.Value));
            }

            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var result = new Dictionary<string, string>();

            var values = value as IEnumerable<Tuple<string, string>>;
            foreach (var val in values)
            {
                result[val.Item1] = val.Item2;
            }

            serializer.Serialize(writer, result);
        }
    }
}
