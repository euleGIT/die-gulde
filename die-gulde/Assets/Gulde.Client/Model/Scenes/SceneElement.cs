using Newtonsoft.Json;

namespace Gulde.Client.Model.Scenes
{
    internal class SceneElement
    {
        [JsonProperty("ones_count")]
        public int OnesCount { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("width")]
        public int Width { get; set; }
        
        [JsonProperty("height")]
        public int Height { get; set; }
        
        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("dummy_element")]
        public DummyElement DummyElement { get; set; }

        [JsonProperty("object_element")]
        public ObjectElement ObjectElement { get; set; }
        
        [JsonProperty("city_element")]
        public CityElement CityElement { get; set; }

        public TransformElement TransformElement => ObjectElement is not null ? ObjectElement :
            DummyElement;
        
        [JsonProperty("skip_length")]
        public int SkipLength { get; set; }
    }
}
