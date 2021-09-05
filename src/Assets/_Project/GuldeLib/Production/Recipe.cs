using System.Collections.Generic;
using GuldeLib.Economy;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace GuldeLib.Production
{
    [CreateAssetMenu(menuName="Recipe")]
    public class Recipe : SerializedScriptableObject
    {
        [OdinSerialize]
        [Required]
        public string Name { get; private set; }

        [OdinSerialize]
        [Required]
        public Dictionary<Item, int> Resources { get; private set; }

        [OdinSerialize]
        [Required]
        public Item Product { get; private set; }

        [OdinSerialize]
        public bool IsExternal { get; private set; }

        [OdinSerialize]
        [MinValue(0f)]
        public float Time { get; private set; }

        [ShowInInspector]
        public float MeanProfitPerHour
        {
            get
            {
                if (!Product || Resources == null) return 0;

                var resourceCost = 0f;

                foreach (var pair in Resources)
                {
                    var item = pair.Key;
                    var amount = pair.Value;

                    resourceCost += item.MeanPrice * amount;
                }

                var productRevenue = Product.MeanPrice;

                return (productRevenue - resourceCost) / Time;
            }
        }
    }
}