using Unity.Entities;
using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Transforms;
using Zhorman.NestedGhostPrefabs.Runtime.Components;
namespace Zhorman.NestedGhostPrefabs.Runtime.Authoring
{
    public class SpawnerAuthoring : MonoBehaviour
    {
        public List<GameObject> firstBornList = new List<GameObject>();

        public bool forceFirstBornBufferCreation = false;
        public bool forceSpawneeBufferCreation = false;

        //[BakingVersion("megacity-metro", 2)]
        public class SpawnerBaker : Baker<SpawnerAuthoring>
        {
            public override void Bake(SpawnerAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.None);
                if (authoring.firstBornList.Count > 0)
                {
                    var spawnerBuffer = AddBuffer<FirstBorn>(entity);
                    AddBuffer<Spawnee>(entity);
                    //spawnerBuffer.Add(new FirstBorn { Value = GetEntity(authoring.firstBornList[0], TransformUsageFlags.Dynamic) });
                    for (var i = 0; i < authoring.firstBornList.Count; i++)
                    {
                        var firstBorn = GetEntity(authoring.firstBornList[i], TransformUsageFlags.Dynamic);
                        spawnerBuffer.Add(new FirstBorn { Value = firstBorn });
                    }
                }
                else
                {
                    if (authoring.forceFirstBornBufferCreation)
                    {
                        AddBuffer<FirstBorn>(entity);
                    }
                    if (authoring.forceSpawneeBufferCreation)
                    {
                        AddBuffer<Spawnee>(entity);
                    }
                }

            }
        }
    }
}