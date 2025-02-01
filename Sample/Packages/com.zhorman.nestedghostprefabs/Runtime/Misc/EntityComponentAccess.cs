using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using System;
using Unity.Burst;
using UnityEditor;
using Unity.Jobs;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using System.Reflection;
using Unity.Collections.NotBurstCompatible;
using Unity.NetCode;

namespace Zhorman.NestedGhostPrefabs.Runtime.Misc
{
    public static unsafe class EntityComponentAccess
    {
        [StructLayout(LayoutKind.Explicit)]
        [NoAlias]
        private unsafe struct BufferHeader
        {
            public const int kMinimumCapacity = 8;

            [NoAlias]
            [FieldOffset(0)] public byte* Pointer;
            [FieldOffset(8)] public int Length;
            [FieldOffset(12)] public int Capacity;

            public static byte* GetElementPointer(BufferHeader* header)
            {
                if (header->Pointer != null)
                    return header->Pointer;

                return (byte*)(header + 1);
            }

            public enum TrashMode
            {
                TrashOldData,
                RetainOldData
            }






            public static void Initialize(BufferHeader* header, int bufferCapacity)
            {
                header->Pointer = null;
                header->Length = 0;
                header->Capacity = bufferCapacity;
            }





            public static void MemsetUnusedMemory(BufferHeader* bufferHeader, int internalCapacity, int elementSize, byte value)
            {
                // If bufferHeader->Pointer is not null it means with rely on a dedicated buffer instead of the internal one (that follows the header) to store the elements.
                // in this case we also have to fully wipe out the internal buffer which is not in use.
                if (bufferHeader->Pointer != null)
                {
                    byte* internalBuffer = (byte*)(bufferHeader + 1);
                    UnsafeUtility.MemSet(internalBuffer, value, internalCapacity * elementSize);
                }

                // Wipe out excess capacity
                var elementCountToClean = bufferHeader->Capacity - bufferHeader->Length;
                var firstElementToClean = bufferHeader->Length;
                var buffer = BufferHeader.GetElementPointer(bufferHeader);
                UnsafeUtility.MemSet(buffer + (firstElementToClean * elementSize), value, elementCountToClean * elementSize);
            }
        }





        public static void RemapEntitiesForDiffer(this EntityManager entityManager,
    NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapping,
    NativeArray<ArchetypeChunk> dstChunks)
        {
            var dstEntityComponentStore = entityManager.GetCheckedEntityDataAccess()->EntityComponentStore;

            new RemapEntitiesManagedForDiffer
            {
                DstChunks = dstChunks,
                EntityRemapping = entityRemapping,
                EntityComponentStore = dstEntityComponentStore
            }.Run();

            new RemapEntitiesUnmanagedFor
            {
                DstChunks = dstChunks,
                EntityRemapping = entityRemapping
            }.Run(dstChunks.Length);
        }
        [BurstCompile]
        public struct RemapEntitiesManagedForDiffer : IJob
        {
            public NativeArray<ArchetypeChunk> DstChunks;
            public NativeArray<EntityRemapUtility.EntityRemapInfo> EntityRemapping;
            [NativeDisableUnsafePtrRestriction]
            internal EntityComponentStore* EntityComponentStore;

            public void Execute()
            {
                for (int ci = 0, cc = DstChunks.Length; ci < cc; ci++)
                {
                    var remapChunk = DstChunks[ci];
                    var chunk = remapChunk.m_Chunk;
                    Archetype* dstArchetype = remapChunk.Archetype.Archetype;
                    EntityComponentStore->ManagedChangesTracker.PatchEntities(dstArchetype, chunk, chunk.Count, EntityRemapping);
                }
            }
        }

        [BurstCompile]
        public struct RemapEntitiesUnmanagedFor : IJobParallelFor
        {
            [Unity.Collections.ReadOnly] public NativeArray<EntityRemapUtility.EntityRemapInfo> EntityRemapping;
            [Unity.Collections.ReadOnly] public NativeArray<ArchetypeChunk> DstChunks;

            public void Execute(int index)
            {
                var remapChunk = DstChunks[index];
                var chunk = remapChunk.m_Chunk;
                Archetype* dstArchetype = remapChunk.Archetype.Archetype;

                var entityCount = chunk.Count;
                PatchEntities(dstArchetype->ScalarEntityPatches + 1,
                    dstArchetype->ScalarEntityPatchCount - 1, dstArchetype->BufferEntityPatches,
                    dstArchetype->BufferEntityPatchCount, chunk.Buffer, entityCount, ref EntityRemapping);
            }
        }

        public static void PatchEntities(EntityRemapUtility.EntityPatchInfo* scalarPatches, int scalarPatchCount,
            EntityRemapUtility.BufferEntityPatchInfo* bufferPatches, int bufferPatchCount,
            byte* chunkBuffer, int entityCount, ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapping)
        {
            // Patch scalars (single components) with entity references.
            for (int p = 0; p < scalarPatchCount; p++)
            {
                byte* entityData = chunkBuffer + scalarPatches[p].Offset;
                for (int i = 0; i != entityCount; i++)
                {
                    Entity* entity = (Entity*)entityData;
                    *entity = RemapEntity(ref remapping, *entity);
                    entityData += scalarPatches[p].Stride;
                }
            }

            // Patch buffers that contain entity references
            for (int p = 0; p < bufferPatchCount; ++p)
            {
                byte* bufferData = chunkBuffer + bufferPatches[p].BufferOffset;

                for (int i = 0; i != entityCount; ++i)
                {
                    BufferHeader* header = (BufferHeader*)bufferData;

                    byte* elemsBase = BufferHeader.GetElementPointer(header) + bufferPatches[p].ElementOffset;
                    int elemCount = header->Length;

                    for (int k = 0; k != elemCount; ++k)
                    {
                        Entity* entityPtr = (Entity*)elemsBase;
                        *entityPtr = RemapEntity(ref remapping, *entityPtr);
                        elemsBase += bufferPatches[p].ElementStride;
                    }

                    bufferData += bufferPatches[p].BufferStride;
                }
            }
        }











        public static Entity RemapEntity(ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapping, Entity source)
        {
            return RemapEntity((EntityRemapUtility.EntityRemapInfo*)remapping.GetUnsafeReadOnlyPtr(), source);
        }
        public static Entity RemapEntity(EntityRemapUtility.EntityRemapInfo* remapping, Entity source)
        {
            if (source.Version == remapping[source.Index].SourceVersion)
                return remapping[source.Index].Target;
            else
            {
                return source;
            }
        }






































