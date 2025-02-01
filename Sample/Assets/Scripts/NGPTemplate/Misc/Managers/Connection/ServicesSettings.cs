using UnityEngine;

namespace NGPTemplate.Misc
{
    [CreateAssetMenu(fileName = "ServicesSettings", menuName = "Services/Services Settings")]
    public class ServicesSettings : ScriptableObject
    {
        public MatchmakerType MatchmakerTypeRequested;
        public ConnectionType ConnectionTypeRequested;
    }
}
