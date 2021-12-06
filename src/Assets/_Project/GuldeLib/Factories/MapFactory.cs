using GuldeLib.Maps;
using UnityEngine;

namespace GuldeLib.Factories
{
    public class MapFactory : Factory<Map>
    {
        public MapFactory(GameObject gameObject = null, GameObject parentObject = null) : base(gameObject, parentObject)
        {
        }

        public override GameObject Create(Map map)
        {
            var entityRegistryFactory = new EntityRegistryFactory(GameObject);
            entityRegistryFactory.Create(map.EntityRegistry.Value);

            var navFactory = new NavFactory(GameObject);
            navFactory.Create(map.Nav.Value);

            var mapComponent = GameObject.AddComponent<MapComponent>();
            mapComponent.Size = map.Size.Value;

            return GameObject;
        }

        public override GameObject Generate() => null;
    }
}