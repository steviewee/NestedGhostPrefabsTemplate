using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using NGPTemplate.Components;
namespace NGPTemplate.Authoring
{
    public class DeathVFXSpawnPointAuthoring : MonoBehaviour
    {
        public GameObject socket;
        public class Baker : Baker<DeathVFXSpawnPointAuthoring>
        {
            public override void Bake(DeathVFXSpawnPointAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                AddComponent(entity, new DeathVFXSpawnPoint
                {
                    Value = GetEntity(authoring.socket, TransformUsageFlags.Dynamic)
                });
            }
        }
    }

}