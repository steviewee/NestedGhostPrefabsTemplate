
using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;
using Unity.NetCode;
using Zhorman.NestedGhostPrefabs.Runtime.Authoring;

namespace Zhorman.NestedGhostPrefabs.Runtime.Authoring
{
    /// <summary>
    /// This needs major rework, but I just can't be bothered
    /// </summary>
    /// <seealso cref="NestedGhostAuthoring"/>
    [DisallowMultipleComponent]
    public class NestedGhostInspectionAuthoring : MonoBehaviour
    {
        // TODO: This doesn't support multi-edit.
        public static bool forceBake;
        public static bool forceRebuildInspector = true;
        public static bool forceSave;

        /// <summary>
        /// List of all saved modifications that the user has applied to this entity.
        /// If not set, defaults to whatever Attribute values the user has setup on each <see cref="GhostInstance"/>.
        /// </summary>
        [FormerlySerializedAs("m_ComponentOverrides")]
        [SerializeField]
        public GhostAuthoringInspectionComponent.ComponentOverride[] ComponentOverrides = Array.Empty<GhostAuthoringInspectionComponent.ComponentOverride>();

        ///<summary>Not the fastest way but on average is taking something like 10-50us or less to find the type,
        ///so seem reasonably fast even with tens of components per prefab.</summary>
        static Type FindTypeFromFullTypeNameInAllAssemblies(string fullName)
        {
            // TODO - Consider using the TypeManager.
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = a.GetType(fullName, false);
                if (type != null)
                    return type;
            }
            return default;
        }

        [ContextMenu("Force Re-Bake Prefab")]
        void ForceBake()
        {
            forceBake = true;
            forceRebuildInspector = true;
        }

        /// <summary>Notifies of all invalid overrides.</summary>
        public void LogErrorIfComponentOverrideIsInvalid()
        {
            for (var i = 0; i < ComponentOverrides.Length; i++)
            {
                ref var mod = ref ComponentOverrides[i];
                var compType = FindTypeFromFullTypeNameInAllAssemblies(mod.FullTypeName);
                if (compType == null)
                {
                    Debug.LogError($"Ghost Prefab '{name}' has an invalid 'Component Override' targeting an unknown component type '{mod.FullTypeName}'. " +
                                   "If this type has been renamed, you will unfortunately need to manually re-add this override. If it has been deleted, simply re-commit this prefab.");
                }
            }
        }

        /// <remarks>Note that this operation is not saved. Ensure you call <see cref="SavePrefabOverride"/>.</remarks>
        public ref GhostAuthoringInspectionComponent.ComponentOverride GetOrAddPrefabOverride(Type managedType, EntityGuid entityGuid, GhostPrefabType defaultPrefabType)
        {
            if (!gameObject || !this)
                throw new ArgumentException($"Attempting to GetOrAddPrefabOverride for entityGuid '{entityGuid}' to '{this}', but GameObject and/or InspectionComponent has been destroyed!");

            if (gameObject.GetInstanceID() != entityGuid.OriginatingId && !TryGetFirstMatchingGameObjectInChildren(gameObject.transform, entityGuid, out _))
            {
                throw new ArgumentException($"Attempting to GetOrAddPrefabOverride for entityGuid '{entityGuid}' to '{this}', but entityGuid does not match our gameObject, nor our children!");
            }

            if (TryFindExistingOverrideIndex(managedType, entityGuid, out var index))
            {
                return ref ComponentOverrides[index];
            }

            // Did not find, so add:
            ref var found = ref AddComponentOverrideRaw();
            found = new GhostAuthoringInspectionComponent.ComponentOverride
            {
                EntityIndex = entityGuid.b,
                FullTypeName = managedType.FullName,
            };
            found.Reset();
            found.PrefabType = defaultPrefabType;
            return ref found;
        }

        public ref GhostAuthoringInspectionComponent.ComponentOverride AddComponentOverrideRaw()
        {
            Array.Resize(ref ComponentOverrides, ComponentOverrides.Length + 1);
            return ref ComponentOverrides[ComponentOverrides.Length - 1];
        }

