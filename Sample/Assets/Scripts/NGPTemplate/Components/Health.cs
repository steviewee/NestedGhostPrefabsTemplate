using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace NGPTemplate.Components
{
    [GhostComponent]
    public struct Health : IComponentData
    {
        public float MaxHealth;
        [GhostField(Quantization = 100)]
        public float CurrentHealth;

        public readonly bool IsDead()
        {
            return CurrentHealth <= 0f;
        }
    }
}