using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Rendering;
using NGPTemplate.Components;
namespace NGPTemplate.Authoring
{

    /// <summary>
    /// NetcodeSamples authoring that sets a ghost to the debug color specified in <see cref="NetworkIdDebugColorUtility"/>.
    /// </summary>
    [UnityEngine.DisallowMultipleComponent]
    public class SetPlayerToDebugColorAuthoring : UnityEngine.MonoBehaviour
    {
        class SetPlayerToDebugColorBaker : Baker<SetPlayerToDebugColorAuthoring>
        {
            public override void Bake(SetPlayerToDebugColorAuthoring authoring)
            {
                SetPlayerToDebugColor component = default(SetPlayerToDebugColor);
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
                AddComponent(entity, new URPMaterialPropertyBaseColor() {Value = new float4(1, 0, 0, 1)});
            }
        }
    }

    
}
