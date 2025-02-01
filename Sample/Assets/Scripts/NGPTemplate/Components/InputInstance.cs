using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace NGPTemplate.Components
{
    [GhostComponent]
    public struct InputInstance : IBufferElementData
    {
        [GhostField]
        public Entity Value;
        [GhostField]
        public int Prefab;
    }
}