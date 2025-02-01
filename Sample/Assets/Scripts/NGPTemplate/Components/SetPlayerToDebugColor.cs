 using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Rendering;

namespace NGPTemplate.Components
{
    /// <summary>
    /// Denotes that a ghost will be set to the debug color specified in <see cref="NetworkIdDebugColorUtility"/>.
    /// </summary>
    public struct SetPlayerToDebugColor : IComponentData
    {
    }
}