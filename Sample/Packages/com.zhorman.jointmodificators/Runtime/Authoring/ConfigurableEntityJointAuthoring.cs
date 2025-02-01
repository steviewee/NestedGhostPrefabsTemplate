using Unity.Assertions;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;
using System.Runtime.CompilerServices;
using Unity.Physics;
using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using FloatRange = Unity.Physics.Math.FloatRange;
using Unity.Transforms;
using Zhorman.JointModificators.Runtime.Components;
using System.Linq;
using Unity.Burst.CompilerServices;
namespace Zhorman.JointModificators.Runtime.Authoring
{
    public class ConfigurableEntityJointAuthoring : BallAndSocketJoint
    {
        public bool motorsEnabled = false;

        public bool positionalMotorEnabled = false;

        public bool linearVelocityMotorEnabled = false;

        public bool rotationMotorEnabled = false;

        public bool angularVelocityMotorEnabled = false;

        //public bool jointsEnabled = false;

        //public bool linearJointEnabled = false;

        //public bool angularJointEnabled = false;



        // Linear motion constraints
        public ConfigurableJointMotion xMotion = ConfigurableJointMotion.Free;
        public ConfigurableJointMotion yMotion = ConfigurableJointMotion.Free;
        public ConfigurableJointMotion zMotion = ConfigurableJointMotion.Free;

        // Angular motion constraints
        public ConfigurableJointMotion angularXMotion = ConfigurableJointMotion.Free;
        public ConfigurableJointMotion angularYMotion = ConfigurableJointMotion.Free;
        public ConfigurableJointMotion angularZMotion = ConfigurableJointMotion.Free;

        public bool autoSetConnectedIgnoresScale = false;

        // Joint axes
        public Vector3 axis = Vector3.right;
        public Vector3 secondaryAxis = Vector3.up;

        // Linear limits and springs (de-structured)
        public float xLimit;
        public float xLimitBounciness;
        public float xLimitContactDistance;

        public float yLimit;
        public float yLimitBounciness;
        public float yLimitContactDistance;

        public float zLimit;
        public float zLimitBounciness;
        public float zLimitContactDistance;

        // Combined limit (linearLimit)
        public float linearLimit;
        public float linearLimitBounciness;
        public float linearLimitContactDistance;

        // Linear limit springs (de-structured)
        public float xLimitSpring;
        public float xLimitDamper;

        public float yLimitSpring;
        public float yLimitDamper;

        public float zLimitSpring;
        public float zLimitDamper;

        // Combined limit spring (linearLimitSpring)
        public float linearLimitSpring;
        public float linearLimitDamper;

        // Angular limits and springs (de-structured)
        public float lowAngularXLimit;
        public float lowAngularXLimitBounciness;
        public float lowAngularXLimitContactDistance;

        public float highAngularXLimit;
        public float highAngularXLimitBounciness;
        public float highAngularXLimitContactDistance;

        public float angularYLimit;
        public float angularYLimitBounciness;
        public float angularYLimitContactDistance;

        public float angularZLimit;
        public float angularZLimitBounciness;
        public float angularZLimitContactDistance;

        // Angular limit springs (de-structured)
        public float angularXLimitSpring;
        public float angularXLimitDamper;

        public float angularYZLimitSpring;
        public float angularYZLimitDamper;

        // Linear drive settings (de-structured)
        public float xDrivePositionSpring;
        public float xDrivePositionDamper;
        public float xDriveMaximumForce;
        public bool useXDriveAcceleration;

        public float yDrivePositionSpring;
        public float yDrivePositionDamper;
        public float yDriveMaximumForce;
        public bool useYDriveAcceleration;

        public float zDrivePositionSpring;
        public float zDrivePositionDamper;
        public float zDriveMaximumForce;
        public bool useZDriveAcceleration;

        // Slerp drive (de-structured)
        public float slerpDrivePositionSpring;
        public float slerpDrivePositionDamper;
        public float slerpDriveMaximumForce;
        public bool useSlerpDriveAcceleration;

        // Angular drive settings (de-structured)
        public float angularXDrivePositionSpring;
        public float angularXDrivePositionDamper;
        public float angularXDriveMaximumForce;
        public bool useAngularXDriveAcceleration;

        public float angularYZDrivePositionSpring;
        public float angularYZDrivePositionDamper;
        public float angularYZDriveMaximumForce;
        public bool useAngularYZDriveAcceleration;

        // Target properties for the joint
        public Quaternion targetRotation = Quaternion.identity;
        public Vector3 targetPosition = Vector3.zero;
        public Vector3 targetVelocity = Vector3.zero;
        public Vector3 targetAngularVelocity = Vector3.zero;

        // Drive mode for rotation
        public RotationDriveMode rotationDriveMode = RotationDriveMode.XYAndZ;

        // Breakable joint properties
        public float breakForce = Mathf.Infinity;
        public float breakTorque = Mathf.Infinity;

        // Projection mode properties
        public JointProjectionMode projectionMode = JointProjectionMode.None;
        public float projectionDistance = 0.1f;
        public float projectionAngle = 0.1f;

