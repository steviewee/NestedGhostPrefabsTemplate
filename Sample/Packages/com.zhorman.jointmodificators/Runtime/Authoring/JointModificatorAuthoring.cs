using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Zhorman.JointModificators.Runtime.Components;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Transforms;
using UnityEngine;
using FloatRange = Unity.Physics.Math.FloatRange;
using Unity.Assertions;
using JetBrains.Annotations;
namespace Zhorman.JointModificators.Runtime.Authoring{
    public class JointModificatorAuthoring : MonoBehaviour
    {
        public BaseJoint targetJoint;  

        public int targetJointIndex=0;
        public class JointModificatorBaker : Baker<JointModificatorAuthoring>
        {
            PhysicsConstrainedBodyPair GetConstrainedBodyPair(BaseJoint authoring)
            {
                return new PhysicsConstrainedBodyPair(
                    authoring.LocalBody == null ? Entity.Null : GetEntity(authoring.LocalBody.gameObject, TransformUsageFlags.Dynamic),
                    authoring.ConnectedBody == null ? Entity.Null : GetEntity(authoring.ConnectedBody, TransformUsageFlags.Dynamic),
                    authoring.EnableCollision
                );
            }
            uint GetWorldIndexFromBaseJoint(BaseJoint authoring)
            {
                var physicsBody = GetComponent<PhysicsBodyAuthoring>(authoring);
                uint worldIndex = physicsBody.WorldIndex;
                if (authoring.ConnectedBody == null)
                {
                    return worldIndex;
                }

                var connectedBody = GetComponent<PhysicsBodyAuthoring>(authoring.ConnectedBody);
                if (connectedBody != null)
                {
                    Unity.Assertions.Assert.AreEqual(worldIndex, connectedBody.WorldIndex);
                }

                return worldIndex;
            }
            public static BaseJoint FindBaseJointInParent(Transform child)
            {
                Transform current = child.parent;

                while (current != null)
                {
                    BaseJoint joint = current.GetComponent<BaseJoint>();
                    if (joint != null)
                    {
                        return joint;
                    }
                    current = current.parent;
                }

                return null; // Return null if no BaseJoint is found.
            }
            public override void Bake(JointModificatorAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                AddComponent(entity, new RootEntityLink
                {
                    Value = GetEntity(authoring.transform.root.gameObject, TransformUsageFlags.Dynamic),
                });


                BaseJoint targetJoint = authoring.targetJoint;
                if (targetJoint == null)
                {
                    targetJoint = FindBaseJointInParent(authoring.transform);
                    if (targetJoint == null)
                    {
                        Debug.LogWarning($"JointModificatorAuthoring, null target joint on {authoring.gameObject.name}");
                        AddComponent(entity, new FailedJointModificator
                        {

                        });
                        return;
                    }

                }
                if (!targetJoint.enabled)
                {
                    Debug.LogWarning($"JointModificatorAuthoring, disabled target joint on {authoring.gameObject.name}");
                    AddComponent(entity, new FailedJointModificator
                    {

                    });
                    return;
                }
                if (targetJoint is LimitedDistanceJoint)
                {
                    LimitedDistanceJoint limitedDistanceJoint = targetJoint as LimitedDistanceJoint;
                    var physicsJoint = PhysicsJoint.CreateLimitedDistance(limitedDistanceJoint.PositionLocal, limitedDistanceJoint.PositionInConnectedEntity, new Unity.Physics.Math.FloatRange(limitedDistanceJoint.MinDistance, limitedDistanceJoint.MaxDistance));
                    physicsJoint.SetImpulseEventThresholdAllConstraints(limitedDistanceJoint.MaxImpulse);

                    var constraintBodyPair = GetConstrainedBodyPair(limitedDistanceJoint);
                    uint worldIndex = GetWorldIndexFromBaseJoint(limitedDistanceJoint);
                    /*
                    var jm = AddBuffer<JointModificator>(entity);
                    jm.Add(new JointModificator
                    {
                        //copyModificatorToJoint = authoring.copyModificatorToJoint,
                        physicsConstrainedBodyPair = constraintBodyPair,
                        worldIndex = worldIndex,
                    });
                    */

                    AddComponent(entity, new JointModificator
                    {

                        physicsConstrainedBodyPair = constraintBodyPair,

                        jointHash = physicsJoint.GetHashCode(),
                        worldIndex = worldIndex,
                    });
                    Debug.Log("JointModificatorAuthoring baked LimitedDistanceJoint");
                }
                else if (targetJoint is LimitDOFJoint)
                {
                    LimitDOFJoint limitDOFJoint = targetJoint as LimitDOFJoint;
                    RigidTransform bFromA = math.mul(math.inverse(limitDOFJoint.worldFromB), limitDOFJoint.worldFromA);
                    PhysicsJoint physicsJoint = limitDOFJoint.CreateLimitDOFJoint(bFromA);
                    var constraintBodyPair = GetConstrainedBodyPair(limitDOFJoint);
                    uint worldIndex = GetWorldIndexFromBaseJoint(limitDOFJoint);/*
                    var jm = AddBuffer<JointModificator>(entity);
                    jm.Add(new JointModificator
                    {

                        physicsConstrainedBodyPair = constraintBodyPair,
                        worldIndex = worldIndex,
                    });*/

                    AddComponent(entity, new JointModificator
                    {

                        physicsConstrainedBodyPair = constraintBodyPair,
                        jointHash = physicsJoint.GetHashCode(),
                        worldIndex = worldIndex,
                    });

                    Debug.Log($"JointModificatorAuthoring baked LimitDOFJoint");
                }
                else if (targetJoint is LimitedHingeJoint)
                {
                    LimitedHingeJoint limitedHingeJoint = targetJoint as LimitedHingeJoint;
                    var physicsJoint = PhysicsJoint.CreateLimitedHinge(
                                    new BodyFrame
                                    {
                                        Axis = math.normalize(limitedHingeJoint.HingeAxisLocal),
                                        PerpendicularAxis = math.normalize(limitedHingeJoint.PerpendicularAxisLocal),
                                        Position = limitedHingeJoint.PositionLocal
                                    },
                                    new BodyFrame
                                    {
                                        Axis = math.normalize(limitedHingeJoint.HingeAxisInConnectedEntity),
                                        PerpendicularAxis = math.normalize(limitedHingeJoint.PerpendicularAxisInConnectedEntity),
                                        Position = limitedHingeJoint.PositionInConnectedEntity
                                    },
                                    math.radians(new Unity.Physics.Math.FloatRange(limitedHingeJoint.MinAngle, limitedHingeJoint.MaxAngle))
                                );

                    physicsJoint.SetImpulseEventThresholdAllConstraints(limitedHingeJoint.MaxImpulse);

                    var constraintBodyPair = GetConstrainedBodyPair(limitedHingeJoint);
                    uint worldIndex = GetWorldIndexFromBaseJoint(limitedHingeJoint);/*
                    var jm = AddBuffer<JointModificator>(entity);
                    jm.Add(new JointModificator
                    {

                        physicsConstrainedBodyPair = constraintBodyPair,
                        worldIndex = worldIndex,
                    }); */

                    AddComponent(entity, new JointModificator
                    {

                        physicsConstrainedBodyPair = constraintBodyPair,
                        jointHash = physicsJoint.GetHashCode(),
                        worldIndex = worldIndex,
                    });

                    Debug.Log("JointModificatorAuthoring baked LimitedHingeJoint");
                }
                else if (targetJoint is PrismaticJoint)
                {
                    PrismaticJoint prismaticJoint = targetJoint as PrismaticJoint;
                    var physicsJoint = PhysicsJoint.CreatePrismatic(
                                    new BodyFrame
                                    {
                                        Axis = prismaticJoint.AxisLocal,
                                        PerpendicularAxis = prismaticJoint.PerpendicularAxisLocal,
                                        Position = prismaticJoint.PositionLocal
                                    },
                                    new BodyFrame
                                    {
                                        Axis = prismaticJoint.AxisInConnectedEntity,
                                        PerpendicularAxis = prismaticJoint.PerpendicularAxisInConnectedEntity,
                                        Position = prismaticJoint.PositionInConnectedEntity
                                    },
                                    new Unity.Physics.Math.FloatRange(prismaticJoint.MinDistanceOnAxis, prismaticJoint.MaxDistanceOnAxis)
                                );

                    physicsJoint.SetImpulseEventThresholdAllConstraints(prismaticJoint.MaxImpulse);



                    var constraintBodyPair = GetConstrainedBodyPair(prismaticJoint);
                    uint worldIndex = GetWorldIndexFromBaseJoint(prismaticJoint);/*
                    var jm = AddBuffer<JointModificator>(entity);
                    jm.Add(new JointModificator
                    {

                        physicsConstrainedBodyPair = constraintBodyPair,
                        worldIndex = worldIndex,
                    }); */

                    AddComponent(entity, new JointModificator
                    {

                        physicsConstrainedBodyPair = constraintBodyPair,
                        jointHash = physicsJoint.GetHashCode(),
                        worldIndex = worldIndex,
                    });

                    Debug.Log("JointModificatorAuthoring baked PrismaticJoint");
                }
                else if (targetJoint is RagdollJoint)
                {
                    RagdollJoint ragdollJoint = targetJoint as RagdollJoint;
                    PhysicsJoint.CreateRagdoll(
                                    new BodyFrame { Axis = ragdollJoint.TwistAxisLocal, PerpendicularAxis = ragdollJoint.PerpendicularAxisLocal, Position = ragdollJoint.PositionLocal },
                                    new BodyFrame { Axis = ragdollJoint.TwistAxisInConnectedEntity, PerpendicularAxis = ragdollJoint.PerpendicularAxisInConnectedEntity, Position = ragdollJoint.PositionInConnectedEntity },
                                    math.radians(ragdollJoint.MaxConeAngle),
                                    math.radians(new Unity.Physics.Math.FloatRange(ragdollJoint.MinPerpendicularAngle, ragdollJoint.MaxPerpendicularAngle)),
                                    math.radians(new Unity.Physics.Math.FloatRange(ragdollJoint.MinTwistAngle, ragdollJoint.MaxTwistAngle)),
                                    out var primaryCone,
                                    out var perpendicularCone
                                );

                    primaryCone.SetImpulseEventThresholdAllConstraints(ragdollJoint.MaxImpulse);
                    perpendicularCone.SetImpulseEventThresholdAllConstraints(ragdollJoint.MaxImpulse);


                    var constraintBodyPair = GetConstrainedBodyPair(ragdollJoint);
                    uint worldIndex = GetWorldIndexFromBaseJoint(ragdollJoint);
                    AddBuffer<PhysicsJointCompanion>(entity);
                    if (authoring.targetJointIndex == 1)
                    {
                        AddComponent(entity, new JointModificator
                        {

                            //targetJoint = primaryCone,
                            physicsConstrainedBodyPair = constraintBodyPair,
                            jointHash = primaryCone.GetHashCode(),
                            worldIndex = worldIndex,
                            companionship = true,
                        });

                    }
                    else
                    {
                        AddComponent(entity, new JointModificator
                        {

                            //targetJoint = perpendicularCone,
                            physicsConstrainedBodyPair = constraintBodyPair,
                            jointHash = perpendicularCone.GetHashCode(),
                            worldIndex = worldIndex,
                            companionship = true,
                        });
                    }
                    /*
                    var jm = AddBuffer<JointModificator>(entity);
                    jm.Add(new JointModificator
                    {
                        //targetJoint = primaryCone,
                        physicsConstrainedBodyPair = constraintBodyPair,
                        worldIndex = worldIndex,
                    });
                    jm.Add(new JointModificator
                    {
                        //targetJoint = perpendicularCone,
                        physicsConstrainedBodyPair = constraintBodyPair,
                        worldIndex = worldIndex,
                    });
                    /*
                    AddComponent(entity, new JointModificator
                    {
                        //targetJoint = primaryCone,
                        secondaryJoint = perpendicularCone,
                        physicsConstrainedBodyPair = constraintBodyPair,
                        jointHash=physicsJoint.GetHashCode(),
                        worldIndex = worldIndex,
                    });
                    */
                    Debug.Log("JointModificatorAuthoring baked RagdollJoint");
                }
                else if (targetJoint is RigidJoint)
                {
                    RigidJoint rigidJoint = targetJoint as RigidJoint;
                    var physicsJoint = PhysicsJoint.CreateFixed(
                                    new RigidTransform(rigidJoint.OrientationLocal, rigidJoint.PositionLocal),
                                    new RigidTransform(rigidJoint.OrientationInConnectedEntity, rigidJoint.PositionInConnectedEntity)
                                );

                    physicsJoint.SetImpulseEventThresholdAllConstraints(rigidJoint.MaxImpulse);


                    var constraintBodyPair = GetConstrainedBodyPair(rigidJoint);
                    uint worldIndex = GetWorldIndexFromBaseJoint(rigidJoint);/*
                    var jm = AddBuffer<JointModificator>(entity);
                    jm.Add(new JointModificator
                    {

                        physicsConstrainedBodyPair = constraintBodyPair,
                        worldIndex = worldIndex,
                    }); */

                    AddComponent(entity, new JointModificator
                    {

                        physicsConstrainedBodyPair = constraintBodyPair,
                        jointHash = physicsJoint.GetHashCode(),
                        worldIndex = worldIndex,
                    });

                    Debug.Log("JointModificatorAuthoring baked RigidJoint");
                }
                else if (targetJoint is FreeHingeJoint)
                {
                    FreeHingeJoint freeHingeJoint = targetJoint as FreeHingeJoint;
                    Unity.Physics.Math.CalculatePerpendicularNormalized(freeHingeJoint.HingeAxisLocal, out var perpendicularLocal, out _);
                    Unity.Physics.Math.CalculatePerpendicularNormalized(freeHingeJoint.HingeAxisInConnectedEntity, out var perpendicularConnected, out _);

                    var physicsJoint = PhysicsJoint.CreateHinge(
                        new BodyFrame { Axis = freeHingeJoint.HingeAxisLocal, Position = freeHingeJoint.PositionLocal, PerpendicularAxis = perpendicularLocal },
                        new BodyFrame { Axis = freeHingeJoint.HingeAxisInConnectedEntity, Position = freeHingeJoint.PositionInConnectedEntity, PerpendicularAxis = perpendicularConnected }
                    );

                    physicsJoint.SetImpulseEventThresholdAllConstraints(freeHingeJoint.MaxImpulse);


                    var constraintBodyPair = GetConstrainedBodyPair(freeHingeJoint);
                    uint worldIndex = GetWorldIndexFromBaseJoint(freeHingeJoint);
                    /*
                    var jm = AddBuffer<JointModificator>(entity);
                    jm.Add(new JointModificator
                    {

                        physicsConstrainedBodyPair = constraintBodyPair,
                        worldIndex = worldIndex,
                    }); */

                    AddComponent(entity, new JointModificator
                    {

                        physicsConstrainedBodyPair = constraintBodyPair,
                        jointHash = physicsJoint.GetHashCode(),
                        worldIndex = worldIndex,
                    });

                    Debug.Log("JointModificatorAuthoring baked FreeHingeJoint");
                }
                else if (targetJoint is ConfigurableEntityJointAuthoring)
                {
                    ConfigurableEntityJointAuthoring configurableEntityJoint = targetJoint as ConfigurableEntityJointAuthoring;

                    (bool linear, bool angular) = GetConfigurableJointForModificator(configurableEntityJoint, out PhysicsConstrainedBodyPair physicsConstrainedBodyPair, out PhysicsJoint linearJoint, out PhysicsJoint angularJoint);

                    Debug.LogWarning($"linear && angular?  {linear} && {angular}");

                    if (linear|| angular)
                    {
                        bool companionship = false;
                        if (linear && angular)
                        {
                            companionship = true;
                            AddBuffer<PhysicsJointCompanion>(entity);
                        }
                        if ((authoring.targetJointIndex == 0 && linear) ||!angular)
                        {
                            AddComponent(entity, new JointModificator
                            {

                                physicsConstrainedBodyPair = physicsConstrainedBodyPair,
                                jointHash = linearJoint.GetHashCode(),
                                targetJoint = linearJoint,
                                worldIndex = 0,
                                companionship = companionship,
                            });
                        }
                        else
                        {
                            AddComponent(entity, new JointModificator
                            {
                                physicsConstrainedBodyPair = physicsConstrainedBodyPair,
                                jointHash = angularJoint.GetHashCode(),
                                targetJoint = angularJoint,
                                worldIndex = 0,
                                companionship = companionship,
                            });
                        }

                    }
                    else
                    {
                        AddComponent(entity, new FailedJointModificator
                        {

                        });
                    }

                    
                }
                else
                {
                    Debug.Log($"JointModificatorAuthoring has no idea what this joint could be on {authoring.gameObject.name}");
                    AddComponent(entity, new FailedJointModificator
                    {

                    });
                }

            }


