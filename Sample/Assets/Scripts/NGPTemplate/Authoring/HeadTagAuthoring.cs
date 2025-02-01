using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using NGPTemplate.Components;
using Unity.Physics.Authoring;
namespace NGPTemplate.Authoring
{
    public class HeadTagAuthoring : MonoBehaviour
    {
        //[BakingVersion("megacity-metro", 2)]
        public class HeadTagBaker : Baker<HeadTagAuthoring>
        {
            public override void Bake(HeadTagAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                Entity body = Entity.Null;
                if (authoring.gameObject.TryGetComponent<BaseJoint>(out BaseJoint basedJoint))
                {
                    if (basedJoint.ConnectedBody != null)
                    {
                        body = GetEntity(basedJoint.ConnectedBody.gameObject, TransformUsageFlags.Dynamic);
                    }
                }
                 
                AddComponent(entity, new HeadTag
                {
                    cameraY = 0f,
                    body = body,
                });
            }
        }
    }
}


