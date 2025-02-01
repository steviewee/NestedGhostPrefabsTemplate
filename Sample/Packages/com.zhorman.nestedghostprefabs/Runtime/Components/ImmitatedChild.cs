using Unity.Entities;
using Unity.NetCode;

namespace Zhorman.NestedGhostPrefabs.Runtime.Components
{
    [GhostComponent]
    public struct ImmitatedChild : IBufferElementData
    {
        [GhostField]
        public Entity Value;
    }
}