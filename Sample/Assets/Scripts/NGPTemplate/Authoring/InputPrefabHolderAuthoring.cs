using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using NGPTemplate.Components;
using System.Collections.Generic;

namespace NGPTemplate.Authoring
{
    [DisallowMultipleComponent]
    public class InputPrefabHolderAuthoring : MonoBehaviour
    {
        public List<GameObject> inputPrefabs = new List<GameObject>();
        public class Baker : Baker<InputPrefabHolderAuthoring>
        {
            public override void Bake(InputPrefabHolderAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var inputPrefabBuffer = AddBuffer<InputPrefab>(entity);
                for (var i = 0; i < authoring.inputPrefabs.Count; i++)
                {
                    var inputPrefab = GetEntity(authoring.inputPrefabs[i], TransformUsageFlags.Dynamic);
                    inputPrefabBuffer.Add(new InputPrefab { Value = inputPrefab });
                }
            }
        }
    }
}
