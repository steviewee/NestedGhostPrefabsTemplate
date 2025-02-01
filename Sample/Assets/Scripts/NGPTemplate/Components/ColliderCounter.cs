using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;
namespace NGPTemplate.Components
{
    [GhostComponent]
    public struct ColliderCounter : IBufferElementData
    {
        [GhostField]
        public Entity Value;
    }
    /*
    public struct MoveTypeData
    {
        public string id;
        public float anhorAway;
        public float stepHeight;
        public float legAnchor;
        public float legUpSpeed;
        public float legDownSpeed;
        public float legMaxSpeed;
        public float legMinSpeed;
        public float legSpeedMultiplayer;
        public float bodyMoveMultiplayer;
        public float nodeFindAreaStationary;
        public float nodeFindAreaMoving;
        public float maxLegDistance;
        public float graphOffset;
    }
    */
}
