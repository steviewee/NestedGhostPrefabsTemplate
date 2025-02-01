using Unity.Entities;
using UnityEngine;
using System.Collections.Generic;
namespace Zhorman.NestedGhostPrefabs.Runtime
{
    public class TestAuthoring : MonoBehaviour
    {
        public GameObject testTarget;

        public class TestBaker : Baker<TestAuthoring>
        {
            public override void Bake(TestAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                var testTarget = GetEntity(authoring.testTarget, TransformUsageFlags.Dynamic);

                AddComponent(entity, new Test { ghostedField = testTarget, non_GhostedField=testTarget });


            }
        }
    }
}