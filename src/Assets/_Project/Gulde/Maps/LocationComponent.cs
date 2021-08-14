using System;
using System.Collections.Generic;
using System.Linq;
using Gulde.Economy;
using Gulde.Entities;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Gulde.Maps
{
    [RequireComponent(typeof(EntityRegistryComponent))]
    public class LocationComponent : SerializedMonoBehaviour
    {

        [ShowInInspector]
        [ListDrawerSettings(Expanded = true)]
        public List<ExchangeComponent> Exchanges => GetComponentsInChildren<ExchangeComponent>().ToList();

        [OdinSerialize]
        [ReadOnly]
        public EntityRegistryComponent EntityRegistry { get; private set; }

        [OdinSerialize]
        public Vector3Int EntryCell { get; set; }

        HashSet<EntityComponent> Entities => EntityRegistry.Entities;

        void Awake()
        {
            EntityRegistry = GetComponent<EntityRegistryComponent>();

            EntityRegistry.Registered -= OnEntityRegistered;
            EntityRegistry.Unregistered -= OnEntityUnregistered;

            EntityRegistry.Registered += OnEntityRegistered;
            EntityRegistry.Unregistered += OnEntityUnregistered;
        }

        void OnEntityRegistered(object sender, EntityEventArgs e)
        {
            var entityComponent = e.Entity;
            if (!entityComponent) return;

            entityComponent.Location = this;
        }

        void OnEntityUnregistered(object sender, EntityEventArgs e)
        {
            var entityComponent = e.Entity;
            if (!entityComponent) return;

            entityComponent.Location = null;
        }

        #region OdinInspector

        void OnValidate()
        {
            EntityRegistry = GetComponent<EntityRegistryComponent>();

            EntityRegistry.Registered -= OnEntityRegistered;
            EntityRegistry.Unregistered -= OnEntityUnregistered;

            EntityRegistry.Registered += OnEntityRegistered;
            EntityRegistry.Unregistered += OnEntityUnregistered;
        }

        #endregion
    }
}