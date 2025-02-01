using Unity.Entities;
using Unity.NetCode;
namespace Zhorman.NestedGhostPrefabs.Runtime.Components
{
    [GhostComponent]
    public struct GhostRootLink : IComponentData
    {
        [GhostField]
        public Entity Value;
    }
}
