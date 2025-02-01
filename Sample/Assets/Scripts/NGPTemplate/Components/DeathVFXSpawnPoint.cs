using Unity.Entities;
namespace NGPTemplate.Components
{
    public struct DeathVFXSpawnPoint : IComponentData
    {
        public Entity Value;
        public int HasProcessedDeath;
    }
}
