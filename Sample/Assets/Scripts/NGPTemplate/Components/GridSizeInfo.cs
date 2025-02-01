using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Burst;
using NGPTemplate.Components;
namespace NGPTemplate.Components
{
    [GhostComponent]
    [GhostEnabledBit]
    public struct GridSizeInfo : IComponentData, IEnableableComponent
    {
        [GhostField]
        public int xGridSize;
        [GhostField]
        public int zGridSize;
        [GhostField]
        public bool empty;
    }
}