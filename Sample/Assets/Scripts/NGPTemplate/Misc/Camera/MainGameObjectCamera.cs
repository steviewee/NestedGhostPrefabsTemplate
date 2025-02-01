using UnityEngine;

namespace NGPTemplate.Misc
{
    /// <summary>
    /// This class allows the <see cref="MainCameraSystem"/> to sync its position to the current player character position in the Client World.
    /// </summary>
    //[RequireComponent(typeof(Camera))]
    public class MainGameObjectCamera : MonoBehaviour
    {
        public static Camera Instance;

        public GameObject Camera;

        void Awake()
        {
            // We already have a main camera and don't need a new one.
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            if(Camera == null)
            {
                if(gameObject.TryGetComponent< Camera>(out var cam))
                {
                    Instance = cam;
                }
                else
                {
                    Instance = transform.GetComponentInChildren<Camera>();
                }
            }
            else
            {
                Instance = Camera.GetComponent<Camera>();
            }
        }

        void OnDestroy()
        {
            if (Instance == GetComponent<Camera>())
            {
                Instance = null;
            }
        }
    }
}
