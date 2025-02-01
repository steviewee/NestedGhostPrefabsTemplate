 using Unity.Entities;
using UnityEngine;

namespace NGPTemplate.Components
{
    public struct VfxAttributeSettings : IComponentData
    {

        public float LowVfxSpawnCount;
        public float MidVfxSpawnCount;
        public float HighVfxSpawnCount;
    }
}