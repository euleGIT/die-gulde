﻿using System.Collections.Generic;
using Assets.Gulde.Client.Model.Scenes;
using Newtonsoft.Json;

namespace Gulde.Client.Model.Scenes
{
    internal class DummyElement
    {
        [JsonProperty("transform")]
        public ElementTransform Transform { get; set; }
        
        [JsonProperty("transforms")]
        public List<ElementTransform> Transforms { get; set; }
    }
}
