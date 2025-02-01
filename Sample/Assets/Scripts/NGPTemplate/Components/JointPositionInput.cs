using Unity.Mathematics;
using Unity.NetCode;

namespace NGPTemplate.Components
{
    /// <summary>
    /// Capture the user input and apply them to a component for later uses
    /// </summary>
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct JointPositionInput : IInputComponentData
    {
        public float3 targetPos;
    }
}