        // Other properties like swap bodies
        public bool swapBodies = false;
        public override void UpdateAuto()
        {
            base.UpdateAuto();
        }






        
        public class ConfigurableBaker : JointBaker<ConfigurableEntityJointAuthoring>
        {
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
                var xDrive = new JointDrive { positionSpring = joint.angularXDrivePositionSpring, positionDamper = joint.angularXDrivePositionDamper, maximumForce = joint.angularXDriveMaximumForce,useAcceleration = joint.useAngularXDriveAcceleration };
                var yzDrive = new JointDrive { positionSpring = joint.angularYZDrivePositionSpring, positionDamper = joint.angularYZDrivePositionDamper, maximumForce = joint.angularYZDriveMaximumForce , useAcceleration = joint.useAngularYZDriveAcceleration };
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
            public void CreateJointEntityBlocks(ConfigurableEntityJointAuthoring joint, quaternion jointFrameOrientation, FixedList512Bytes<Constraint> constraints, Entity localBody, Entity connectedBody,bool linearJoint, bool angularJoint)
            {
                if (constraints.IsEmpty)
                    return; // don't create empty joints, a double-check

                var bodyAFromJoint = new BodyFrame { };
                var bodyBFromJoint = new BodyFrame { };
                SetupBodyFrames(jointFrameOrientation, joint, connectedBody, ref bodyAFromJoint, ref bodyBFromJoint);

                PhysicsConstrainedBodyPair physicsConstrainedBodyPair = GetConstrainedBodyPair(joint, localBody, connectedBody);

                uint worldIndex = GetWorldIndex(joint);

                var block = new FixedList512Bytes<Constraint>();
                Debug.LogWarning($"linearJoint && angularJoint?  {linearJoint} && {angularJoint}");

                NativeArray<PhysicsJoint> joints = new NativeArray<PhysicsJoint>(2, Allocator.Temp);
                for (int i = 0; i < constraints.Length; i += 3)
                {
                    block.Clear();

                    int countInBlock = math.min(constraints.Length - i, 3);

                    for (int j = i; j < i + countInBlock; ++j)
                        block.Add(constraints[j]);

                    var thisJoint = new PhysicsJoint();
                    thisJoint.SetConstraints(block);
                    thisJoint.BodyAFromJoint = bodyAFromJoint;
                    thisJoint.BodyBFromJoint = bodyBFromJoint;

                    thisJoint.SetImpulseEventThresholdAllConstraints(
                        joint.breakForce * Time.fixedDeltaTime,
                        joint.breakTorque * Time.fixedDeltaTime
                    );
                    joints[i / 3] = thisJoint;
                }

                if (linearJoint && angularJoint)
                {
                    Debug.Log("k");
 
                    using (var jointEntities = new NativeList<Entity>(2, Allocator.Temp))
                    {
                        CreateJointEntities(worldIndex, physicsConstrainedBodyPair, joints, jointEntities);
                    }
                }
                else if (linearJoint)
                {
                    Debug.Log("y");
                    CreateJointEntity(worldIndex, physicsConstrainedBodyPair, joints[0]);
                }
                else
                {
                    Debug.Log("s");
                    CreateJointEntity(worldIndex, physicsConstrainedBodyPair, joints[1]);
                }
            }


            public override void Bake(ConfigurableEntityJointAuthoring authoring)
            {
                var localEntity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                authoring.UpdateAuto();
                bool connectedBodyIsNull = authoring.ConnectedBody == null;
                var connectedEntity = connectedBodyIsNull ? Entity.Null : GetEntity(authoring.ConnectedBody.gameObject, TransformUsageFlags.Dynamic);

                var jointFrameOrientation = GetJointFrameOrientation(authoring.axis, authoring.secondaryAxis);

                var allConstraints = new FixedList512Bytes<Constraint>();

                (float constrainedMass, float constrainedInertia) = GetConstrainedBodyMassAndInertia(authoring.LocalBody, authoring.ConnectedBody, !connectedBodyIsNull);


                bool linearJoint = ConvertLinearDofs(authoring, constrainedMass, jointFrameOrientation, ref allConstraints);
                bool angularJoint = ConvertAngularDofs(authoring, constrainedInertia, jointFrameOrientation, ref allConstraints);

                if(linearJoint || angularJoint)
                {
                    CreateJointEntityBlocks(authoring, jointFrameOrientation, allConstraints, localEntity, connectedEntity, linearJoint, angularJoint);
                }



            }



        }

    }
}


