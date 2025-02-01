using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Unity.Physics;
using Zhorman.JointModificators.Runtime.Authoring;
using Unity.Physics.Authoring;

[CustomEditor(typeof(ConfigurableEntityJointAuthoring))]
    [CanEditMultipleObjects]
    public class ConfigurableEntityJointAuthoringEditor : Editor
    {
        bool motorProperties;
        bool jointProperties;
        public override void OnInspectorGUI()
        {
            //serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            // Get the target object
            ConfigurableEntityJointAuthoring joint = (ConfigurableEntityJointAuthoring)target;

            // Display LocalBody and ConnectedBody
            joint.ConnectedBody = (PhysicsBodyAuthoring)EditorGUILayout.ObjectField("Connected Body", joint.ConnectedBody, typeof(PhysicsBodyAuthoring), true);
            // Show PositionLocal and PositionInConnectedEntity (float3)
            joint.PositionLocal = DrawFloat3Field("Position Local", joint.PositionLocal);
            joint.axis = DrawFloat3Field("Axis", joint.axis);
            //joint.EditPivots = EditorGUILayout.Toggle("Edit Pivots", joint.EditPivots);
            joint.AutoSetConnected = EditorGUILayout.Toggle("Auto Set Connected", joint.AutoSetConnected);
            if (joint.AutoSetConnected)
            {
                ++EditorGUI.indentLevel;
                joint.autoSetConnectedIgnoresScale = EditorGUILayout.Toggle("Auto Set Ignores Scale", joint.autoSetConnectedIgnoresScale);
                --EditorGUI.indentLevel;
            }


            joint.PositionInConnectedEntity = DrawFloat3Field("Position In Connected Entity", joint.PositionInConnectedEntity);
            joint.secondaryAxis = DrawFloat3Field("Secondary Axis", joint.secondaryAxis);
            //EditorGUILayout.LabelField("Linear Motion Settings", EditorStyles.boldLabel);
            joint.xMotion = (ConfigurableJointMotion)EditorGUILayout.EnumPopup("X Motion", joint.xMotion);
            joint.yMotion = (ConfigurableJointMotion)EditorGUILayout.EnumPopup("Y Motion", joint.yMotion);
            joint.zMotion = (ConfigurableJointMotion)EditorGUILayout.EnumPopup("Z Motion", joint.zMotion);

            // Angular Motion
            //EditorGUILayout.LabelField("Angular Motion Settings", EditorStyles.boldLabel);
            joint.angularXMotion = (ConfigurableJointMotion)EditorGUILayout.EnumPopup("Angular X Motion", joint.angularXMotion);
            joint.angularYMotion = (ConfigurableJointMotion)EditorGUILayout.EnumPopup("Angular Y Motion", joint.angularYMotion);
            joint.angularZMotion = (ConfigurableJointMotion)EditorGUILayout.EnumPopup("Angular Z Motion", joint.angularZMotion);
            joint.motorsEnabled = EditorGUILayout.Toggle("Use motors", joint.motorsEnabled);

            if (joint.motorsEnabled)
            {
                ++EditorGUI.indentLevel;
                motorProperties = EditorGUILayout.Foldout(motorProperties, "Motor properties");
                if (motorProperties)
                {
                    ++EditorGUI.indentLevel;
                    EditorGUILayout.LabelField("Linear Drives", EditorStyles.boldLabel);
                    ++EditorGUI.indentLevel;
                    EditorGUILayout.LabelField("XDrive", EditorStyles.boldLabel);
                    ++EditorGUI.indentLevel;
                    joint.xDrivePositionSpring = EditorGUILayout.FloatField("xDrivePositionSpring", joint.xDrivePositionSpring);
                    joint.xDrivePositionDamper = EditorGUILayout.FloatField("xDrivePositionDamper", joint.xDrivePositionDamper);
                    joint.xDriveMaximumForce = EditorGUILayout.FloatField("xDriveMaximumForce", joint.xDriveMaximumForce);
                    joint.useXDriveAcceleration = EditorGUILayout.Toggle("XDriveAcceleration", joint.useXDriveAcceleration);
                    --EditorGUI.indentLevel;
                    EditorGUILayout.LabelField("YDrive", EditorStyles.boldLabel);
                    ++EditorGUI.indentLevel;
                    joint.yDrivePositionSpring = EditorGUILayout.FloatField("yDrivePositionSpring", joint.yDrivePositionSpring);
                    joint.yDrivePositionDamper = EditorGUILayout.FloatField("yDrivePositionDamper", joint.yDrivePositionDamper);
                    joint.yDriveMaximumForce = EditorGUILayout.FloatField("yDriveMaximumForce", joint.yDriveMaximumForce);
                    joint.useYDriveAcceleration = EditorGUILayout.Toggle("YDriveAcceleration", joint.useYDriveAcceleration);
                    --EditorGUI.indentLevel;
                    EditorGUILayout.LabelField("ZDrive", EditorStyles.boldLabel);
                    ++EditorGUI.indentLevel;
                    joint.zDrivePositionSpring = EditorGUILayout.FloatField("zDrivePositionSpring", joint.zDrivePositionSpring);
                    joint.zDrivePositionDamper = EditorGUILayout.FloatField("zDrivePositionDamper", joint.zDrivePositionDamper);
                    joint.zDriveMaximumForce = EditorGUILayout.FloatField("zDriveMaximumForce", joint.zDriveMaximumForce);
                    joint.useZDriveAcceleration = EditorGUILayout.Toggle("ZDriveAcceleration", joint.useZDriveAcceleration);
                    --EditorGUI.indentLevel;
                    joint.targetPosition = EditorGUILayout.Vector3Field("Target Position", joint.targetPosition);
                    joint.targetVelocity = EditorGUILayout.Vector3Field("Target Velocity", joint.targetVelocity);
                    --EditorGUI.indentLevel;
                    EditorGUILayout.LabelField("Angular Drives", EditorStyles.boldLabel);
                    ++EditorGUI.indentLevel;
                    joint.rotationDriveMode = (RotationDriveMode)EditorGUILayout.EnumPopup("Rotation Drive Mode", joint.rotationDriveMode);
                    ++EditorGUI.indentLevel;
                    if (joint.rotationDriveMode == RotationDriveMode.XYAndZ)
                    {
                        EditorGUILayout.LabelField("AngularXDrive", EditorStyles.boldLabel);
                        ++EditorGUI.indentLevel;
                        joint.angularXDrivePositionSpring = EditorGUILayout.FloatField("angularXDrivePositionSpring", joint.angularXDrivePositionSpring);
                        joint.angularXDrivePositionDamper = EditorGUILayout.FloatField("angularXDrivePositionDamper", joint.angularXDrivePositionDamper);
                        joint.angularXDriveMaximumForce = EditorGUILayout.FloatField("angularXDriveMaximumForce", joint.angularXDriveMaximumForce);
                        joint.useAngularXDriveAcceleration = EditorGUILayout.Toggle("AngularXDriveAcceleration", joint.useAngularXDriveAcceleration);
                        --EditorGUI.indentLevel;
                        EditorGUILayout.LabelField("AngularYZDrive", EditorStyles.boldLabel);
                        ++EditorGUI.indentLevel;
                        joint.angularYZDrivePositionSpring = EditorGUILayout.FloatField("angularYZDrivePositionSpring", joint.angularYZDrivePositionSpring);
                        joint.angularYZDrivePositionDamper = EditorGUILayout.FloatField("angularYZDrivePositionDamper", joint.angularYZDrivePositionDamper);
                        joint.angularYZDriveMaximumForce = EditorGUILayout.FloatField("angularYZDriveMaximumForce", joint.angularYZDriveMaximumForce);
                        joint.useAngularYZDriveAcceleration = EditorGUILayout.Toggle("AngularYZDriveAcceleration", joint.useAngularYZDriveAcceleration);
                        --EditorGUI.indentLevel;

                    }
                    else
                    {
                        EditorGUILayout.LabelField("Slerp Drive", EditorStyles.boldLabel);
                        ++EditorGUI.indentLevel;
                        joint.slerpDrivePositionSpring = EditorGUILayout.FloatField("slerpDrivePositionSpring", joint.slerpDrivePositionSpring);
                        joint.slerpDrivePositionDamper = EditorGUILayout.FloatField("slerpDrivePositionDamper", joint.slerpDrivePositionDamper);
                        joint.slerpDriveMaximumForce = EditorGUILayout.FloatField("slerpDriveMaximumForce", joint.slerpDriveMaximumForce);
                        joint.useSlerpDriveAcceleration = EditorGUILayout.Toggle("SlerpDriveAcceleration", joint.useSlerpDriveAcceleration);
                        --EditorGUI.indentLevel;
                    }
                    --EditorGUI.indentLevel;
                    joint.targetRotation = Quaternion.Euler(EditorGUILayout.Vector3Field("Target Rotation (Euler)", joint.targetRotation.eulerAngles));
                    joint.targetAngularVelocity = EditorGUILayout.Vector3Field("Target Angular Velocity", joint.targetAngularVelocity);

                    --EditorGUI.indentLevel;

                    // Angular Drives

                }
                --EditorGUI.indentLevel;
            }
            //joint.jointsEnabled = EditorGUILayout.Toggle("Use joints", joint.jointsEnabled);
            bool linearJointEnabled = joint.xMotion != ConfigurableJointMotion.Free || joint.yMotion != ConfigurableJointMotion.Free || joint.zMotion != ConfigurableJointMotion.Free;
            bool angularJointEnabled = joint.angularXMotion != ConfigurableJointMotion.Free || joint.angularYMotion != ConfigurableJointMotion.Free || joint.angularZMotion != ConfigurableJointMotion.Free;


            if (linearJointEnabled || angularJointEnabled)//(joint.jointsEnabled)
            {
                bool linearJointLimited = joint.xMotion == ConfigurableJointMotion.Limited || joint.yMotion == ConfigurableJointMotion.Limited || joint.zMotion == ConfigurableJointMotion.Limited;
                bool angularJointLimited = joint.angularXMotion == ConfigurableJointMotion.Limited || joint.angularYMotion == ConfigurableJointMotion.Limited || joint.angularZMotion == ConfigurableJointMotion.Limited;
                jointProperties = EditorGUILayout.Foldout(jointProperties, "Joint properties");

                if (jointProperties)
                {
                    ++EditorGUI.indentLevel;
                    if (linearJointLimited || angularJointLimited)
                    {
                        //++EditorGUI.indentLevel;
                        //joint.linearJointEnabled = EditorGUILayout.Toggle("Linear Joint", joint.linearJointEnabled);
                        if (linearJointLimited)//(joint.linearJointEnabled)
                        {
                            ++EditorGUI.indentLevel;
                            // Soft Joint Limits (manually drawing the limit fields)
                            EditorGUILayout.LabelField("Soft Joint Limit", EditorStyles.boldLabel);
                            ++EditorGUI.indentLevel;
                            joint.linearLimit = EditorGUILayout.FloatField("linearLimit", joint.linearLimit);
                            joint.linearLimitBounciness = EditorGUILayout.FloatField("linearLimitBounciness", joint.linearLimitBounciness);
                            joint.linearLimitContactDistance = EditorGUILayout.FloatField("linearLimitContactDistance", joint.linearLimitContactDistance);
                            --EditorGUI.indentLevel;
                            EditorGUILayout.LabelField("Soft Joint Limit Spring", EditorStyles.boldLabel);
                            ++EditorGUI.indentLevel;
                            joint.linearLimitSpring = EditorGUILayout.FloatField("linearLimitSpring", joint.linearLimitSpring);
                            joint.linearLimitDamper = EditorGUILayout.FloatField("linearLimitDamper", joint.linearLimitDamper);
                            --EditorGUI.indentLevel;
                            --EditorGUI.indentLevel;
                        }
                        //joint.angularJointEnabled = EditorGUILayout.Toggle("Angular Joint", joint.angularJointEnabled);
                        if (angularJointLimited)//(joint.angularJointEnabled)
                        {
                            ++EditorGUI.indentLevel;
                            EditorGUILayout.LabelField("Low Angular X Limit", EditorStyles.boldLabel);
                            ++EditorGUI.indentLevel;
                            joint.lowAngularXLimit = EditorGUILayout.FloatField("lowAngularXLimit", joint.lowAngularXLimit);
                            joint.lowAngularXLimitBounciness = EditorGUILayout.FloatField("lowAngularXLimitBounciness", joint.lowAngularXLimitBounciness);
                            joint.lowAngularXLimitContactDistance = EditorGUILayout.FloatField("lowAngularXLimitContactDistance", joint.lowAngularXLimitContactDistance);
                            --EditorGUI.indentLevel;
                            EditorGUILayout.LabelField("High Angular X Limit", EditorStyles.boldLabel);
                            ++EditorGUI.indentLevel;
                            joint.highAngularXLimit = EditorGUILayout.FloatField("highAngularXLimit", joint.highAngularXLimit);
                            joint.highAngularXLimitBounciness = EditorGUILayout.FloatField("highAngularXLimitBounciness", joint.highAngularXLimitBounciness);
                            joint.highAngularXLimitContactDistance = EditorGUILayout.FloatField("highAngularXLimitContactDistance", joint.highAngularXLimitContactDistance);
                            --EditorGUI.indentLevel;
                            EditorGUILayout.LabelField("Angular X Limit Spring", EditorStyles.boldLabel);
                            ++EditorGUI.indentLevel;
                            joint.angularXLimitSpring = EditorGUILayout.FloatField("angularXLimitSpring", joint.angularXLimitSpring);
                            joint.angularXLimitDamper = EditorGUILayout.FloatField("angularXLimitDamper", joint.angularXLimitDamper);
                            --EditorGUI.indentLevel;
                            EditorGUILayout.LabelField("Angular Y Limit", EditorStyles.boldLabel);
                            ++EditorGUI.indentLevel;
                            joint.angularYLimit = EditorGUILayout.FloatField("angularYLimit", joint.angularYLimit);
                            joint.angularYLimitBounciness = EditorGUILayout.FloatField("angularYLimitBounciness", joint.angularYLimitBounciness);
                            joint.angularYLimitContactDistance = EditorGUILayout.FloatField("angularYLimitContactDistance", joint.angularYLimitContactDistance);
                            --EditorGUI.indentLevel;
                            EditorGUILayout.LabelField("Angular Z Limit", EditorStyles.boldLabel);
                            ++EditorGUI.indentLevel;
                            joint.angularZLimit = EditorGUILayout.FloatField("angularZLimit", joint.angularZLimit);
                            joint.angularZLimitBounciness = EditorGUILayout.FloatField("angularZLimitBounciness", joint.angularZLimitBounciness);
                            joint.angularZLimitContactDistance = EditorGUILayout.FloatField("angularZLimitContactDistance", joint.angularZLimitContactDistance);
                            --EditorGUI.indentLevel;
                            EditorGUILayout.LabelField("Angular YZ Limit Spring", EditorStyles.boldLabel);
                            ++EditorGUI.indentLevel;
                            joint.angularYZLimitSpring = EditorGUILayout.FloatField("angularYZLimitSpring", joint.angularYZLimitSpring);
                            joint.angularYZLimitDamper = EditorGUILayout.FloatField("angularYZLimitDamper", joint.angularYZLimitDamper);
                            --EditorGUI.indentLevel;
                            --EditorGUI.indentLevel;
                        }
                        // Projection Mode Properties

                        //--EditorGUI.indentLevel;
                    }
                    EditorGUILayout.LabelField("Projection Mode Properties", EditorStyles.boldLabel);
                    ++EditorGUI.indentLevel;
                    joint.projectionMode = (JointProjectionMode)EditorGUILayout.EnumPopup("Projection Mode", joint.projectionMode);

                    joint.projectionDistance = EditorGUILayout.FloatField("Projection Distance", joint.projectionDistance);
                    joint.projectionAngle = EditorGUILayout.FloatField("Projection Angle", joint.projectionAngle);
                    --EditorGUI.indentLevel;

                    // Breakable Joint Properties
                    EditorGUILayout.LabelField("Breakable Joint Properties", EditorStyles.boldLabel);
                    ++EditorGUI.indentLevel;
                    joint.breakForce = EditorGUILayout.FloatField("Break Force", joint.breakForce);
                    joint.breakTorque = EditorGUILayout.FloatField("Break Torque", joint.breakTorque);
                    --EditorGUI.indentLevel;
                    --EditorGUI.indentLevel;
                }


            }

            joint.MaxImpulse = EditorGUILayout.FloatField("Max Impulse", joint.MaxImpulse.x);



            //EditorGUILayout.ObjectField(new GUIContent("Localbody"),joint.LocalBody,typeof(PhysicsBodyAuthoring),true);
            //joint.ConnectedBody=EditorGUILayout.ObjectField(new GUIContent("ConnectedBody"), joint.LocalBody, typeof(PhysicsBodyAuthoring), true);
            // Linear Motion











            // Swap Bodies
            joint.swapBodies = EditorGUILayout.Toggle("Swap Bodies", joint.swapBodies);

            joint.EnableCollision = EditorGUILayout.Toggle("Enable Collision", joint.EnableCollision);
            //serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())//(GUI.changed)
            {
                //EditorUtility.ClearDirty(target);
                EditorUtility.SetDirty(target);
            }

        }
        /*
    if (joint.motorsEnabled)
    {
        motorProperties = EditorGUILayout.Foldout(motorProperties, "Motor properties");
        if (motorProperties)
        {
            // Drive Mode
            joint.rotationDriveMode = (RotationDriveMode)EditorGUILayout.EnumPopup("Rotation Drive Mode", joint.rotationDriveMode);
            if (joint.rotationDriveMode == RotationDriveMode.XYAndZ)
            {
                // Linear Drives
                EditorGUILayout.LabelField("Linear Drives", EditorStyles.boldLabel);
                joint.xDrive = DrawJointDriveField("X Drive", joint.xDrive);
                joint.yDrive = DrawJointDriveField("Y Drive", joint.yDrive);
                joint.zDrive = DrawJointDriveField("Z Drive", joint.zDrive);
            }
            else
            {
                EditorGUILayout.LabelField("Slerp Drive", EditorStyles.boldLabel);
                joint.slerpDrive = DrawJointDriveField("Z Drive", joint.zDrive);
            }
            joint.targetPosition = EditorGUILayout.Vector3Field("Target Position", joint.targetPosition);
            joint.targetVelocity = EditorGUILayout.Vector3Field("Target Velocity", joint.targetVelocity);


            // Angular Drives
            EditorGUILayout.LabelField("Angular Drives", EditorStyles.boldLabel);
            joint.angularXDrive = DrawJointDriveField("Angular X Drive", joint.angularXDrive);
            joint.angularYZDrive = DrawJointDriveField("Angular YZ Drive", joint.angularYZDrive);
            // Target Properties
            //EditorGUILayout.LabelField("Target Properties", EditorStyles.boldLabel);
            joint.targetRotation = Quaternion.Euler(EditorGUILayout.Vector3Field("Target Rotation (Euler)", joint.targetRotation.eulerAngles));
            joint.targetAngularVelocity = EditorGUILayout.Vector3Field("Target Angular Velocity", joint.targetAngularVelocity);
        }
    }




    jointProperties = EditorGUILayout.Foldout(jointProperties, "Joint properties");
    if (jointProperties)
    {

        // Soft Joint Limits (manually drawing the limit fields)
        EditorGUILayout.LabelField("Soft Joint Limits", EditorStyles.boldLabel);



        joint.xLimit = new SoftJointLimit { limit = EditorGUILayout.FloatField("X Limit", joint.xLimit.limit) };//DrawSoftJointLimitField("X Limit", joint.xLimit);
        joint.yLimit = DrawSoftJointLimitField("Y Limit", joint.yLimit);
        //var zLimitProperty = serializedObject.FindProperty("zLimit.limit");
        //EditorGUILayout.PropertyField(zLimitProperty);

        joint.zLimit = DrawSoftJointLimitField("Z Limit", joint.zLimit);
        joint.linearLimit = DrawSoftJointLimitField("Limit", joint.linearLimit);
        // Soft Joint Limit Springs
        EditorGUILayout.LabelField("Soft Joint Limit Springs", EditorStyles.boldLabel);
        joint.xLimitSpring = DrawSoftJointLimitSpringField("X Limit", joint.xLimitSpring);
        joint.yLimitSpring = DrawSoftJointLimitSpringField("Y Limit", joint.yLimitSpring);
        joint.zLimitSpring = DrawSoftJointLimitSpringField("Z Limit", joint.zLimitSpring);
        joint.linearLimitSpring = DrawSoftJointLimitSpringField("Limit", joint.linearLimitSpring);
        // Angular Limits
        joint.lowAngularXLimit = DrawSoftJointLimitField("Low Angular X Limit", joint.lowAngularXLimit);
        joint.highAngularXLimit = DrawSoftJointLimitField("High Angular X Limit", joint.highAngularXLimit);
        joint.angularYLimit = DrawSoftJointLimitField("Angular Y Limit", joint.angularYLimit);
        joint.angularZLimit = DrawSoftJointLimitField("Angular Z Limit", joint.angularZLimit);

        // Angular Limit Springs
        EditorGUILayout.LabelField("Angular Limit Springs", EditorStyles.boldLabel);
        joint.angularXLimitSpring = DrawSoftJointLimitSpringField("Angular X Limit", joint.angularXLimitSpring);
        joint.angularYZLimitSpring = DrawSoftJointLimitSpringField("Angular YZ Limit", joint.angularYZLimitSpring);


        // Projection Mode Properties
        EditorGUILayout.LabelField("Projection Mode Properties", EditorStyles.boldLabel);
        joint.projectionMode = (JointProjectionMode)EditorGUILayout.EnumPopup("Projection Mode", joint.projectionMode);

        joint.projectionDistance = EditorGUILayout.FloatField("Projection Distance", joint.projectionDistance);
        joint.projectionAngle = EditorGUILayout.FloatField("Projection Angle", joint.projectionAngle);


        // Breakable Joint Properties
        EditorGUILayout.LabelField("Breakable Joint Properties", EditorStyles.boldLabel);
        joint.breakForce = EditorGUILayout.FloatField("Break Force", joint.breakForce);
        joint.breakTorque = EditorGUILayout.FloatField("Break Torque", joint.breakTorque);
    }

    */

















        /*
    #pragma warning disable 649
        [AutoPopulate] SerializedProperty xLimit;
    #pragma warning restore 649

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(xLimit);


            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }
        */
        /*
        public override void OnInspectorGUI()
        {
            // Reference to the target object
            ConfigurableEntityJointAuthoring joint = (ConfigurableEntityJointAuthoring)target;

            // Store the current value of xLimit
            SoftJointLimit xLimit = joint.xLimit;

            // Begin change check
            EditorGUI.BeginChangeCheck();

            // Display and modify the individual fields of xLimit
            float newLimit = EditorGUILayout.FloatField("X Limit", xLimit.limit);
            float newBounciness = EditorGUILayout.FloatField("Bounciness", xLimit.bounciness);
            float newContactDistance = EditorGUILayout.FloatField("Contact Distance", xLimit.contactDistance);

            // Check if any changes were made
            if (EditorGUI.EndChangeCheck())
            {
                // Record the object for undo/redo
                Undo.RecordObject(joint, "Modify X Limit");

                // Assign the modified values to xLimit
                xLimit.limit = newLimit;
                xLimit.bounciness = newBounciness;
                xLimit.contactDistance = newContactDistance;

                // Assign the modified struct back to the MonoBehaviour
                joint.xLimit = xLimit;

                // Mark the object as dirty to ensure changes are saved in the scene
                EditorUtility.SetDirty(joint);

                // Apply the prefab modifications so that changes persist
                PrefabUtility.RecordPrefabInstancePropertyModifications(joint);

                // Apply the modified properties
                serializedObject.ApplyModifiedProperties();
            }
        }
        */
        /*
        SerializedProperty xLimitProp;
        SerializedProperty limitProp;
        SerializedProperty bouncinessProp;
        SerializedProperty contactDistanceProp;

        void OnEnable()
        {
            // Find the property corresponding to xLimit
            xLimitProp = serializedObject.FindProperty("xLimit");

            // Now find individual properties inside SoftJointLimit
            limitProp = xLimitProp.FindPropertyRelative("m_Limit");
            bouncinessProp = xLimitProp.FindPropertyRelative("m_Bounciness");
            contactDistanceProp = xLimitProp.FindPropertyRelative("m_ContactDistance");
        }

        public override void OnInspectorGUI()
        {
            // Ensure serialized object is updated
            serializedObject.Update();

            // Draw the fields for the SoftJointLimit
            EditorGUILayout.PropertyField(limitProp, new GUIContent("X Limit"));
            EditorGUILayout.PropertyField(bouncinessProp, new GUIContent("Bounciness"));
            EditorGUILayout.PropertyField(contactDistanceProp, new GUIContent("Contact Distance"));

            // Apply the changes to the serialized object
            serializedObject.ApplyModifiedProperties();
        }
        */
        /*
        bool motorProperties;
        bool jointProperties;
        SerializedProperty lookAtPoint;

        void OnEnable()
        {
            lookAtPoint = serializedObject.FindProperty("xLimit");
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(lookAtPoint);
            serializedObject.ApplyModifiedProperties();
        }
        */
        /*
        public override void OnInspectorGUI()
        {
            //serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            // Get the target object
            ConfigurableEntityJointAuthoring joint = (ConfigurableEntityJointAuthoring)target;

            // Display LocalBody and ConnectedBody
            joint.ConnectedBody = (PhysicsBodyAuthoring)EditorGUILayout.ObjectField("Connected Body", joint.ConnectedBody, typeof(PhysicsBodyAuthoring), true);
            // Show PositionLocal and PositionInConnectedEntity (float3)
            joint.PositionLocal = DrawFloat3Field("Position Local", joint.PositionLocal);
            joint.axis = DrawFloat3Field("Axis", joint.axis);
            //joint.EditPivots = EditorGUILayout.Toggle("Edit Pivots", joint.EditPivots);
            joint.AutoSetConnected = EditorGUILayout.Toggle("Auto Set Connected", joint.AutoSetConnected);
            joint.PositionInConnectedEntity = DrawFloat3Field("Position In Connected Entity", joint.PositionInConnectedEntity);
            joint.secondaryAxis = DrawFloat3Field("Secondary Axis", joint.secondaryAxis);
            //EditorGUILayout.LabelField("Linear Motion Settings", EditorStyles.boldLabel);
            joint.xMotion = (ConfigurableJointMotion)EditorGUILayout.EnumPopup("X Motion", joint.xMotion);
            joint.yMotion = (ConfigurableJointMotion)EditorGUILayout.EnumPopup("Y Motion", joint.yMotion);
            joint.zMotion = (ConfigurableJointMotion)EditorGUILayout.EnumPopup("Z Motion", joint.zMotion);

            // Angular Motion
            //EditorGUILayout.LabelField("Angular Motion Settings", EditorStyles.boldLabel);
            joint.angularXMotion = (ConfigurableJointMotion)EditorGUILayout.EnumPopup("Angular X Motion", joint.angularXMotion);
            joint.angularYMotion = (ConfigurableJointMotion)EditorGUILayout.EnumPopup("Angular Y Motion", joint.angularYMotion);
            joint.angularZMotion = (ConfigurableJointMotion)EditorGUILayout.EnumPopup("Angular Z Motion", joint.angularZMotion);
            joint.motorsEnabled = EditorGUILayout.Toggle("Use motors", joint.motorsEnabled);
            if (joint.motorsEnabled)
            {
                motorProperties = EditorGUILayout.Foldout(motorProperties, "Motor properties");
                if (motorProperties)
                {
                    // Drive Mode
                    joint.rotationDriveMode = (RotationDriveMode)EditorGUILayout.EnumPopup("Rotation Drive Mode", joint.rotationDriveMode);
                    if (joint.rotationDriveMode == RotationDriveMode.XYAndZ)
                    {
                        // Linear Drives
                        EditorGUILayout.LabelField("Linear Drives", EditorStyles.boldLabel);
                        joint.xDrive = DrawJointDriveField("X Drive", joint.xDrive);
                        joint.yDrive = DrawJointDriveField("Y Drive", joint.yDrive);
                        joint.zDrive = DrawJointDriveField("Z Drive", joint.zDrive);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Slerp Drive", EditorStyles.boldLabel);
                        joint.slerpDrive = DrawJointDriveField("Z Drive", joint.zDrive);
                    }
                    joint.targetPosition = EditorGUILayout.Vector3Field("Target Position", joint.targetPosition);
                    joint.targetVelocity = EditorGUILayout.Vector3Field("Target Velocity", joint.targetVelocity);


                    // Angular Drives
                    EditorGUILayout.LabelField("Angular Drives", EditorStyles.boldLabel);
                    joint.angularXDrive = DrawJointDriveField("Angular X Drive", joint.angularXDrive);
                    joint.angularYZDrive = DrawJointDriveField("Angular YZ Drive", joint.angularYZDrive);
                    // Target Properties
                    //EditorGUILayout.LabelField("Target Properties", EditorStyles.boldLabel);
                    joint.targetRotation = Quaternion.Euler(EditorGUILayout.Vector3Field("Target Rotation (Euler)", joint.targetRotation.eulerAngles));
                    joint.targetAngularVelocity = EditorGUILayout.Vector3Field("Target Angular Velocity", joint.targetAngularVelocity);
                }
            }




            jointProperties = EditorGUILayout.Foldout(jointProperties, "Joint properties");
            if (jointProperties)
            {

                // Soft Joint Limits (manually drawing the limit fields)
                EditorGUILayout.LabelField("Soft Joint Limits", EditorStyles.boldLabel);



                joint.xLimit = new SoftJointLimit { limit = EditorGUILayout.FloatField("X Limit", joint.xLimit.limit) };//DrawSoftJointLimitField("X Limit", joint.xLimit);
                joint.yLimit = DrawSoftJointLimitField("Y Limit", joint.yLimit);
                //var zLimitProperty = serializedObject.FindProperty("zLimit.limit");
                //EditorGUILayout.PropertyField(zLimitProperty);

                joint.zLimit = DrawSoftJointLimitField("Z Limit", joint.zLimit);
                joint.linearLimit = DrawSoftJointLimitField("Limit", joint.linearLimit);
                // Soft Joint Limit Springs
                EditorGUILayout.LabelField("Soft Joint Limit Springs", EditorStyles.boldLabel);
                joint.xLimitSpring = DrawSoftJointLimitSpringField("X Limit", joint.xLimitSpring);
                joint.yLimitSpring = DrawSoftJointLimitSpringField("Y Limit", joint.yLimitSpring);
                joint.zLimitSpring = DrawSoftJointLimitSpringField("Z Limit", joint.zLimitSpring);
                joint.linearLimitSpring = DrawSoftJointLimitSpringField("Limit", joint.linearLimitSpring);
                // Angular Limits
                joint.lowAngularXLimit = DrawSoftJointLimitField("Low Angular X Limit", joint.lowAngularXLimit);
                joint.highAngularXLimit = DrawSoftJointLimitField("High Angular X Limit", joint.highAngularXLimit);
                joint.angularYLimit = DrawSoftJointLimitField("Angular Y Limit", joint.angularYLimit);
                joint.angularZLimit = DrawSoftJointLimitField("Angular Z Limit", joint.angularZLimit);

                // Angular Limit Springs
                EditorGUILayout.LabelField("Angular Limit Springs", EditorStyles.boldLabel);
                joint.angularXLimitSpring = DrawSoftJointLimitSpringField("Angular X Limit", joint.angularXLimitSpring);
                joint.angularYZLimitSpring = DrawSoftJointLimitSpringField("Angular YZ Limit", joint.angularYZLimitSpring);


                // Projection Mode Properties
                EditorGUILayout.LabelField("Projection Mode Properties", EditorStyles.boldLabel);
                joint.projectionMode = (JointProjectionMode)EditorGUILayout.EnumPopup("Projection Mode", joint.projectionMode);

                joint.projectionDistance = EditorGUILayout.FloatField("Projection Distance", joint.projectionDistance);
                joint.projectionAngle = EditorGUILayout.FloatField("Projection Angle", joint.projectionAngle);


                // Breakable Joint Properties
                EditorGUILayout.LabelField("Breakable Joint Properties", EditorStyles.boldLabel);
                joint.breakForce = EditorGUILayout.FloatField("Break Force", joint.breakForce);
                joint.breakTorque = EditorGUILayout.FloatField("Break Torque", joint.breakTorque);
            }






            joint.MaxImpulse = EditorGUILayout.FloatField("Max Impulse", joint.MaxImpulse.x);



            //EditorGUILayout.ObjectField(new GUIContent("Localbody"),joint.LocalBody,typeof(PhysicsBodyAuthoring),true);
            //joint.ConnectedBody=EditorGUILayout.ObjectField(new GUIContent("ConnectedBody"), joint.LocalBody, typeof(PhysicsBodyAuthoring), true);
            // Linear Motion











            // Swap Bodies
            joint.swapBodies = EditorGUILayout.Toggle("Swap Bodies", joint.swapBodies);

            joint.EnableCollision = EditorGUILayout.Toggle("Enable Collision", joint.EnableCollision);
            //serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())//(GUI.changed)
            {
                    //EditorUtility.ClearDirty(target);
                    EditorUtility.SetDirty(target);
            }

        }
        */

        private float3 DrawFloat3Field(string label, float3 value)
        {
            Vector3 vector = new Vector3(value.x, value.y, value.z);
            vector = EditorGUILayout.Vector3Field(label, vector);
            return new float3(vector.x, vector.y, vector.z);
        }

        private SoftJointLimit DrawSoftJointLimitField(string label, SoftJointLimit limit)
        {
            limit.limit = EditorGUILayout.FloatField(label, limit.limit);
            return limit;
        }

        private SoftJointLimitSpring DrawSoftJointLimitSpringField(string label, SoftJointLimitSpring spring)
        {
            spring.spring = EditorGUILayout.FloatField(label + " Spring", spring.spring);
            spring.damper = EditorGUILayout.FloatField(label + " Damper", spring.damper);
            return spring;
        }

        private JointDrive DrawJointDriveField(string label, JointDrive drive)
        {
            drive.positionSpring = EditorGUILayout.FloatField(label + " Position Spring", drive.positionSpring);
            drive.positionDamper = EditorGUILayout.FloatField(label + " Position Damper", drive.positionDamper);
            drive.maximumForce = EditorGUILayout.FloatField(label + " Maximum Force", drive.maximumForce);
            return drive;
        }
    }

