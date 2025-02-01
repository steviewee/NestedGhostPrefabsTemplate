using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using Unity.Burst;
using Unity.Physics;

namespace Zhorman.JointModificators.Runtime.Components
{

    public struct JointModificator : IComponentData
    {
        public int jointHash;
        public PhysicsJoint targetJoint;
        public PhysicsConstrainedBodyPair physicsConstrainedBodyPair;
        public uint worldIndex;
        public bool companionship;
    }
}