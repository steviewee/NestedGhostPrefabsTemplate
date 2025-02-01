using System.Collections.Generic;
using System.Diagnostics;
using Unity.Entities;
using UnityEngine;
using UnityEngine.VFX;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace NGPTemplate.Components
{
    public struct VfxDictionaryValueStruct
    {
        public VisualEffect Vfx;
        public VFXEventAttribute Payload;
    }

    public enum VfxType
    {
        MachineGunBullet,
        ShotgunBullet,
        Laser,
        Plasma,
        Rocket,
        Death,
    }
    public enum BulletType
    {
        Other,
        MachineGun,
        Shotgun,
    }

    public struct VfxHitRequest : IComponentData
    {
        public VfxType VfxHitType;
        public float LowCount;
        public float MidCount;
        public float HighCount;
        public Vector3 Position;
        public Vector3 HitNormal;
        public float HitRadius; //Used only in the Rocket projectile
    }
}