        /// <summary>Saves this component override. Attempts to remove it if it's default.</summary>
        public void SavePrefabOverride(ref GhostAuthoringInspectionComponent.ComponentOverride componentOverride, string reason)
        {
            forceSave = true;

            // Remove the override entirely if its no longer overriding anything.
            if (!componentOverride.HasOverriden)
            {
                var index = FindExistingOverrideIndex(ref componentOverride);
                RemoveComponentOverrideByIndex(index);
            }
        }

        /// <summary>Replaces this element with the last, then resizes -1.</summary>
        /// <param name="index">Index to remove.</param>
        public void RemoveComponentOverrideByIndex(int index)
        {
            if (ComponentOverrides.Length == 0) return;
            if (index < ComponentOverrides.Length - 1)
            {
                ComponentOverrides[index] = ComponentOverrides[ComponentOverrides.Length - 1];
            }
            Array.Resize(ref ComponentOverrides, ComponentOverrides.Length - 1);
        }

        int FindExistingOverrideIndex(ref GhostAuthoringInspectionComponent.ComponentOverride currentOverride)
        {
            for (int i = 0; i < ComponentOverrides.Length; i++)
            {
                if (string.Equals(ComponentOverrides[i].FullTypeName, currentOverride.FullTypeName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            throw new InvalidOperationException("Unable to find index of override, which should be impossible as we're passing currentOverride by ref!");
        }

        /// <summary>Does a depth first search to find an element in the transform hierarchy matching this EntityGuid.</summary>
        /// <param name="current">Root element to search from.</param>
        /// <param name="entityGuid">Query: First to match with this EntityGuid.</param>
        /// <param name="foundGameObject">First element matching the query. Will be set to null otherwise.</param>
        /// <returns>True if found.</returns>
        static bool TryGetFirstMatchingGameObjectInChildren(Transform current, EntityGuid entityGuid, out GameObject foundGameObject)
        {
            if (current.gameObject.GetInstanceID() == entityGuid.OriginatingId)
            {
                foundGameObject = current.gameObject;
                return true;
            }

            if (current.childCount == 0)
            {
                foundGameObject = null;
                return false;
            }

            for (int i = 0; i < current.childCount; i++)
            {
                var child = current.GetChild(i);
                if (TryGetFirstMatchingGameObjectInChildren(child, entityGuid, out foundGameObject))
                {
                    return true;
                }
            }
            foundGameObject = null;
            return false;
        }

        /// <summary>Finds all <see cref="NestedGhostInspectionAuthoring"/>'s on this Ghost Authoring Prefab (including in children), and adds all <see cref="ComponentOverrides"/> to a single list.</summary>
        /// <param name="ghostAuthoring">Root prefab to search from.</param>
        /// <param name="validate"></param>
        public static List<(GameObject, GhostAuthoringInspectionComponent.ComponentOverride)> CollectAllComponentOverridesInInspectionComponents(NestedGhostAuthoring ghostAuthoring, bool validate)
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

        public static List<NestedGhostInspectionAuthoring> CollectAllInspectionComponents(NestedGhostAuthoring ghostAuthoring)
        {
            var inspectionComponents = new List<NestedGhostInspectionAuthoring>(8);
            ghostAuthoring.gameObject.GetComponents(inspectionComponents);
            ghostAuthoring.GetComponentsInChildren(inspectionComponents);
            return inspectionComponents;
        }



        public bool TryFindExistingOverrideIndex(Type managedType, in EntityGuid guid, out int index)
        {
            var managedTypeFullName = managedType.FullName;
            return TryFindExistingOverrideIndex(managedTypeFullName, guid.b, out index);
        }

        public bool TryFindExistingOverrideIndex(string managedTypeFullName, in ulong entityGuid, out int index)
        {
            for (index = 0; index < ComponentOverrides.Length; index++)
            {
                ref var componentOverride = ref ComponentOverrides[index];
                if (componentOverride.EntityIndex == entityGuid && string.Equals(componentOverride.FullTypeName, managedTypeFullName, StringComparison.OrdinalIgnoreCase))
                {
                    componentOverride.DidCorrectlyMap = true;
                    return true;
                }
            }
            index = -1;
            return false;
        }
    }
}
