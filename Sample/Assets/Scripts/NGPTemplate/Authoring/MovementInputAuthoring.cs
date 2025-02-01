//using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using NGPTemplate.Components;

namespace NGPTemplate.Authoring
{
    [DisallowMultipleComponent]
    public class MovementInputAuthoring : MonoBehaviour
    {
        public class Baker : Baker<MovementInputAuthoring>
        {
            public override void Bake(MovementInputAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new MovementInput());
            }
        }
    }
}
