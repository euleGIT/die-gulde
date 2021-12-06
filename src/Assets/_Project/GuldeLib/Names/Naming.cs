using GuldeLib.Factories;
using GuldeLib.Generators;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace GuldeLib.Names
{
    [CreateAssetMenu(menuName = "Names/Naming")]
    public class Naming : TypeObject<Naming>
    {
        [Required]
        [Generatable]
        [OdinSerialize]
        public virtual GeneratableName Name { get; set; } = new GeneratableName();

        [Required]
        [Generatable]
        [OdinSerialize]
        public virtual GeneratableName FriendlyName { get; set; } = new GeneratableName();

        public override bool HasNaming => false;
    }
}