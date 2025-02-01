using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using NGPTemplate.Components;

namespace NGPTemplate.Authoring
{
    public class HealthAuthoring : MonoBehaviour
    {
        public float MaxHealth = 100f;

        public class Baker : Baker<HealthAuthoring>
        {
            public override void Bake(HealthAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new Health
                {
                    MaxHealth = authoring.MaxHealth,
                    CurrentHealth = authoring.MaxHealth,
                });
            }
        }
    }


}
