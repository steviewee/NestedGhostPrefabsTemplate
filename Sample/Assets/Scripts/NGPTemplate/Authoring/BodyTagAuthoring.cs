using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using NGPTemplate.Components;
namespace NGPTemplate.Authoring
{
    public class BodyTagAuthoring : MonoBehaviour
    {
        public GameObject head;
        public class BodyTagBaker : Baker<BodyTagAuthoring>
        {
            public override void Bake(BodyTagAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                var head = GetEntity(authoring.head, TransformUsageFlags.Dynamic);

                AddComponent(entity, new BodyTag
                {
                    head = head,
                });
            }
        }
    }

}