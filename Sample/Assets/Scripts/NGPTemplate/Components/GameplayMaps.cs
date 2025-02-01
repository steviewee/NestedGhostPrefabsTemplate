/*
using Unity.Entities;
using Unity.NetCode;
namespace NGPTemplate.Components
{
    /// <summary>
    /// This buffer is used to map clients using their <see cref="NetworkId"/> index as a key and this struct as a value,
    /// making it easy to find Entities relating to that specific client.
    /// </summary>
    public struct GameplayMaps : IBufferElementData
    {
        /// <summary>The <see cref="NetworkStreamConnection"/> entity for this <see cref="NetworkId"/> index.</summary>
        public Entity ConnectionEntity;
        /// <summary>The <see cref="FirstPersonPlayer"/> entity for this <see cref="NetworkId"/> index.</summary>
        public Entity FirstPersonPlayersEntity;
        /// <summary>The <see cref="FirstPersonCharacterControl"/> entity for this <see cref="NetworkId"/> index.</summary>
        public Entity CharacterControllerEntity;

        /// <summary>If != default, need to remap this to the entity.</summary>
        public NetworkId RemapTo;
    }
}
*/