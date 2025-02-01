using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
namespace Zhorman.NestedGhostPrefabs.Runtime.Components
{
    public struct LinkedEntityRelink : IBufferElementData
    {
        public Entity Value;
    }
    public struct GhostGroupRequest : IBufferElementData
    {
        public Entity Value;
    }
}
