using UnityEngine;
using Unity.Entities;
namespace NGPTemplate.Components
{
    /// <summary>
    /// Placed in the GameScene subscene, the SpectatorSpawnPoint components are used by the <see cref="ClientGameSystem"/>
    /// to spawn the spectator controller during a game session.
    /// </summary>
    public struct SpectatorSpawnPoint : IComponentData
    {
    }
}