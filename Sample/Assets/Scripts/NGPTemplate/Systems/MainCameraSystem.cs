using Unity.Entities;
using Unity.Transforms;
using NGPTemplate.Components;
using NGPTemplate.Misc;
using System.Diagnostics;

namespace NGPTemplate.Authoring
{
    /// <summary>
    /// Updates the <see cref="MainGameObjectCamera"/> postion to match the current player <see cref="MainCamera"/> component position if it exists.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class MainCameraSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<MainCamera>();
        }

        protected override void OnUpdate()
        {
            if (MainGameObjectCamera.Instance != null)
            {
                try
                {
                    // Move Camera:
                    Entity mainEntityCameraEntity = SystemAPI.GetSingletonEntity<MainCamera>();
                    MainCamera mainCamera = SystemAPI.GetSingleton<MainCamera>();
                    LocalToWorld targetLocalToWorld = SystemAPI.GetComponent<LocalToWorld>(mainEntityCameraEntity);
                    MainGameObjectCamera.Instance.transform.SetPositionAndRotation(targetLocalToWorld.Position,
                        targetLocalToWorld.Rotation);
                    MainGameObjectCamera.Instance.fieldOfView = mainCamera.CurrentFov;
                }
                catch
                {
                    Entities
                        .ForEach((Entity entity,in MainCamera inputData) =>
                        {
                            UnityEngine.Debug.Log($"Found entity{entity.Index}:{entity.Version}");
                        })
                        .ScheduleParallel();
                }

            }
        }
    }
}
