using Newtonsoft.Json;

namespace Cryptob.Core.Configuration
{
    public class GeneralConfig
    {
        public string ActiveStrategyName{ get; set; }

        [JsonIgnore]
        public Strategy ActiveStrategy => Strategy.Parse(ActiveStrategyName);
    }
}
