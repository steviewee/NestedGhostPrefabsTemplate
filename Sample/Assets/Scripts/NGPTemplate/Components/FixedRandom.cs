  using Unity.Entities;
using Unity.NetCode;
using Random = Unity.Mathematics.Random;
namespace NGPTemplate.Components
{  
    public struct FixedRandom : IComponentData
    {
        public Random Random;
    }
}