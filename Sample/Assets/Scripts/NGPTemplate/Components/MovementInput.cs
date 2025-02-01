////using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace NGPTemplate.Components
{
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct MovementInput : IInputComponentData
    {
        public float2 moveInput;
        public InputEvent jumpPressed;
        public InputEvent crouchPressed;
        public InputEvent crouchReleased;
        public InputEvent shootPressed;
        public InputEvent shootReleased;
        public bool aimHeld;
    }
}