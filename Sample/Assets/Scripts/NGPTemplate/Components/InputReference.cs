using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
namespace NGPTemplate.Components
{
    [GhostComponent]
    public partial struct InputReference : IBufferElementData
    {
        [GhostField]
        public Entity Value;
        [GhostField]
        public int Prefab;
    }
}