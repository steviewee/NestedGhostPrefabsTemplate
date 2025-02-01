using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using NGPTemplate.Components;
namespace NGPTemplate.Authoring
{
    public class EntityFolowerAuthoring : MonoBehaviour
    {
        /*
        public class EntityFollowerBaker : Baker<EntityFolowerAuthoring>
        {
            public override void Bake(EntityFolowerAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject.transform.parent, TransformUsageFlags.Dynamic);

                AddComponent(entity, new EntityFollower
                {
                    followerPrefab = authoring.gameObject
                });

            }
        }
        */
    }
    public class EntityFoloweeAuthoring : MonoBehaviour
    {
        /*
        public class EntityFollowerBaker : Baker<EntityFoloweeAuthoring>
        {
            public override void Bake(EntityFoloweeAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject.transform.parent, TransformUsageFlags.Dynamic);

                AddComponent(entity, new EntityFollower
                {
                    followerPrefab = authoring.gameObject
                });

            }
        }
        */
    }
    [UpdateInGroup(typeof(PreBakingSystemGroup), OrderLast = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [RequireMatchingQueriesForUpdate]
    public partial class EntityFollowerSpawnerSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float dT = SystemAPI.Time.DeltaTime;
            GameObject[] followers = GameObject.FindGameObjectsWithTag("Follower");
            foreach (GameObject follower in followers)
            {

                if (follower.TryGetComponent<EntityFolowerAuthoring>(out EntityFolowerAuthoring entityFolowerAuthoring))
                {

                }

            }
        }
    }
}
