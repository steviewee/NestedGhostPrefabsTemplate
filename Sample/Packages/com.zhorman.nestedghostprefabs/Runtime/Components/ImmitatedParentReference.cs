using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using UnityEngine;

namespace Zhorman.NestedGhostPrefabs.Runtime.Components
{
    [GhostComponent]
    [GhostEnabledBit]
    public struct ImmitatedParentReference : IComponentData, IEnableableComponent
    {
        [GhostField]
        public Entity Value;

        public Entity previousValue;
        [GhostField]
        public bool oldlyParented;
    }
}