using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Transforms;

namespace Zhorman.NestedGhostPrefabs.Runtime.Systems
{
    [GhostComponent]
    public struct ImmitatedDeltaMatrix : IComponentData
    {
        [GhostField]
        public LocalTransform Value;
    }
}