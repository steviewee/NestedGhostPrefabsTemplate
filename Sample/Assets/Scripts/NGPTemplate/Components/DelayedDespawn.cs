using Unity.Entities;
using Unity.NetCode;

namespace NGPTemplate.Components
{
    [GhostComponent]
    [GhostEnabledBit]
    public struct DelayedDespawn : IComponentData, IEnableableComponent
    {
        public uint Ticks;
        public byte HasHandledPreDespawn;
    }
}
