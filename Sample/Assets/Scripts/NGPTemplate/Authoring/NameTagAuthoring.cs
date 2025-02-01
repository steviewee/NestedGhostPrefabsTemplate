using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using NGPTemplate.Components;

namespace NGPTemplate.Authoring
{
    [DisallowMultipleComponent]
    public class NameTagAuthoring : MonoBehaviour
    {
        public class Baker : Baker<NameTagAuthoring>
        {
            public override void Bake(NameTagAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new NameTag{
                });
            }
        }
    }
    


}
