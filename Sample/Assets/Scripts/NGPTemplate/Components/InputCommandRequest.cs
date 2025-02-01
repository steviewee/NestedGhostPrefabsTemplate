using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace NGPTemplate.Components
{
    [GhostComponent]
    public struct InputCommandRequest : IBufferElementData
    {
        [GhostField]
        public int Value;
    }
}