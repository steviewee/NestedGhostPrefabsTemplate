using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEditor;
using Unity.NetCode.Hybrid;
using Zhorman.NestedGhostPrefabs.Runtime.Components;
using Unity.Entities.Hybrid.Baking;


#if UNITY_EDITOR
using Unity.Entities.Build;
#endif
namespace Zhorman.NestedGhostPrefabs.Runtime.Authoring
{
    [RequireComponent(typeof(LinkedEntityGroupAuthoring))]
    [DisallowMultipleComponent]
    public class NestedGhostAuthoring : MonoBehaviour
    {
        void OnEnable()
        {
            // included so tick box appears in Editor
        }
#if UNITY_EDITOR
        void OnValidate()
        {
            ////Debug.Log("onvalidate");
            if (gameObject.scene.IsValid())
                return;
            ////Debug.Log("onvalidate1");
            var path = UnityEditor.AssetDatabase.GetAssetPath(gameObject);
            if (string.IsNullOrEmpty(path))
                return;
            ////Debug.Log("onvalidate2");
            var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            if (!string.Equals(guid, PrefabId, StringComparison.OrdinalIgnoreCase))
            {
                ////Debug.Log("onvalidate3");
                PrefabId = guid;
                var path2 = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
                if (string.IsNullOrEmpty(path2))
                    return;
                ////Debug.Log("onvalidate4");
                UnnestedPrefabId = UnityEditor.AssetDatabase.AssetPathToGUID(path2);
            }
        }
#endif
        /// <summary>
        /// Force the ghost baker to treat this GameObject as if it was a prefab. This is used if you want to programmatically create
        /// a ghost prefab as a GameObject and convert it to an Entity prefab with ConvertGameObjectHierarchy.
        /// </summary>
        [NonSerialized] public bool ForcePrefabConversion;

