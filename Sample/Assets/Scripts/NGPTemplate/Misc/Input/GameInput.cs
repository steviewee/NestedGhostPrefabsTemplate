using UnityEngine;

namespace NGPTemplate.Misc
{
    /// <summary>
    /// This class is used to initialize the <see cref="FPSInputActions"/> from the InputSystem and access it from the Gameplay and Debug systems.
    /// </summary>
    public static class GameInput
    {
        /// <summary>
        /// This initialization is required in the Editor to avoid the instance from a previous Playmode to stay alive in the next session.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RuntimeInitializeOnLoad()
        {
            Debug.Log("GameInput initialized");
            Actions = new FPSInputActions();
            Actions.Enable();
        }

        public static FPSInputActions Actions { get; private set; } = null!;
    }
}
