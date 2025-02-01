using Unity.Entities;
using UnityEngine;
using NGPTemplate.Components;

namespace NGPTemplate.Authoring
{
    public class SpectatorSpawnPointAuthoring : MonoBehaviour
    {
        public class Baker : Baker<SpectatorSpawnPointAuthoring>
        {
            public override void Bake(SpectatorSpawnPointAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new SpectatorSpawnPoint());
            }
        }
    }
}
