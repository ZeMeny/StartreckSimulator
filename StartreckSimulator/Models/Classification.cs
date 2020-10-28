using Newtonsoft.Json;

namespace StartreckSimulator.Models
{
    public class Classification
    {
        [JsonProperty(PropertyName = "type")]
        public ClassificationTypes Type { get; set; }

        [JsonProperty(PropertyName = "confidence")]
        public int Confidence { get; set; }
    }
}