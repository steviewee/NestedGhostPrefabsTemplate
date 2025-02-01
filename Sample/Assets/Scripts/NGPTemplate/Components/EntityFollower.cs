using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace NGPTemplate.Components
{
    public partial class EntityFollower : IComponentData
    {
        public GameObject followerPrefab;
    }
    public partial class EntityFollowee : IComponentData
    {

    }
}

/*


public class EntityFollower : MonoBehaviour
{
    public bool clientOnly = false;
    public 
    public class EntityFollowerBaker : Baker<EntityFollower>
    {
        public override void Bake(EntityFollower authoring)
        {
            var entity = GetEntity(authoring.gameObject.transform.parent, TransformUsageFlags.Dynamic);

            AddComponent(entity, new JointManager
            {
                jointPrefab = prefabEntity,
            });

        }
    }
}
[BurstCompile]
[UpdateInGroup(typeof(BakingSystemGroup), OrderLast = true)]
[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
public partial struct JointReplacementSystem : ISystem
{
    EntityQuery jointQuery;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        jointQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<PhysicsJoint, PhysicsConstrainedBodyPair>().WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
.WithNone<GhostInstance>()
.Build(ref state);
        state.RequireForUpdate<JointManager>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        Debug.Log("JointReplacementSystemUpdated");

        EntityCommandBuffer ecb = new(Allocator.Temp);
        Entity jointPrefab = SystemAPI.GetSingleton<JointManager>().jointPrefab;
        NativeArray<Entity> entities2 = jointQuery.ToEntityArray(Allocator.Temp);
        Entity newJointEntity = ecb.Instantiate(jointPrefab);
        ecb.DestroyEntity(entities2);
        ecb.Playback(state.EntityManager);
    }
}
*/