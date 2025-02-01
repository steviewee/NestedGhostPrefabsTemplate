using Unity.Entities;
using UnityEngine;

namespace Zhorman.JointModificators.Runtime
{
    public struct RootEntityLink : IComponentData
    {
        public Entity Value;
    }
}

