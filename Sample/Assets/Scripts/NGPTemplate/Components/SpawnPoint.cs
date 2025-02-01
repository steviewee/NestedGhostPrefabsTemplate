  using Unity.Entities;
using UnityEngine;

namespace NGPTemplate.Components
{
  
    /// <summary>
    /// Placed in the GameScene subscene, the SpawnPoint components are used by the <see cref="ServerGameSystem"/>
    /// to spawn player characters during a game session.
    /// </summary>
    public struct SpawnPoint : IComponentData
    {
    }
}