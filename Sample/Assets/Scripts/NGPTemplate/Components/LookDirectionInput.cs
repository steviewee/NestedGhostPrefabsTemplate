using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.NetCode;

namespace NGPTemplate.Components
{
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct LookDirectionInput : IInputComponentData
    {
        public float2 Value;
    }
}