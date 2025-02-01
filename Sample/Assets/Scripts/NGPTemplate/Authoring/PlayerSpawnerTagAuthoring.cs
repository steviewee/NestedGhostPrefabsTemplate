using Unity.Entities;
using NGPTemplate.Components;
using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Transforms;
namespace NGPTemplate.Authoring
{
    public class PlayerSpawnerTagAuthoring : MonoBehaviour
    {
        //[BakingVersion("megacity-metro", 2)]
        public class PlayerSpawnerTagBaker : Baker<PlayerSpawnerTagAuthoring>
        {
            public override void Bake(PlayerSpawnerTagAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.None);
                AddComponent<PlayerSpawnerTag>(entity);
            }
        }
    }
}