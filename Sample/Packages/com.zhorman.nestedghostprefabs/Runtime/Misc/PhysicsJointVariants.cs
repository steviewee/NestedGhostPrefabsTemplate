#if USING_JOINT_MODIFICATORS && USING_UNITY_PHYSICS


using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.GraphicsIntegration;
using Unity.NetCode;
namespace Zhorman.NestedGhostPrefabs.Runtime.Components
{
    /// <summary>
    /// Default serialization variant for the PhysicsConstrainedBodyPair. Necessary to synchronize joints
    /// </summary>
    [GhostComponentVariation(typeof(PhysicsConstrainedBodyPair), nameof(PhysicsConstrainedBodyPair))]
//    [GhostEnabledBit]
    [GhostComponent(PrefabType = GhostPrefabType.All, SendTypeOptimization = GhostSendType.OnlyPredictedClients, SendDataForChildEntity = true)]//idk
    public struct PhysicsConstrainedBodyPairDefaultVariant
    {
        [GhostField]
        public EntityPair Entities;

        /// <summary>
        /// Specifies whether the two bodies in the pair should generate contact events.
        /// </summary>
        [GhostField]
        public int EnableCollision;
    }


    /// <summary>
    /// Default serialization variant for the PhysicsJointCompanion
    /// </summary>
    [GhostComponentVariation(typeof(PhysicsJointCompanion), nameof(PhysicsJointCompanion))]
//    [GhostEnabledBit]
    [GhostComponent(PrefabType = GhostPrefabType.All, SendTypeOptimization = GhostSendType.OnlyPredictedClients, SendDataForChildEntity = true)]//idk
    public struct PhysicsJointCompanionDefaultVariant
    {
        [GhostField]
        public Entity JointEntity;
    }
    [GhostComponentVariation(typeof(PhysicsJoint), nameof(PhysicsJoint))]
//    [GhostEnabledBit]
    [GhostComponent(PrefabType = GhostPrefabType.All, SendTypeOptimization = GhostSendType.OnlyPredictedClients, SendDataForChildEntity = true)]//idk
    public struct PhysicsJointDefaultVariant
    {
        [GhostField(Composite = true, Quantization = 0, Smoothing = SmoothingAction.InterpolateAndExtrapolate)]
        public BodyFrame m_BodyAFromJoint;
        [GhostField(Composite = true, Quantization = 0, Smoothing = SmoothingAction.InterpolateAndExtrapolate)]
        public BodyFrame m_BodyBFromJoint;
        [GhostField]
        public byte m_Version;
        [GhostField]
        public JointType m_JointType;
        [GhostField(Composite = true, Quantization = 0)]
        public ConstraintBlock3 m_Constraints;
    }

    sealed partial class MyComponentDefaultVariantSystem : DefaultVariantSystemBase
    {
        protected override void RegisterDefaultVariants(Dictionary<ComponentType, Rule> defaultVariants)
        {
            defaultVariants.Add(typeof(PhysicsConstrainedBodyPair), Rule.ForAll(typeof(PhysicsConstrainedBodyPairDefaultVariant)));
            defaultVariants.Add(typeof(PhysicsJointCompanion), Rule.ForAll(typeof(PhysicsJointCompanionDefaultVariant)));
            defaultVariants.Add(typeof(PhysicsJoint), Rule.ForAll(typeof(PhysicsJointDefaultVariant)));
        }
    }
}

#endif