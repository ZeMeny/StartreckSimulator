using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Serialization;
using StartreckSimulator.Properties;

namespace StartreckSimulator.Models
{
    public static class Extensions
    {
        public static string ToJson<T>(this T obj)
        {
            return JsonConvert.SerializeObject(obj, new JsonSerializerSettings
            {
                Converters = {new StringEnumConverter()},
                Formatting = Formatting.Indented,
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                }
            });
        }

        public static bool IsValid(this string str, out AggregateException errors)
        {
            errors = null;
            var schema = JSchema.Parse(Resources.mrs_classification_schema);
            var json = JObject.Parse(str);
            bool retVal = json.IsValid(schema, out IList<string> errorMessages);
            if (errorMessages.Count > 0)
            {
                errors = new AggregateException(errorMessages.Select(x=>new Exception(x)));
            }

            return retVal;
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
