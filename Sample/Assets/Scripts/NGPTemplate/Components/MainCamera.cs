 using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace NGPTemplate.Components
{
    /// <summary>
    /// This Camera component is added to the player character entity by the <see cref="ClientGameSystem"/>
    /// after the server spawns a new character.
    ///
    /// It is used by the <see cref="MainCameraSystem"/> to position the GameObject of the MainCamera at the player position.
    /// </summary>
    [GhostComponent]
    public struct MainCamera : IComponentData
    {
        public MainCamera(float fov)
        {
            BaseFov = fov;
            CurrentFov = fov;
        }
        [GhostField]
        public float BaseFov;
        [GhostField]
        public float CurrentFov;
    }
    [GhostComponent]
    public struct MainCameraPivot : IComponentData
    {
        [GhostField]
        public Entity position;
        [GhostField]
        public bool forceNoPivot;
        [GhostField]
        public Entity pivot;
        [GhostField]
        public float verticalOffset;
    }
}