/*
public class ConfigurableEntityJointAuthoring : BallAndSocketJoint
{
    public bool motorsEnabled = false;

    public bool positionalMotorEnabled = false;

    public bool linearVelocityMotorEnabled = false;

    public bool rotationMotorEnabled = false;

    public bool angularVelocityMotorEnabled = false;

    //public bool jointsEnabled = false;

    //public bool linearJointEnabled = false;

    //public bool angularJointEnabled = false;



    // Linear motion constraints
    public ConfigurableJointMotion xMotion = ConfigurableJointMotion.Free;
    public ConfigurableJointMotion yMotion = ConfigurableJointMotion.Free;
    public ConfigurableJointMotion zMotion = ConfigurableJointMotion.Free;

    // Angular motion constraints
    public ConfigurableJointMotion angularXMotion = ConfigurableJointMotion.Free;
    public ConfigurableJointMotion angularYMotion = ConfigurableJointMotion.Free;
    public ConfigurableJointMotion angularZMotion = ConfigurableJointMotion.Free;

    public bool autoSetConnectedIgnoresScale = false;

    // Joint axes
    public Vector3 axis = Vector3.right;
    public Vector3 secondaryAxis = Vector3.up;

    // Linear limits and springs (de-structured)
    public float xLimit;
    public float xLimitBounciness;
    public float xLimitContactDistance;

    public float yLimit;
    public float yLimitBounciness;
    public float yLimitContactDistance;

    public float zLimit;
    public float zLimitBounciness;
    public float zLimitContactDistance;

    // Combined limit (linearLimit)
    public float linearLimit;
    public float linearLimitBounciness;
    public float linearLimitContactDistance;

    // Linear limit springs (de-structured)
    public float xLimitSpring;
    public float xLimitDamper;

    public float yLimitSpring;
    public float yLimitDamper;

    public float zLimitSpring;
    public float zLimitDamper;

    // Combined limit spring (linearLimitSpring)
    public float linearLimitSpring;
    public float linearLimitDamper;

    // Angular limits and springs (de-structured)
    public float lowAngularXLimit;
    public float lowAngularXLimitBounciness;
    public float lowAngularXLimitContactDistance;

    public float highAngularXLimit;
    public float highAngularXLimitBounciness;
    public float highAngularXLimitContactDistance;

    public float angularYLimit;
    public float angularYLimitBounciness;
    public float angularYLimitContactDistance;

    public float angularZLimit;
    public float angularZLimitBounciness;
    public float angularZLimitContactDistance;

    // Angular limit springs (de-structured)
    public float angularXLimitSpring;
    public float angularXLimitDamper;

    public float angularYZLimitSpring;
    public float angularYZLimitDamper;

    // Linear drive settings (de-structured)
    public float xDrivePositionSpring;
    public float xDrivePositionDamper;
    public float xDriveMaximumForce;

    public float yDrivePositionSpring;
    public float yDrivePositionDamper;
    public float yDriveMaximumForce;

    public float zDrivePositionSpring;
    public float zDrivePositionDamper;
    public float zDriveMaximumForce;

    // Slerp drive (de-structured)
    public float slerpDrivePositionSpring;
    public float slerpDrivePositionDamper;
    public float slerpDriveMaximumForce;

    // Angular drive settings (de-structured)
    public float angularXDrivePositionSpring;
    public float angularXDrivePositionDamper;
    public float angularXDriveMaximumForce;

    public float angularYZDrivePositionSpring;
    public float angularYZDrivePositionDamper;
    public float angularYZDriveMaximumForce;

    // Target properties for the joint
    public Quaternion targetRotation = Quaternion.identity;
    public Vector3 targetPosition = Vector3.zero;
    public Vector3 targetVelocity = Vector3.zero;
    public Vector3 targetAngularVelocity = Vector3.zero;

    // Drive mode for rotation
    public RotationDriveMode rotationDriveMode = RotationDriveMode.Slerp;

    // Breakable joint properties
    public float breakForce = Mathf.Infinity;
    public float breakTorque = Mathf.Infinity;

    // Projection mode properties
    public JointProjectionMode projectionMode = JointProjectionMode.None;
    public float projectionDistance = 0.1f;
    public float projectionAngle = 0.1f;

    // Other properties like swap bodies
    public bool swapBodies = false;

    class ConfigurableBaker : JointBaker<ConfigurableEntityJointAuthoring>
    {
        public override void Bake(ConfigurableEntityJointAuthoring authoring)
        {
            var localEntity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

            if (authoring.ConnectedBody == null)
            {
                Debug.LogWarning($"authoring configurableEntityJoint.ConnectedBody == null");
            }
            else
            {
                Debug.LogWarning($"authoring configurableEntityJoint.ConnectedBody != null");
            }

            var connectedEntity = authoring.ConnectedBody == null ? Entity.Null : GetEntity(authoring.ConnectedBody.gameObject, TransformUsageFlags.Dynamic);
            var entity = CreateAdditionalEntity(TransformUsageFlags.None, true);
            Entity positionalMotorEntity = Entity.Null;
            Entity linearVelocityMotorEntity = Entity.Null;
            Entity rotationMotorEntity = Entity.Null;
            Entity angularVelocityMotorEntity = Entity.Null;
            if (authoring.motorsEnabled)
            {
                if (authoring.positionalMotorEnabled)
                {
                    positionalMotorEntity = CreateAdditionalEntity(TransformUsageFlags.Dynamic, false);
                }
                if (authoring.linearVelocityMotorEnabled)
                {
                    linearVelocityMotorEntity = CreateAdditionalEntity(TransformUsageFlags.Dynamic, false);
                }
                if (authoring.rotationMotorEnabled)
                {
                    rotationMotorEntity = CreateAdditionalEntity(TransformUsageFlags.Dynamic, false);
                }
                if (authoring.angularVelocityMotorEnabled)
                {
                    angularVelocityMotorEntity = CreateAdditionalEntity(TransformUsageFlags.Dynamic, false);
                }
            }

            Entity linearJointEntity = Entity.Null;
            Entity angularJointEntity = Entity.Null;
            bool linearJointEnabled = authoring.xMotion != ConfigurableJointMotion.Free || authoring.yMotion != ConfigurableJointMotion.Free || authoring.zMotion != ConfigurableJointMotion.Free;
            bool angularJointEnabled = authoring.angularXMotion != ConfigurableJointMotion.Free || authoring.angularYMotion != ConfigurableJointMotion.Free || authoring.angularZMotion != ConfigurableJointMotion.Free;
            if (linearJointEnabled)
            {
                linearJointEntity = CreateAdditionalEntity(TransformUsageFlags.Dynamic, false);
            }
            if (angularJointEnabled)
            {
                angularJointEntity = CreateAdditionalEntity(TransformUsageFlags.Dynamic, false);
            }
            float3 PositionInConnectedEntity = 0f;
            if (authoring.AutoSetConnected)
            {
                if (connectedEntity == Entity.Null)
                {
                    PositionInConnectedEntity = authoring.transform.position;
                }
                else
                {
                    if (authoring.autoSetConnectedIgnoresScale)
                    {
                        Transform connectedTransform = authoring.ConnectedBody.transform;
                        Matrix4x4 scaleIndependentMatrix = Matrix4x4.TRS(
    connectedTransform.position,
    connectedTransform.rotation,
    Vector3.one // Ignore scale by setting it to (1, 1, 1)
);
                        PositionInConnectedEntity = scaleIndependentMatrix.inverse.MultiplyPoint3x4(authoring.transform.position);//authoring.ConnectedBody.transform.worldToLocalMatrix.MultiplyPoint3x4(authoring.transform.position);
                    }
                    else
                    {
                        PositionInConnectedEntity = authoring.ConnectedBody.transform.worldToLocalMatrix.MultiplyPoint3x4(authoring.transform.position);
                    }


                }
            }
            AddComponent(entity, new ConfigurableEntityJoint
            {
                motorsEnabled = authoring.motorsEnabled,


                positionalMotorEntity = positionalMotorEntity,
                linearVelocityMotorEntity = linearVelocityMotorEntity,
                rotationMotorEntity = rotationMotorEntity,
                angularVelocityMotorEntity = angularVelocityMotorEntity,

                linearJointEntity = linearJointEntity,
                angularJointEntity = angularJointEntity,
                localBody = localEntity,

                connectedBody = connectedEntity,

                enableCollision = authoring.EnableCollision,
                maxImpulse = authoring.MaxImpulse,
                positionLocal = authoring.PositionLocal,
                positionInConnectedEntity = PositionInConnectedEntity,

                // Linear motion constraints
                xMotion = authoring.xMotion,
                yMotion = authoring.yMotion,
                zMotion = authoring.zMotion,

                // Angular motion constraints
                angularXMotion = authoring.angularXMotion,
                angularYMotion = authoring.angularYMotion,
                angularZMotion = authoring.angularZMotion,

                // Joint axes
                axis = authoring.axis,
                secondaryAxis = authoring.secondaryAxis,

                // Linear limits and springs
                //xLimit = new SoftJointLimit {limit=authoring.xLimit, bounciness=authoring.xLimitBounciness,contactDistance=authoring.xLimitContactDistance },
                //yLimit = new SoftJointLimit { limit = authoring.yLimit, bounciness = authoring.yLimitBounciness, contactDistance = authoring.yLimitContactDistance },
                //zLimit = new SoftJointLimit { limit = authoring.zLimit, bounciness = authoring.zLimitBounciness, contactDistance = authoring.zLimitContactDistance },
                // Combined limit
                linearLimit = new SoftJointLimit { limit = authoring.linearLimit, bounciness = authoring.linearLimitBounciness, contactDistance = authoring.linearLimitContactDistance },

                //xLimitSpring = new SoftJointLimitSpring { spring = authoring.xLimitSpring, damper = authoring.xLimitDamper},
                //yLimitSpring = new SoftJointLimitSpring { spring = authoring.yLimitSpring, damper = authoring.yLimitDamper },
                //zLimitSpring = new SoftJointLimitSpring { spring = authoring.zLimitSpring, damper = authoring.zLimitDamper },
                // Combined limit spring
                linearLimitSpring = new SoftJointLimitSpring { spring = authoring.linearLimitSpring, damper = authoring.linearLimitDamper },
                // Angular limits and springs
                lowAngularXLimit = new SoftJointLimit { limit = authoring.lowAngularXLimit, bounciness = authoring.lowAngularXLimitBounciness, contactDistance = authoring.lowAngularXLimitContactDistance },
                highAngularXLimit = new SoftJointLimit { limit = authoring.highAngularXLimit, bounciness = authoring.highAngularXLimitBounciness, contactDistance = authoring.highAngularXLimitContactDistance },
                angularYLimit = new SoftJointLimit { limit = authoring.angularYLimit, bounciness = authoring.angularYLimitBounciness, contactDistance = authoring.angularYLimitContactDistance },
                angularZLimit = new SoftJointLimit { limit = authoring.angularZLimit, bounciness = authoring.angularZLimitBounciness, contactDistance = authoring.angularZLimitContactDistance },

                angularXLimitSpring = new SoftJointLimitSpring { spring = authoring.angularXLimitSpring, damper = authoring.angularXLimitDamper },
                angularYZLimitSpring = new SoftJointLimitSpring { spring = authoring.angularYZLimitSpring, damper = authoring.angularYZLimitDamper },

                // Linear drive settings
                xDrive = new JointDrive { positionSpring = authoring.xDrivePositionSpring, positionDamper = authoring.xDrivePositionDamper, maximumForce = authoring.xDriveMaximumForce },
                yDrive = new JointDrive { positionSpring = authoring.yDrivePositionSpring, positionDamper = authoring.yDrivePositionDamper, maximumForce = authoring.yDriveMaximumForce },
                zDrive = new JointDrive { positionSpring = authoring.zDrivePositionSpring, positionDamper = authoring.zDrivePositionDamper, maximumForce = authoring.zDriveMaximumForce },

                slerpDrive = new JointDrive { positionSpring = authoring.slerpDrivePositionSpring, positionDamper = authoring.slerpDrivePositionDamper, maximumForce = authoring.slerpDriveMaximumForce },

                // Angular drive settings
                angularXDrive = new JointDrive { positionSpring = authoring.angularXDrivePositionSpring, positionDamper = authoring.angularXDrivePositionDamper, maximumForce = authoring.angularXDriveMaximumForce },
                angularYZDrive = new JointDrive { positionSpring = authoring.angularYZDrivePositionSpring, positionDamper = authoring.angularYZDrivePositionDamper, maximumForce = authoring.angularYZDriveMaximumForce },

                // Target properties for the joint
                targetRotation = authoring.targetRotation,
                targetPosition = authoring.targetPosition,
                targetVelocity = authoring.targetVelocity,
                targetAngularVelocity = authoring.targetAngularVelocity,

                // Drive mode for rotation
                rotationDriveMode = authoring.rotationDriveMode,

                // Breakable joint properties
                breakForce = authoring.breakForce,
                breakTorque = authoring.breakTorque,

                // Projection mode properties
                projectionMode = authoring.projectionMode,
                projectionDistance = authoring.projectionDistance,
                projectionAngle = authoring.projectionAngle,

                // Other properties like swap bodies
                swapBodies = authoring.motorsEnabled,

            });
        }


    }

}

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
[UpdateInGroup(typeof(BakingSystemGroup))]
[UpdateAfter(typeof(BeginJointBakingSystem))]
[UpdateBefore(typeof(EndJointBakingSystem))]
public partial struct SpawneeSettingSystem : ISystem
{
    EntityQuery configurableEntityJointQuery;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        configurableEntityJointQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<ConfigurableEntityJoint>().WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
.Build(ref state);


        state.RequireForUpdate(configurableEntityJointQuery);
    }
    public void OnUpdate(ref SystemState state)
    {

        EntityCommandBuffer ecb = new(Allocator.Temp);
        NativeArray<Entity> configurableEntityJointEntities = configurableEntityJointQuery.ToEntityArray(Allocator.Temp);
        NativeArray<ConfigurableEntityJoint> configurableEntityJoints = configurableEntityJointQuery.ToComponentDataArray<ConfigurableEntityJoint>(Allocator.Temp);
        for (int i = 0; i < configurableEntityJointEntities.Length; i++)
        {
            var entity = configurableEntityJointEntities[i];
            var joint = configurableEntityJoints[i];
            if (joint.motorsEnabled || joint.linearJointEntity != Entity.Null || joint.angularJointEntity != Entity.Null)
            {
                PhysicsBodyAuthoringData localBody = new PhysicsBodyAuthoringData();
                LocalToWorld localLocalToWorld = new LocalToWorld();
                if (joint.localBody != Entity.Null)
                {

                    Debug.LogWarning($"Does localHave: {state.EntityManager.HasComponent<PhysicsMass>(joint.localBody)}? or {state.EntityManager.HasComponent<PhysicsBodyAuthoringData>(joint.localBody)}?");

                    localBody = state.EntityManager.GetComponentData<PhysicsBodyAuthoringData>(joint.localBody);
                    localLocalToWorld = state.EntityManager.GetComponentData<LocalToWorld>(joint.localBody);
                }
                PhysicsBodyAuthoringData connectedBody = new PhysicsBodyAuthoringData();
                LocalToWorld connectedLocalToWorld = new LocalToWorld();
                if (joint.connectedBody != Entity.Null)
                {
                    Debug.LogWarning($"Does localHave: {state.EntityManager.HasComponent<PhysicsMass>(joint.connectedBody)}? or {state.EntityManager.HasComponent<PhysicsBodyAuthoringData>(joint.connectedBody)}?");
                    connectedBody = state.EntityManager.GetComponentData<PhysicsBodyAuthoringData>(joint.connectedBody);
                    connectedLocalToWorld = state.EntityManager.GetComponentData<LocalToWorld>(joint.connectedBody);
                }
                PhysicsConstrainedBodyPair physicsConstrainedBodyPair = new PhysicsConstrainedBodyPair(joint.localBody, joint.connectedBody, joint.enableCollision);


                var linearLocks =
                    GetAxesWithMotionType(ConfigurableJointMotion.Locked, joint.xMotion, joint.yMotion, joint.zMotion);
                var linearLimited =
                    GetAxesWithMotionType(ConfigurableJointMotion.Limited, joint.xMotion, joint.yMotion, joint.zMotion);
                var angularFree =
                    GetAxesWithMotionType(ConfigurableJointMotion.Free, joint.angularXMotion, joint.angularYMotion, joint.angularZMotion);
                var angularLocks =
                    GetAxesWithMotionType(ConfigurableJointMotion.Locked, joint.angularXMotion, joint.angularYMotion, joint.angularZMotion);
                var angularLimited =
                    GetAxesWithMotionType(ConfigurableJointMotion.Limited, joint.angularXMotion, joint.angularYMotion, joint.angularZMotion);

                var jointFrameOrientation = GetJointFrameOrientation(joint.axis, joint.secondaryAxis);

                // Determine if a motor is present:
                bool requiredDoF_forLinearMotors = math.any(linearLocks) && math.all(angularLocks) && math.all(!linearLimited);
                bool requiredDoF_forAngularMotors = math.all(linearLocks) && math.any(angularLocks) && math.all(!linearLimited);
                var jointData = CreateConfigurableJoint(joint, localBody, localLocalToWorld, connectedBody, connectedLocalToWorld,
                    jointFrameOrientation, linearLocks, linearLimited, angularFree, angularLocks, angularLimited);

                uint worldIndex = GetWorldIndex();// (joint);
                if (joint.linearJointEntity != Entity.Null)
                {
                    ecb.AddComponent(joint.linearJointEntity, physicsConstrainedBodyPair);
                    ecb.AddComponent(joint.linearJointEntity, jointData.LinearJoint);
                    ecb.AddSharedComponent(joint.linearJointEntity, new PhysicsWorldIndex { Value = 0 });
                }
                if (joint.angularJointEntity != Entity.Null)
                {
                    ecb.AddComponent(joint.angularJointEntity, physicsConstrainedBodyPair);
                    ecb.AddComponent(joint.angularJointEntity, jointData.AngularJoint);
                    ecb.AddSharedComponent(joint.angularJointEntity, new PhysicsWorldIndex { Value = 0 });
                }

                if (joint.linearJointEntity != Entity.Null && joint.angularJointEntity != Entity.Null)
                {
                    var companions = ecb.AddBuffer<PhysicsJointCompanion>(joint.linearJointEntity);
                    companions.Add(new PhysicsJointCompanion { JointEntity = joint.angularJointEntity });

                    var companions1 = ecb.AddBuffer<PhysicsJointCompanion>(joint.angularJointEntity);
                    companions1.Add(new PhysicsJointCompanion { JointEntity = joint.linearJointEntity });
                }


            }

        }



        ecb.Playback(state.EntityManager);
    }

    void CreateJointEntities(ref SystemState state, EntityCommandBuffer ecb, uint worldIndex, PhysicsConstrainedBodyPair constrainedBodyPair, NativeArray<PhysicsJoint> joints, NativeList<Entity> newJointEntities = default)
    {
        Debug.Log("CreateJointEntities0");
        if (!joints.IsCreated || joints.Length == 0)
            return;
        Debug.Log("CreateJointEntities1");
        if (newJointEntities.IsCreated)
            newJointEntities.Clear();
        else
            newJointEntities = new NativeList<Entity>(joints.Length, Allocator.Temp);

        // create all new joints
        var multipleJoints = joints.Length > 1;
        Debug.Log("CreateJointEntities2");
        foreach (var joint in joints)
        {
            Debug.Log("CreateJointEntities3");
            Entity jointEntity = ecb.CreateEntity();
            //ecb.AddSharedComponent(jointEntity, new PhysicsWorldIndex(worldIndex));

            ecb.AddComponent(jointEntity, constrainedBodyPair);
            ecb.AddComponent(jointEntity, joint);
            ecb.AddComponent(jointEntity, new Parent { Value = constrainedBodyPair.EntityA });
            newJointEntities.Add(jointEntity);
        }

        if (multipleJoints)
        {
            Debug.Log("CreateJointEntities4");
            // set companion buffers for new joints
            for (var i = 0; i < joints.Length; ++i)
            {
                Debug.Log("CreateJointEntities5");
                var companions = ecb.AddBuffer<PhysicsJointCompanion>(newJointEntities[i]);
                for (var j = 0; j < joints.Length; ++j)
                {
                    Debug.Log("CreateJointEntities6");
                    if (i == j)
                        continue;
                    Debug.Log("CreateJointEntities7");
                    companions.Add(new PhysicsJointCompanion { JointEntity = newJointEntities[j] });
                }
            }
        }
    }
    uint GetWorldIndex()//(UnityEngine.Component c)
    {
        // World Indices are not supported in current built-in physics implementation, which makes it unavailable with legacy baking.
        return 0;
    }

    float GetConstrainedBodyMass(bool bodyAExists, PhysicsBodyAuthoringData bodyA, PhysicsBodyAuthoringData bodyB, bool bodyBExists)
    {
        float mass = 0;

        if (bodyAExists && bodyA.IsDynamic)
        {
            mass += bodyA.Mass;
        }

        if (bodyBExists && bodyB.IsDynamic)
        {
            mass += bodyB.Mass;
        }

        return mass > 0 ? mass : 1.0f;
    }
    CombinedJoint CreateConfigurableJoint(ConfigurableEntityJoint joint, PhysicsBodyAuthoringData localBody, LocalToWorld localLocalToWorld, PhysicsBodyAuthoringData connectedBody, LocalToWorld connectedLocalToWorld,
        quaternion jointFrameOrientation, bool3 linearLocks, bool3 linearLimited, bool3 angularFree, bool3 angularLocks, bool3 angularLimited)
    {
        float constrainedMass = GetConstrainedBodyMass(joint.localBody != Entity.Null, localBody, connectedBody, joint.connectedBody != Entity.Null);

        var angularConstraints = new FixedList512Bytes<Constraint>();
        var linearConstraints = new FixedList512Bytes<Constraint>();
        if (angularLimited[0])
        {
            ConvertDamperSettings(joint.angularXLimitSpring.spring, joint.angularXLimitSpring.damper, constrainedMass, out var springFrequency, out var dampingRatio);

            Constraint constraint = Constraint.Twist(
                0,
                math.radians(new FloatRange(-joint.highAngularXLimit.limit, -joint.lowAngularXLimit.limit).Sorted()),
                joint.breakTorque * Time.fixedDeltaTime,
                springFrequency,
                dampingRatio);
            angularConstraints.Add(constraint);
        }

        if (angularLimited[1])
        {
            ConvertDamperSettings(joint.angularYZLimitSpring.spring, joint.angularYZLimitSpring.damper, constrainedMass, out var springFrequency, out var dampingRatio);

            Constraint constraint = Constraint.Twist(
                1,
                math.radians(new FloatRange(-joint.angularYLimit.limit, joint.angularYLimit.limit).Sorted()),
                joint.breakTorque * Time.fixedDeltaTime,
                springFrequency,
                dampingRatio);

            angularConstraints.Add(constraint);
        }

        if (angularLimited[2])
        {
            ConvertDamperSettings(joint.angularYZLimitSpring.spring, joint.angularYZLimitSpring.damper, constrainedMass, out var springFrequency, out var dampingRatio);

            Constraint constraint = Constraint.Twist(
                2,
                math.radians(new FloatRange(-joint.angularZLimit.limit, joint.angularZLimit.limit).Sorted()),
                joint.breakTorque * Time.fixedDeltaTime,
                springFrequency,
                dampingRatio);

            angularConstraints.Add(constraint);
        }
        if (math.any(linearLimited))
        {
            ConvertDamperSettings(joint.linearLimitSpring.spring, joint.linearLimitSpring.damper, constrainedMass, out var springFrequency, out var dampingRatio);

            linearConstraints.Add(new Constraint
            {
                ConstrainedAxes = linearLimited,
                Type = ConstraintType.Linear,
                Min = 0f,
                Max = joint.linearLimit.limit,  // allow movement up to limit from anchor
                SpringFrequency = springFrequency,
                DampingRatio = dampingRatio,
                MaxImpulse = joint.breakForce * Time.fixedDeltaTime,
            });
        }

        if (math.any(linearLocks))
        {
            linearConstraints.Add(new Constraint
            {
                ConstrainedAxes = linearLocks,
                Type = ConstraintType.Linear,
                Min = joint.linearLimit.limit,    // lock at distance from anchor
                Max = joint.linearLimit.limit,
                SpringFrequency = Constraint.DefaultSpringFrequency, // default spring-damper (stiff)
                DampingRatio = Constraint.DefaultDampingRatio, // default spring-damper (stiff)
                MaxImpulse = joint.breakForce * Time.fixedDeltaTime,
            });
        }

        if (math.any(angularLocks))
        {
            angularConstraints.Add(new Constraint
            {
                ConstrainedAxes = angularLocks,
                Type = ConstraintType.Angular,
                Min = 0,
                Max = 0,
                SpringFrequency = Constraint.DefaultSpringFrequency, // default spring-damper (stiff)
                DampingRatio = Constraint.DefaultDampingRatio, // default spring-damper (stiff)
                MaxImpulse = joint.breakTorque * Time.fixedDeltaTime,
            });
        }

        var bodyAFromJoint = new BodyFrame { };
        var bodyBFromJoint = new BodyFrame { };
        SetupBodyFrames(joint, localLocalToWorld, connectedLocalToWorld, jointFrameOrientation, ref bodyAFromJoint, ref bodyBFromJoint);


        var combinedJoint = new CombinedJoint();
        combinedJoint.LinearJoint.SetConstraints(linearConstraints);
        combinedJoint.LinearJoint.BodyAFromJoint = bodyAFromJoint;
        combinedJoint.LinearJoint.BodyBFromJoint = bodyBFromJoint;
        combinedJoint.AngularJoint.SetConstraints(angularConstraints);
        combinedJoint.AngularJoint.BodyAFromJoint = bodyAFromJoint;
        combinedJoint.AngularJoint.BodyBFromJoint = bodyBFromJoint;

        return combinedJoint;
    }
    private void SetupBodyFrames(ConfigurableEntityJoint joint, LocalToWorld localLocalToWorld, LocalToWorld connectedLocalToWorld, quaternion jointFrameOrientation, ref BodyFrame bodyAFromJoint, ref BodyFrame bodyBFromJoint)
    {
        Debug.Log($"joint {joint.positionInConnectedEntity} : {joint.positionLocal}");
        RigidTransform worldFromBodyA = Math.DecomposeRigidBodyTransform(localLocalToWorld.Value);
        RigidTransform worldFromBodyB = joint.connectedBody == null
            ? RigidTransform.identity
            : Math.DecomposeRigidBodyTransform(connectedLocalToWorld.Value);

        var anchorPos = GetScaledLocalAnchorPosition(localLocalToWorld, joint.positionLocal);
        var worldFromJointA = math.mul(
            new RigidTransform(localLocalToWorld.Rotation, localLocalToWorld.Position),
            new RigidTransform(jointFrameOrientation, anchorPos)
        );
        bodyAFromJoint = new BodyFrame(math.mul(math.inverse(worldFromBodyA), worldFromJointA));
        Debug.LogWarning($"authoring math.mul(math.inverse(worldFromBodyA), worldFromJointA): {math.mul(math.inverse(worldFromBodyA), worldFromJointA)} : math.mul(math.inverse({worldFromBodyA.rot} | {worldFromBodyA.pos}), {worldFromJointA.rot}|{worldFromJointA.pos})");
        //var connectedEntity = GetEntity(joint.connectedBody, TransformUsageFlags.Dynamic);
        var isConnectedBodyConverted =
            joint.connectedBody != Entity.Null;
        Debug.Log($"authoring: {isConnectedBodyConverted} {joint.connectedBody == null} || {joint.connectedBody != Entity.Null}");
        RigidTransform bFromA = isConnectedBodyConverted ? math.mul(math.inverse(worldFromBodyB), worldFromBodyA) : worldFromBodyA;
        RigidTransform bFromBSource =
            isConnectedBodyConverted ? RigidTransform.identity : worldFromBodyB;
        float3 connectedAnchorPos = joint.positionInConnectedEntity;

        Debug.LogWarning($"authoring {joint.localBody} ----------------------------------------------------------------------------------------");
        Debug.LogWarning($"authoring math.mul(bFromA.rot, bodyAFromJoint.Axis): {math.mul(bFromA.rot, bodyAFromJoint.Axis)} : math.mul({bFromA.rot}, {bodyAFromJoint.Axis})");
        Debug.LogWarning($"authoring math.mul(bFromA.rot, bodyAFromJoint.Axis): {math.mul(bFromA.rot, bodyAFromJoint.Axis)} : math.mul({bFromA.rot}, {bodyAFromJoint.Axis})");
        Debug.LogWarning($"authoring math.mul(bFromBSource, new float4(connectedAnchorPos, 1f)).xyz: {math.mul(bFromBSource, new float4(connectedAnchorPos, 1f)).xyz} : math.mul({bFromBSource.rot}|{bFromBSource.pos}, {new float4(connectedAnchorPos, 1f)}).xyz");
        bodyBFromJoint = new BodyFrame
        {


            Axis = math.mul(bFromA.rot, bodyAFromJoint.Axis),
            PerpendicularAxis = math.mul(bFromA.rot, bodyAFromJoint.PerpendicularAxis),
            Position = math.mul(bFromBSource, new float4(connectedAnchorPos, 1f)).xyz

        };
        Debug.LogWarning($"----------------------------------------------------------------------------------------");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static float3 GetScaledLocalAnchorPosition(in LocalToWorld localLocalToWorld, in float3 anchorPosition)
    {
        Debug.LogWarning("lossyScale is not yet implemented");
        // account for scale in the body transform if any

        // else:

        return anchorPosition;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static quaternion GetJointFrameOrientation(float3 axis, float3 secondaryAxis)
    {
        // classic Unity uses a different approach than BodyFrame.ValidateAxes() for ortho-normalizing degenerate inputs
        // ortho-normalizing here ensures behavior is consistent with classic Unity
        var a = (Vector3)axis;
        var p = (Vector3)secondaryAxis;
        Vector3.OrthoNormalize(ref a, ref p);
        return new BodyFrame { Axis = a, PerpendicularAxis = p }.AsRigidTransform().rot;
    }

    bool3 GetAxesWithMotionType(
       ConfigurableJointMotion motionType,
       ConfigurableJointMotion x, ConfigurableJointMotion y, ConfigurableJointMotion z
   ) => new bool3(x == motionType, y == motionType, z == motionType);

    void ConvertDamperSettings(float inSpringConstant, float inDampingCoefficient, float inConstrainedMass,
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
}
*/






















