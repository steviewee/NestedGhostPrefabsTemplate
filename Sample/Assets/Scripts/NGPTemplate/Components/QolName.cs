using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.NetCode;

namespace NGPTemplate.Components
{
    [GhostComponent(OwnerSendType = SendToOwnerType.All)]
    public struct QolNameComponent : IComponentData
    {
        [GhostField]
        public FixedString32Bytes name;//
    }
}