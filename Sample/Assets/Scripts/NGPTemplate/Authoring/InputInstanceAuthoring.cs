using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using NGPTemplate.Components;
using System.Collections.Generic;

namespace NGPTemplate.Authoring
{
    [DisallowMultipleComponent]
    public class InputInstanceAuthoring : MonoBehaviour
    {
        public class Baker : Baker<InputInstanceAuthoring>
        {
            public override void Bake(InputInstanceAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var inputPrefabBuffer = AddBuffer<InputInstance>(entity);
            }
        }
    }
}
