using Unity.Entities;
using Zhorman.NestedGhostPrefabs.Runtime.Components;
using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Transforms;
namespace Zhorman.NestedGhostPrefabs.Runtime.Authoring
{
    public class ImmitatedParentAuthoring : MonoBehaviour
    {

        public GameObject parent;

        //[BakingVersion("megacity-metro", 2)]
        public class ImmitatedParentBaker : Baker<ImmitatedParentAuthoring>
        {
            public override void Bake(ImmitatedParentAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                if (authoring.parent != null)
                {
                    AddComponent<ImmitatedParentReference>(entity,new ImmitatedParentReference { Value = GetEntity(authoring.parent, TransformUsageFlags.Dynamic) });//add stuff
                }

            }
        }
    }
}