            public (bool, bool) GetConfigurableJointForModificator(ConfigurableEntityJointAuthoring authoring, out PhysicsConstrainedBodyPair physicsConstrainedBodyPair, out PhysicsJoint linearJoint, out PhysicsJoint angularJoint)
            {
                physicsConstrainedBodyPair = new PhysicsConstrainedBodyPair { };
                linearJoint = new PhysicsJoint { };
                angularJoint = new PhysicsJoint { };


                var localEntity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                authoring.UpdateAuto();
                bool connectedBodyIsNull = authoring.ConnectedBody == null;
                var connectedEntity = connectedBodyIsNull ? Entity.Null : GetEntity(authoring.ConnectedBody.gameObject, TransformUsageFlags.Dynamic);

                var jointFrameOrientation = GetJointFrameOrientation(authoring.axis, authoring.secondaryAxis);

                var allConstraints = new FixedList512Bytes<Constraint>();

                (float constrainedMass, float constrainedInertia) = GetConstrainedBodyMassAndInertia(authoring.LocalBody, authoring.ConnectedBody, !connectedBodyIsNull);


                bool addLinearJoint = ConvertLinearDofs(authoring, constrainedMass, jointFrameOrientation, ref allConstraints);
                bool addAngularJoint = ConvertAngularDofs(authoring, constrainedInertia, jointFrameOrientation, ref allConstraints);

                if (!(addLinearJoint || addAngularJoint))
                {
                    return (false,false);
                }

                var bodyAFromJoint = new BodyFrame { };
                var bodyBFromJoint = new BodyFrame { };
                SetupBodyFrames(jointFrameOrientation, authoring, connectedEntity, ref bodyAFromJoint, ref bodyBFromJoint);

                physicsConstrainedBodyPair = GetConstrainedBodyPair(authoring, localEntity, connectedEntity);

                var block = new FixedList512Bytes<Constraint>();
                NativeArray<PhysicsJoint> joints = new NativeArray<PhysicsJoint>(2, Allocator.Temp);
                for (int i = 0; i < allConstraints.Length; i += 3)
                {
                    block.Clear();

                    int countInBlock = math.min(allConstraints.Length - i, 3);

                    for (int j = i; j < i + countInBlock; ++j)
                        block.Add(allConstraints[j]);

                    var thisJoint = new PhysicsJoint();
                    thisJoint.SetConstraints(block);
                    thisJoint.BodyAFromJoint = bodyAFromJoint;
                    thisJoint.BodyBFromJoint = bodyBFromJoint;

                    thisJoint.SetImpulseEventThresholdAllConstraints(
                        authoring.breakForce * Time.fixedDeltaTime,
                        authoring.breakTorque * Time.fixedDeltaTime
                    );
                    joints[i / 3] = thisJoint;
                }
                linearJoint = joints[0];
                angularJoint = joints[1];
                return (addLinearJoint, addAngularJoint);
            }
            public quaternion GetJointFrameOrientation(float3 axis, float3 secondaryAxis)
            {
                // classic Unity uses a different approach than BodyFrame.ValidateAxes() for ortho-normalizing degenerate inputs
                // ortho-normalizing here ensures behavior is consistent with classic Unity
                var a = (Vector3)axis;
                var p = (Vector3)secondaryAxis;
                Vector3.OrthoNormalize(ref a, ref p);
                return new BodyFrame { Axis = a, PerpendicularAxis = p }.AsRigidTransform().rot;
            }
            public (float, float) GetConstrainedBodyMassAndInertia(PhysicsBodyAuthoring bodyA, PhysicsBodyAuthoring bodyB, bool bodyBExists)
            {
                float mass = 0;
                float approxInertia = 0;



                if (bodyA.MotionType == BodyMotionType.Dynamic)
                {
                    MassDistribution massDistributionA = bodyA.CustomMassDistribution;
                    mass += bodyA.Mass;
                    approxInertia += 1f / 3f * (massDistributionA.InertiaTensor.x + massDistributionA.InertiaTensor.y + massDistributionA.InertiaTensor.z);
                }

                if (bodyBExists && bodyB.MotionType == BodyMotionType.Dynamic)
                {
                    MassDistribution massDistributionB = bodyB.CustomMassDistribution;
                    mass += bodyB.Mass;
                    approxInertia += 1f / 3f * (massDistributionB.InertiaTensor.x + massDistributionB.InertiaTensor.y + massDistributionB.InertiaTensor.z);
                }

                return (mass > 0 ? mass : 1.0f, approxInertia > 0 ? approxInertia : 1.0f);
            }
            public void IdentifyIfLinearMotor(ConfigurableEntityJointAuthoring joint, out bool3 isPositionMotor, out bool3 isLinearVelocityMotor, out float3 maxForceForLinearMotor)
            {
                // shortcuts for the code below
                float3 targetPosition = joint.targetPosition;
                float3 targetVelocity = joint.targetVelocity;
                var xDrive = new JointDrive { positionSpring = joint.xDrivePositionSpring, positionDamper = joint.xDrivePositionDamper, maximumForce = joint.xDriveMaximumForce, useAcceleration = joint.useXDriveAcceleration };
                var yDrive = new JointDrive { positionSpring = joint.yDrivePositionSpring, positionDamper = joint.yDrivePositionDamper, maximumForce = joint.yDriveMaximumForce, useAcceleration = joint.useYDriveAcceleration };
                var zDrive = new JointDrive { positionSpring = joint.zDrivePositionSpring, positionDamper = joint.zDrivePositionDamper, maximumForce = joint.zDriveMaximumForce, useAcceleration = joint.useZDriveAcceleration };
                var name = joint.name;

                CheckPerAxis(targetPosition.x, targetVelocity.x, xDrive.positionSpring, xDrive.positionDamper, xDrive.maximumForce, name,
                    out isPositionMotor.x, out isLinearVelocityMotor.x);

                CheckPerAxis(targetPosition.y, targetVelocity.y, yDrive.positionSpring, yDrive.positionDamper, yDrive.maximumForce, name,
                    out isPositionMotor.y, out isLinearVelocityMotor.y);

                CheckPerAxis(targetPosition.z, targetVelocity.z, zDrive.positionSpring, zDrive.positionDamper, zDrive.maximumForce, name,
                    out isPositionMotor.z, out isLinearVelocityMotor.z);

                maxForceForLinearMotor = new float3(xDrive.maximumForce, yDrive.maximumForce, zDrive.maximumForce);
            }
            public static void CheckPerAxis(float targetPosition, float targetVelocity, float spring, float damper, float force, string name,
                out bool isPositionMotor, out bool isVelocityMotor)
            {
                // for comparisons we must use an absolute value
                targetPosition = math.abs(targetPosition);
                targetVelocity = math.abs(targetVelocity);
                spring = math.abs(spring);
                damper = math.abs(damper);
                force = math.abs(force);

                float threshold = 0.0001f;

                // Initialize for early exit / default values
                isPositionMotor = false;
                isVelocityMotor = false;

                if (force <= threshold) return; // if no force applied at all, it isn't a motor

                // If target position==0 AND target velocity==0 AND spring==0 AND damper== 0. This is not a motor.
                if (targetPosition <= threshold && targetVelocity <= threshold &&
                    spring <= threshold && damper <= threshold)
                {
                    // Nothing is set. This is not a motor. Likely most common case
                    return;
                }

                if (targetPosition > threshold && targetVelocity > threshold)
                {
                    Assert.IsTrue(true,
                        $"Configurable Joint Baking Failed for {name}: Invalid configuration. Both target position and target velocity are non-zero.");
                    return;
                }

                // If the target velocity is set but the target position is not, then it is a velocity motor (this does not guarantee the motor will function)
                if (targetPosition <= threshold && targetVelocity > threshold)
                {
                    isVelocityMotor = true;
                    return;
                }

                // If the target position is set but the target velocity is not, then it is a position motor (this does not guarantee the motor will function)
                if (targetPosition > threshold && targetVelocity <= threshold)
                {
                    isPositionMotor = true;
                    return;
                }

                // If the target position and target velocity are both zero, then it depends on the value of spring and damping
                if (targetPosition <= threshold && targetVelocity <= threshold)
                {
                    if (spring <= threshold)
                    {
                        if (damper > threshold)
                        {
                            isVelocityMotor = true;  // spring=0, damper!=0
                        }
                        //else // case already covered (nothing is set)
                    }
                    else
                    {
                        // Regardless of damping value, if spring!=0, this is a position motor
                        isPositionMotor = true;
                    }
                }
            }
            public void IdentifyIfAngularMotor(ConfigurableEntityJointAuthoring joint, out bool3 isPositionMotor, out bool3 isVelocityMotor, out float3 maxForceForMotor)
            {
                // shortcuts for the code below
                quaternion targetRotation = joint.targetRotation;
                float3 targetVelocity = joint.targetAngularVelocity;
                var xDrive = new JointDrive { positionSpring = joint.angularXDrivePositionSpring, positionDamper = joint.angularXDrivePositionDamper, maximumForce = joint.angularXDriveMaximumForce, useAcceleration = joint.useAngularXDriveAcceleration };
                var yzDrive = new JointDrive { positionSpring = joint.angularYZDrivePositionSpring, positionDamper = joint.angularYZDrivePositionDamper, maximumForce = joint.angularYZDriveMaximumForce, useAcceleration = joint.useAngularYZDriveAcceleration };
                var name = joint.name;

                CheckPerAxis(targetRotation.value.x, targetVelocity.x, xDrive.positionSpring, xDrive.positionDamper, xDrive.maximumForce, name,
                    out isPositionMotor.x, out isVelocityMotor.x);

                CheckPerAxis(targetRotation.value.y, targetVelocity.y, yzDrive.positionSpring, yzDrive.positionDamper, yzDrive.maximumForce, name,
                    out isPositionMotor.y, out isVelocityMotor.y);

                CheckPerAxis(targetRotation.value.z, targetVelocity.z, yzDrive.positionSpring, yzDrive.positionDamper, yzDrive.maximumForce, name,
                    out isPositionMotor.z, out isVelocityMotor.z);

                maxForceForMotor = new float3(xDrive.maximumForce, yzDrive.maximumForce, yzDrive.maximumForce);
            }
            public void ConvertSpringDamperSettings(float inSpringConstant, float inDampingCoefficient, float inConstrainedMass,
                out float outSpringFrequency, out float outDampingRatio)
            {
                const float threshold = 0.001f;

                if (inSpringConstant <= threshold)
                {
                    // Case: k=0, c=0: Want a stiff constraint
                    // Case: k=0, c!=0: Velocity motor case. Use damping coefficient as if it as a ratio (an approximation making it easier to tune)
                    outSpringFrequency = Constraint.DefaultSpringFrequency;
                    outDampingRatio = inDampingCoefficient <= threshold ?
                        Constraint.DefaultDampingRatio : inDampingCoefficient;
                }
                else
                {
                    // Case: k!=0, c=0: Calculate for k and use damping ratio as 0
                    // Case: k!=0, c!=0: Calculate both terms
                    outSpringFrequency = JacobianUtilities.CalculateSpringFrequencyFromSpringConstant(inSpringConstant, inConstrainedMass);
                    outDampingRatio = inDampingCoefficient <= threshold ?
                        0.0f : JacobianUtilities.CalculateDampingRatio(inSpringConstant, inDampingCoefficient, inConstrainedMass);
                }
            }
            public bool3 GetAxesWithMotionType(
        ConfigurableJointMotion motionType,
        ConfigurableJointMotion x, ConfigurableJointMotion y, ConfigurableJointMotion z
    ) => new bool3(x == motionType, y == motionType, z == motionType);
            public bool ConvertLinearDofs(ConfigurableEntityJointAuthoring joint, float constrainedMass, quaternion jointFrameOrientation, ref FixedList512Bytes<Constraint> constraints)
            {
                var linearLocks = GetAxesWithMotionType(ConfigurableJointMotion.Locked, joint.xMotion, joint.yMotion, joint.zMotion);
                var linearLimited = GetAxesWithMotionType(ConfigurableJointMotion.Limited, joint.xMotion, joint.yMotion, joint.zMotion);
                var linearFree = GetAxesWithMotionType(ConfigurableJointMotion.Free, joint.xMotion, joint.yMotion, joint.zMotion);

                IdentifyIfLinearMotor(joint, out bool3 isPositionMotor, out bool3 isLinearVelocityMotor, out float3 maxForceForLinearMotor);

                Assert.IsTrue(math.csum(new int3(isLinearVelocityMotor)) <= 1, $"Unity.Physics doesn't fully support double and triple motorization of linear velocity at the moment, only one of those motors will work, game object {joint.name}");

                int fixup = 0;

                if (math.any(linearLocks))
                {
                    fixup++;
                    constraints.Add(new Constraint
                    {
                        ConstrainedAxes = linearLocks,
                        Type = ConstraintType.Linear,
                        Min = 0, // if it's locked, then it's locked at zero
                        Max = 0,
                        SpringFrequency = Constraint.DefaultSpringFrequency,
                        DampingRatio = Constraint.DefaultDampingRatio,
                        MaxImpulse = joint.breakForce * Time.fixedDeltaTime
                    });
                }

                if (math.any(linearLimited))
                {
                    fixup++;
                    ConvertSpringDamperSettings(joint.linearLimitSpring, joint.linearLimitDamper, constrainedMass, out float springFrequency, out float dampingRatio);

                    constraints.Add(new Constraint
                    {
                        ConstrainedAxes = linearLimited,
                        Type = ConstraintType.Linear,
                        Min = 0,
                        Max = joint.linearLimit,
                        SpringFrequency = springFrequency,
                        DampingRatio = dampingRatio,
                        MaxImpulse = joint.breakForce * Time.fixedDeltaTime
                    });
                }

                float3 linearStiffness = new float3(joint.xDrivePositionSpring, joint.yDrivePositionSpring, joint.zDrivePositionSpring);
                float3 linearDamping = new float3(joint.xDrivePositionDamper, joint.yDrivePositionDamper, joint.zDrivePositionDamper);

                for (int axis = 0; axis < 3; ++axis)
                {
                    if (!isPositionMotor[axis] && !isLinearVelocityMotor[axis])
                        continue; // skip axis if no position and no velocity drive either

                    if (linearLocks[axis])
                        continue; // skip if this axis is locked

                    ConvertSpringDamperSettings(linearStiffness[axis], linearDamping[axis], constrainedMass, out float motorFrequency, out float motorDampingRatio);
                    fixup++;
                    constraints.Add(new Constraint
                    {
                        ConstrainedAxes = new bool3(axis == 0, axis == 1, axis == 2),
                        Type = isPositionMotor[axis] ? ConstraintType.PositionMotor : ConstraintType.LinearVelocityMotor,
                        Min = -math.INFINITY, // looks like the limits should be enforced by the limit spring instead of the hard limit here
                        Max = math.INFINITY,
                        SpringFrequency = motorFrequency,
                        DampingRatio = motorDampingRatio,
                        MaxImpulse = maxForceForLinearMotor * Time.fixedDeltaTime,
                        Target = isPositionMotor[axis] ? -joint.targetPosition : -joint.targetVelocity,
                    });
                }
                while (fixup != 3)
                {
                    constraints.Add(new Constraint
                    {

                    });
                    fixup++;
                }

                if (math.all(linearFree) && (!math.all(isPositionMotor) || !math.all(isLinearVelocityMotor)))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            public bool ConvertAngularDofs(ConfigurableEntityJointAuthoring joint, float constrainedMass, quaternion jointFrameOrientation, ref FixedList512Bytes<Constraint> constraints)
            {
                var angularLocks = GetAxesWithMotionType(ConfigurableJointMotion.Locked, joint.angularXMotion, joint.angularYMotion, joint.angularZMotion);
                var angularLimited = GetAxesWithMotionType(ConfigurableJointMotion.Limited, joint.angularXMotion, joint.angularYMotion, joint.angularZMotion);
                var angularFree = GetAxesWithMotionType(ConfigurableJointMotion.Free, joint.angularXMotion, joint.angularYMotion, joint.angularZMotion);

                IdentifyIfAngularMotor(joint, out bool3 isRotationMotor, out bool3 isAngularVelocityMotor, out float3 maxForceForAngularMotor);

                Assert.IsTrue(joint.rotationDriveMode != RotationDriveMode.Slerp, $"Slerp drive mode is not supported by the conversion at the moment, defaulting to twist and swing instead, game object {joint.name}");
                int fixup = 0;

                if (math.any(angularLocks))
                {
                    fixup++;
                    constraints.Add(new Constraint
                    {
                        ConstrainedAxes = angularLocks,
                        Type = ConstraintType.Angular,
                        Min = 0, // if it's locked, then it's locked at zero
                        Max = 0,
                        SpringFrequency = Constraint.DefaultSpringFrequency,
                        DampingRatio = Constraint.DefaultDampingRatio,
                        MaxImpulse = joint.breakForce * Time.fixedDeltaTime
                    });
                }

                if (math.any(angularLimited))
                {
                    fixup++;
                    // have to do angular limits per axis, unfortunately, because of the twist-swing thing that is asymmetric
                    if (angularLimited.x)
                    {
                        ConvertSpringDamperSettings(joint.angularXLimitSpring, joint.angularXLimitDamper, constrainedMass, out float springFrequencyX, out float dampingRatioX);

                        constraints.Add(new Constraint
                        {
                            ConstrainedAxes = new bool3(true, false, false),
                            Type = ConstraintType.Angular,
                            Min = -math.radians(joint.highAngularXLimit),
                            Max = -math.radians(joint.lowAngularXLimit),
                            SpringFrequency = springFrequencyX,
                            DampingRatio = dampingRatioX,
                            MaxImpulse = joint.breakForce * Time.fixedDeltaTime
                        });
                    }

                    if (angularLimited.y)
                    {
                        ConvertSpringDamperSettings(joint.angularYZLimitSpring, joint.angularYZLimitDamper, constrainedMass, out float springFrequencyYZ, out float dampingRatioYZ);

                        constraints.Add(new Constraint
                        {
                            ConstrainedAxes = new bool3(false, true, false),
                            Type = ConstraintType.Angular,
                            Min = -math.radians(joint.angularYLimit),
                            Max = math.radians(joint.angularYLimit),
                            SpringFrequency = springFrequencyYZ,
                            DampingRatio = dampingRatioYZ,
                            MaxImpulse = joint.breakForce * Time.fixedDeltaTime
                        });
                    }

                    if (angularLimited.z)
                    {
                        ConvertSpringDamperSettings(joint.angularYZLimitSpring, joint.angularYZLimitDamper, constrainedMass, out float springFrequencyYZ, out float dampingRatioYZ);

                        constraints.Add(new Constraint
                        {
                            ConstrainedAxes = new bool3(false, false, true),
                            Type = ConstraintType.Angular,
                            Min = -math.radians(joint.angularZLimit),
                            Max = math.radians(joint.angularZLimit),
                            SpringFrequency = springFrequencyYZ,
                            DampingRatio = dampingRatioYZ,
                            MaxImpulse = joint.breakForce * Time.fixedDeltaTime
                        });
                    }
                }

                // the above branch creates up to three constraints, despite having constraint creation in four spots
                // proof: assume an axis is locked, then it can't be limited at the same time

                float3 angularStiffness = new float3(joint.angularXDrivePositionSpring, joint.angularYZDrivePositionSpring, joint.angularYZDrivePositionSpring);
                float3 angularDamping = new float3(joint.angularXDrivePositionDamper, joint.angularYZDrivePositionDamper, joint.angularYZDrivePositionDamper);

                for (int axis = 0; axis < 3; ++axis)
                {
                    if (!isRotationMotor[axis] && !isAngularVelocityMotor[axis])
                        continue; // skip axis if no position and no velocity drive either

                    if (angularLocks[axis])
                        continue; // skip if this axis is locked

                    ConvertSpringDamperSettings(angularStiffness[axis], angularDamping[axis], constrainedMass, out float motorFrequency, out float motorDampingRatio);
                    fixup++;
                    constraints.Add(new Constraint
                    {
                        ConstrainedAxes = new bool3(axis == 0, axis == 1, axis == 2),
                        Type = isRotationMotor[axis] ? ConstraintType.RotationMotor : ConstraintType.AngularVelocityMotor,
                        Min = -math.INFINITY,
                        Max = math.INFINITY,
                        SpringFrequency = motorFrequency,
                        DampingRatio = motorDampingRatio,
                        MaxImpulse = maxForceForAngularMotor * Time.fixedDeltaTime,
                        Target = isRotationMotor[axis] ? -math.radians(joint.targetRotation.eulerAngles) : joint.targetAngularVelocity,
                    });
                }

                while (fixup != 3)
                {
                    constraints.Add(new Constraint
                    {

                    });
                    fixup++;
                }




                if (math.all(angularFree) && (!math.all(isRotationMotor) || !math.all(isAngularVelocityMotor)))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            public float3 GetScaledLocalAnchorPosition(in Transform bodyTransform, in float3 anchorPosition)
            {
                // account for scale in the body transform if any
                var kScaleEpsilon = 0.0001f;
                if (math.lengthsq((float3)bodyTransform.lossyScale - new float3(1f)) > kScaleEpsilon)
                {
                    var localToWorld = bodyTransform.localToWorldMatrix;
                    var rigidBodyTransform = Math.DecomposeRigidBodyTransform(localToWorld);

                    // extract local skew matrix if non-identity world scale detected to re-position the local joint anchor position
                    var skewMatrix = math.mul(math.inverse(new float4x4(rigidBodyTransform)), localToWorld);
                    return math.mul(skewMatrix, new float4(anchorPosition, 1)).xyz;
                }
                // else:

                return anchorPosition;
            }
            public PhysicsConstrainedBodyPair GetConstrainedBodyPair(in ConfigurableEntityJointAuthoring joint, Entity localBody, Entity connectedBody) =>
new PhysicsConstrainedBodyPair(
localBody,
connectedBody,
joint.EnableCollision
);
            public void SetupBodyFrames(quaternion jointFrameOrientation, ConfigurableEntityJointAuthoring joint, Entity connectedBody, ref BodyFrame bodyAFromJoint, ref BodyFrame bodyBFromJoint)
            {
                RigidTransform worldFromBodyA = Math.DecomposeRigidBodyTransform(joint.transform.localToWorldMatrix);
                RigidTransform worldFromBodyB = joint.ConnectedBody == null
                    ? RigidTransform.identity
                    : Math.DecomposeRigidBodyTransform(joint.ConnectedBody.transform.localToWorldMatrix);

                var anchorPos = GetScaledLocalAnchorPosition(joint.transform, joint.PositionLocal);
                var worldFromJointA = math.mul(
                    new RigidTransform(joint.transform.rotation, joint.transform.position),
                    new RigidTransform(jointFrameOrientation, anchorPos)
                );
                bodyAFromJoint = new BodyFrame(math.mul(math.inverse(worldFromBodyA), worldFromJointA));

                var isConnectedBodyConverted =
                    joint.ConnectedBody == null || connectedBody != Entity.Null;

                RigidTransform bFromA = isConnectedBodyConverted ? math.mul(math.inverse(worldFromBodyB), worldFromBodyA) : worldFromBodyA;
                RigidTransform bFromBSource =
                    isConnectedBodyConverted ? RigidTransform.identity : worldFromBodyB;

                float3 connectedAnchorPos = joint.ConnectedBody
                    ? GetScaledLocalAnchorPosition(joint.ConnectedBody.transform, joint.PositionInConnectedEntity)
                    : joint.PositionInConnectedEntity;
                bodyBFromJoint = new BodyFrame
                {
                    Axis = math.mul(bFromA.rot, bodyAFromJoint.Axis),
                    PerpendicularAxis = math.mul(bFromA.rot, bodyAFromJoint.PerpendicularAxis),
                    Position = math.mul(bFromBSource, new float4(connectedAnchorPos, 1f)).xyz
                };
            }












        }


        public void SetJoint()
        {
            targetJoint = gameObject.GetComponentInParent<BaseJoint>();
        }

    }
}

