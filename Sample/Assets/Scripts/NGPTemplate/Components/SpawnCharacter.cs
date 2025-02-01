  using Unity.Entities;
using Unity.NetCode;
namespace NGPTemplate.Components
{
    public struct SpawnCharacter : IComponentData
    {
        public Entity ClientEntity;
        public float Delay;
        public int Prefab;
    }
}