using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
namespace NGPTemplate.Components
{
    public partial struct BodyTag : IComponentData
    {
        public Entity head;

        public float currentMoveSpeed;
    }
}