        /// <summary>
        /// The ghost mode used if you do not manually change it using a GhostSpawnClassificationSystem.
        /// If set to OwnerPredicted the ghost will be predicted on the client which owns it and interpolated elsewhere.
        /// You must not change the mode using a classification system if using owner predicted.
        /// </summary>
        [Tooltip("The `GhostMode` used when first spawned (assuming you do not manually change it, using a GhostSpawnClassificationSystem).\n\nIf set to 'Owner Predicted', the ghost will be 'Predicted' on the client which owns it, and 'Interpolated' on all others. If using 'Owner Predicted', you cannot change the ghost mode via a classification system.")]
        public GhostMode DefaultGhostMode = GhostMode.Interpolated;
        /// <summary>
        /// The ghost modes supported by this ghost. This will perform some more optimizations at authoring time but make it impossible to change ghost mode at runtime.
        /// </summary>
        [Tooltip("Every `GhostMode` supported by this ghost. Setting this to either 'Predicted' or 'Interpolated' will allow NetCode to perform some more optimizations at authoring time. However, it makes it impossible to change ghost mode at runtime.")]
        public GhostModeMask SupportedGhostModes = GhostModeMask.All;
        /// <summary>
        /// This setting is only for optimization, the ghost will be sent when modified regardless of this setting.
        /// Optimizing for static makes snapshots slightly larger when they change, but smaller when they do not change.
        /// </summary>
        [Tooltip("Bandwidth and CPU optimization:\n\n - <b>Static</b> - This ghost will only be added to a snapshot when its ghost values actually change.\n<i>Examples: Barrels, trees, dropped items, asteroids etc.</i>\n\n - <b>Dynamic</b> - This ghost will be replicated at a regular interval, regardless of whether or not its values have changed, allowing for more aggressive compression.\n<i>Examples: Character controllers, missiles, important gameplay items like CTF flags and footballs etc.</i>\n\n<i>Marking a ghost as `Static` makes snapshots slightly larger when replicated values change, but smaller when they do not.</i>")]
        public GhostOptimizationMode OptimizationMode = GhostOptimizationMode.Dynamic;
        /// <summary>
        /// If not all ghosts can fit in a snapshot only the most important ghosts will be sent. Higher importance means the ghost is more likely to be sent.
        /// </summary>
        [Tooltip(@"<b>Importance</b> determines how ghost chunks are prioritized against each other when working out what to send in the upcoming snapshot. Higher values are sent more frequently. Applied at the chunk level.
<i>Simplified example: When comparing a gameplay-critical <b>Player</b> ghost with an <b>Importance</b> of 100 to a cosmetic <b>Cone</b> ghost with an <b>Importance</b> of 1, the <b>Player</b> ghost will likely be sent 100 times for every 1 time the <b>Cone</b> will be.</i>")]
        [Min(1)]
        public int Importance = 1;

        /// <summary>
        ///     The theoretical maximum send frequency (in Hz) for ghost chunks of this ghost prefab type (excluding a few nuanced exceptions).
        ///     Important Note: The MaxSendRate only denotes the maximum possible replication frequency, and cannot be enforced in all cases.
        ///     Other factors (like <see cref="ClientServerTickRate.NetworkTickRate"/>, ghost instance count, <see cref="Importance"/>,
        ///     Importance-Scaling, <see cref="GhostSendSystemData.DefaultSnapshotPacketSize"/>, and structural changes etc.)
        ///     will determine the final/live send rate.
        /// </summary>
        /// <remarks>
        /// Use this to brute-force reduce the bandwidth consumption of your most impactful ghost types.
        /// Note: Predicted ghosts are particularly impacted by this, as a lower value here reduces rollback and re-simulation frequency
        /// (as we only rollback and re-simulate a predicted ghost after it is received), which can save client CPU cycles in aggregate.
        /// However, it may cause larger client misprediction errors, which leads to larger corrections.
        /// </remarks>
        [Tooltip(@"The <b>theoretical</b> maximum send frequency (in <b>Hertz</b>) for ghost chunks of this ghost prefab type.

<b>Important Note:</b> The <b>MaxSendRate</b> only denotes the maximum possible replication frequency. Other factors (like <b>NetworkTickRate</b>, ghost instance count, <b>Importance</b>, <b>Importance-Scaling</b>, <b>DefaultSnapshotPacketSize</b> etc.) will determine the live send rate.

<i>Use this to brute-force reduce the bandwidth consumption of your most impactful ghost types.</i>")]
        public byte MaxSendRate;

        /// <summary>
        /// For internal use only, the prefab GUID used to distinguish between different variant of the same prefab.
        /// </summary>
        [Tooltip("The prefab GUID used to distinguish between different variant of the same prefab")]
        [SerializeField] public string PrefabId = "";
        [Tooltip("The entitie's original prefab GUID")]
        [SerializeField] public string UnnestedPrefabId = "";
        /// <summary>
        /// Add a GhostOwner tracking which connection owns this component.
        /// You must set the GhostOwner to a valid NetworkId.Value at runtime.
        /// </summary>
        [Tooltip("Automatically adds a `GhostOwner`, which allows the server to set (and track) which connection owns this ghost. In your server code, you must set the `GhostOwner` to a valid `NetworkId.Value` at runtime.")]
        public bool HasOwner;
        /// <summary>
        /// Automatically adds the <see cref="AutoCommandTarget"/> component to your ghost prefab,
        /// which enables the "Auto Command Target" feature, which automatically sends all `ICommandData` and `IInputComponentData`
        /// buffers to the server (assuming the ghost is owned by the current connection, and `AutoCommandTarget.Enabled` is true).
        /// </summary>
        [Tooltip("Enables the \"Auto Command Target\" feature, which automatically sends all `ICommandData` and `IInputComponentData` auto-generated buffers to the server if the following conditions are met: \n\n - The ghost is owned by the current connection (handled by user-code).\n\n - The `AutoCommandTarget` component is added to the ghost entity (enabled by this checkbox), and it's `[GhostField] public bool Enabled;` field is true (the default value).\n\nSupports both predicted and interpolated ghosts.")]
        public bool SupportAutoCommandTarget = true;
        /// <summary>
        /// Add a CommandDataInterpolationDelay component so the interpolation delay of each client is tracked.
        /// This is used for server side lag-compensation.
        /// </summary>
        [Tooltip("Add a `CommandDataInterpolationDelay` component so the interpolation delay of each client is tracked.\n\nThis is used for server side lag-compensation (it allows the server to more accurately estimate how far behind your interpolated ghosts are, leading to better hit registration, for example).\n\nThis should be enabled if you expect to use input commands (from this 'Owner Predicted' ghost) to interact with other, 'Interpolated' ghosts (example: shooting or hugging another 'Player').")]
        public bool TrackInterpolationDelay;
        /// <summary>
        /// Add a GhostGroup component which makes it possible for this entity to be the root of a ghost group.
        /// </summary>
        [Tooltip("Add a `GhostGroup` component, which makes it possible for this entity to be the root of a 'Ghost Group'.\n\nA 'Ghost Group' is a collection of ghosts who must always be replicated in the same snapshot, which is useful (for example) when trying to keep an item like a weapon in sync with the player carrying it.\n\nTo use this feature, you must add the target ghost entity to this `GhostGroup` buffer at runtime (e.g. when the weapon is first picked up by the player).\n\n<i>Note that GhostGroups slow down serialization, as they force entity chunk random-access. Therefore, prefer other solutions.</i>")]
        public bool GhostGroup;
        [Tooltip("Bakes GhostBuffer and populates it with children, or, on children, makes them bake the \"GhostChildEntity\" tag")]
        public bool ImmidiateGhostGrouping;
        /// <summary>
        /// Force this ghost to be quantized and copied to the snapshot format once for all connections instead
        /// of once per connection. This can save CPU time in the ghost send system if the ghost is
        /// almost always sent to at least one connection, and it contains many serialized components, serialized
        /// components on child entities or serialized buffers. A common case where this can be useful is the ghost
        /// for the character / player.
        /// </summary>
        [Tooltip("CPU optimization that forces this ghost to be quantized and copied to the snapshot format <b>once for all connections</b> (instead of once <b>per connection</b>). This can save CPU time in the `GhostSendSystem` assuming all of the following:\n\n - The ghost contains many serialized components, serialized components on child entities, or serialized buffers.\n\n - The ghost is almost always sent to at least one connection.\n\n<i>Example use-cases: Players, important gameplay items like footballs and crowns, global entities like map settings and dynamic weather conditions.</i>")]
        public bool UsePreSerialization;
        /// <summary>
        /// <para>
        /// Only for client, force <i>predicted spawn ghost</i> of this type to rollback and re-predict their state from the tick client spawned them until
        /// the authoritative server spawn has been received and classified. In order to save some CPU, the ghost state is rollback only in case a
        /// new snapshot has been received, and it contains new predicted ghost data for this or other ghosts.
        /// </para>
        /// <para>
        /// By default this options is set to false, meaning that predicted spawned ghost by the client never rollback their original state and re-predict
        /// until the authoritative data is received. This behaviour is usually fine in many situation and it is cheaper in term of CPU.
        /// </para>
        /// </summary>
        [Tooltip("Only for client, force <i>predicted spawn ghost</i> of this type to rollback and re-predict their state from their spawn tick until the authoritative server spawn has been received and classified. In order to save some CPU, the ghost state is rollback only in case a new snapshot has been received, and it contains new predicted ghost data for this or other ghosts.\nBy default this options is set to false, meaning that predicted spawned ghost by the client never rollback their original state and re-predict until the authoritative data is received. This behaviour is usually fine in many situation and it is cheaper in term of CPU.")]
        public bool RollbackPredictedSpawnedGhostState;
        /// <summary>
        /// <para>
        /// Client CPU optimization, force <i>predicted ghost</i> of this type to replay and re-predict their state from the last received snapshot tick in case of a structural change
        /// or in general when an entry for the entity cannot be found in the prediction backup (see <see cref="GhostPredictionHistorySystem"/>).
        /// </para>
        /// <para>
        /// By default this options is set to true, to preserve the original 1.0 behavior. Once the optimization is turned on, removing or adding replicated components from the predicted ghost on the client may cause issue on the restored value. Please check the documentation, in particular the Prediction edge case and known issue.
        /// </para>
        /// </summary>
        [Tooltip("Client CPU optimization, force <i>predicted ghost</i> of this type to replay and re-predict their state from the last received snapshot tick in case of a structural change or in general when an entry for the entity cannot be found in the prediction backup.\nBy default this options is set to false, to preserve the original 1.0 behavior. Once the optimization is turned on, removing or adding replicated components from the predicted ghost on the client may cause some issue in regard the restored value when the component is re-added. Please check the documentation for more details, in particular the <i>Prediction edge case and known issue</i> section.")]
        public bool RollbackPredictionOnStructuralChanges = true;

        [Tooltip("When enabled on <b>child</b>, will network the parent-child relationship using the \"DesiredParent\" component")]
        public bool NetworkedParentship = true;
        [Tooltip("When this and \"NetworkedParentship\" is enabled on <b>child</b>, will <b>immitate</b> the child-parent relationship using \"ImmitatedParentReference\"")]
        public bool UseImmitatedParenting = false;
        /// <summary>
        /// Unparents this entity from the Prefab
        /// <para>
        /// By default this options is set to false, to preserve original behavior.
        /// </para>
        /// </summary>
        [Tooltip("Unparents this entity from the Prefab if false")]
        public bool ShouldBeUnParented = false;


        [Tooltip("Use <b>the</b> original prefab if nested. Can be used to save some memory if you have identical, unchanged, child prefabs in different prefab nests")]
        public bool UseOriginal = false;
        [Tooltip("Automatically rereferences all references of this entity in the whole prefab in \"SpawneeSettingSystem\" after being spawned.\n \"SpawneeSettingSystem\" manually loops though all components and buffers in the entity. \n Disable this if you want to code your own Rereferencing system <b>before</b> \"SpawneeSettingSystem\"")]
        public bool Rereferencing = true;
        [Tooltip("Allows this ghost's non ghost children to reference other ghosts. Enabling this costs spawning performance")]
        public bool NonGhostRereferencing = false;
        [Tooltip("Relinks LinkedEntityGroup back after spawning")]
        public bool LinkedEntityRelinking = false;
        /// <summary>
        /// Validate the name of the GameObject prefab.
        /// </summary>
        /// <param name="ghostNameHash">Outputs the hash generated from the name.</param>
        /// <returns>The FS equivalent of the gameObject.name.</returns>
        public FixedString64Bytes GetAndValidateGhostName(out ulong ghostNameHash)
        {

            var ghostName = gameObject.name;
            var ghostNameFs = new FixedString64Bytes();
            var nameCopyError = FixedStringMethods.CopyFromTruncated(ref ghostNameFs, ghostName);
            ghostNameHash = TypeHash.FNV1A64(ghostName);
            if (nameCopyError != CopyError.None)
                Debug.LogError($"{nameCopyError} when saving GhostName \"{ghostName}\" into FixedString64Bytes, became: \"{ghostNameFs}\"!", this);
            return ghostNameFs;
        }
        public FixedString64Bytes GetAndValidateGhostNameNested(GameObject target, out ulong ghostNameHash)
        {
            // Traverse the hierarchy to build the full name with nested parents
            var fullName = target.name;
            var currentParent = target.transform.parent;

            while (currentParent != null)
            {
                fullName = $"{currentParent.name}/{fullName}";
                currentParent = currentParent.parent;
            }

            // Prepare the FixedString64Bytes and handle truncation
            var ghostNameFs = new FixedString64Bytes();
            var nameCopyError = FixedStringMethods.CopyFromTruncated(ref ghostNameFs, fullName);
            ghostNameHash = TypeHash.FNV1A64(fullName);

            // Log an error if the name was truncated
            if (nameCopyError != CopyError.None)
            {
                Debug.LogError($"{nameCopyError} when saving GhostName \"{fullName}\" into FixedString64Bytes, became: \"{ghostNameFs}\"!", this);
            }

            return ghostNameFs;
        }
        public FixedString64Bytes GetAndValidateGhostNameAdvanced(out ulong ghostNameHash)
        {

            var ghostName = gameObject.name + GetPrefabDepth(gameObject);
            var ghostNameFs = new FixedString64Bytes();
            var nameCopyError = FixedStringMethods.CopyFromTruncated(ref ghostNameFs, ghostName);
            ghostNameHash = TypeHash.FNV1A64(ghostName);
            if (nameCopyError != CopyError.None)
                Debug.LogError($"{nameCopyError} when saving GhostName \"{ghostName}\" into FixedString64Bytes, became: \"{ghostNameFs}\"!", this);
            return ghostNameFs;
        }
        public int GetPrefabDepth(GameObject gameObject)
        {
            int depth = 0;
            Transform current = gameObject.transform;

            // Traverse up the hierarchy until there are no more parents
            while (current.parent != null)
            {
                depth++;
                current = current.parent;
            }

            return depth;
        }
        public GhostPrefabCreation.Config AsConfig(FixedString64Bytes ghostName)
        {
            return new GhostPrefabCreation.Config
            {
                Name = ghostName,
                Importance = Importance,
                MaxSendRate = MaxSendRate,
                SupportedGhostModes = SupportedGhostModes,
                DefaultGhostMode = DefaultGhostMode,
                OptimizationMode = OptimizationMode,
                UsePreSerialization = UsePreSerialization,
                PredictedSpawnedGhostRollbackToSpawnTick = RollbackPredictedSpawnedGhostState,
                RollbackPredictionOnStructuralChanges = RollbackPredictionOnStructuralChanges,
            };
        }
        /// <summary>True if we can apply the <see cref="GhostSendType"/> optimization on this Ghost.</summary>
        public bool SupportsSendTypeOptimization => SupportedGhostModes != GhostModeMask.All || DefaultGhostMode == GhostMode.OwnerPredicted;
    }
    //[BakingVersion("megacity-metro", 2)]
    public class NestedGhostBaker : Baker<NestedGhostAuthoring>
    {
        public bool CheckIfNested(GameObject gameObject)
        {
            if (gameObject.transform.parent != null)
            {
                return false;
            }
            NestedGhostAuthoring childAuthoring = gameObject.GetComponentInChildren<NestedGhostAuthoring>();
            return childAuthoring != null;
        }
        public static List<GameObject> CheckIfNestedAndReturnAllChildrenWithAuthoring(GameObject gameObject)
        {

            // Check if the GameObject is a root object
            if (gameObject.transform.parent != null)
            {
                return null; // Return an empty list since it's not a root
            }


            // Get all components of the specified type in the hierarchy
            NestedGhostAuthoring[] components = gameObject.GetComponentsInChildren<NestedGhostAuthoring>(false); // Include inactive objects

            // Create a list to hold the GameObjects with the component
            List<GameObject> gameObjectsWithComponent = new List<GameObject>();

            // Add all GameObjects with the component, excluding the root itself
            foreach (NestedGhostAuthoring component in components)
            {
                if (component.gameObject != gameObject) // Exclude the root GameObject itself
                {
                    gameObjectsWithComponent.Add(component.gameObject);
                }
            }

            return gameObjectsWithComponent;

        }



        public NetcodeConversionTarget GetNetcodeTarget(bool isPrefab)
        {
            // Detect target using build settings (This is used from sub scenes)
#if UNITY_EDITOR
#if USING_PLATFORMS_PACKAGE
            if (self.TryGetBuildConfigurationComponent<NetCodeConversionSettings>(out var settings))
            {
                //Debug.LogWarning("BuildSettings conversion for: " + settings.Target);
                return settings.Target;
            }
#endif

            var settingAsset = this.GetDotsSettings();
            if (settingAsset is INetCodeConversionTarget asset)
            {
                return asset.NetcodeTarget;
            }
#endif

            // Prefabs are always converted as client and server when using convert to entity since they need to have a single blob asset
            if (!isPrefab)
            {
                if (this.IsClient())
                    return NetcodeConversionTarget.Client;
                if (this.IsServer())
                    return NetcodeConversionTarget.Server;
            }

            return NetcodeConversionTarget.ClientAndServer;
        }
        void IterateChildren(DynamicBuffer<GhostGroup> ghostGroup, Transform parent)
        {
            ////Debug.Log($"Iterating over {parent.name}");
            foreach (Transform child in parent)
            {
                if (child.GetComponent<GhostAuthoringComponent>() != null || child.GetComponent<NestedGhostAuthoring>() != null)
                {
                    ////Debug.Log($"Adding to ghost group {child}");
                    ghostGroup.Add(new GhostGroup { Value = GetEntity(child.gameObject, TransformUsageFlags.Dynamic) });
                }
                IterateChildren(ghostGroup, child);
            }
        }
        void IterateChildren(DynamicBuffer<LinkedEntityRelink> ghostGroup, Transform parent)
        {
            ////Debug.Log($"Iterating over {parent.name}");
            foreach (Transform child in parent)
            {
                if (child.GetComponent<GhostAuthoringComponent>() != null || child.GetComponent<NestedGhostAuthoring>() != null)
                {
                    ////Debug.Log($"Adding to ghost group {child}");
                    ghostGroup.Add(new LinkedEntityRelink { Value = GetEntity(child.gameObject, TransformUsageFlags.Dynamic) });
                }
                IterateChildren(ghostGroup, child);
            }
        }

        void IterateAllChildren(DynamicBuffer<LinkedEntityRelink> ghostGroup, Transform parent)
        {
            ////Debug.Log($"Iterating over {parent.name}");
            foreach (Transform child in parent)
            {
                if (child.GetComponent<GhostAuthoringComponent>() != null || child.GetComponent<NestedGhostAuthoring>() != null)
                {
                    ////Debug.Log($"Adding to ghost group {child}");
                    ghostGroup.Add(new LinkedEntityRelink { Value = GetEntity(child.gameObject, TransformUsageFlags.Dynamic) });
                    IterateAllChildrenUnderGhost(ghostGroup, child);
                }
                else
                {
                    IterateAllChildren(ghostGroup, child);
                }
            }
        }
        void IterateAllChildrenUnderGhost(DynamicBuffer<LinkedEntityRelink> ghostGroup, Transform parent)
        {
            foreach (Transform child in parent)
            {
                ghostGroup.Add(new LinkedEntityRelink { Value = GetEntity(child.gameObject, TransformUsageFlags.Dynamic) });
                IterateAllChildrenUnderGhost(ghostGroup, child);
            }
        }
        void IterateChildren(DynamicBuffer<GhostGroupRequest> ghostGroup, Transform parent)
        {
            ////Debug.Log($"Iterating over {parent.name}");
            foreach (Transform child in parent)
            {
                if (child.GetComponent<GhostAuthoringComponent>() != null || child.GetComponent<NestedGhostAuthoring>() != null)
                {
                    ////Debug.Log($"Adding to ghost group {child}");
                    ghostGroup.Add(new GhostGroupRequest { Value = GetEntity(child.gameObject, TransformUsageFlags.Dynamic) });
                }
                IterateChildren(ghostGroup, child);
            }
        }
        void IterateChildren(DynamicBuffer<GhostGroup> ghostGroup, DynamicBuffer<LinkedEntityRelink> relinks, Transform parent)
        {
            ////Debug.Log($"Iterating over {parent.name}");
            foreach (Transform child in parent)
            {
                if (child.GetComponent<GhostAuthoringComponent>() != null || child.GetComponent<NestedGhostAuthoring>() != null)
                {
                    Entity childEntity = GetEntity(child.gameObject, TransformUsageFlags.Dynamic);
                    ghostGroup.Add(new GhostGroup { Value = childEntity });
                    relinks.Add(new LinkedEntityRelink { Value = childEntity });
                }
                IterateChildren(ghostGroup, relinks, child);
            }
        }
        void IterateChildren(DynamicBuffer<GhostGroupRequest> ghostGroup, DynamicBuffer<LinkedEntityRelink> relinks, Transform parent)
        {
            ////Debug.Log($"Iterating over {parent.name}");
            foreach (Transform child in parent)
            {
                if (child.GetComponent<GhostAuthoringComponent>() != null || child.GetComponent<NestedGhostAuthoring>() != null)
                {
                    Entity childEntity = GetEntity(child.gameObject, TransformUsageFlags.Dynamic);
                    ghostGroup.Add(new GhostGroupRequest { Value = childEntity });
                    relinks.Add(new LinkedEntityRelink { Value = childEntity });
                }
                IterateChildren(ghostGroup, relinks, child);
            }
        }
        void IterateChildren(NativeList<Entity> ghostList, Transform parent)
        {
            ////Debug.Log($"Iterating over {parent.name}");
            foreach (Transform child in parent)
            {
                if (child.GetComponent<GhostAuthoringComponent>() != null || child.GetComponent<NestedGhostAuthoring>() != null)
                {
                    ////Debug.Log($"Adding to ghost group {child}");
                    ghostList.Add(GetEntity(child.gameObject, TransformUsageFlags.Dynamic));
                }
                IterateChildren(ghostList, child);
            }
        }
        bool IsThereAGhostGroupAbove(Transform child)
        {
            if (child == null) return false; // Reached the root without finding the component

            if (child.TryGetComponent<GhostAuthoringComponent>(out GhostAuthoringComponent component))
            {
                if (component.GhostGroup)
                {
                    return true;
                }

            }
            else if (child.TryGetComponent<NestedGhostAuthoring>(out NestedGhostAuthoring converterComponent))
            {
                if (converterComponent.GhostGroup)
                {
                    return true;
                }
            }

            return IsThereAGhostGroupAbove(child.parent); // Recurse to the parent
        }
        internal static List<NestedGhostInspectionAuthoring> CollectAllInspectionComponents(NestedGhostAuthoring ghostAuthoring)
        {
            var inspectionComponents = new List<NestedGhostInspectionAuthoring>(8);
            ghostAuthoring.gameObject.GetComponents(inspectionComponents);
            ghostAuthoring.GetComponentsInChildren(inspectionComponents);
            return inspectionComponents;
        }
        public List<(GameObject, GhostAuthoringInspectionComponent.ComponentOverride)> CollectAllComponentOverridesInInspectionComponents(NestedGhostAuthoring ghostAuthoring, bool validate)
        {

            var inspectionComponents = CollectAllInspectionComponents(ghostAuthoring);
            var allComponentOverrides = new List<(GameObject, GhostAuthoringInspectionComponent.ComponentOverride)>(inspectionComponents.Count * 4);
            foreach (var inspectionComponent in inspectionComponents)
            {
                if (validate)
                    inspectionComponent.LogErrorIfComponentOverrideIsInvalid();

                foreach (var componentOverride in inspectionComponent.ComponentOverrides)
                {
                    allComponentOverrides.Add((inspectionComponent.gameObject, componentOverride));
                }
            }

            return allComponentOverrides;
        }
        public override void Bake(NestedGhostAuthoring authoring)
        {
            var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
            bool isPrefab = !authoring.gameObject.scene.IsValid() || authoring.ForcePrefabConversion;
            var target = GetNetcodeTarget(isPrefab);
#if UNITY_EDITOR
            //Debug.Log($"{authoring.gameObject.name} PrefabId = {authoring.PrefabId}; unnestedprefabId = {authoring.UnnestedPrefabId};");
            if (!isPrefab)
            {
                if (authoring.UseOriginal)
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(authoring.UnnestedPrefabId);
                    GameObject prefab = null;
                    if (!String.IsNullOrEmpty(path))
                    {
                        prefab = (GameObject)UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(GameObject));
                        Entity prefabEntity = GetEntity(prefab, TransformUsageFlags.Dynamic);
                    }
                    else
                    {
                        Debug.LogError($"Couldn't find {authoring.gameObject.name}'s prefab. PrefabId = {authoring.PrefabId}; UnnestedPrefabId = {authoring.UnnestedPrefabId};");
                    }
                }
                else
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(authoring.PrefabId);
                    GameObject prefab = null;
                    if (!String.IsNullOrEmpty(path))
                    {
                        prefab = (GameObject)UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(GameObject));
                        Entity prefabEntity = GetEntity(prefab, TransformUsageFlags.Dynamic);
                    }
                    else
                    {
                        Debug.LogError($"Couldn't find {authoring.gameObject.name}'s prefab. PrefabId = {authoring.PrefabId}; UnnestedPrefabId = {authoring.UnnestedPrefabId};");
                    }
                }


            }
#endif

            GhostType ghostType;
            var ghostName = authoring.GetAndValidateGhostNameNested(authoring.gameObject, out var ghostNameHash);
            if (authoring.transform.parent != null)
            {

                AddComponent(entity, new GhostRootLink
                {
                    Value = GetEntity(authoring.transform.root.gameObject, TransformUsageFlags.Dynamic),
                });

                if (authoring.UseOriginal)
                {
                    ghostType = GhostType.FromHash128String(authoring.UnnestedPrefabId);
                }
                else
                {
                    var uuid5 = new GhostPrefabCreation.SHA1($"f17641b8-279a-94b1-1b84-487e72d49ab5{ghostName}");
                    // I need an unique identifier and should not clash with any loaded prefab, use uuid5 with a namespace + ghost name
                    ghostType = GhostType.FromHash128(GhostPrefabCreation.ConvertHash128ToUUID5(uuid5.ToHash128()));
                }

            }
            else
            {
                ghostType = GhostType.FromHash128String(authoring.PrefabId);


                AddComponent(entity, new GhostRoot
                {

                });
            }

            if (authoring.HasOwner == true)
            {
                if (authoring.SupportAutoCommandTarget)
                    AddComponent(entity, new AutoCommandTarget { Enabled = true });
                if (authoring.TrackInterpolationDelay)
                    AddComponent(entity, default(CommandDataInterpolationDelay));
                AddComponent(entity, new GhostOwnerIsLocal
                {

                });
                AddComponent(entity, new GhostOwner
                {

                });
            }
            
            if (authoring.GhostGroup)
            {
                if (authoring.ImmidiateGhostGrouping)
                {
                    if (authoring.transform.parent != null)
                    {
                        if (IsThereAGhostGroupAbove(authoring.transform))
                        {
                            AddComponent<GhostChildEntity>(entity);
                        }
                        else
                        {
                            var ghostGroup = AddBuffer<GhostGroup>(entity);
                            IterateChildren(ghostGroup, authoring.transform);
                        }
                    }
                    else
                    {
                        var ghostGroup = AddBuffer<GhostGroup>(entity);
                        IterateChildren(ghostGroup, authoring.transform);
                    }
                }
                else
                {
                    if (authoring.transform.parent != null)
                    {
                        ////Debug.Log($"{authoring.name} has parent!");
                        if (IsThereAGhostGroupAbove(authoring.transform))
                        {
                            AddComponent<GhostGroupAddChild>(entity);
                        }
                        else
                        {
                            var ghostGroupRequest = AddBuffer<GhostGroupRequest>(entity);
                            IterateChildren(ghostGroupRequest, authoring.transform);
                        }
                    }
                    else
                    {
                        var ghostGroupRequest = AddBuffer<GhostGroupRequest>(entity);
                        IterateChildren(ghostGroupRequest, authoring.transform);
                    }
                }

            }
            else
            {
                if (authoring.ImmidiateGhostGrouping)
                {
                    if (authoring.transform.parent != null)
                    {
                        if (IsThereAGhostGroupAbove(authoring.transform))
                        {
                            AddComponent<GhostChildEntity>(entity);
                        }
                    }
                }
                else
                {

                    if (authoring.transform.parent != null)
                    {

                        if (IsThereAGhostGroupAbove(authoring.transform))
                        {
                            AddComponent<GhostGroupAddChild>(entity);
                        }
                    }
                }
            }
            if (authoring.LinkedEntityRelinking)
            {
                var relinks = AddBuffer<LinkedEntityRelink>(entity);
                IterateAllChildren(relinks, authoring.transform);
            }
            
            var allComponentOverrides = CollectAllComponentOverridesInInspectionComponents(authoring, true);
            var overrideBuffer = AddBuffer<GhostAuthoringComponentOverridesBaking>(entity);
            foreach (var componentOverride in allComponentOverrides)
            {
                overrideBuffer.Add(new GhostAuthoringComponentOverridesBaking
                {
                    FullTypeNameID = TypeManager.CalculateFullNameHash(componentOverride.Item2.FullTypeName),
                    GameObjectID = componentOverride.Item1.GetInstanceID(),
                    EntityGuid = componentOverride.Item2.EntityIndex,
                    PrefabType = (int)componentOverride.Item2.PrefabType,
                    SendTypeOptimization = (int)componentOverride.Item2.SendTypeOptimization,
                    ComponentVariant = componentOverride.Item2.VariantHash
                });
            }





            if (authoring.NetworkedParentship)
            {
                if (authoring.UseImmitatedParenting)
                {
                    AddComponent(entity, new ImmitatedParentReference
                    {
                        Value = authoring.transform.parent == null ? (Entity.Null) : (authoring.ShouldBeUnParented ? Entity.Null : GetEntity(authoring.transform.parent, TransformUsageFlags.Dynamic)),
                    });

                    if (authoring.ShouldBeUnParented)
                    {
                        SetComponentEnabled<ImmitatedParentReference>(entity, false);
                    }
                }
                else
                {
                    AddComponent(entity, new DesiredParent
                    {
                        NextParent = authoring.transform.parent == null ? (Entity.Null) : (authoring.ShouldBeUnParented ? Entity.Null : GetEntity(authoring.transform.parent, TransformUsageFlags.Dynamic)),
                        shouldBeUnParented = authoring.ShouldBeUnParented,
                    });
                }
            }
            if (authoring.Rereferencing)
            {
                if (authoring.NonGhostRereferencing)
                {
                    var relink = AddBuffer<RelinkNonGhostChildrenReference>(entity);
                    IterateNonGhostChildren(relink, authoring.transform);
                }
            }

            var bakingConfig = new GhostPrefabConfigBaking
            {
                Authoring = null,//this isn't used anywhere so...
                Config = authoring.AsConfig(ghostName),
            };

            var activeInScene = IsActive();

            AddComponent(entity, new GhostAuthoringComponentBakingData
            {
                GhostName = ghostName,
                GhostNameHash = ghostNameHash,
                BakingConfig = bakingConfig,
                GhostType = ghostType,
                Target = target,
                IsPrefab = isPrefab,
                IsActive = activeInScene
            });


            AddComponent(entity, new NestedGhostAdditionalData
            {
                NetworkedParenting = authoring.NetworkedParentship,
                ShouldBeUnParented = authoring.ShouldBeUnParented,
                UseImmitatedParenting = authoring.UseImmitatedParenting,
                Unnested = authoring.transform.parent==null,
                NonGhostRereferencing = authoring.NonGhostRereferencing,
                Rereferencing = authoring.Rereferencing,
            });
            if (isPrefab)
            {
                AddComponent<GhostPrefabMetaData>(entity);
                if (target == NetcodeConversionTarget.ClientAndServer)
                    // Flag this prefab as needing runtime stripping
                    AddComponent<GhostPrefabRuntimeStrip>(entity);
            }

            if (isPrefab && (target != NetcodeConversionTarget.Server) && (bakingConfig.Config.SupportedGhostModes != GhostModeMask.Interpolated))
                AddComponent<PredictedGhostSpawnRequest>(entity);


        }
        void IterateNonGhostChildren(DynamicBuffer<RelinkNonGhostChildrenReference> relink, Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.GetComponent(typeof(GhostAuthoringComponent)) == null && child.GetComponent(typeof(NestedGhostAuthoring)) == null)
                {
                    relink.Add(new RelinkNonGhostChildrenReference { Value = i, Reference = GetEntity(child, TransformUsageFlags.Dynamic) });
                    IterateNonGhostChildren(relink, child);
                }
            }
        }
    }

}


