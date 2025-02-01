using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace NGPTemplate.Components
{
    [GhostComponent(SendTypeOptimization = GhostSendType.OnlyPredictedClients)]
    public struct LastKnownTargetOrientation : IComponentData
    {
        [GhostField]
        public float2 Value;
    }
}