using Unity.Entities;
using UnityEngine;
using NGPTemplate.Components;

namespace NGPTemplate.Authoring
{
    public class VfxAttributeSettingsAuthoring : MonoBehaviour
    {
        public BulletType IsBulletType = BulletType.Other;

        public float LowVfxSpawnCount = 8f;
        public float MidVfxSpawnCount = 32f;
        public float HighVfxSpawnCount = 128f;

        class Baker : Baker<VfxAttributeSettingsAuthoring>
        {
            public override void Bake(VfxAttributeSettingsAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new VfxAttributeSettings()
                {

                    LowVfxSpawnCount = authoring.LowVfxSpawnCount,
                    MidVfxSpawnCount = authoring.MidVfxSpawnCount,
                    HighVfxSpawnCount = authoring.HighVfxSpawnCount,
                });
            }
        }
    }
    

}
