using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using NGPTemplate.Components;

namespace NGPTemplate.Authoring
{
    public class CameraRequestAuthoring : MonoBehaviour
    {
        public GameObject socket;
        public bool forceNoPivot = false;
        public class Baker : Baker<CameraRequestAuthoring>
        {
            public override void Bake(CameraRequestAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new CameraRequest
                {
                    Value = GetEntity(authoring.socket, TransformUsageFlags.Dynamic)
                }) ;
                AddComponent(entity, new MainCameraPivot
                {
                    forceNoPivot = authoring.forceNoPivot,//
                });
            }
        }
    }



}
