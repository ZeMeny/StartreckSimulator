using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace StartreckSimulator.Models
{
    public static class Extensions
    {
        public static string ToJson(this object obj)
        {
            //return JsonConvert.SerializeObject(obj, Formatting.Indented, new StringEnumConverter());
            var name = obj.GetType().Name.ToLower();
            var jv = JToken.FromObject(obj);
            return new JObject(new JProperty(name, jv)).ToString();
        }
    }
}
