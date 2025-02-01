using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using System;
using Unity.NetCode;
using System.Runtime.InteropServices;
using Unity.Burst;
using UnityEditor;
namespace Zhorman.NestedGhostPrefabs.Runtime.Components
{
    [GhostComponent(PrefabType =GhostPrefabType.All, SendDataForChildEntity = true)]
    public struct NestedGhostAdditionalData : IComponentData
    {
        public bool NetworkedParenting;

        public bool ShouldBeUnParented;

        public bool UseImmitatedParenting;

        public bool Unnested;

        public bool NonGhostRereferencing;

        public bool Rereferencing;
        [GhostField]
        public bool waited;
    }
    public struct RelinkNonGhostChildrenReference : IBufferElementData
    {
        public int Value;
        public Entity Reference;
    }
    public struct RootEntityLink : IComponentData
    {
        public Entity Value;
    }
    public struct GhostRoot : IComponentData
    {

    }
    public struct GhostGroupAddChild : IComponentData
    {

    }
    [GhostComponent(SendDataForChildEntity = true)]//idk
    [GhostEnabledBit]
    public struct DesiredParent : IComponentData, IEnableableComponent
    {
        [GhostField]
        public Entity NextParent;
        [GhostField]
        public Entity PreviousParent;
        [GhostField]
        public bool shouldBeUnParented;
    }


}