using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace NGPTemplate.Components
{
    public struct ConnectionReference : IComponentData
    {
        public Entity Value;
    }
}