/*
Axis = bodyAFromJoint.Axis,
PerpendicularAxis =bodyAFromJoint.PerpendicularAxis,
Position =connectedAnchorPos
*/
/*
float3 connectedAnchorPos = joint.connectedBody
? GetScaledLocalAnchorPosition(joint.connectedBody.transform, joint.connectedAnchor)
: joint.connectedAnchor;
*/

/*
        RigidTransform worldFromBodyA = Math.DecomposeRigidBodyTransform(localLocalToWorld.Value);
        RigidTransform worldFromBodyB = joint.connectedBody == null
            ? RigidTransform.identity
            : Math.DecomposeRigidBodyTransform(connectedLocalToWorld.Value);

        var anchorPos = GetScaledLocalAnchorPosition(localLocalToWorld, joint.positionLocal);
        var worldFromJointA = math.mul(
            new RigidTransform(localLocalToWorld.Rotation, localLocalToWorld.Position),
            new RigidTransform(jointFrameOrientation, anchorPos)
        );
        bodyAFromJoint = new BodyFrame(math.mul(math.inverse(worldFromBodyA), worldFromJointA));

        var isConnectedBodyConverted =
            joint.connectedBody == null || joint.connectedBody != Entity.Null;

        RigidTransform bFromA = isConnectedBodyConverted ? math.mul(math.inverse(worldFromBodyB), worldFromBodyA) : worldFromBodyA;
        RigidTransform bFromBSource =
            isConnectedBodyConverted ? RigidTransform.identity : worldFromBodyB;

        float3 connectedAnchorPos = joint.connectedBody != Entity.Null
            ? GetScaledLocalAnchorPosition(connectedLocalToWorld, joint.positionInConnectedEntity)
            : joint.positionInConnectedEntity;
        bodyBFromJoint = new BodyFrame
        {
            Axis = math.mul(bFromA.rot, bodyAFromJoint.Axis),
            PerpendicularAxis = math.mul(bFromA.rot, bodyAFromJoint.PerpendicularAxis),
            Position = math.mul(bFromBSource, new float4(connectedAnchorPos, 1f)).xyz
        };


        */