using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace NGPTemplate.Components
{
    /// <summary>
    /// Client to server. Required by the server.
    /// TODO - Server should time out players who don't send this.
    /// </summary>
    public struct ClientJoinRequestRpc : IRpcCommand
    {
        public FixedString128Bytes PlayerName;

        public JoinType joinType;
    }
    public enum JoinType
    {
        None,//Something broke
        Player,
        Spectator,
    }
}