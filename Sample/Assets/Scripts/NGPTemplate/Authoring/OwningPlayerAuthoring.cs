//using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using NGPTemplate.Components;

namespace NGPTemplate.Authoring
{
    [DisallowMultipleComponent]
    public class OwningPlayerAuthoring : MonoBehaviour
    {
        public class Baker : Baker<OwningPlayerAuthoring>
        {
            public override void Bake(OwningPlayerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.WorldSpace);
                AddComponent(entity, new OwningPlayer());
                //AddComponent(entity, new OwningPlayerRequest());
                //SetComponentEnabled<OwningPlayerRequest>(entity, falsey);
                AddComponent(entity, new DelayedDespawn());
                SetComponentEnabled<DelayedDespawn>(entity, false);
            }
        }
    }
}
