using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Entities.Editor;
using Zhorman.NestedGhostPrefabs.Runtime.Authoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

using Unity.NetCode;


    // TODO: Undo/redo is broken in the Editor.
    // TODO: Support copy/paste individual meta datas + main components.
    // TODO: Support multi-object-edit.
    // TODO: Support light-mode.

    /// <summary>UIToolkit drawer for <see cref="NestedGhostInspectionAuthoring"/>.</summary>
    [CustomEditor(typeof(NestedGhostInspectionAuthoring))]
    class NestedGhostInspectionAuthoringEditor : UnityEditor.Editor
    {
        const string k_ExpandKey = "NetCode.Inspection.Expand.";
        const string k_PackageId = "Packages/com.unity.netcode";
        const string k_AutoBakeKey = "AutoBake";

        // TODO - Manually loaded prefabs as uss is not working.
        static Texture2D PrefabEntityIcon => AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.entities/Editor Default Resources/icons/dark/Entity/EntityPrefab.png");
        static Texture2D ComponentIcon => AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.entities/Editor Default Resources/icons/dark/Components/Component.png");

        internal static NestedGhostPrefabPreview prefabPreview { get; private set; }
        internal static readonly Dictionary<NestedGhostInspectionAuthoring, NestedBakedResult> cachedNestedBakedResults = new(4);
        internal static bool isPrefabEditable { get; private set; }
        internal static bool hasCachedBakingResult => cachedNestedBakedResults.ContainsKey(inspection);
        internal static NestedGhostInspectionAuthoring inspection { get; private set; }

        VisualElement m_Root;
        VisualElement m_ResultsPane;

        HelpBox m_UnableToFindComponentHelpBox;
        HelpBox m_NoEntityHelpBox;
        private Toggle m_AutoBakeToggle;
        private Button m_BakeButton;
        private int m_NumComponentsOnThisInspection;


        void OnEnable()
        {
            inspection = target as NestedGhostInspectionAuthoring;

            isPrefabEditable = true;
            EditorApplication.update += OnUpdate;
            Undo.undoRedoPerformed += RequestRebuildInspector;
            m_NumComponentsOnThisInspection = NestedGhostPrefabPreview.CountComponents(inspection.gameObject);
        }

        void OnDisable()
        {
            EditorApplication.update -= OnUpdate;
            Undo.undoRedoPerformed -= RequestRebuildInspector;
        }

        void OnUpdate()
        {
            inspection = target as NestedGhostInspectionAuthoring;
            if (m_AutoBakeToggle == null || !inspection)
            {
                //Debug.Log($"NestedGhostInspectionAuthoringEditor returned because {m_AutoBakeToggle == null} || {!inspection}");
                return;
            }


            // Check for changes:
            if (TryGetEntitiesAssociatedWithAuthoringGameObject(out var NestedBakedGameObjectResult))
            {
                var hasChanged = NestedBakedGameObjectResult.NumComponents != m_NumComponentsOnThisInspection;
                if (hasChanged)
                {
                    //Debug.Log($"NestedGhostInspectionAuthoringEditor hasChanged!");
                    NestedBakedGameObjectResult.NumComponents = m_NumComponentsOnThisInspection;

                    if (m_AutoBakeToggle.value)
                    {
                        NestedGhostInspectionAuthoring.forceBake = true;
                    }

                }
            }

            if (NestedGhostInspectionAuthoring.forceBake)
            {
                //Debug.Log($"NestedGhostInspectionAuthoringEditor forceBake = true");
                BakeNetCodePrefab();
            }


            if (NestedGhostInspectionAuthoring.forceSave)
            {
                //Debug.Log($"NestedGhostInspectionAuthoringEditor forceSave = true");
                NestedGhostInspectionAuthoring.forceSave = false;
                NestedGhostInspectionAuthoring.forceRebuildInspector = true;

                EditorSceneManager.MarkSceneDirty(inspection.gameObject.scene);
                Array.Sort(inspection.ComponentOverrides);
                EditorUtility.SetDirty(inspection);
            }

            if (NestedGhostInspectionAuthoring.forceRebuildInspector)
            {
                //Debug.Log($"NestedGhostInspectionAuthoringEditor forceRebuildInspector = true");
                RebuildWindow();
            }

        }

        internal bool TryGetEntitiesAssociatedWithAuthoringGameObject(out NestedBakedGameObjectResult result)
        {
            if (TryGetNestedBakedResultAssociatedWithAuthoringGameObject(out var NestedBakedResult))
            {
                result = NestedBakedResult.GetInspectionResult(inspection);
                return result != null;
            }

            result = default;
            return false;
        }

        internal bool TryGetNestedBakedResultAssociatedWithAuthoringGameObject(out NestedBakedResult result)
        {
            if (cachedNestedBakedResults.TryGetValue(inspection, out result))
            {
                return true;
            }

            if (NestedGhostInspectionAuthoring.forceBake)
            {
                BakeNetCodePrefab();
                if (cachedNestedBakedResults.TryGetValue(inspection, out result))
                {
                    return true;
                }
            }
            return false;
        }

        public void BakeNetCodePrefab()
        {
            var ghostAuthoring = FindRootNestedGhostAuthoring();

            // These allow interop with NestedGhostInspectionAuthoringEditor.
            prefabPreview = new NestedGhostPrefabPreview();

            try
            {
                prefabPreview.BakeEntireNetcodePrefab(ghostAuthoring, inspection, cachedNestedBakedResults);
            }
            catch
            {
                cachedNestedBakedResults.Remove(inspection);
                throw;
            }
        }

        private static NestedGhostAuthoring FindRootNestedGhostAuthoring()
        {
            var ghostAuthoring = inspection.GetComponent<NestedGhostAuthoring>()
                                 ?? PrefabUtility.GetNearestPrefabInstanceRoot(inspection)?.GetComponent<NestedGhostAuthoring>()
                                 ?? inspection.transform.root.GetComponent<NestedGhostAuthoring>();
            return ghostAuthoring;
        }

        static void RequestRebuildInspector() => NestedGhostInspectionAuthoring.forceRebuildInspector = true;

        public override VisualElement CreateInspectorGUI()
        {
            if (m_Root != null) return m_Root;

            inspection = target as NestedGhostInspectionAuthoring;

            m_Root = new VisualElement();
            m_Root.style.overflow = new StyleEnum<Overflow>(Overflow.Hidden);
            m_Root.style.flexShrink = 1;

            var ss = AssetDatabase.LoadAssetAtPath<StyleSheet>(Path.Combine(k_PackageId, "Editor/Authoring/GhostAuthoringEditor.uss"));
            m_Root.styleSheets.Add(ss);

            m_BakeButton = new Button(HandleBakeButtonClicked);
            m_BakeButton.name = "RefreshButton";
            m_BakeButton.text = "Refresh";
            m_BakeButton.tooltip = "Trigger this prefab to be NestedBaked, allowing you to view and edit netcode-related settings on a per-entity and per-component basis.";
            m_BakeButton.style.height = 32;
            m_Root.Add(m_BakeButton);

            m_AutoBakeToggle = new Toggle("Auto-Refresh");
            m_AutoBakeToggle.name = "Auto-Refresh Toggle";
            m_AutoBakeToggle.value = GetShouldExpand(k_AutoBakeKey, true);
            m_AutoBakeToggle.RegisterValueChangedCallback(evt => HandleAutoBakeValueChanged());
            HandleAutoBakeValueChanged();
            m_AutoBakeToggle.tooltip = "When enabled, Unity will automatically bake the selected prefab the first time automatically every time it changes. Disableable as it's a slow operation. Your preference is saved locally.";
            m_Root.Add(m_AutoBakeToggle);

            m_UnableToFindComponentHelpBox = new HelpBox($"Unable to find associated {nameof(NestedGhostAuthoring)} in root or parent. " +
                                                         $"Either ensure it exists, or remove this component.", HelpBoxMessageType.Error);
            m_Root.Add(m_UnableToFindComponentHelpBox);

            m_NoEntityHelpBox = new HelpBox($"This GameObject does not create any Entities during baking.", HelpBoxMessageType.Info);
            m_Root.Add(m_NoEntityHelpBox);

            // TODO - Support edge-case where user adds an override to a type and then disables it in code.
            // TODO - Explicitly support changing variant but not anything else if the user does not add the `[SupportPrefabOverrides]` attribute.

            m_ResultsPane = new VisualElement();
            m_ResultsPane.name = "ResultsPane";

            m_Root.Add(m_ResultsPane);

            RebuildWindow();

            return m_Root;
        }

        private void HandleAutoBakeValueChanged()
        {
            SetShouldExpand(k_AutoBakeKey, m_AutoBakeToggle.value);
            SetVisualElementVisibility(m_BakeButton, !m_AutoBakeToggle.value);

            if (m_AutoBakeToggle.value && !hasCachedBakingResult)
                NestedGhostInspectionAuthoring.forceBake = true;
        }

        private void HandleBakeButtonClicked()
        {
            NestedGhostInspectionAuthoring.forceBake = true;
        }

        void RebuildWindow()
        {
            inspection = target as NestedGhostInspectionAuthoring;

            if (m_Root == null) CreateInspectorGUI();
            NestedGhostInspectionAuthoring.forceRebuildInspector = false;
            m_ResultsPane.Clear();

            var hasEntitiesForThisGameObject = TryGetEntitiesAssociatedWithAuthoringGameObject(out var NestedBakedGameObjectResult);
            //if(hasEntitiesForThisGameObject && !NestedBakedGameObjectResult.SourceGameObject)
            //{
            //    Debug.LogWarning($"SetVisualElementVisibility {hasEntitiesForThisGameObject} and {!NestedBakedGameObjectResult.SourceGameObject}");
            //}

            SetVisualElementVisibility(m_UnableToFindComponentHelpBox, hasEntitiesForThisGameObject && !NestedBakedGameObjectResult.SourceGameObject);
           // if (hasEntitiesForThisGameObject && NestedBakedGameObjectResult.NestedBakedEntities.Count == 0)
            //{
           //     Debug.LogWarning($"m_NoEntityHelpBox {hasEntitiesForThisGameObject} and {NestedBakedGameObjectResult.NestedBakedEntities.Count == 0}");
           // }
            SetVisualElementVisibility(m_NoEntityHelpBox, hasEntitiesForThisGameObject && NestedBakedGameObjectResult.NestedBakedEntities.Count == 0);

            var isEditable = hasCachedBakingResult && isPrefabEditable;
            m_ResultsPane.SetEnabled(isEditable);

            if (!hasEntitiesForThisGameObject)
            {
            //    Debug.LogWarning($"!hasEntitiesForThisGameObject {!hasEntitiesForThisGameObject}");
                return;
            }


            for (var entityIndex = 0; entityIndex < NestedBakedGameObjectResult.NestedBakedEntities.Count; entityIndex++)
            {
                const int arbitraryMaxNumAdditionalEntitiesWeCanDisplay = 20;
                if (entityIndex > arbitraryMaxNumAdditionalEntitiesWeCanDisplay + 1)
                {
                    m_ResultsPane.Add(new HelpBox($"Authoring GameObject '{NestedBakedGameObjectResult.SourceGameObject.name}' creates {NestedBakedGameObjectResult.NestedBakedEntities.Count} \"Additional\" entities ({(NestedBakedGameObjectResult.NestedBakedEntities.Count - entityIndex)} are hidden)." +
                                                  " For performance reasons, we cannot display this many." +
                                                  " If you must add a ComponentOverride for an additional entity, please attempt to do so by modifying the YAML directly. ", HelpBoxMessageType.Warning));
                    break;
                }

                var NestedBakedEntityResult = NestedBakedGameObjectResult.NestedBakedEntities[entityIndex];
                var entityHeader = new FoldoutHeaderElement("EntityLabel", NestedBakedEntityResult.EntityName,
                    $"[{NestedBakedEntityResult.Guid}] {(NestedBakedEntityResult.EntityIndex + 1)} / {NestedBakedGameObjectResult.NestedBakedEntities.Count}",
                    "Displays the entity or entities created during Baking of this GameObject.", false);

                entityHeader.AddToClassList("ghost-inspection-entity-header");
                entityHeader.style.marginLeft = 10;
                //entityLabel.label.AddToClassList("ghost-inspection-entity-header__label");
                entityHeader.icon.AddToClassList("ghost-inspection-entity-header__icon");
                entityHeader.icon.style.backgroundImage = PrefabEntityIcon;
                entityHeader.foldout.text += (NestedBakedEntityResult.IsPrimaryEntity) ? " (Primary)" : " (Additional)";
                m_ResultsPane.Add(entityHeader);

                var allComponents = NestedBakedEntityResult.NestedBakedComponents;
                var replicated = new List<NestedBakedComponentItem>(allComponents.Count);
                var nonReplicated = new List<NestedBakedComponentItem>(allComponents.Count);
                foreach (var component in allComponents)
                {
                    if (component.anyVariantIsSerialized)
                        replicated.Add(component);
                    else nonReplicated.Add(component);
                }

                var toggleKey = NestedBakedEntityResult.Guid.ToString();
                var replicatedContainer = CreateReplicationHeaderElement(entityHeader.foldout.contentContainer, replicated,
                    "ReplicatedLabel", "Meta-data for GhostComponents", "Lists all netcode meta-data for replicated (i.e. synced) component types.",
                    NestedGhostAuthoringEditor.netcodeColor, true, toggleKey);

                // Prefer default variants:
                if (NestedBakedEntityResult.GoParent.SourceInspection.ComponentOverrides.Length > 0)
                {
                    replicatedContainer.contentContainer.Add(
                        new HelpBox($"If you intend to use one Variant across <b>all Ghosts</b> (e.g. `Translation - 2D` for a 2D game), prefer to set it as the \"Default Variant\" by implementing `RegisterDefaultVariants` " +
                                    "in your own system (derived from `DefaultVariantSystemBase`), rather than using these controls. " +
                                    "You can also set defaults for `GhostPrefabTypes` via the `GhostComponentAttribute`.", HelpBoxMessageType.Info));
                }
                else
                {
                    m_ResultsPane.Add(
                        new HelpBox($"Note that this Inspection Component is optional. As you haven't made any overrides, you can safely remove this component.", HelpBoxMessageType.Info));
                }

                // Warn about replicating child components:
                if (!NestedBakedEntityResult.IsRoot)
                {
                    if (replicated.Any(x => x.serializationStrategy.IsSerialized != 0))
                    {
                        replicatedContainer.contentContainer.Add(new HelpBox("Note: Serializing child entities is relatively slow. " +
                                                                             "Prefer to have multiple Ghosts with faked parenting, if possible.", HelpBoxMessageType.Warning));
                    }
                }

                CreateReplicationHeaderElement(entityHeader.foldout.contentContainer, nonReplicated,
                    "NonReplicatedLabel", "Meta-data for non-replicated Components", "Lists all netcode meta-data for non-replicated component types.",
                    Color.white, false, toggleKey);
            }

            // Display invalid overrides:
            if (inspection.ComponentOverrides.Any(x => !x.DidCorrectlyMap))
            {
                //.
                var title = new HelpBox("Detected duplicated or otherwise invalid serialized 'Component Overrides'! You can remove them by pressing the buttons below.", HelpBoxMessageType.Error);
                title.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
                title.style.overflow = new StyleEnum<Overflow>(Overflow.Visible);
                m_ResultsPane.Add(title);

                //.
                for (var i = 0; i < inspection.ComponentOverrides.Length; i++)
                {
                    var @override = inspection.ComponentOverrides[i];
                    if (@override.DidCorrectlyMap)
                        continue;

                    var button = new Button();
                    void RemoveOverride()
                    {
                        if (inspection.TryFindExistingOverrideIndex(@override.FullTypeName, @override.EntityIndex, out var foundIndex))
                            inspection.RemoveComponentOverrideByIndex(foundIndex);
                        else UnityEngine.Debug.LogError($"Unable to remove ComponentOverride {@override}, as now can no longer find it in the list!");
                        m_ResultsPane.Remove(button);
                        NestedGhostInspectionAuthoring.forceSave = true;
                    }
                    button.clicked += RemoveOverride;
                    button.name = "ComponentOverrideError";
                    button.text = $"{@override.FullTypeName} [Entity {@override.EntityIndex}]\nPrefab Type [{@override.PrefabType}]\nSend Optimization [{GetNameForGhostSendType(@override.SendTypeOptimization)}]\nVariant [{@override.VariantHash}]\n<i>Click to remove.</i>";
                    button.style.backgroundColor = NestedGhostAuthoringEditor.brokenColorUIToolkit;
                    button.style.color = NestedGhostAuthoringEditor.brokenColorUIToolkitText;
                    button.style.flexGrow = 1;
                    m_ResultsPane.Add(button);
                }
            }
        }

        static void SetVisualElementVisibility(VisualElement visualElement, bool visibleCondition)
        {
            visualElement.style.display = new StyleEnum<DisplayStyle>(visibleCondition ? DisplayStyle.Flex : DisplayStyle.None);
        }

        VisualElement CreateReplicationHeaderElement(VisualElement parentContent, List<NestedBakedComponentItem> NestedBakedComponents, string headerName, string title, string tooltip, Color iconTintColor, bool isReplicated, string toggleKey)
        {
            var header = new FoldoutHeaderElement(headerName, title, $"{NestedBakedComponents.Count}", tooltip, true);
            header.AddToClassList("ghost-inspection-replication-header");
            //header.label.AddToClassList("ghost-inspection-replication-header");
            header.icon.AddToClassList("ghost-inspection-entity-header__icon");
            header.icon.style.unityBackgroundImageTintColor = iconTintColor;
            header.icon.style.backgroundImage = ComponentIcon;
            parentContent.Add(header);

            var componentListView = new VisualElement();
            componentListView.AddToClassList("ghost-inspection-entity-content");
            header.foldout.contentContainer.Add(componentListView);

            if (NestedBakedComponents.Count > 0)
            {
                for (var i = 0; i < NestedBakedComponents.Count; i++)
                {
                    var metaData = NestedBakedComponents[i];
                    var metaDataRootElement = CreateMetaDataInspector(metaData);
                    componentListView.Add(metaDataRootElement);
                }
            }
            else
            {
                header.SetEnabled(false);
            }


            toggleKey += (isReplicated ? ".RepToggle" : ".NonRepToggle");
            header.foldout.RegisterCallback<ClickEvent>(OnFoldoutToggled);
            void OnFoldoutToggled(ClickEvent evt)
            {
                SetShouldExpand(toggleKey, header.foldout.value);
            }
            var shouldExpandFoldout = GetShouldExpand(toggleKey, NestedBakedComponents.Count > 0 && isReplicated);
            header.foldout.SetValueWithoutNotify(shouldExpandFoldout);

            return componentListView;
        }

        VisualElement CreateMetaDataInspector(NestedBakedComponentItem NestedBakedComponent)
        {
            static OverrideTracking CreateOverrideTracking(NestedBakedComponentItem NestedBakedComponentItem, VisualElement insertIntoOverrideTracking)
            {
                return new OverrideTracking("MetaDataInspector", insertIntoOverrideTracking, NestedBakedComponentItem.HasPrefabOverride(),
                    "Reset Entire Component", NestedBakedComponentItem.RemoveEntirePrefabOverride, true);
            }

            if (NestedBakedComponent.anyVariantIsSerialized || NestedBakedComponent.HasMultipleVariantsExcludingDontSerializeVariant)
            {
                var componentMetaDataFoldout = new Foldout();
                componentMetaDataFoldout.name = "ComponentMetaDataFoldout";
                componentMetaDataFoldout.text = NestedBakedComponent.managedType.Name;
                componentMetaDataFoldout.style.alignContent = new StyleEnum<Align>(Align.Center);
                componentMetaDataFoldout.style.marginBottom = 3;
                componentMetaDataFoldout.style.flexShrink = 1;
                componentMetaDataFoldout.SetValueWithoutNotify(true);
                componentMetaDataFoldout.focusable = false;

                var toggle = componentMetaDataFoldout.Q<Toggle>();
                toggle.style.flexShrink = 1;
                toggle.style.marginLeft = 0; // Don't -12px.
                var foldoutLabel = toggle.Q<Label>(className: UssClasses.UIToolkit.Toggle.Text); // TODO - DropdownField should expose!
                LabelStyle(foldoutLabel);
                var checkmark = toggle.Q<VisualElement>(className: UssClasses.UIToolkit.Toggle.Checkmark);
                checkmark.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);

                var toggleChild = toggle.Q<VisualElement>(className: UssClasses.UIToolkit.BaseField.Input); // TODO - DropdownField should expose!;
                InsertGhostModeToggles(NestedBakedComponent, toggleChild);

                var sendToOwnerDropdown = CreateSentToOwnerDropdown(NestedBakedComponent);
                componentMetaDataFoldout.Add(sendToOwnerDropdown);

                var sendOptimizationDropdown = CreateSentOptimizationDropdown(NestedBakedComponent);
                componentMetaDataFoldout.Add(sendOptimizationDropdown);

                var variantDropdown = CreateVariantDropdown(NestedBakedComponent);
                variantDropdown.SetEnabled(NestedBakedComponent.DoesAllowVariantModification);
                componentMetaDataFoldout.Add(variantDropdown);

                if (NestedBakedComponent.serializationStrategy.IsInput != 0)
                {
                    var inputComponent = new HelpBox("Sending inputs is handled automatically. These settings denote how a clients inputs are replicated to other clients (e.g. to improve prediction of other players).", HelpBoxMessageType.Info);
                    componentMetaDataFoldout.Add(inputComponent);
                }

                var parent = foldoutLabel.parent;
                var parentIndex = foldoutLabel.parent.IndexOf(foldoutLabel);
                var overrideTracking = CreateOverrideTracking(NestedBakedComponent, foldoutLabel);
                parent.Insert(parentIndex, overrideTracking);
                return componentMetaDataFoldout;
            }

            var componentMetaDataLabel = new Label();
            InsertGhostModeToggles(NestedBakedComponent, componentMetaDataLabel);
            componentMetaDataLabel.name = "ComponentMetaDataLabel";
            componentMetaDataLabel.text = NestedBakedComponent.managedType.Name;
            componentMetaDataLabel.style.alignSelf = new StyleEnum<Align>(Align.Stretch);
            componentMetaDataLabel.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleLeft);
            // TODO - The text here doesn't clip properly because the buttons are CHILDREN of the label. I.e. The buttons are INSIDE the labels rect.
            LabelStyle(componentMetaDataLabel);

            return CreateOverrideTracking(NestedBakedComponent, componentMetaDataLabel);
        }

        static bool GetShouldExpand(string key, bool defaultValue) => EditorPrefs.GetBool(k_ExpandKey + key, defaultValue);

        static void SetShouldExpand(string key, bool value) => EditorPrefs.SetBool(k_ExpandKey + key, value);

        static void LabelStyle(Label label)
        {
            label.style.flexShrink = 1;
            label.style.minWidth = 1;
            label.style.overflow = new StyleEnum<Overflow>(Overflow.Hidden);
        }

        static VisualElement CreateVariantDropdown(NestedBakedComponentItem NestedBakedComponent)
        {
            var dropdown = new DropdownField
            {
                name = "VariantDropdownField",
                label = "Variant",
                tooltip = @"Variants change how a components fields are serialized (i.e. replicated).
Use this dropdown to select which variant is used on this component (on this specific ghost entity, and thus; ghost type).

Note that:

 - <b>Components added to the root entity</b> will default to the ""Default Serializer"" (the serializer generated by the SourceGenerators), unless you have modified the default (via a `DefaultVariantSystemBase` derived system).

 - <b>Components added to child (and additional) entities</b> will default to the `DontSerializeVariant` global variant, as serializing children involves entity memory random-access, which is expensive.",
            };

            if (!NestedBakedComponent.DoesAllowVariantModification)
                dropdown.tooltip += "\n\n<color=grey>This dropdown is currently disabled as either a) this type has a [DontSupportPrefabOverrides] attribute or b) there are no other variants.</color>";

            DropdownStyle(dropdown);

            for (var i = 0; i < NestedBakedComponent.availableSerializationStrategies.Length; i++)
            {
                dropdown.choices.Add(NestedBakedComponent.availableSerializationStrategyDisplayNames[i]);
            }

            // Set current value:
            {
                var index = Array.FindIndex(NestedBakedComponent.availableSerializationStrategies, x => x.Hash == NestedBakedComponent.serializationStrategy.Hash);
                if (index >= 0)
                {
                    var selectedVariantName = NestedBakedComponent.availableSerializationStrategyDisplayNames[index];
                    dropdown.SetValueWithoutNotify(selectedVariantName);
                }
                else
                {
                    dropdown.SetValueWithoutNotify($"!! Unknown Variant Hash {NestedBakedComponent.VariantHash} !! (Fallback: {NestedBakedComponent.serializationStrategy.DisplayName.ToString()})");
                    dropdown.style.backgroundColor = NestedGhostAuthoringEditor.brokenColorUIToolkit;
                }
            }

            // Handle value changed.
            dropdown.RegisterValueChangedCallback(evt =>
            {
                var indexOf = Array.IndexOf(NestedBakedComponent.availableSerializationStrategyDisplayNames, evt.newValue);
                if (indexOf >= 0)
                {
                    NestedBakedComponent.serializationStrategy = NestedBakedComponent.availableSerializationStrategies[indexOf];
                    NestedBakedComponent.SaveVariant(false, false);
                    dropdown.style.color = new StyleColor(StyleKeyword.Null);
                }
                else
                {
                    Debug.LogError($"Unable to find variant `{evt.newValue}` to select it! Keeping existing! Try modifying this prefabs YAML.");
                }
            });

            var isOverridenFromDefault = NestedBakedComponent.HasPrefabOverride() && NestedBakedComponent.GetPrefabOverride().IsVariantOverriden;
            var overrideTracking = new OverrideTracking("VariantDropdown", dropdown, isOverridenFromDefault, "Reset Variant", x => NestedBakedComponent.ResetVariantToDefault(), true);
            return overrideTracking;
        }

        /// <summary>Visualizes prefab overrides for custom controls attached to this.</summary>
        class OverrideTracking : VisualElement
        {
            /// <summary>The UI element wrapping the <see cref="ChildRenderingElement"/>, allowing flex-direction:Horizontal.</summary>
            public VisualElement ChildContainer;
            /// <summary>The custom, unknown UI element that we're wrapping this override tracking around.</summary>
            public VisualElement ChildRenderingElement;
            /// <summary>The override widget itself.</summary>
            public VisualElement Override;

            public OverrideTracking(string prefabType, VisualElement mainField, bool defaultOverride, string rightClickResetTitle, Action<DropdownMenuAction> rightClickResetAction, bool shrink)
            {
                name = $"{prefabType}OverrideTracking";
                style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Column); // Ensure the override is BELOW the ChildRenderingElement widget.
                style.alignSelf = new StyleEnum<Align>(Align.Stretch);
                style.alignItems = new StyleEnum<Align>(Align.Center);
                style.flexGrow = 0;
                style.flexShrink = shrink ? 1 : 0;
                style.marginLeft = 5;

                mainField.style.flexGrow = 1;
                mainField.style.flexShrink = 1;
                mainField.style.overflow = new StyleEnum<Overflow>(Overflow.Hidden);
                Add(mainField);

                Override = new VisualElement
                {
                    name = nameof(Override),
                };
                Override.style.height = Override.style.maxHeight = 2;
                Override.style.minWidth = 35;
                Override.style.paddingLeft = Override.style.paddingRight = 2;
                Override.style.paddingTop = Override.style.paddingBottom = 1;

                Override.style.flexGrow = 1;
                Override.style.flexShrink = 1;
                Override.style.alignSelf = new StyleEnum<Align>(Align.Stretch);
                Override.style.backgroundColor = Color.white;
                Add(Override);

                if (defaultOverride)
                {
                    this.AddManipulator(new ContextualMenuManipulator(evt =>
                    {
                        evt.menu.AppendAction(rightClickResetTitle, rightClickResetAction);
                    }));
                }
                SetOverride(defaultOverride);
            }

            void SetOverride(bool isDefaultOverride)
            {
                Override.style.display = new StyleEnum<DisplayStyle>(isDefaultOverride ? DisplayStyle.Flex : DisplayStyle.None);
                Override.MarkDirtyRepaint();
            }
        }

        static VisualElement CreateSentOptimizationDropdown(NestedBakedComponentItem NestedBakedComponent)
        {
            var doesAllowSendTypeOptimizationModification = NestedBakedComponent.DoesAllowSendTypeOptimizationModification;

            var dropdown = new DropdownField();
            dropdown.name = "SendToDropdownField";
            dropdown.label = "Send Optimization";

            dropdown.SetEnabled(doesAllowSendTypeOptimizationModification);

            DropdownStyle(dropdown);

            if (doesAllowSendTypeOptimizationModification)
            {
                dropdown.choices.Add(GetNameForGhostSendType(GhostSendType.DontSend));
                dropdown.choices.Add(GetNameForGhostSendType(GhostSendType.OnlyPredictedClients));
                dropdown.choices.Add(GetNameForGhostSendType(GhostSendType.OnlyInterpolatedClients));
                dropdown.choices.Add(GetNameForGhostSendType(GhostSendType.AllClients));
                dropdown.RegisterValueChangedCallback(OnSendToChanged);
            }

            UpdateUi(GetNameForGhostSendType(NestedBakedComponent.SendTypeOptimization));

            // Handle value changed.
            void OnSendToChanged(ChangeEvent<string> evt)
            {
                var flag = GetFlagForGhostSendTypeOptimization(evt.newValue);
                NestedBakedComponent.SetSendTypeOptimization(flag);
                UpdateUi(evt.newValue);
            }

            void UpdateUi(string buttonValue)
            {
                dropdown.tooltip = $"Optimization that allows you to specify whether or not the server should send (i.e. replicate) the `{NestedBakedComponent.fullname}` component to client ghosts, " +
                    "depending on whether or not a given client is Predicting or Interpolating this ghost." +
                    "\n\nExample: Only send the `PhysicsVelocity` component for \"known always predicted\" ghosts, as interpolated ghosts don't ever need to read the `PhysicsVelocity` Component." +
                    "\n\nNote: This optimization is only possible when we can infer the GhostMode at compile time: I.e. When the NestedGhostAuthoring has `OwnerPredicted` selected, or when `SupportedGhostModes` is set to either `Interpolated` or `Predicted` (but not both).";

                dropdown.tooltip += $"\n\n<color=yellow>The current setting means that {GetTooltipForGhostSendType(NestedBakedComponent.SendTypeOptimization)}</color>";
                if (!doesAllowSendTypeOptimizationModification)
                    dropdown.tooltip += "\n\n<color=grey>This dropdown is currently disabled as either a) this type has a [DontSupportPrefabOverrides] attribute or b) we cannot infer GhostMode.</color>";
                dropdown.tooltip += "\n\nOther send rules may still apply. See documentation for further details.";

                dropdown.value = doesAllowSendTypeOptimizationModification || NestedBakedComponent.serializationStrategy.IsSerialized != 0 ? buttonValue : "n/a";
                dropdown.MarkDirtyRepaint();
            }

            var isOverridenFromDefault = NestedBakedComponent.HasPrefabOverride() && NestedBakedComponent.GetPrefabOverride().IsSendTypeOptimizationOverriden;
            var overrideTracking = new OverrideTracking("SendToDropdown", dropdown, isOverridenFromDefault, "Reset SendType Override", NestedBakedComponent.ResetSendTypeToDefault, true);
            return overrideTracking;
        }

        static VisualElement CreateSentToOwnerDropdown(NestedBakedComponentItem NestedBakedComponent)
        {
            var dropdown = new DropdownField();
            dropdown.name = "SendToDropdownField";
            dropdown.label = "Send To Owner";
            dropdown.tooltip = "<b>Only modifiable via attribute.</b>\n\nDenotes which clients will receive snapshot updates containing this component.\n\nOther send rules may still apply. See documentation for further details.";
            dropdown.SetEnabled(false);
            dropdown.SetValueWithoutNotify(NestedBakedComponent.sendToOwnerType.ToString());
            DropdownStyle(dropdown);

            const bool isOverridenFromDefault = false;
            var overrideTracking = new OverrideTracking("SendToDropdown", dropdown, isOverridenFromDefault, "Reset SendType Override", NestedBakedComponent.ResetSendTypeToDefault, true);
            return overrideTracking;
        }

        static void DropdownStyle(DropdownField dropdownRoot)
        {
            // Root:
            dropdownRoot.style.alignSelf = new StyleEnum<Align>(Align.Stretch);
            dropdownRoot.style.flexGrow = 0;
            dropdownRoot.style.flexShrink = 1;

            // Label:
            var label = dropdownRoot.Q<Label>();
            label.style.alignSelf = new StyleEnum<Align>(Align.Stretch);
            label.style.flexGrow = 0;
            label.style.flexShrink = 1;
            label.style.minWidth = 75;
            label.style.width = 110;

            // Dropdown widget:
            var dropdownWidget = dropdownRoot.Q<VisualElement>(className: UssClasses.UIToolkit.BaseField.Input); // TODO - DropdownField should expose!
            dropdownWidget.style.flexGrow = 1;
            dropdownWidget.style.flexShrink = 1;
            dropdownWidget.style.minWidth = 75;
            dropdownWidget.style.width = 100;
        }

        static string GetTooltipForGhostSendType(GhostSendType ghostSendType)
        {
            switch (ghostSendType)
            {
                case GhostSendType.DontSend: return "this component will <b>not</b> be replicated ever, regardless of what `GhostPrefabType` each ghost is in.";
                case GhostSendType.OnlyInterpolatedClients: return "this component will <b>only</b> be replicated for <b>Interpolated Ghosts</b>.";
                case GhostSendType.OnlyPredictedClients: return "this component will <b>only</b> be replicated for <b>Predicted Ghosts</b>.";
                case GhostSendType.AllClients: return "this component <b>will</b> be replicated for both `Predicted` and `Interpolated` Ghosts.";
                default:
                    throw new ArgumentOutOfRangeException(nameof(ghostSendType), ghostSendType, null);
            }
        }

        static string GetNameForGhostSendType(GhostSendType ghostSendType)
        {
            if ((int)ghostSendType == -1)
                return "not set";
            switch (ghostSendType)
            {
                case GhostSendType.DontSend: return "Never Send";
                case GhostSendType.AllClients: return "Always Send";
                case GhostSendType.OnlyInterpolatedClients: return "Only Send when \"Known Interpolated\"";
                case GhostSendType.OnlyPredictedClients: return "Only Send when \"Known Predicted\"";
                default:
                    throw new ArgumentOutOfRangeException(nameof(ghostSendType), ghostSendType, null);
            }
        }
        static GhostSendType GetFlagForGhostSendTypeOptimization(string ghostSendType)
        {
            for (var type = GhostSendType.DontSend; type <= GhostSendType.AllClients; type++)
            {
                var testName = GetNameForGhostSendType(type);
                if (string.Equals(testName, ghostSendType, StringComparison.OrdinalIgnoreCase))
                    return type;
            }

            throw new ArgumentOutOfRangeException(nameof(ghostSendType), ghostSendType, nameof(GetFlagForGhostSendTypeOptimization));
        }

        void InsertGhostModeToggles(NestedBakedComponentItem NestedBakedComponent, VisualElement parent)
        {
            parent.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            parent.style.flexShrink = 1;

            var separator = new VisualElement();
            separator.name = nameof(separator);
            separator.style.flexGrow = 1;
            separator.style.flexShrink = 1;
            parent.Add(separator);

            var buttonContainer = new VisualElement();
            buttonContainer.name = "GhostPrefabTypeButtons";
            buttonContainer.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            buttonContainer.SetEnabled(NestedBakedComponent.DoesAllowPrefabTypeModification);

            buttonContainer.Add(CreateButton("S", GhostPrefabType.Server, "Server"));
            buttonContainer.Add(CreateButton("IC", GhostPrefabType.InterpolatedClient, "Interpolated Client"));
            buttonContainer.Add(CreateButton("PC", GhostPrefabType.PredictedClient, "Predicted Client"));

            var isOverridenFromDefault = NestedBakedComponent.HasPrefabOverride() && NestedBakedComponent.GetPrefabOverride().IsPrefabTypeOverriden;
            var overrideTracking = new OverrideTracking("PrefabType", buttonContainer, isOverridenFromDefault, $"Reset PrefabType Override", NestedBakedComponent.ResetPrefabTypeToDefault, false);

            parent.Add(overrideTracking);

            VisualElement CreateButton(string abbreviation, GhostPrefabType type, string prefabType)
            {
                var button = new Button();
                //button.Q<Label>().style.alignContent = new StyleEnum<Align>(Align.Center);

                button.text = abbreviation;
                button.style.width = 36;
                button.style.height = 22;
                button.style.marginLeft = 1;
                button.style.marginRight = 1;
                button.style.paddingLeft = 1;
                button.style.paddingRight = 1;

                button.style.alignContent = new StyleEnum<Align>(Align.Center);
                button.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);

                UpdateUi();

                button.clicked += ButtonToggled;

                void ButtonToggled()
                {
                    NestedBakedComponent.TogglePrefabType(type);
                    UpdateUi();
                }
                void UpdateUi()
                {
                    var defaultValue = (NestedBakedComponent.defaultSerializationStrategy.PrefabType & type) != 0;
                    var isSet = (NestedBakedComponent.PrefabType & type) != 0;
                    button.style.backgroundColor = isSet ? new Color(0.17f, 0.17f, 0.17f) : new Color(0.48f, 0.15f, 0.15f);

                    button.tooltip = $"NetCode creates multiple versions of the '{NestedBakedComponent.EntityParent.EntityName}' ghost prefab (one for each mode [Server, Interpolated Client, PredictedClient])." +
                        $"\n\nThis toggle determines if the `{NestedBakedComponent.fullname}` component should be added to the `{prefabType}` version of this ghost." +
                        $" Current value indicates {(isSet ? "<color=green>YES</color>" : "<color=red>NO</color>")} and thus <color=yellow>PrefabType is `{NestedBakedComponent.PrefabType}`</color>." +
                        $"\n\nDefault value is: {(defaultValue ? "YES" : "NO")}\n\nTo disable write-access to this toggle, add a `DontSupportPrefabOverrides` attribute to your component type." +
                        "\n\nRecommendation: It's better practice to create a custom Variant that sets the desired `PrefabType`. This way, said `PrefabType` will be applied automatically to all ghost prefabs.";

                    if (!NestedBakedComponent.DoesAllowPrefabTypeModification)
                        button.tooltip += "\n\n<color=grey>This dropdown is currently disabled as this type has a [DontSupportPrefabOverrides] attribute.</color>";

                    button.MarkDirtyRepaint();
                }

                return button;
            }
        }

        class FoldoutHeaderElement : VisualElement
        {
            public readonly Foldout foldout;
            //public readonly Label label;
            public readonly Image icon;
            public readonly VisualElement rowHeader;

            public FoldoutHeaderElement(string headerName, string labelText, string lengthText, string subElementsTooltip, bool displayCheckmark)
            {
                name = $"{headerName}FoldoutHeader";

                foldout = new Foldout();
                foldout.name = $"{headerName}Foldout";
                foldout.text = labelText;
                foldout.contentContainer.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Column);
                Add(foldout);

                var toggle = foldout.Q<Toggle>();
                toggle.tooltip = subElementsTooltip;
                foldout.focusable = false;

                var checkmark = toggle.Q<VisualElement>(className: UssClasses.UIToolkit.Toggle.Checkmark);
                checkmark.style.display = new StyleEnum<DisplayStyle>(displayCheckmark ? DisplayStyle.Flex : DisplayStyle.None);

                icon = new Image();
                icon.name = $"{headerName}Icon";
                icon.tooltip = subElementsTooltip;
                icon.AddToClassList("entity-info__icon");

                rowHeader = toggle[0];
                rowHeader.style.alignItems = new StyleEnum<Align>(Align.Center);
                rowHeader.style.marginTop = new StyleLength(3);
                rowHeader.style.height = new StyleLength(20);
                rowHeader.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
                rowHeader.Insert(1, icon);

                var lengthLabel = new Label();
                lengthLabel.name = $"{headerName}LengthLabel";
                lengthLabel.style.flexGrow = new StyleFloat(1);
                lengthLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Normal);
                lengthLabel.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleRight);
                lengthLabel.style.justifyContent = new StyleEnum<Justify>(Justify.FlexEnd);
                lengthLabel.style.alignContent = new StyleEnum<Align>(Align.FlexEnd);
                lengthLabel.text = lengthText;
                rowHeader.Add(lengthLabel);
            }
        }
    }

