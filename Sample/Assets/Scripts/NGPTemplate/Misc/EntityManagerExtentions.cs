
/*
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Profiling;
using UnityEngine.Scripting;
using UnityEngine.TestTools;


namespace NGPTemplate.Misc
{
    public static unsafe class EntityManagerExtentions
    {
        public static unsafe void RemapEntitiesForDiffer(this EntityManager entityManager,
                    NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapping,
                    NativeArray<ArchetypeChunk> srcChunks,
                    NativeArray<ArchetypeChunk> dstChunks)
        {
            var dstEntityComponentStore = entityManager.GetCheckedEntityDataAccess()->EntityComponentStore;

            new RemapEntitiesManagedForDiffer
            {
                DstChunks = dstChunks,
                EntityRemapping = entityRemapping,
                EntityComponentStore = dstEntityComponentStore
            }.Run();

            new RemapEntitiesUnmanagedForDiffer
            {
                dstEntityComponentStore = dstEntityComponentStore,
                DstChunks = dstChunks,
                EntityRemapping = entityRemapping
            }.Run(dstChunks.Length);
        }
        [BurstCompile]
        struct RemapEntitiesManagedForDiffer : IJob
        {
            public NativeArray<ArchetypeChunk> DstChunks;
            public NativeArray<EntityRemapUtility.EntityRemapInfo> EntityRemapping;
            [NativeDisableUnsafePtrRestriction]
            public EntityComponentStore* EntityComponentStore;

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
        struct RemapEntitiesUnmanagedForDiffer : IJobParallelFor
        {
            [Collections.ReadOnly] public NativeArray<EntityRemapUtility.EntityRemapInfo> EntityRemapping;
            [Collections.ReadOnly] public NativeArray<ArchetypeChunk> DstChunks;

            [NativeDisableUnsafePtrRestriction] public EntityComponentStore* dstEntityComponentStore;

            public void Execute(int index)
            {
                var remapChunk = DstChunks[index];
                var chunk = remapChunk.m_Chunk;
                Archetype* dstArchetype = remapChunk.Archetype.Archetype;

                var entityCount = chunk.Count;
                EntityRemapUtility.PatchEntities(dstArchetype->ScalarEntityPatches + 1,
                    dstArchetype->ScalarEntityPatchCount - 1, dstArchetype->BufferEntityPatches,
                    dstArchetype->BufferEntityPatchCount, chunk.Buffer, entityCount, ref EntityRemapping);
            }
        }
        /// <summary>
        /// Remaps a source Entity using the <see cref="EntityRemapInfo"/> array.
        /// </summary>
        /// <param name="remapping">The array of <see cref="EntityRemapInfo"/> used to perform the remapping.</param>
        /// <param name="source">The source Entity to remap.</param>
        /// <returns>Returns the remapped Entity ID if it is valid in the current world, otherwise returns Entity.Null.</returns>
        public static Entity RemapEntity(ref NativeArray<EntityRemapInfo> remapping, Entity source)
        {
            return RemapEntity((EntityRemapInfo*)remapping.GetUnsafeReadOnlyPtr(), source);
        }

        /// <summary>
        /// Remaps an entity using the <see cref="EntityRemapInfo"/> array.
        /// </summary>
        /// <param name="remapping">The array of <see cref="EntityRemapInfo"/> used to perform the remapping.</param>
        /// <param name="source">The source Entity to remap.</param>
        /// <returns>Returns the remapped Entity ID if it is valid in the current world, otherwise returns Entity.Null.</returns>
        public static Entity RemapEntity(EntityRemapInfo* remapping, Entity source)
        {
            if (source.Version == remapping[source.Index].SourceVersion)
                return remapping[source.Index].Target;
            else
            {
                // When moving whole worlds, we do not allow any references that aren't in the new world
                // to avoid any kind of accidental references
                return Entity.Null;
            }
        }

    }
}
*/