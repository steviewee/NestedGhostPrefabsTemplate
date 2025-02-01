using System;
using System.Collections.Generic;
using Unity.Entities.Conversion;
using Zhorman.NestedGhostPrefabs.Runtime.Authoring;
using Unity.NetCode;
using Unity.NetCode.Analytics;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.NetCode.Hybrid;
using System.Linq;


    /// <summary>
    /// This needs major rework, but I just can't be bothered
    /// </summary>
    [CustomEditor(typeof(NestedGhostAuthoring))]
    [CanEditMultipleObjects]
    internal class NestedGhostAuthoringEditor : UnityEditor.Editor
    {
        SerializedProperty DefaultGhostMode;
        SerializedProperty SupportedGhostModes;
        SerializedProperty OptimizationMode;
        SerializedProperty HasOwner;
        SerializedProperty SupportAutoCommandTarget;
        SerializedProperty TrackInterpolationDelay;
        SerializedProperty GhostGroup;
        SerializedProperty ImmidiateGhostGrouping;
        SerializedProperty UsePreSerialization;
        SerializedProperty Importance;
        SerializedProperty PredictedSpawnedGhostRollbackToSpawnTick;
        SerializedProperty RollbackPredictionOnStructuralChanges;

        SerializedProperty NetworkedParentship;
        SerializedProperty UseImmitatedParenting;
        SerializedProperty ShouldBeUnParented;
        SerializedProperty UseOriginal;
        SerializedProperty MaxSendRate;
        SerializedProperty Rereferencing;
        SerializedProperty NonGhostRereferencing;
        SerializedProperty LinkedEntityRelinking;




        internal static Color brokenColor = new Color(1f, 0.56f, 0.54f);
        internal static Color brokenColorUIToolkit = new Color(0.35f, 0.19f, 0.19f);
        internal static Color brokenColorUIToolkitText = new Color(0.9f, 0.64f, 0.61f);
        private static readonly GUILayoutOption s_HelperWidth = GUILayout.Width(180);

        /// <summary>Aligned with NetCode for GameObjects.</summary>
        public static Color netcodeColor => new Color(0.91f, 0.55f, 0.86f, 1f);

        void OnEnable()
        {
            DefaultGhostMode = serializedObject.FindProperty(nameof(NestedGhostAuthoring.DefaultGhostMode));
            SupportedGhostModes = serializedObject.FindProperty(nameof(NestedGhostAuthoring.SupportedGhostModes));
            OptimizationMode = serializedObject.FindProperty(nameof(NestedGhostAuthoring.OptimizationMode));
            HasOwner = serializedObject.FindProperty(nameof(NestedGhostAuthoring.HasOwner));
            SupportAutoCommandTarget = serializedObject.FindProperty(nameof(NestedGhostAuthoring.SupportAutoCommandTarget));
            TrackInterpolationDelay = serializedObject.FindProperty(nameof(NestedGhostAuthoring.TrackInterpolationDelay));
            GhostGroup = serializedObject.FindProperty(nameof(NestedGhostAuthoring.GhostGroup));
            ImmidiateGhostGrouping = serializedObject.FindProperty(nameof(NestedGhostAuthoring.ImmidiateGhostGrouping));
            UsePreSerialization = serializedObject.FindProperty(nameof(NestedGhostAuthoring.UsePreSerialization));
            Importance = serializedObject.FindProperty(nameof(NestedGhostAuthoring.Importance));
            PredictedSpawnedGhostRollbackToSpawnTick = serializedObject.FindProperty(nameof(NestedGhostAuthoring.RollbackPredictedSpawnedGhostState));
            RollbackPredictionOnStructuralChanges = serializedObject.FindProperty(nameof(NestedGhostAuthoring.RollbackPredictionOnStructuralChanges));

            NetworkedParentship = serializedObject.FindProperty(nameof(NestedGhostAuthoring.NetworkedParentship));
            UseImmitatedParenting = serializedObject.FindProperty(nameof(NestedGhostAuthoring.UseImmitatedParenting));
            ShouldBeUnParented = serializedObject.FindProperty(nameof(NestedGhostAuthoring.ShouldBeUnParented));
            UseOriginal = serializedObject.FindProperty(nameof(NestedGhostAuthoring.UseOriginal));

            MaxSendRate = serializedObject.FindProperty(nameof(NestedGhostAuthoring.MaxSendRate));
            Rereferencing = serializedObject.FindProperty(nameof(NestedGhostAuthoring.Rereferencing));
            NonGhostRereferencing = serializedObject.FindProperty(nameof(NestedGhostAuthoring.NonGhostRereferencing));
            LinkedEntityRelinking = serializedObject.FindProperty(nameof(NestedGhostAuthoring.LinkedEntityRelinking));
        }

        public override void OnInspectorGUI()
        {
            var authoringComponent = (NestedGhostAuthoring)target;
            var originalColor = GUI.color;

            GUI.color = originalColor;
            // Importance:
            {
                EditorGUILayout.BeginHorizontal();
                var importanceContent = new GUIContent(nameof(Importance), GetImportanceFieldTooltip());
                EditorGUILayout.PropertyField(Importance, importanceContent);
                var editorImportanceSuggestion = ImportanceInlineTooltip(authoringComponent.Importance);
                importanceContent.text = editorImportanceSuggestion.Name;
                GUILayout.Box(importanceContent, s_HelperWidth);
                EditorGUILayout.EndHorizontal();
            }
            // MaxSendRate:
            {
                var hasMaxSendRate = authoringComponent.MaxSendRate != 0;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(MaxSendRate);
                var globalConfig = NetCodeClientAndServerSettings.instance?.GlobalNetCodeConfig;
                var tickRate = globalConfig != null ? globalConfig.ClientServerTickRate : new ClientServerTickRate();
                tickRate.ResolveDefaults();
                var clientTickRate = globalConfig != null ? NetCodeClientAndServerSettings.instance.GlobalNetCodeConfig.ClientTickRate : NetworkTimeSystem.DefaultClientTickRate;
                var sendInterval = tickRate.CalculateNetworkSendIntervalOfGhostInTicks(authoringComponent.MaxSendRate);
                var label = new GUIContent(SendRateInlineTooltip(), MaxSendRate.tooltip);
                GUILayout.Box(label, s_HelperWidth);

                string SendRateInlineTooltip() =>
                    (sendInterval, hasMaxSendRate) switch
                    {
                        (_, false) => "OFF | Every Snapshot",
                        (1, true) => "Every Snapshot",
                        (2, true) => "Every Other Snapshot",
                        (_, true) => $"Every {WithOrdinalSuffix(sendInterval)} Snapshot",
                    } + $" @ {tickRate.NetworkTickRate}Hz";

                EditorGUILayout.EndHorizontal();

                // MaxSendRate warning:
                if (authoringComponent.SupportedGhostModes != GhostModeMask.Predicted)
                {
                    var interpolationBufferWindowInTicks = clientTickRate.CalculateInterpolationBufferTimeInTicks(in tickRate);
                    var delta = sendInterval - interpolationBufferWindowInTicks;
                    if (delta > 0)
                    {
                        EditorGUILayout.HelpBox($"This ghost prefab is using a MaxSendRate value of {authoringComponent.MaxSendRate}, which leads to a maximum send interval of '{label.text}' i.e. every {sendInterval}ms, which is {delta} ticks longer than your maximum interpolation buffer window of {interpolationBufferWindowInTicks} ticks. You are therefore not replicating this ghost often enough to allow it to smoothly interpolate. To fix; either increase MaxSendRate, or increase the size of the interpolation buffer window globally.", MessageType.Warning);
                    }
                }
            }

            EditorGUILayout.PropertyField(SupportedGhostModes);

            var self = (NestedGhostAuthoring) target;
            var isOwnerPredictedError = DefaultGhostMode.enumValueIndex == (int) GhostMode.OwnerPredicted && !self.HasOwner;

            if (SupportedGhostModes.intValue == (int) GhostModeMask.All)
            {
                EditorGUILayout.PropertyField(DefaultGhostMode);

                // Selecting OwnerPredicted on a ghost without a GhostOwner will cause an exception during conversion - display an error for that case in the inspector
                if (isOwnerPredictedError)
                {
                    EditorGUILayout.HelpBox("Setting `Default Ghost Mode` to `Owner Predicted` is not valid unless the Ghost also supports being Owned by a player (via the `Ghost Owner Component`). Please resolve it one of the following ways.", MessageType.Error);
                    GUI.color = brokenColor;
                    if (GUILayout.Button("Enable Ownership via 'Has Owner'?")) HasOwner.boolValue = true;
                    if (GUILayout.Button("Set to `GhostMode.Interpolated`?")) DefaultGhostMode.enumValueIndex = (int) GhostMode.Interpolated;
                    if (GUILayout.Button("Set to `GhostMode.Predicted`?")) DefaultGhostMode.enumValueIndex = (int) GhostMode.Predicted;
                    GUI.color = originalColor;
                }
            }

            EditorGUILayout.PropertyField(OptimizationMode);
            EditorGUILayout.PropertyField(HasOwner);

            if (self.HasOwner)
            {
                EditorGUILayout.PropertyField(SupportAutoCommandTarget);
                EditorGUILayout.PropertyField(TrackInterpolationDelay);
            }
            EditorGUILayout.PropertyField(GhostGroup);

            if(self.GhostGroup)
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(ImmidiateGhostGrouping);
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.PropertyField(UsePreSerialization);

            if(self.SupportedGhostModes != GhostModeMask.Interpolated)
            {
                EditorGUILayout.PropertyField(PredictedSpawnedGhostRollbackToSpawnTick);
                EditorGUILayout.PropertyField(RollbackPredictionOnStructuralChanges);
            }
            EditorGUILayout.PropertyField(NetworkedParentship);
            if (self.NetworkedParentship)
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(UseImmitatedParenting);
                --EditorGUI.indentLevel;
            }
            EditorGUILayout.PropertyField(ShouldBeUnParented);
            EditorGUILayout.PropertyField(UseOriginal);
            EditorGUILayout.PropertyField(Rereferencing);

            if (self.Rereferencing)
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(NonGhostRereferencing);
                --EditorGUI.indentLevel;
            }
            EditorGUILayout.PropertyField(LinkedEntityRelinking);

            if (serializedObject.ApplyModifiedProperties())
            {
                NestedGhostInspectionAuthoring.forceBake = true;
                var allComponentOverridesForGhost = NestedGhostInspectionAuthoring.CollectAllComponentOverridesInInspectionComponents(authoringComponent, false);
                BufferConfigurationData(authoringComponent, allComponentOverridesForGhost.Count);
            }
        }
        void BufferConfigurationData(NestedGhostAuthoring ghostComponent, int numVariants)
        {
            var analyticsData = new GhostConfigurationAnalyticsData
            {
                id = ghostComponent.PrefabId,
                autoCommandTarget = ghostComponent.SupportAutoCommandTarget,
                optimizationMode = ghostComponent.OptimizationMode.ToString(),
                ghostMode = ghostComponent.DefaultGhostMode.ToString(),
                importance = ghostComponent.Importance,
                variance = numVariants,
            };
            NetCodeAnalytics.StoreGhostComponent(analyticsData);
        }
        internal string GetImportanceFieldTooltip()
        {
            var suggestions = NetCodeClientAndServerSettings.instance.CurrentImportanceSuggestions;
            var s = Importance.tooltip;
            foreach (var eis in suggestions)
            {
                var value = eis.MaxValue == uint.MaxValue || eis.MaxValue == eis.MinValue || eis.MaxValue == 0
                    ? $"~{eis.MinValue}" : $"{eis.MinValue} ~ {eis.MaxValue}";
                s += $"\n\n <b>{value}</b> for <b>{eis.Name}</b>\n<i>{eis.Tooltip}</i>";
            }
            return s;
        }

        internal static EditorImportanceSuggestion ImportanceInlineTooltip(long importance)
        {
            var suggestions = NetCodeClientAndServerSettings.instance.CurrentImportanceSuggestions;
            foreach (var eis in suggestions)
            {
                if (importance <= eis.MaxValue)
                {
                    return eis;
                }
            }
            return suggestions.LastOrDefault();
        }

        /// <summary>Adds the ordinal indicator/suffix to an integer.</summary>
        internal static string WithOrdinalSuffix(long number)
        {
            // Numbers in the teens always end with "th".
            if ((number % 100 > 10 && number % 100 < 20))
                return number + "th";
            return (number % 10) switch
            {
                1 => number + "st",
                2 => number + "nd",
                3 => number + "rd",
                _ => number + "th",
            };
        }
    }

