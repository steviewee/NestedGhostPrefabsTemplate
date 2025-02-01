using Unity.Entities;
using UnityEngine;
using NGPTemplate.Components;
namespace NGPTemplate.Authoring
{
    [DisallowMultipleComponent]
    public class MainCameraAuthoring : MonoBehaviour
    {
        public float Fov = 75f;

        public class Baker : Baker<MainCameraAuthoring>
        {
            public override void Bake(MainCameraAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new MainCamera(authoring.Fov));
            }
        }
    }

}
