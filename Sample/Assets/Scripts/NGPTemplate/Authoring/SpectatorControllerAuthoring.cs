using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using NGPTemplate.Components;
namespace NGPTemplate.Authoring
{
    public class SpectatorControllerAuthoring : MonoBehaviour
    {
        public SpectatorController.Parameters Parameters;

        public class Baker : Baker<SpectatorControllerAuthoring>
        {
            public override void Bake(SpectatorControllerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new SpectatorController { Params = authoring.Parameters });
            }
        }
    }

}
