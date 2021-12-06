using System;
using System.Collections.Generic;
using GuldeLib.Entities;
using GuldeLib.Extensions;
using MonoExtensions.Runtime;
using MonoLogger.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GuldeLib.Pathfinding
{
    [RequireComponent(typeof(EntityComponent))]
    public class PathfinderComponent : SerializedMonoBehaviour
    {
        [ShowInInspector]
        [BoxGroup("Settings")]
        public float Speed { get; set; }

        [ShowInInspector]
        [BoxGroup("Info")]
        public Queue<Vector2Int> Waypoints { get; private set; } = new Queue<Vector2Int>();

        [ShowInInspector]
        [BoxGroup("Info")]
        Vector2Int CellPosition => Entity ? Entity.Position.ToCell() : Vector2Int.zero;

        [ShowInInspector]
        [BoxGroup("Info")]
        bool HasWaypoints => Waypoints != null && Waypoints.Count > 0;

        [ShowInInspector]
        [BoxGroup("Info")]
        public float TravelPercentage => RemainingWaypoints / (float)TotalWaypoints;

        [ShowInInspector]
        [FoldoutGroup("Debug")]
        EntityComponent Entity => this.GetCachedComponent<EntityComponent>();

        [ShowInInspector]
        [FoldoutGroup("Debug")]
        int TotalWaypoints { get; set; }

        [ShowInInspector]
        [FoldoutGroup("Debug")]
        int RemainingWaypoints => Waypoints?.Count ?? 0;

        Vector2Int CurrentWaypoint => Waypoints.Peek();
        public WaitForDestinationReached WaitForDestinationReached => new WaitForDestinationReached(this);
        public WaitForDestinationReachedPartly WaitForDestinationReachedPartly(float percentage) => new WaitForDestinationReachedPartly(this, percentage);

        public event EventHandler<CellEventArgs> DestinationChanged;
        public event EventHandler<CellEventArgs> DestinationReached;

        void Awake()
        {
            this.Log("Pathfinding initializing");
        }

        void FixedUpdate()
        {
            if (!HasWaypoints) return;
            var distance = Speed * Locator.Time.TimeScale * Time.fixedDeltaTime;
            this.Log($"Pathfinding will travel distance of {distance} = {Speed} * {Locator.Time.TimeScale} * {Time.fixedDeltaTime}.");
            MoveFrame(distance);
        }

        void MoveFrame(float distance)
        {
            while (distance > 0)
            {
                var direction = Entity.Position.DirectionTo(CurrentWaypoint);
                var distanceToWaypoint = Entity.Position.DistanceTo(CurrentWaypoint);

                var cell = Waypoints.Peek();

                if (distanceToWaypoint > distance)
                {
                    Entity.Position += direction * distance;
                    return;
                }

                Entity.Position = CurrentWaypoint;

                this.Log($"Pathfinding reached waypoint {CurrentWaypoint}");

                Waypoints.Dequeue();

                if (!HasWaypoints)
                {
                    this.Log($"Pathfinding reached destination");

                    DestinationReached?.Invoke(this, new CellEventArgs(cell));
                    return;
                }

                distance -= distanceToWaypoint;
            }
        }

        public void SetDestination(Vector2Int destinationCell)
        {
            var map = Entity.Map ? Entity.Map : Locator.City.Map;

            if (!map)
            {
                this.Log($"Pathfinding can not find path without being registered in a map.", LogType.Warning);
                return;
            }

            Waypoints.Clear();
            DestinationChanged?.Invoke(this, new CellEventArgs(destinationCell));

            this.Log($"Pathfinding sending entity to {destinationCell}");

            if (CellPosition == destinationCell)
            {
                this.Log($"Pathfinding entity was already at {destinationCell}");

                DestinationReached?.Invoke(this, new CellEventArgs(destinationCell));
                return;
            }

            var newWaypoints = Path.FindPath(CellPosition, destinationCell, Entity.Map);
            Waypoints = newWaypoints ?? new Queue<Vector2Int>();

            if (Waypoints.Count == 0)
            {
                this.Log($"Pathfinding couldn't find a path!", LogType.Warning);

                DestinationReached?.Invoke(this, new CellEventArgs(destinationCell));
            }
            else TotalWaypoints = Waypoints.Count;
        }
    }

    public class WaitForDestinationReached : CustomYieldInstruction
    {
        public WaitForDestinationReached(PathfinderComponent pathfinding)
        {
            Pathfinding = pathfinding;
            Pathfinding.DestinationReached += OnDestinationReached;
        }

        void OnDestinationReached(object sender, CellEventArgs e)
        {
            IsDestinationReached = true;
        }

        PathfinderComponent Pathfinding { get; }
        bool IsDestinationReached { get; set; }
        public override bool keepWaiting =>
            !IsDestinationReached && Pathfinding.Waypoints.Count != 0;
    }

    public class WaitForDestinationReachedPartly : CustomYieldInstruction
    {
        public WaitForDestinationReachedPartly(PathfinderComponent pathfinding, float percentage)
        {
            Pathfinding = pathfinding;
            Percentage = percentage;
        }
        PathfinderComponent Pathfinding { get; }
        float Percentage { get; }
        public override bool keepWaiting => Pathfinding.TravelPercentage < Percentage;
    }
}