using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace StartreckSimulator.Models
{
    public static class Extensions
    {
        public static string ToJson<T>(this T obj)
        {
            var root = new
            {
                obj
            };
            return JsonConvert.SerializeObject(obj, new JsonSerializerSettings
            {
                Converters = {new StringEnumConverter()},
                Formatting = Formatting.Indented,
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                }
            });

            //var name = obj.GetType().Name.ToCamelCase();
            //var jv = JObject.FromObject(obj, new JsonSerializer
            //{
            //    Converters = { new StringEnumConverter() },
            //    Formatting = Formatting.Indented,
            //    ContractResolver = new DefaultContractResolver
            //    {
            //        NamingStrategy = new CamelCaseNamingStrategy()
            //    }
            //});
            //return JsonConvert.SerializeObject(new JObject(name, jv), Formatting.Indented);
        }

        public static string ToCamelCase(this string str)
        {
            if (!string.IsNullOrEmpty(str) && str.Length > 1)
            {
                return char.ToLowerInvariant(str[0]) + str.Substring(1);
            }
            return str;
        }
    }
}
