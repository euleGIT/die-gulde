using Newtonsoft.Json;

namespace Gulde.Client.Model.Scenes
{
    internal class GildeScene
    {
        [JsonProperty("scene_elements")]
        public SceneElement[] SceneElements { get; set; }

        //[JsonProperty("element_groups")]
        //public SceneElementGroup[] ElementGroups { get; set; }
    }
}
