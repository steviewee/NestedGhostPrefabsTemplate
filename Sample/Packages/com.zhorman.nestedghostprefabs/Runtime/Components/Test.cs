using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Zhorman.NestedGhostPrefabs.Runtime
{
    [GhostComponent]
    public struct Test : IComponentData
    {
        [GhostField]
        public Entity ghostedField;
        public Entity non_GhostedField;
    }
}
