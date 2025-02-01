using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using NGPTemplate.Components;
using System.Collections.Generic;

namespace NGPTemplate.Authoring
{
    [DisallowMultipleComponent]
    public class InputCommandRequestAuthoring : MonoBehaviour
    {
        public List<int> targetInputPrefabs = new List<int>();
        public class Baker : Baker<InputCommandRequestAuthoring>
        {
            public override void Bake(InputCommandRequestAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var inputPrefabBuffer = AddBuffer<InputCommandRequest>(entity);
                for (var i = 0; i < authoring.targetInputPrefabs.Count; i++)
                {
                    inputPrefabBuffer.Add(new InputCommandRequest { Value = authoring.targetInputPrefabs[i] });
                }
                AddBuffer<InputReference>(entity);
            }
        }
    }
}
