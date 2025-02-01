using Unity.Entities;
using Unity.NetCode;
namespace NGPTemplate.Components
{
    [GhostComponent]
    public struct OwningPlayer : IComponentData
    {
        [GhostField]
        public Entity Value;

        public Entity PreviousValue;

        public bool SetNull;

    }
}