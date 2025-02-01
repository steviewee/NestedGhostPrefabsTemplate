using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using System;
using Unity.Transforms;
using Unity.Collections;
using NGPTemplate.Components;
namespace NGPTemplate.Authoring
{
    public class QolNameAuthoring : MonoBehaviour
    {
        public string Name;
        public void SetName()
        {
            Name = gameObject.name;
        }
        //[BakingVersion("megacity-metro", 2)]
        public class QolNameBaker : Baker<QolNameAuthoring>
        {
            public override void Bake(QolNameAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.None);
                if(authoring.Name == null) {
                    AddComponent(entity, new QolNameComponent
                    {
                        name = authoring.gameObject.name,
                    });
                }
                else
                {
                    AddComponent(entity, new QolNameComponent
                    {
                        name = authoring.Name
                    });
                }

            }
        }

    }

}
