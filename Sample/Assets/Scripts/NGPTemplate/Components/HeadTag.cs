using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
namespace NGPTemplate.Components
{
    [GhostComponent]
    public partial struct HeadTag : IComponentData
    {
        [GhostField]
        public float cameraY;
        [GhostField]
        public Entity body;
    }
}