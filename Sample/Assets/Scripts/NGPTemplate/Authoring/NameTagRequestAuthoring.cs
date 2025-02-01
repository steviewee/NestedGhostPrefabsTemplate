using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using NGPTemplate.Components;
namespace NGPTemplate.Authoring
{
    public class NameTagRequestAuthoring : MonoBehaviour
    {
        public GameObject socket;
        public class Baker : Baker<NameTagRequestAuthoring>
        {
            public override void Bake(NameTagRequestAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                AddComponent(entity, new NameTagRequest
                {
                    Value = GetEntity(authoring.socket, TransformUsageFlags.Dynamic)
                });
            }
        }
    }

}