        public static void ListComponents(EntityManager entityManager, Entity entity)
        {
            // Get all component types from the source entity
            var componentTypes = entityManager.GetComponentTypes(entity, Allocator.Temp);

            // Loop through all components in the source entity
            foreach (var componentType in componentTypes)
            {
                //Debug.Log($"looking to copy {componentType.GetDebugTypeName()}");
                // Skip the component if it's in the blacklist
                var typeIndex = componentType.TypeIndex;
                // if (componentPtr == null)
                //    continue;
                var componentDataType = componentType.GetManagedType();
                Debug.Log($"Found {componentType.GetDebugTypeName()} in entity:{entity}");
                /*
                void* componentPtr;
                try
                {
                    // Get the raw pointer to the component data
                    componentPtr = entityManager.GetComponentDataRawRW(entity, typeIndex);
                }
                catch
                {
                    if (componentType.IsZeroSized)
                    {
                        //Debug.Log("IsZeroSized");
                    }
                    continue;
                }
                if (componentPtr == null)
                    continue;
                IntPtr componentIntPtr = new IntPtr(componentPtr);
                if (componentType.IsZeroSized)
                {
                    //Debug.Log("componentType");
                }
                else if (componentType.IsCleanupBufferComponent)
                {
                    //Debug.LogWarning("[EntityComponentAccess] CleanupBufferComponent processing is not yet implemented!");
                }
                else if (componentType.IsBuffer)
                {

                }
                else if (componentType.IsManagedComponent)
                {
                    //Debug.LogWarning("[EntityComponentAccess] SharedComponent processing is not yet implemented!");
                }
                else if (componentType.IsCleanupSharedComponent)
                {
                    //Debug.LogWarning("[EntityComponentAccess] SharedComponent processing is not yet implemented!");
                }
                else if (componentType.IsSharedComponent)
                {
                    //Debug.LogWarning("[EntityComponentAccess] SharedComponent processing is not yet implemented!");
                }
                else if (componentType.IsChunkComponent)
                {
                    //Debug.LogWarning("[EntityComponentAccess] ChunkComponent processing is not yet implemented!");
                }
                else if (componentType.IsCleanupComponent)
                {

                }
                else if (componentType.IsComponent)
                {


                }
                else
                {
                    //Debug.Log($"{componentType.GetDebugTypeName()} did not make it");
                }
                */
            }


            // Dispose of the temporary component types array
            componentTypes.Dispose();
        }
        public static void CopyComponentsECB(EntityManager entityManager, EntityCommandBuffer ecb, Entity entity, Entity targetEntity)
        {
            // Get all component types from the source entity
            var componentTypes = entityManager.GetComponentTypes(entity, Allocator.Temp);

            // Loop through all components in the source entity
            foreach (var componentType in componentTypes)
            {

                var typeIndex = componentType.TypeIndex;
                // if (componentPtr == null)
                //    continue;
                var componentDataType = componentType.GetManagedType();
                void* componentPtr;
                try
                {
                    // Get the raw pointer to the component data
                    componentPtr = entityManager.GetComponentDataRawRW(entity, typeIndex);
                }
                catch
                {
                    if (componentType.IsZeroSized)
                    {
                        //Debug.Log("IsZeroSized");
                        ecb.AddComponent(targetEntity, componentDataType);
                    }
                    continue;
                }
                if (componentPtr == null)
                    continue;
                IntPtr componentIntPtr = new IntPtr(componentPtr);
                if (componentType.IsZeroSized)
                {
                    //Debug.Log("componentType");
                    // Check if the target entity already has the component
                    if (!entityManager.HasComponent(targetEntity, componentType))
                    {
                        var genericMethod = typeof(EntityComponentAccess)
.GetMethod(nameof(CopyAddComponentECB), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
.MakeGenericMethod(componentDataType);
                        genericMethod.Invoke(null, new object[] { ecb, targetEntity, componentIntPtr });
                        //ecb.AddComponent(targetEntity, componentType);
                    }
                }
                else if (componentType.IsCleanupBufferComponent)
                {
                    //Debug.LogWarning("[EntityComponentAccess] CleanupBufferComponent processing is not yet implemented!");
                }
                else if (componentType.IsBuffer)
                {
                    if (!entityManager.HasComponent(targetEntity, componentType))
                    {
                        var genericMethod = typeof(EntityComponentAccess)
.GetMethod(nameof(CopyAddBufferECB), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
.MakeGenericMethod(componentDataType);
                        genericMethod.Invoke(null, new object[] { ecb, entityManager, targetEntity, entity });//, componentIntPtr });
                                                                                                              //ecb.AddComponent(targetEntity, componentType);
                    }
                    else
                    {
                        var genericMethod = typeof(EntityComponentAccess)
.GetMethod(nameof(CopySetBufferECB), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
.MakeGenericMethod(componentDataType);
                        genericMethod.Invoke(null, new object[] { ecb, entityManager, targetEntity, entity }); //, targetEntity, componentIntPtr });
                    }
                    ////Debug.LogWarning("[EntityComponentAccess] BufferComponent processing is not yet implemented!");
                }
                else if (componentType.IsManagedComponent)
                {
                    //Debug.LogWarning("[EntityComponentAccess] SharedComponent processing is not yet implemented!");
                }
                else if (componentType.IsCleanupSharedComponent)
                {
                    //Debug.LogWarning("[EntityComponentAccess] SharedComponent processing is not yet implemented!");
                }
                else if (componentType.IsSharedComponent)
                {
                    //Debug.LogWarning("[EntityComponentAccess] SharedComponent processing is not yet implemented!");
                }
                else if (componentType.IsChunkComponent)
                {
                    //Debug.LogWarning("[EntityComponentAccess] ChunkComponent processing is not yet implemented!");
                }
                else if (componentType.IsCleanupComponent)
                {
                    //Debug.Log("IsCleanupComponent");
                    if (entityManager.HasComponent(targetEntity, componentType))
                    {
                        var genericMethod = typeof(EntityComponentAccess)
.GetMethod(nameof(CopySetCleanupComponentECB), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
.MakeGenericMethod(componentDataType);
                        genericMethod.Invoke(null, new object[] { ecb, targetEntity, componentIntPtr });
                    }
                    else
                    {
                        var genericMethod = typeof(EntityComponentAccess)
.GetMethod(nameof(CopyAddCleanupComponentECB), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
.MakeGenericMethod(componentDataType);
                        genericMethod.Invoke(null, new object[] { ecb, targetEntity, componentIntPtr });
                    }
                }
                else if (componentType.IsComponent)
                {
                    //Debug.Log("IsComponent");
                    if (entityManager.HasComponent(targetEntity, componentType))
                    {
                        //Debug.Log("targetEntity does not have");
                        var genericMethod = typeof(EntityComponentAccess)
.GetMethod(nameof(CopySetComponentECB), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
.MakeGenericMethod(componentDataType);
                        genericMethod.Invoke(null, new object[] { ecb, targetEntity, componentIntPtr });
                    }
                    else
                    {
                        //Debug.Log("targetEntity does not have");
                        var genericMethod = typeof(EntityComponentAccess)
.GetMethod(nameof(CopyAddComponentECB), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
.MakeGenericMethod(componentDataType);
                        genericMethod.Invoke(null, new object[] { ecb, targetEntity, componentIntPtr });
                    }

                }
                else
                {
                    //Debug.Log($"{componentType.GetDebugTypeName()} did not make it");
                }
                break;



            }

            // Dispose of the temporary component types array
            componentTypes.Dispose();
        }
        public static void CopyTargetComponentsECB(EntityManager entityManager, EntityCommandBuffer ecb, Entity entity, Entity targetEntity, NativeArray<ComponentType> targets)
        {
            // Get all component types from the source entity
            var componentTypes = entityManager.GetComponentTypes(entity, Allocator.Temp);

            // Loop through all components in the source entity
            foreach (var componentType in componentTypes)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    if (componentType == targets[i])
                    {
                        var typeIndex = componentType.TypeIndex;
                        // if (componentPtr == null)
                        //    continue;
                        var componentDataType = componentType.GetManagedType();
                        void* componentPtr;
                        try
                        {
                            // Get the raw pointer to the component data
                            componentPtr = entityManager.GetComponentDataRawRW(entity, typeIndex);
                        }
                        catch
                        {
                            if (componentType.IsZeroSized)
                            {
                                //Debug.Log("IsZeroSized");
                                ecb.AddComponent(targetEntity, componentDataType);
                            }
                            continue;
                        }
                        if (componentPtr == null)
                            continue;
                        IntPtr componentIntPtr = new IntPtr(componentPtr);
                        if (componentType.IsZeroSized)
                        {
                            //Debug.Log("componentType");
                            // Check if the target entity already has the component
                            if (!entityManager.HasComponent(targetEntity, componentType))
                            {
                                var genericMethod = typeof(EntityComponentAccess)
    .GetMethod(nameof(CopyAddComponentECB), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
    .MakeGenericMethod(componentDataType);
                                genericMethod.Invoke(null, new object[] { ecb, targetEntity, componentIntPtr });
                                //ecb.AddComponent(targetEntity, componentType);
                            }
                        }
                        else if (componentType.IsCleanupBufferComponent)
                        {
                            //Debug.LogWarning("[EntityComponentAccess] CleanupBufferComponent processing is not yet implemented!");
                        }
                        else if (componentType.IsBuffer)
                        {
                            if (!entityManager.HasComponent(targetEntity, componentType))
                            {
                                var genericMethod = typeof(EntityComponentAccess)
    .GetMethod(nameof(CopyAddBufferECB), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
    .MakeGenericMethod(componentDataType);
                                genericMethod.Invoke(null, new object[] { ecb, entityManager, targetEntity, entity });//, componentIntPtr });
                                                                                                                      //ecb.AddComponent(targetEntity, componentType);
                            }
                            else
                            {
                                var genericMethod = typeof(EntityComponentAccess)
    .GetMethod(nameof(CopySetBufferECB), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
    .MakeGenericMethod(componentDataType);
                                genericMethod.Invoke(null, new object[] { ecb, entityManager, targetEntity, entity }); //, targetEntity, componentIntPtr });
                            }
                            ////Debug.LogWarning("[EntityComponentAccess] BufferComponent processing is not yet implemented!");
                        }
                        else if (componentType.IsManagedComponent)
                        {
                            //Debug.LogWarning("[EntityComponentAccess] SharedComponent processing is not yet implemented!");
                        }
                        else if (componentType.IsCleanupSharedComponent)
                        {
                            //Debug.LogWarning("[EntityComponentAccess] SharedComponent processing is not yet implemented!");
                        }
                        else if (componentType.IsSharedComponent)
                        {
                            //Debug.LogWarning("[EntityComponentAccess] SharedComponent processing is not yet implemented!");
                        }
                        else if (componentType.IsChunkComponent)
                        {
                            //Debug.LogWarning("[EntityComponentAccess] ChunkComponent processing is not yet implemented!");
                        }
                        else if (componentType.IsCleanupComponent)
                        {
                            //Debug.Log("IsCleanupComponent");
                            if (entityManager.HasComponent(targetEntity, componentType))
                            {
                                var genericMethod = typeof(EntityComponentAccess)
    .GetMethod(nameof(CopySetCleanupComponentECB), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
    .MakeGenericMethod(componentDataType);
                                genericMethod.Invoke(null, new object[] { ecb, targetEntity, componentIntPtr });
                            }
                            else
                            {
                                var genericMethod = typeof(EntityComponentAccess)
    .GetMethod(nameof(CopyAddCleanupComponentECB), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
    .MakeGenericMethod(componentDataType);
                                genericMethod.Invoke(null, new object[] { ecb, targetEntity, componentIntPtr });
                            }
                        }
                        else if (componentType.IsComponent)
                        {
                            //Debug.Log("IsComponent");
                            if (entityManager.HasComponent(targetEntity, componentType))
                            {
                                //Debug.Log("targetEntity does not have");
                                var genericMethod = typeof(EntityComponentAccess)
    .GetMethod(nameof(CopySetComponentECB), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
    .MakeGenericMethod(componentDataType);
                                genericMethod.Invoke(null, new object[] { ecb, targetEntity, componentIntPtr });
                            }
                            else
                            {
                                //Debug.Log("targetEntity does not have");
                                var genericMethod = typeof(EntityComponentAccess)
    .GetMethod(nameof(CopyAddComponentECB), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
    .MakeGenericMethod(componentDataType);
                                genericMethod.Invoke(null, new object[] { ecb, targetEntity, componentIntPtr });
                            }

                        }
                        else
                        {
                            //Debug.Log($"{componentType.GetDebugTypeName()} did not make it");
                        }
                        break;
                    }
                }

            }

            // Dispose of the temporary component types array
            componentTypes.Dispose();
        }

        public static void CopyComponentsWithBlacklistECB(EntityManager entityManager, EntityCommandBuffer ecb, Entity entity, Entity targetEntity, NativeArray<ComponentType> blacklist)
        {
            // Get all component types from the source entity
            var componentTypes = entityManager.GetComponentTypes(entity, Allocator.Temp);

            // Loop through all components in the source entity
            foreach (var componentType in componentTypes)
            {
                //Debug.Log($"looking to copy {componentType.GetDebugTypeName()}");
                // Skip the component if it's in the blacklist
                bool isBlacklisted = false;
                for (int i = 0; i < blacklist.Length; i++)
                {
                    if (componentType == blacklist[i])
                    {
                        isBlacklisted = true;
                        break;
                    }
                }
                var typeIndex = componentType.TypeIndex;
                // if (componentPtr == null)
                //    continue;
                var componentDataType = componentType.GetManagedType();
                if (!isBlacklisted)
                {
                    void* componentPtr;
                    try
                    {
                        // Get the raw pointer to the component data
                        componentPtr = entityManager.GetComponentDataRawRW(entity, typeIndex);
                    }
                    catch
                    {
                        if (componentType.IsZeroSized)
                        {
                            //Debug.Log("IsZeroSized");
                            ecb.AddComponent(targetEntity, componentDataType);
                        }
                        continue;
                    }
                    if (componentPtr == null)
                        continue;
                    IntPtr componentIntPtr = new IntPtr(componentPtr);
                    if (componentType.IsZeroSized)
                    {
                        //Debug.Log("componentType");
                        // Check if the target entity already has the component
                        if (!entityManager.HasComponent(targetEntity, componentType))
                        {
                            var genericMethod = typeof(EntityComponentAccess)
.GetMethod(nameof(CopyAddComponentECB), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
.MakeGenericMethod(componentDataType);
                            genericMethod.Invoke(null, new object[] { ecb, targetEntity, componentIntPtr });
                            //ecb.AddComponent(targetEntity, componentType);
                        }
                    }
                    else if (componentType.IsCleanupBufferComponent)
                    {
                        //Debug.LogWarning("[EntityComponentAccess] CleanupBufferComponent processing is not yet implemented!");
                    }
                    else if (componentType.IsBuffer)
                    {
                        if (!entityManager.HasComponent(targetEntity, componentType))
                        {
                            var genericMethod = typeof(EntityComponentAccess)
.GetMethod(nameof(CopyAddBufferECB), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
.MakeGenericMethod(componentDataType);
                            genericMethod.Invoke(null, new object[] { ecb, entityManager, entity, targetEntity });//, componentIntPtr });
                            //ecb.AddComponent(targetEntity, componentType);
                        }
                        else
                        {
                            var genericMethod = typeof(EntityComponentAccess)
.GetMethod(nameof(CopySetBufferECB), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
.MakeGenericMethod(componentDataType);
                            genericMethod.Invoke(null, new object[] { ecb, entityManager, entity, targetEntity }); //, targetEntity, componentIntPtr });
                        }
                        ////Debug.LogWarning("[EntityComponentAccess] BufferComponent processing is not yet implemented!");
                    }
                    else if (componentType.IsManagedComponent)
                    {
                        //Debug.LogWarning("[EntityComponentAccess] SharedComponent processing is not yet implemented!");
                    }
                    else if (componentType.IsCleanupSharedComponent)
                    {
                        //Debug.LogWarning("[EntityComponentAccess] SharedComponent processing is not yet implemented!");
                    }
                    else if (componentType.IsSharedComponent)
                    {
                        //Debug.LogWarning("[EntityComponentAccess] SharedComponent processing is not yet implemented!");
                    }
                    else if (componentType.IsChunkComponent)
                    {
                        //Debug.LogWarning("[EntityComponentAccess] ChunkComponent processing is not yet implemented!");
                    }
                    else if (componentType.IsCleanupComponent)
                    {
                        //Debug.Log("IsCleanupComponent");
                        if (entityManager.HasComponent(targetEntity, componentType))
                        {
                            var genericMethod = typeof(EntityComponentAccess)
.GetMethod(nameof(CopySetCleanupComponentECB), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
.MakeGenericMethod(componentDataType);
                            genericMethod.Invoke(null, new object[] { ecb, targetEntity, componentIntPtr });
                        }
                        else
                        {
                            var genericMethod = typeof(EntityComponentAccess)
.GetMethod(nameof(CopyAddCleanupComponentECB), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
.MakeGenericMethod(componentDataType);
                            genericMethod.Invoke(null, new object[] { ecb, targetEntity, componentIntPtr });
                        }
                    }
                    else if (componentType.IsComponent)
                    {
                        //Debug.Log("IsComponent");
                        if (entityManager.HasComponent(targetEntity, componentType))
                        {
                            //Debug.Log("targetEntity does not have");
                            var genericMethod = typeof(EntityComponentAccess)
.GetMethod(nameof(CopySetComponentECB), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
.MakeGenericMethod(componentDataType);
                            genericMethod.Invoke(null, new object[] { ecb, targetEntity, componentIntPtr });
                        }
                        else
                        {
                            //Debug.Log("targetEntity does not have");
                            var genericMethod = typeof(EntityComponentAccess)
.GetMethod(nameof(CopyAddComponentECB), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
.MakeGenericMethod(componentDataType);
                            genericMethod.Invoke(null, new object[] { ecb, targetEntity, componentIntPtr });
                        }

                    }
                    else
                    {
                        //Debug.Log($"{componentType.GetDebugTypeName()} did not make it");
                    }

                }
                else
                {
                    //Debug.Log($"found 1 blacklistedComponent, {componentType.GetDebugTypeName()}");
                }
            }

            // Dispose of the temporary component types array
            componentTypes.Dispose();
        }
        public static void CopyAddBufferECB<T>(EntityCommandBuffer ecb, EntityManager entityManager, Entity sourceEntity, Entity targetEntity) where T : unmanaged, IBufferElementData
        {
            // Get the source buffer from the entityManager (this way you know the length of the buffer)
            DynamicBuffer<T> sourceBuffer = entityManager.GetBuffer<T>(sourceEntity);

            // Create the new buffer for the target entity
            var newBuffer = ecb.AddBuffer<T>(targetEntity);

            // Copy the elements from the source buffer to the new buffer
            for (int i = 0; i < sourceBuffer.Length; i++)
            {
                newBuffer.Add(sourceBuffer[i]);
            }
        }

        public static void CopySetBufferECB<T>(EntityCommandBuffer ecb, EntityManager entityManager, Entity sourceEntity, Entity targetEntity) where T : unmanaged, IBufferElementData
        {
            // Get the source buffer from the entityManager (this way you know the length of the buffer)
            DynamicBuffer<T> sourceBuffer = entityManager.GetBuffer<T>(sourceEntity);

            // Create the new buffer for the target entity
            //var newBuffer = ecb.AddBuffer<T>(targetEntity);
            var existingBuffer = ecb.SetBuffer<T>(targetEntity);
            existingBuffer.Clear();
            // Copy the elements from the source buffer to the new buffer
            for (int i = 0; i < sourceBuffer.Length; i++)
            {
                existingBuffer.Add(sourceBuffer[i]);
            }
        }
        /*
        public static void CopySetBufferECB<T>(EntityCommandBuffer ecb, Entity targetEntity, IntPtr bufferIntPtr) where T : unmanaged, IBufferElementData
        {
            void* bufferPtr = bufferIntPtr.ToPointer();
            ref DynamicBuffer<T> buffer = ref UnsafeUtility.AsRef<DynamicBuffer<T>>(bufferPtr);
            var existingBuffer = ecb.SetBuffer<T>(targetEntity);
            existingBuffer.Clear();
            // Copy contents of the original buffer into the new buffer
            foreach (var element in buffer)
            {
                existingBuffer.Add(element);
            }
        }
        */
        /*
        public static void CopyAddBufferECB<T>(EntityCommandBuffer ecb, Entity targetEntity, IntPtr componentIntPtr) where T : unmanaged, IBufferElementData
        {
            void* componentPtr = componentIntPtr.ToPointer();
            ref DynamicBuffer<T> buffer = ref UnsafeUtility.AsRef< DynamicBuffer<T>>(componentPtr);
            //var boxedComponent = (IBufferElementData)(object)component;
            ecb.AddBuffer<T>(targetEntity);
            //ecb.AddBuffer(targetEntity, (dynamic)boxedComponent);
           // ecb.SetComponent(targetEntity, (dynamic)boxedComponent);
        }

        public static void CopySetBufferECB<T>(EntityCommandBuffer ecb, Entity targetEntity, IntPtr componentIntPtr) where T : unmanaged, IComponentData
        {
            void* componentPtr = componentIntPtr.ToPointer();    
            ref T component = ref UnsafeUtility.AsRef<T>(componentPtr);
            var boxedComponent = (IComponentData)(object)component;

            ecb.SetComponent(targetEntity, (dynamic)boxedComponent);
        }
        */
        public static void CopySetCleanupComponentECB<T>(EntityCommandBuffer ecb, Entity targetEntity, IntPtr componentIntPtr) where T : unmanaged, ICleanupComponentData
        {
            void* componentPtr = componentIntPtr.ToPointer();
            ref T component = ref UnsafeUtility.AsRef<T>(componentPtr);
            var boxedComponent = (ICleanupComponentData)(object)component;

            ecb.SetComponent(targetEntity, (dynamic)boxedComponent);
        }
        public static void CopyAddCleanupComponentECB<T>(EntityCommandBuffer ecb, Entity targetEntity, IntPtr componentIntPtr) where T : unmanaged, ICleanupComponentData
        {
            void* componentPtr = componentIntPtr.ToPointer();
            ref T component = ref UnsafeUtility.AsRef<T>(componentPtr);
            var boxedComponent = (ICleanupComponentData)(object)component;

            ecb.AddComponent(targetEntity, (dynamic)boxedComponent);
        }
        public static void CopySetComponentECB<T>(EntityCommandBuffer ecb, Entity targetEntity, IntPtr componentIntPtr) where T : unmanaged, IComponentData
        {
            void* componentPtr = componentIntPtr.ToPointer();
            ref T component = ref UnsafeUtility.AsRef<T>(componentPtr);
            var boxedComponent = (IComponentData)(object)component;

            ecb.SetComponent(targetEntity, (dynamic)boxedComponent);
        }
        public static void CopyAddComponentECB<T>(EntityCommandBuffer ecb, Entity targetEntity, IntPtr componentIntPtr) where T : unmanaged, IComponentData
        {
            void* componentPtr = componentIntPtr.ToPointer();
            ref T component = ref UnsafeUtility.AsRef<T>(componentPtr);
            var boxedComponent = (IComponentData)(object)component;

            ecb.AddComponent(targetEntity, (dynamic)boxedComponent);
        }
        /*
        public static void GetComponentFromIntPtr<T>(IntPtr componentIntPtr, out IComponentData output) where T : unmanaged, IComponentData
        {
            void* componentPtr = componentIntPtr.ToPointer();
            ref T component = ref UnsafeUtility.AsRef<T>(componentPtr);
            var boxedComponent = (IComponentData)(object)component;

            output = (dynamic)boxedComponent;
        }
        */
        /*
        public static unsafe void FindAndReplaceAllFieldsInEntityComponentsDictionary<K>(EntityManager entityManager, Entity entity, Dictionary<K, K> replacements) where K : unmanaged
        {
            ////Debug.Log($"looking at entity {entity.Index}:{entity.Version}");
            // Get all component types associated with the entity
            var componentTypes = entityManager.GetComponentTypes(entity);
            //entityManager.Ge

            // Iterate through all component types
            foreach (var componentType in componentTypes)
            {
                //Debug.Log($"looking to replace {componentType.GetDebugTypeName()}");
                var typeIndex = componentType.TypeIndex;
                void* componentPtr;
                try
                {
                    // Get the raw pointer to the component data
                    componentPtr = entityManager.GetComponentDataRawRW(entity, typeIndex);
                }
                catch
                {
                    continue;
                }

                if (componentPtr == null)
                    continue;

                // Get the type of the component
                var componentDataType = componentType.GetManagedType();
                if (componentType.IsZeroSized)
                {

                }
                else if (componentType.IsCleanupBufferComponent)
                {
                    var genericMethod = typeof(EntityComponentAccess)
    .GetMethod(nameof(PrepareFindAndReplaceAllFieldsInCleanupBufferDictionarySimple), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
    .MakeGenericMethod(componentDataType, typeof(K));

                    // Convert the void* to IntPtr to pass it as an object
                    IntPtr componentIntPtr = new IntPtr(componentPtr);
                    // Call the generic method and pass the EntityManager as well
                    genericMethod.Invoke(null, new object[] { entityManager, entity, replacements });
                }
                else if (componentType.IsBuffer)
                {



                    var genericMethod = typeof(EntityComponentAccess)
    .GetMethod(nameof(PrepareFindAndReplaceAllFieldsInBufferDictionarySimple), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
    .MakeGenericMethod(componentDataType, typeof(K));

                    // Convert the void* to IntPtr to pass it as an object
                    IntPtr componentIntPtr = new IntPtr(componentPtr);
                    // Call the generic method and pass the EntityManager as well
                    genericMethod.Invoke(null, new object[] { entityManager, entity, replacements });
                }
                else if (componentType.IsManagedComponent)
                {
                    var genericMethod = typeof(EntityComponentAccess)
    .GetMethod(nameof(PrepareFindAndReplaceAllFieldsInManagedComponentDictionary), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
    .MakeGenericMethod(componentDataType, typeof(K));

                    // Convert the void* to IntPtr to pass it as an object
                    IntPtr componentIntPtr = new IntPtr(componentPtr);
                    // Call the generic method and pass the EntityManager as well
                    genericMethod.Invoke(null, new object[] { entityManager, entity, componentIntPtr, replacements });
                }
                else if (componentType.IsCleanupSharedComponent)
                {
                    var genericMethod = typeof(EntityComponentAccess)
    .GetMethod(nameof(PrepareFindAndReplaceAllFieldsInCleanupSharedComponentDictionary), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
    .MakeGenericMethod(componentDataType, typeof(K));

                    // Convert the void* to IntPtr to pass it as an object
                    IntPtr componentIntPtr = new IntPtr(componentPtr);
                    // Call the generic method and pass the EntityManager as well
                    genericMethod.Invoke(null, new object[] { entityManager, entity, componentIntPtr, replacements });
                }
                else if (componentType.IsSharedComponent)
                {
                    var genericMethod = typeof(EntityComponentAccess)
    .GetMethod(nameof(PrepareFindAndReplaceAllFieldsInSharedComponentDictionary), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
    .MakeGenericMethod(componentDataType, typeof(K));

                    // Convert the void* to IntPtr to pass it as an object
                    IntPtr componentIntPtr = new IntPtr(componentPtr);
                    // Call the generic method and pass the EntityManager as well
                    genericMethod.Invoke(null, new object[] { entityManager, entity, componentIntPtr, replacements });
                }
                else if (componentType.IsChunkComponent)
                {
                    //Debug.LogWarning("[EntityComponentAccess] ChunkComponent processing is not yet implemented!");
                }
                else if (componentType.IsCleanupComponent)
                {
                    var genericMethod = typeof(EntityComponentAccess)
    .GetMethod(nameof(PrepareFindAndReplaceAllFieldsInCleanupComponentDictionarySimple), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
    .MakeGenericMethod(componentDataType, typeof(K));

                    // Convert the void* to IntPtr to pass it as an object
                    IntPtr componentIntPtr = new IntPtr(componentPtr);
                    // Call the generic method and pass the EntityManager as well
                    genericMethod.Invoke(null, new object[] { entityManager, entity, replacements });
                }
                else if (componentType.IsComponent)
                {
                    // Use reflection to create a generic method for updating
                    var genericMethod = typeof(EntityComponentAccess)
                        .GetMethod(nameof(PrepareFindAndReplaceAllFieldsInComponentECBDictionarySimple), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
                        .MakeGenericMethod(componentDataType, typeof(K));

                    // Convert the void* to IntPtr to pass it as an object
                    IntPtr componentIntPtr = new IntPtr(componentPtr);
                    // Call the generic method and pass the EntityManager as well
                    genericMethod.Invoke(null, new object[] { entityManager, entity, replacements });
                }



            }
        }
        */
        private static unsafe void PrepareFindAndReplaceAllFieldsInComponentDictionary<T, K>(EntityManager entityManager, Entity entity, IntPtr componentIntPtr, Dictionary<K, K> replacements) where T : unmanaged, IComponentData
        {
            // Convert IntPtr back to void* and then to a reference of the actual component type
            void* componentPtr = componentIntPtr.ToPointer();
            ref T component = ref UnsafeUtility.AsRef<T>(componentPtr);

            // Apply the update logic
            FindAndReplaceAllFieldsInComponentDictionary(ref component, replacements);

            // Now that the component is updated, we need to cast it to an IComponentData to call SetComponentData
            var boxedComponent = (IComponentData)(object)component;

            // Use EntityManager's SetComponentObjectData to set the updated component data
            entityManager.SetComponentData(entity, (dynamic)boxedComponent);
        }


















































        /*

        [GenerateTestsForBurstCompatibility]
        public static unsafe void FindAndReplaceAllEntityReferencesInEntityComponentsUsingHashMap(EntityManager entityManager, EntityCommandBuffer ecb, Entity entity, ref NativeParallelHashMap<Entity, Entity> map)
        {
            //var ecbParallel = ecb.AsParallelWriter();
            var componentTypes = entityManager.GetComponentTypes(entity, Allocator.TempJob);


            for (int i = 0; i < componentTypes.Length; i++)
            {
                if (componentTypes[i].IsZeroSized || !componentTypes[i].HasEntityReferences || componentTypes[i].IsManagedComponent)
                {
                    continue;
                }
                var componentSize = TypeManager.GetTypeInfo(componentTypes[i].TypeIndex).ElementSize;
                byte* data = (byte*)entityManager.GetComponentDataRawRW(entity, componentTypes[i].TypeIndex) + (index * componentSize);

                ReplaceEntityInData(data, componentTypes[i].TypeIndex, map);

            }
            componentTypes.Dispose();
        }
        */
        /*
var job = new ReplaceEntityFieldsInComponentJob()
{
    ecbParallel = ecbParallel,
    componentTypes = componentTypes,
};
JobHandle jobHandle = job.Schedule(componentTypes.Length, 4);//idk what value to put here honestly
jobHandle.Complete();
*/
        /*


        [GenerateTestsForBurstCompatibility]
        public static unsafe void FindAndReplaceAllEntityReferencesInEntityComponentsUsingHashMapWithBlackList(EntityManager entityManager, EntityCommandBuffer ecb, Entity entity, ref NativeParallelHashMap<Entity, Entity> replacements, NativeArray<ComponentType> blacklist)
        {
            var ecbParallel = ecb.AsParallelWriter();
            var componentTypes = entityManager.GetComponentTypes(entity, Allocator.TempJob);
            NativeQueue<ComponentType> filteredQueue = new NativeQueue<ComponentType>(Allocator.TempJob);
            var filterJob = new FilterComponentTypesJob()
            {
                InputComponentTypes = componentTypes,
                BlackListedComponentTypes = blacklist,
                FilteredComponentsQueue = filteredQueue.AsParallelWriter(),
            };

            JobHandle jobHandle = filterJob.Schedule(componentTypes.Length, 4);//idk what value to put here honestly
            jobHandle.Complete();
            componentTypes.Dispose();
            var filteredcomponentTypes = new NativeArray<ComponentType>(filteredQueue.Count, Allocator.TempJob);
            int i = 0;
            while (filteredQueue.TryDequeue(out var componentType))
            {
                filteredcomponentTypes[i++] = componentType;
            }
            filteredQueue.Dispose();



            filteredcomponentTypes[0].TypeIndex








            var job = new ReplaceEntityFieldsInComponentJob()
            {
                ecbParallel = ecbParallel,
                componentTypes = filteredcomponentTypes,
            };
            jobHandle = job.Schedule(componentTypes.Length, 4);//idk what value to put here honestly
            jobHandle.Complete();
            filteredcomponentTypes.Dispose();









        }
        */
        /*
        [BurstCompile]
        public struct FilterComponentTypesJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<ComponentType> InputComponentTypes;
            [ReadOnly] public NativeArray<ComponentType> BlackListedComponentTypes;
            [WriteOnly] public NativeQueue<ComponentType>.ParallelWriter FilteredComponentsQueue;

            public void Execute(int index)
            {
                var componentType = InputComponentTypes[index];

                if (componentType.HasEntityReferences)
                {
                    bool isBlacklisted = false;
                    for (int i = 0; i < BlackListedComponentTypes.Length; i++)
                    {
                        if (componentType == BlackListedComponentTypes[i])
                        {
                            isBlacklisted = true;
                            break;
                        }
                    }
                    if (!isBlacklisted)
                    {
                        FilteredComponentsQueue.Enqueue(componentType);
                    }
                }
            }
        }




        [BurstCompile]
        public struct ReplaceEntityFieldsInComponentMapJob : IJobParallelFor
        {
            [WriteOnly]
            internal EntityCommandBuffer.ParallelWriter ecbParallel;
            [ReadOnly]
            internal NativeArray<ComponentType> componentTypes;
            [ReadOnly]
            internal NativeParallelHashMap<Entity,Entity> map;
            public void Execute(int index)
            {

            }
        }
        [BurstCompile]
        public struct ReplaceEntityFieldsInComponentJob : IJobParallelFor
        {
            [WriteOnly]
            internal EntityCommandBuffer.ParallelWriter ecbParallel;
            [ReadOnly]
            internal NativeArray<ComponentType> componentTypes;
            [ReadOnly]
            internal NativeArray<EntityRemap> map;
            public void Execute(int index)
            {
                ReplaceEntityInData()
            }
        }
        */






        /*

        public static Entity RemapEntity(NativeParallelHashMap<Entity, Entity> map, Entity source)
        {
            if (map.TryGetValue(source, out Entity entity))
            {
                return entity;
            }
            return source;
        }

        public static void ReplaceEntities(NativeParallelHashMap<Entity, Entity> map,
            EntityRemapUtility.EntityPatchInfo* scalarPatches, int scalarPatchCount,
                    EntityRemapUtility.BufferEntityPatchInfo* bufferPatches, int bufferPatchCount,
                    byte* chunkBuffer, int entityCount)
        {
            // Replace scalars (single components) with entity references.
            for (int p = 0; p < scalarPatchCount; p++)
            {
                byte* entityData = chunkBuffer + scalarPatches[p].Offset;
                for (int i = 0; i != entityCount; i++)
                {
                    Entity* entity = (Entity*)entityData;
                    *entity = RemapEntity(map, *entity);
                    entityData += scalarPatches[p].Stride;
                }
            }

            // Replace buffers that contain entity references
            for (int p = 0; p < bufferPatchCount; ++p)
            {
                byte* bufferData = chunkBuffer + bufferPatches[p].BufferOffset;

                for (int i = 0; i != entityCount; ++i)
                {
                    BufferHeader* header = (BufferHeader*)bufferData;

                    byte* elemsBase = BufferHeader.GetElementPointer(header) + bufferPatches[p].ElementOffset;
                    int elemCount = header->Length;

                    for (int k = 0; k != elemCount; ++k)
                    {
                        Entity* entityPtr = (Entity*)elemsBase;
                        *entityPtr = RemapEntity(map, *entityPtr);
                        elemsBase += bufferPatches[p].ElementStride;
                    }

                    bufferData += bufferPatches[p].BufferStride;
                }
            }
        }

        public struct EntityRemap
        {
            public Entity source;
            public Entity dest;
        }
        public static Entity RemapEntity(NativeArray<EntityRemap> map, int remappingCount, Entity source)
        {
            // When instantiating prefabs,
            // internal references are remapped.
            for (int i = 0; i != remappingCount; i++)
            {
                if (source == map[i].source)
                    return map[i].dest;
            }
            // And external references are kept.
            return source;
        }
        public static void ReplaceEntities(NativeArray<EntityRemap> map,
            EntityRemapUtility.EntityPatchInfo* scalarPatches, int scalarPatchCount,
                    EntityRemapUtility.BufferEntityPatchInfo* bufferPatches, int bufferPatchCount,
                    byte* chunkBuffer, int entityCount)
        {
            int length = map.Length;
            // Replace scalars (single components) with entity references.
            for (int p = 0; p < scalarPatchCount; p++)
            {
                byte* entityData = chunkBuffer + scalarPatches[p].Offset;
                for (int i = 0; i != entityCount; i++)
                {
                    Entity* entity = (Entity*)entityData;
                    *entity = RemapEntity(map, length, *entity);
                    entityData += scalarPatches[p].Stride;
                }
            }

            // Replace buffers that contain entity references
            for (int p = 0; p < bufferPatchCount; ++p)
            {
                byte* bufferData = chunkBuffer + bufferPatches[p].BufferOffset;

                for (int i = 0; i != entityCount; ++i)
                {
                    BufferHeader* header = (BufferHeader*)bufferData;

                    byte* elemsBase = BufferHeader.GetElementPointer(header) + bufferPatches[p].ElementOffset;
                    int elemCount = header->Length;

                    for (int k = 0; k != elemCount; ++k)
                    {
                        Entity* entityPtr = (Entity*)elemsBase;
                        *entityPtr = RemapEntity(map, length, *entityPtr);
                        elemsBase += bufferPatches[p].ElementStride;
                    }

                    bufferData += bufferPatches[p].BufferStride;
                }
            }
        }


        public static void ReplaceEntityInData(byte* data, TypeIndex typeIndex, NativeParallelHashMap<Entity, Entity> map)
        {
            //         if (!TypeManager.HasEntityReferences(typeIndex))
            //             return;

            var offsets = TypeManager.GetEntityOffsets(typeIndex, out var offsetCount);
            for (int i = 0; i < offsetCount; i++)
            {
                Entity* entity = (Entity*)(data + offsets[i].Offset);
                if (map.ContainsKey(*entity))
                {
                    *entity = map[*entity];
                }
            }
        }



        */












        [GenerateTestsForBurstCompatibility]
        public static unsafe void FindAndReplaceAllFieldsInEntityComponentsDictionaryWithBlackList<K>(EntityManager entityManager, EntityCommandBuffer ecb, Entity entity, Dictionary<K, K> replacements, NativeArray<ComponentType> blacklist) where K : unmanaged
        {
            ////Debug.Log($"looking at entity {entity.Index}:{entity.Version}");
            // Get all component types associated with the entity
            var componentTypes = entityManager.GetComponentTypes(entity);
            //entityManager.Ge

            // Iterate through all component types
            foreach (var componentType in componentTypes)
            {
                //Debug.Log($"looking to replace {componentType.GetDebugTypeName()}");
                bool isBlacklisted = false;
                for (int i = 0; i < blacklist.Length; i++)
                {
                    if (componentType == blacklist[i])
                    {
                        isBlacklisted = true;
                        break;
                    }
                }
                if (!isBlacklisted)
                {
                    var typeIndex = componentType.TypeIndex;
                    void* componentPtr;
                    try
                    {
                        // Get the raw pointer to the component data
                        componentPtr = entityManager.GetComponentDataRawRW(entity, typeIndex);
                    }
                    catch
                    {
                        //Debug.Log("Couldn't get the raw pointer to the component data");
                        continue;
                    }

                    if (componentPtr == null)
                    {
                        //Debug.Log("componentPtr == null");
                        continue;
                    }


                    // Get the type of the component
                    var componentDataType = componentType.GetManagedType();
                    if (componentType.IsZeroSized)
                    {
                        //Debug.Log("IsZeroSized");
                    }
                    else if (componentType.IsCleanupBufferComponent)
                    {
                        //Debug.Log("IsCleanupBufferComponent");
                        var genericMethod = typeof(EntityComponentAccess)
        .GetMethod(nameof(PrepareFindAndReplaceAllFieldsInCleanupBufferDictionarySimple), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
        .MakeGenericMethod(componentDataType, typeof(K));

                        // Convert the void* to IntPtr to pass it as an object
                        //IntPtr componentIntPtr = new IntPtr(componentPtr);
                        // Call the generic method and pass the EntityManager as well
                        genericMethod.Invoke(null, new object[] { entityManager, entity, replacements });
                    }
                    else if (componentType.IsBuffer)
                    {
                        //Debug.Log("IsBuffer");


                        var genericMethod = typeof(EntityComponentAccess)
        .GetMethod(nameof(PrepareFindAndReplaceAllFieldsInBufferDictionarySimple), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
        .MakeGenericMethod(componentDataType, typeof(K));

                        // Convert the void* to IntPtr to pass it as an object
                        // IntPtr componentIntPtr = new IntPtr(componentPtr);
                        // Call the generic method and pass the EntityManager as well
                        genericMethod.Invoke(null, new object[] { entityManager, entity, replacements });
                    }
                    else if (componentType.IsManagedComponent)
                    {
                        //Debug.Log("IsManagedComponent");
                        var genericMethod = typeof(EntityComponentAccess)
        .GetMethod(nameof(PrepareFindAndReplaceAllFieldsInManagedComponentECBDictionary), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
        .MakeGenericMethod(componentDataType, typeof(K));

                        // Convert the void* to IntPtr to pass it as an object
                        IntPtr componentIntPtr = new IntPtr(componentPtr);
                        // Call the generic method and pass the EntityManager as well
                        genericMethod.Invoke(null, new object[] { ecb, entity, componentIntPtr, replacements });
                    }
                    else if (componentType.IsCleanupSharedComponent)
                    {
                        //Debug.Log("IsCleanupSharedComponent");
                        var genericMethod = typeof(EntityComponentAccess)
        .GetMethod(nameof(PrepareFindAndReplaceAllFieldsInCleanupSharedComponentECBDictionary), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
        .MakeGenericMethod(componentDataType, typeof(K));

                        // Convert the void* to IntPtr to pass it as an object
                        IntPtr componentIntPtr = new IntPtr(componentPtr);
                        // Call the generic method and pass the EntityManager as well
                        genericMethod.Invoke(null, new object[] { ecb, entity, componentIntPtr, replacements });
                    }
                    else if (componentType.IsSharedComponent)
                    {
                        //Debug.Log("IsSharedComponent");
                        var genericMethod = typeof(EntityComponentAccess)
        .GetMethod(nameof(PrepareFindAndReplaceAllFieldsInSharedComponentECBDictionary), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
        .MakeGenericMethod(componentDataType, typeof(K));

                        // Convert the void* to IntPtr to pass it as an object
                        IntPtr componentIntPtr = new IntPtr(componentPtr);
                        // Call the generic method and pass the EntityManager as well
                        genericMethod.Invoke(null, new object[] { ecb, entity, componentIntPtr, replacements });
                    }
                    else if (componentType.IsChunkComponent)
                    {
                        //Debug.LogWarning("[EntityComponentAccess] ChunkComponent processing is not yet implemented!");
                    }
                    else if (componentType.IsCleanupComponent)
                    {
                        //Debug.Log("IsCleanupComponent");
                        var genericMethod = typeof(EntityComponentAccess)
        .GetMethod(nameof(PrepareFindAndReplaceAllFieldsInCleanupComponentDictionarySimple), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
        .MakeGenericMethod(componentDataType, typeof(K));

                        // Convert the void* to IntPtr to pass it as an object
                        IntPtr componentIntPtr = new IntPtr(componentPtr);
                        // Call the generic method and pass the EntityManager as well
                        genericMethod.Invoke(null, new object[] { entityManager, ecb, entity, replacements });
                    }
                    else if (componentType.IsComponent)
                    {
                        //Debug.Log("IsComponent");
                        // Use reflection to create a generic method for updating
                        var genericMethod = typeof(EntityComponentAccess)
                            .GetMethod(nameof(PrepareFindAndReplaceAllFieldsInComponentECBDictionarySimple), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
                            .MakeGenericMethod(componentDataType, typeof(K));

                        // Convert the void* to IntPtr to pass it as an object
                        IntPtr componentIntPtr = new IntPtr(componentPtr);
                        // Call the generic method and pass the EntityManager as well
                        genericMethod.Invoke(null, new object[] { entityManager, ecb, entity, replacements });
                    }
                    else
                    {
                        //Debug.Log("Else???");
                    }
                }
                else
                {
                    //Debug.Log($"found 1 blacklistedComponent, {componentType.GetDebugTypeName()}");
                }

            }
        }
        public static unsafe void FindAndReplaceAllFieldsInEntityComponentsDictionary<K>(EntityManager entityManager, EntityCommandBuffer ecb, Entity entity, Dictionary<K, K> replacements) where K : unmanaged
        {
            ////Debug.Log($"looking at entity {entity.Index}:{entity.Version}");
            // Get all component types associated with the entity
            var componentTypes = entityManager.GetComponentTypes(entity);
            //entityManager.Ge

            // Iterate through all component types
            foreach (var componentType in componentTypes)
            {
                //Debug.Log($"looking to replace {componentType.GetDebugTypeName()}");
                var typeIndex = componentType.TypeIndex;
                void* componentPtr;
                try
                {
                    // Get the raw pointer to the component data
                    componentPtr = entityManager.GetComponentDataRawRW(entity, typeIndex);
                }
                catch
                {
                    continue;
                }

                if (componentPtr == null)
                    continue;

                // Get the type of the component
                var componentDataType = componentType.GetManagedType();
                if (componentType.IsZeroSized)
                {

                }
                else if (componentType.IsCleanupBufferComponent)
                {
                    var genericMethod = typeof(EntityComponentAccess)
    .GetMethod(nameof(PrepareFindAndReplaceAllFieldsInCleanupBufferDictionarySimple), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
    .MakeGenericMethod(componentDataType, typeof(K));

                    // Convert the void* to IntPtr to pass it as an object
                    //IntPtr componentIntPtr = new IntPtr(componentPtr);
                    // Call the generic method and pass the EntityManager as well
                    genericMethod.Invoke(null, new object[] { entityManager, entity, replacements });
                }
                else if (componentType.IsBuffer)
                {



                    var genericMethod = typeof(EntityComponentAccess)
    .GetMethod(nameof(PrepareFindAndReplaceAllFieldsInBufferDictionarySimple), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
    .MakeGenericMethod(componentDataType, typeof(K));

                    // Convert the void* to IntPtr to pass it as an object
                    //IntPtr componentIntPtr = new IntPtr(componentPtr);
                    // Call the generic method and pass the EntityManager as well
                    genericMethod.Invoke(null, new object[] { entityManager, entity, replacements });
                }
                else if (componentType.IsManagedComponent)
                {
                    var genericMethod = typeof(EntityComponentAccess)
    .GetMethod(nameof(PrepareFindAndReplaceAllFieldsInManagedComponentECBDictionary), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
    .MakeGenericMethod(componentDataType, typeof(K));

                    // Convert the void* to IntPtr to pass it as an object
                    IntPtr componentIntPtr = new IntPtr(componentPtr);
                    // Call the generic method and pass the EntityManager as well
                    genericMethod.Invoke(null, new object[] { ecb, entity, componentIntPtr, replacements });
                }
                else if (componentType.IsCleanupSharedComponent)
                {
                    var genericMethod = typeof(EntityComponentAccess)
    .GetMethod(nameof(PrepareFindAndReplaceAllFieldsInCleanupSharedComponentECBDictionary), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
    .MakeGenericMethod(componentDataType, typeof(K));

                    // Convert the void* to IntPtr to pass it as an object
                    IntPtr componentIntPtr = new IntPtr(componentPtr);
                    // Call the generic method and pass the EntityManager as well
                    genericMethod.Invoke(null, new object[] { ecb, entity, componentIntPtr, replacements });
                }
                else if (componentType.IsSharedComponent)
                {
                    var genericMethod = typeof(EntityComponentAccess)
    .GetMethod(nameof(PrepareFindAndReplaceAllFieldsInSharedComponentECBDictionary), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
    .MakeGenericMethod(componentDataType, typeof(K));

                    // Convert the void* to IntPtr to pass it as an object
                    IntPtr componentIntPtr = new IntPtr(componentPtr);
                    // Call the generic method and pass the EntityManager as well
                    genericMethod.Invoke(null, new object[] { ecb, entity, componentIntPtr, replacements });
                }
                else if (componentType.IsChunkComponent)
                {
                    //Debug.LogWarning("[EntityComponentAccess] ChunkComponent processing is not yet implemented!");
                }
                else if (componentType.IsCleanupComponent)
                {
                    var genericMethod = typeof(EntityComponentAccess)
    .GetMethod(nameof(PrepareFindAndReplaceAllFieldsInCleanupComponentDictionarySimple), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
    .MakeGenericMethod(componentDataType, typeof(K));

                    // Convert the void* to IntPtr to pass it as an object
                    IntPtr componentIntPtr = new IntPtr(componentPtr);
                    // Call the generic method and pass the EntityManager as well
                    genericMethod.Invoke(null, new object[] { entityManager, ecb, entity, replacements });
                }
                else if (componentType.IsComponent)
                {
                    // Use reflection to create a generic method for updating
                    var genericMethod = typeof(EntityComponentAccess)
                        .GetMethod(nameof(PrepareFindAndReplaceAllFieldsInComponentECBDictionarySimple), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
                        .MakeGenericMethod(componentDataType, typeof(K));

                    // Convert the void* to IntPtr to pass it as an object
                    IntPtr componentIntPtr = new IntPtr(componentPtr);
                    // Call the generic method and pass the EntityManager as well
                    genericMethod.Invoke(null, new object[] { entityManager, ecb, entity, replacements });
                }



            }
        }
        private static unsafe void PrepareFindAndReplaceAllFieldsInComponentECBDictionary<T, K>(EntityCommandBuffer ecb, Entity entity, IntPtr componentIntPtr, Dictionary<K, K> replacements) where T : unmanaged, IComponentData
        {
            // Get the pointer to the component data
            void* componentPtr = componentIntPtr.ToPointer();
            ref T component = ref UnsafeUtility.AsRef<T>(componentPtr);

            // Apply replacements (your existing logic)
            FindAndReplaceAllFieldsInComponentDictionary(ref component, replacements);

            // Instead of using dynamic, use reflection to call the SetComponent method with the correct type
            var boxedComponent = (IComponentData)(object)component;
            Type componentType = boxedComponent.GetType();

            // Find the SetComponent<T>() method using reflection
            var method = typeof(EntityCommandBuffer).GetMethod("SetComponent").MakeGenericMethod(componentType);

            // Invoke the method with the correct type
            method.Invoke(ecb, new object[] { entity, boxedComponent });
        }
        /*
        private static unsafe void PrepareFindAndReplaceAllFieldsInComponentECBDictionary<T, K>(EntityCommandBuffer ecb, Entity entity, IntPtr componentIntPtr, Dictionary<K, K> replacements) where T : unmanaged, IComponentData
        {
            // Convert IntPtr back to void* and then to a reference of the actual component type
            void* componentPtr = componentIntPtr.ToPointer();
            ref T component = ref UnsafeUtility.AsRef<T>(componentPtr);

            // Apply the update logic
            FindAndReplaceAllFieldsInComponentDictionary(ref component, replacements);

            // Now that the component is updated, we need to cast it to an IComponentData to call SetComponentData
            var boxedComponent = (IComponentData)(object)component;

            // Use EntityManager's SetComponentObjectData to set the updated component data
            ecb.SetComponent(entity, (dynamic)boxedComponent);
        }
        */
        private static unsafe void PrepareFindAndReplaceAllFieldsInCleanupBufferDictionary<T, K>(EntityManager entityManager, Entity entity, IntPtr componentIntPtr, Dictionary<K, K> replacements) where T : unmanaged, ICleanupBufferElementData
        {
            void* componentPtr = componentIntPtr.ToPointer();

            ref T component = ref UnsafeUtility.AsRef<T>(componentPtr);
            var typeIndex = TypeManager.GetTypeIndex(typeof(T));
            BufferHeader* bufferHeader = (BufferHeader*)componentPtr;
            var componentType = entityManager.GetComponentTypeHandle<T>(false);
            var componentTypeInfo = TypeManager.GetTypeInfo(typeIndex);
            int elementSize = componentTypeInfo.ElementSize;
            int bufferLength = bufferHeader->Length;
            void* bufferDataPtr = BufferHeader.GetElementPointer(bufferHeader);

            for (int i = 0; i < bufferLength; i++)
            {
                void* elementPtr = (byte*)bufferDataPtr + (i * elementSize);

                // Process the elements
                var element = UnsafeUtility.ReadArrayElement<T>(bufferDataPtr, i);
                FindAndReplaceAllFieldsInComponentDictionary(ref element, replacements);
                UnsafeUtility.WriteArrayElement(bufferDataPtr, i, element);
            }






            // Apply the update logic

            ////Debug.LogWarning($"Dont know where to put this buffer {component.GetType().Name}");
        }
        private static unsafe void PrepareFindAndReplaceAllFieldsInCleanupComponentDictionary<T, K>(EntityManager entityManager, Entity entity, IntPtr componentIntPtr, Dictionary<K, K> replacements) where T : unmanaged, ICleanupComponentData
        {
            // Convert IntPtr back to void* and then to a reference of the actual component type
            void* componentPtr = componentIntPtr.ToPointer();
            ref T component = ref UnsafeUtility.AsRef<T>(componentPtr);

            // Apply the update logic
            FindAndReplaceAllFieldsInComponentDictionary(ref component, replacements);

            // Now that the component is updated, we need to cast it to an IComponentData to call SetComponentData
            var boxedComponent = (ICleanupComponentData)(object)component;

            // Use EntityManager's SetComponentObjectData to set the updated component data
            entityManager.SetComponentData(entity, (dynamic)boxedComponent);
        }

        private static unsafe void PrepareFindAndReplaceAllFieldsInCleanupComponentECBDictionary<T, K>(EntityCommandBuffer ecb, Entity entity, IntPtr componentIntPtr, Dictionary<K, K> replacements) where T : unmanaged, ICleanupComponentData
        {
            // Convert IntPtr back to void* and then to a reference of the actual component type
            void* componentPtr = componentIntPtr.ToPointer();
            ref T component = ref UnsafeUtility.AsRef<T>(componentPtr);

            // Apply the update logic
            FindAndReplaceAllFieldsInComponentDictionary(ref component, replacements);

            // Now that the component is updated, we need to cast it to an IComponentData to call SetComponentData
            var boxedComponent = (ICleanupComponentData)(object)component;

            // Use EntityManager's SetComponentObjectData to set the updated component data
            ecb.SetComponent(entity, (dynamic)boxedComponent);
        }
        private static unsafe void PrepareFindAndReplaceAllFieldsInCleanupSharedComponentDictionary<T, K>(EntityManager entityManager, Entity entity, IntPtr componentIntPtr, Dictionary<K, K> replacements) where T : unmanaged, ICleanupSharedComponentData
        {
            // Convert IntPtr back to void* and then to a reference of the actual component type
            void* componentPtr = componentIntPtr.ToPointer();
            ref T component = ref UnsafeUtility.AsRef<T>(componentPtr);
            // Apply the update logic
            FindAndReplaceAllFieldsInComponentDictionary(ref component, replacements);

            // Now that the component is updated, we need to cast it to an IComponentData to call SetComponentData
            var boxedComponent = (ICleanupSharedComponentData)(object)component;

            // Use EntityManager's SetComponentObjectData to set the updated component data
            entityManager.SetComponentData(entity, (dynamic)boxedComponent);
        }
        private static unsafe void PrepareFindAndReplaceAllFieldsInCleanupSharedComponentECBDictionary<T, K>(EntityCommandBuffer ecb, Entity entity, IntPtr componentIntPtr, Dictionary<K, K> replacements) where T : unmanaged, ICleanupSharedComponentData
        {
            // Convert IntPtr back to void* and then to a reference of the actual component type
            void* componentPtr = componentIntPtr.ToPointer();
            ref T component = ref UnsafeUtility.AsRef<T>(componentPtr);
            // Apply the update logic
            FindAndReplaceAllFieldsInComponentDictionary(ref component, replacements);

            // Now that the component is updated, we need to cast it to an IComponentData to call SetComponentData
            var boxedComponent = (ICleanupSharedComponentData)(object)component;

            // Use EntityManager's SetComponentObjectData to set the updated component data
            ecb.SetComponent(entity, (dynamic)boxedComponent);
        }
        private static unsafe void PrepareFindAndReplaceAllFieldsInBufferECBDictionarySimple<T, K>(EntityManager entityManager, EntityCommandBuffer ecb, Entity entity, Dictionary<K, K> replacements) where T : unmanaged, IBufferElementData
        {
            var buffer = entityManager.GetBuffer<T>(entity);
            NativeArray<T> values = new NativeArray<T>(buffer.Capacity, Allocator.Temp);

            for (int i = 0; i < buffer.Length; i++)
            {
                values[i] = FindAndReplaceAllFieldsInComponentDictionary(buffer[i], replacements);
            }
            buffer.Clear();
            for (int i = 0; i < values.Length; i++)
            {
                //Debug.Log($"Trying to add {values[i]} back");
                buffer.Add(values[i]);
            }
            for (int i = 0; i < values.Length; i++)
            {
                //Debug.Log($"Trying to add {values[i]} again");
                ecb.AppendToBuffer<T>(entity, values[i]);
            }
        }
        private static unsafe void PrepareFindAndReplaceAllFieldsInComponentECBDictionarySimple<T, K>(EntityManager entityManager, EntityCommandBuffer ecb, Entity entity, Dictionary<K, K> replacements) where T : unmanaged, IComponentData
        {
            var component = entityManager.GetComponentData<T>(entity);

            component = FindAndReplaceAllFieldsInComponentDictionary(component, replacements);
            ecb.SetComponent(entity, component);
        }
        private static unsafe void PrepareFindAndReplaceAllFieldsInCleanupComponentDictionarySimple<T, K>(EntityManager entityManager, EntityCommandBuffer ecb, Entity entity, Dictionary<K, K> replacements) where T : unmanaged, ICleanupComponentData
        {
            var component = entityManager.GetComponentData<T>(entity);

            component = FindAndReplaceAllFieldsInComponentDictionary(component, replacements);
            ecb.SetComponent(entity, component);
        }
        private static unsafe void PrepareFindAndReplaceAllFieldsInCleanupBufferDictionarySimple<T, K>(EntityManager entityManager, Entity entity, Dictionary<K, K> replacements) where T : unmanaged, ICleanupBufferElementData
        {
            var buffer = entityManager.GetBuffer<T>(entity);
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = FindAndReplaceAllFieldsInComponentDictionary(buffer[i], replacements);
            }


        }
        private static unsafe void PrepareFindAndReplaceAllFieldsInBufferDictionarySimple<T, K>(EntityManager entityManager, Entity entity, Dictionary<K, K> replacements) where T : unmanaged, IBufferElementData
        {
            var buffer = entityManager.GetBuffer<T>(entity);
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = FindAndReplaceAllFieldsInComponentDictionary(buffer[i], replacements);
            }


        }
        /*


// Convert IntPtr back to void* and then to a reference of the actual component type
void* componentPtr = componentIntPtr.ToPointer();

ref T component = ref UnsafeUtility.AsRef<T>(componentPtr);
var typeIndex = TypeManager.GetTypeIndex(typeof(T));
BufferHeader* bufferHeader = (BufferHeader*)componentPtr;
var componentType = entityManager.GetComponentTypeHandle<T>(false);
var componentTypeInfo = TypeManager.GetTypeInfo(typeIndex);
int elementSize = componentTypeInfo.ElementSize;
int bufferLength = bufferHeader->Length;
void* bufferDataPtr = BufferHeader.GetElementPointer(bufferHeader);

for (int i = 0; i < bufferLength; i++)
{
    void* elementPtr = (byte*)bufferDataPtr + (i * elementSize);

    // Process the elements
    var element = UnsafeUtility.ReadArrayElement<T>(bufferDataPtr, i);
    FindAndReplaceAllFieldsInComponentDictionary(ref element, replacements);
    UnsafeUtility.WriteArrayElement(bufferDataPtr, i, element);
}

ecb.SetBuffer<T>(entity);



*/
        // Apply the update logic

        ////Debug.LogWarning($"Dont know where to put this buffer {component.GetType().Name}");
        // Now that the component is updated, we need to cast it to an IComponentData to call SetComponentData
        //var boxedComponent = (IBufferElementData)(object)component;

        // Use EntityManager's SetComponentObjectData to set the updated component data
        //ecb.SetBuffer(entity, (dynamic)boxedComponent);
        private static unsafe void PrepareFindAndReplaceAllFieldsInBufferDictionary<T, K>(EntityManager entityManager, Entity entity, IntPtr componentIntPtr, Dictionary<K, K> replacements) where T : unmanaged, IBufferElementData
        {
            // Convert IntPtr back to void* and then to a reference of the actual component type
            void* componentPtr = componentIntPtr.ToPointer();

            ref T component = ref UnsafeUtility.AsRef<T>(componentPtr);
            var typeIndex = TypeManager.GetTypeIndex(typeof(T));
            BufferHeader* bufferHeader = (BufferHeader*)componentPtr;
            var componentType = entityManager.GetComponentTypeHandle<T>(false);
            var componentTypeInfo = TypeManager.GetTypeInfo(typeIndex);
            int elementSize = componentTypeInfo.ElementSize;
            int bufferLength = bufferHeader->Length;
            void* bufferDataPtr = BufferHeader.GetElementPointer(bufferHeader);

            for (int i = 0; i < bufferLength; i++)
            {
                void* elementPtr = (byte*)bufferDataPtr + (i * elementSize);

                // Process the elements
                var element = UnsafeUtility.ReadArrayElement<T>(bufferDataPtr, i);
                FindAndReplaceAllFieldsInComponentDictionary(ref element, replacements);
                UnsafeUtility.WriteArrayElement(bufferDataPtr, i, element);
            }






            // Apply the update logic

            //Debug.LogWarning($"Dont know where to put this buffer {component.GetType().Name}");
            // Now that the component is updated, we need to cast it to an IComponentData to call SetComponentData
            //var boxedComponent = (IBufferElementData)(object)component;

            // Use EntityManager's SetComponentObjectData to set the updated component data
            //ecb.SetBuffer(entity, (dynamic)boxedComponent);
        }
        private static unsafe void PrepareFindAndReplaceAllFieldsInSharedComponentDictionary<T, K>(EntityManager entityManager, Entity entity, IntPtr componentIntPtr, Dictionary<K, K> replacements) where T : unmanaged, ISharedComponentData
        {
            // Convert IntPtr back to void* and then to a reference of the actual component type
            void* componentPtr = componentIntPtr.ToPointer();
            ref T component = ref UnsafeUtility.AsRef<T>(componentPtr);

            // Apply the update logic
            FindAndReplaceAllFieldsInComponentDictionary(ref component, replacements);

            // Now that the component is updated, we need to cast it to an IComponentData to call SetComponentData
            var boxedComponent = (ISharedComponentData)(object)component;

            // Use EntityManager's SetComponentObjectData to set the updated component data
            entityManager.SetComponentData(entity, (dynamic)boxedComponent);
        }

        private static unsafe void PrepareFindAndReplaceAllFieldsInSharedComponentECBDictionary<T, K>(EntityCommandBuffer ecb, Entity entity, IntPtr componentIntPtr, Dictionary<K, K> replacements) where T : unmanaged, ISharedComponentData
        {
            // Convert IntPtr back to void* and then to a reference of the actual component type
            void* componentPtr = componentIntPtr.ToPointer();
            ref T component = ref UnsafeUtility.AsRef<T>(componentPtr);

            // Apply the update logic
            FindAndReplaceAllFieldsInComponentDictionary(ref component, replacements);

            // Now that the component is updated, we need to cast it to an IComponentData to call SetComponentData
            var boxedComponent = (ISharedComponentData)(object)component;

            // Use EntityManager's SetComponentObjectData to set the updated component data
            ecb.SetComponent(entity, (dynamic)boxedComponent);
        }
        private static unsafe void PrepareFindAndReplaceAllFieldsInManagedComponentECBDictionary<T, K>(EntityCommandBuffer ecb, Entity entity, IntPtr componentIntPtr, Dictionary<K, K> replacements) where T : class, IComponentData
        {
            //Debug.LogWarning("[EntityComponentAccess] ManagedComponent processing is not yet implemented!");
        }
        private static unsafe void PrepareFindAndReplaceAllFieldsInManagedComponentDictionary<T, K>(EntityManager entityManager, Entity entity, IntPtr componentIntPtr, Dictionary<K, K> replacements) where T : class, IComponentData
        {
            //Debug.LogWarning("[EntityComponentAccess] ManagedComponent processing is not yet implemented!");
        }
        private static T FindAndReplaceAllFieldsInComponentDictionary<T, K>(T component, Dictionary<K, K> replacements)
            where T : unmanaged
        {
            var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                var fieldType = field.FieldType;

                if (fieldType == typeof(K))
                {
                    var currentValue = (K)field.GetValueDirect(__makeref(component));
                    ////Debug.Log($"in this nested struct we found a field with entity{currentEntity.Index}:{currentEntity.Version}");
                    if (replacements.TryGetValue(currentValue, out var replacementValue))
                    {
                        //Debug.LogWarning($"Found a replacement for {currentValue}, replacing with {replacementValue}");
                        field.SetValueDirect(__makeref(component), replacementValue);
                    }
                }
                else if (fieldType.IsArray)
                {
                    field.SetValueDirect(__makeref(component), FindAndReplaceAllFieldsInArrayFieldDictionary(field.GetValueDirect(__makeref(component)), replacements));
                }
                else if (fieldType.IsValueType && !fieldType.IsPrimitive && !fieldType.IsEnum)
                {
                    field.SetValueDirect(__makeref(component), FindAndReplaceAllFieldsInNestedStructDictionary(field.GetValueDirect(__makeref(component)), replacements));
                }
                else if (typeof(IDictionary).IsAssignableFrom(fieldType))
                {
                    field.SetValueDirect(__makeref(component), HandleDictionaryField(field.GetValueDirect(__makeref(component)), replacements));
                }
            }
            return component;
        }
        private static void FindAndReplaceAllFieldsInComponentDictionary<T, K>(ref T component, Dictionary<K, K> replacements)
            where T : unmanaged
        {
            var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                var fieldType = field.FieldType;

                if (fieldType == typeof(K))
                {
                    var currentValue = (K)field.GetValueDirect(__makeref(component));
                    ////Debug.Log($"in this nested struct we found a field with entity{currentEntity.Index}:{currentEntity.Version}");
                    if (replacements.TryGetValue(currentValue, out var replacementValue))
                    {
                        //Debug.LogWarning($"Found a replacement for {currentValue}, replacing with {replacementValue}");
                        field.SetValueDirect(__makeref(component), replacementValue);
                    }
                }
                else if (fieldType.IsArray)
                {
                    field.SetValueDirect(__makeref(component), FindAndReplaceAllFieldsInArrayFieldDictionary(field.GetValueDirect(__makeref(component)), replacements));
                }
                else if (fieldType.IsValueType && !fieldType.IsPrimitive && !fieldType.IsEnum)
                {
                    field.SetValueDirect(__makeref(component), FindAndReplaceAllFieldsInNestedStructDictionary(field.GetValueDirect(__makeref(component)), replacements));
                }
                else if (typeof(IDictionary).IsAssignableFrom(fieldType))
                {
                    field.SetValueDirect(__makeref(component), HandleDictionaryField(field.GetValueDirect(__makeref(component)), replacements));
                }
            }
        }




        private static object FindAndReplaceAllFieldsInNestedStructDictionary<T>(object fieldValue, Dictionary<T, T> replacements)
        {
            var fields = fieldValue.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                var fieldType = field.FieldType;

                // Handle generic type T
                if (fieldType == typeof(T))
                {
                    var currentValue = (T)field.GetValue(fieldValue);
                    ////Debug.Log($"In this nested struct we found a field with type {typeof(T)}: {currentValue}");

                    // Check if the field value exists in the replacements dictionary
                    if (replacements.TryGetValue(currentValue, out var replacementValue))
                    {
                        //Debug.Log($"Found a replacement for {currentValue}, replacing with {replacementValue}");
                        field.SetValue(fieldValue, replacementValue);
                    }
                }
                else if (fieldType.IsArray)
                {
                    field.SetValue(fieldValue, FindAndReplaceAllFieldsInArrayFieldDictionary(field.GetValue(fieldValue), replacements));
                }
                else if (fieldType.IsValueType && !fieldType.IsPrimitive && !fieldType.IsEnum)
                {
                    // Recursively process nested structs
                    field.SetValue(fieldValue, FindAndReplaceAllFieldsInNestedStructDictionary(field.GetValue(fieldValue), replacements));
                }
                else if (typeof(IDictionary).IsAssignableFrom(fieldType))
                {
                    field.SetValue(fieldValue, HandleDictionaryField(field.GetValue(fieldValue), replacements));
                }
            }
            return (fieldValue);
            // Set the modified field value back to the component
            //fieldStruct.SetValue(component, fieldValue);
        }

        private static object FindAndReplaceAllFieldsInArrayFieldDictionary<T>(object fieldValue, Dictionary<T, T> replacements)
        {
            var arrayValue = (Array)fieldValue;
            var elementType = arrayValue.GetType().GetElementType();

            if (elementType == typeof(T))
            {
                for (int i = 0; i < arrayValue.Length; i++)
                {
                    var currentElement = (T)arrayValue.GetValue(i);
                    if (replacements.TryGetValue(currentElement, out var replacementValue))
                    {
                        arrayValue.SetValue(replacementValue, i);
                    }
                }
            }
            else if (elementType.IsValueType && !elementType.IsPrimitive && !elementType.IsEnum)
            {
                // Recursively process each struct element in the array
                for (int i = 0; i < arrayValue.Length; i++)
                {
                    var element = arrayValue.GetValue(i);
                    arrayValue.SetValue(FindAndReplaceAllFieldsInNestedStructDictionary(element, replacements), i);
                }
            }
            return arrayValue;
        }

        private static object HandleDictionaryField<T>(object fieldValue, Dictionary<T, T> replacements)
        {
            var dictionaryValue = (IDictionary)fieldValue;
            var keys = new ArrayList(dictionaryValue.Keys);
            var newDictionaryValue = dictionaryValue;
            foreach (var key in keys)
            {
                var value = dictionaryValue[key];
                var keyType = key.GetType();
                var valueType = value.GetType();
                object newKey = key;
                object newValue = value;
                if (keyType == typeof(T))
                {
                    if (replacements.TryGetValue((T)key, out var replacementKey))
                    {
                        newKey = replacementKey;
                    }
                }
                else if (keyType.IsValueType && !keyType.IsPrimitive && !keyType.IsEnum)
                {
                    // Recursively handle key or value if they are structs


                    newValue = FindAndReplaceAllFieldsInNestedStructDictionary(key, replacements);

                }
                if (valueType == typeof(T))
                {
                    if (replacements.TryGetValue((T)value, out var replacementValue))
                    {
                        newValue = replacementValue;
                    }
                }
                else if (valueType.IsValueType && !valueType.IsPrimitive && !valueType.IsEnum)
                {


                    newKey = FindAndReplaceAllFieldsInNestedStructDictionary(value, replacements);

                }
                newDictionaryValue[newKey] = newValue;
                // Handle key replacement if key type is T


                // Handle value replacement if value type is T



            }
            return newDictionaryValue;
        }

    }

}