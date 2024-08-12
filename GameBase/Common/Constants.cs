
using System.Text;

namespace Project.Common
{
    public enum eGameServerStatus
    {
        /// <summary>
        /// Server is open for connections
        /// </summary>
        GSS_Open = 0,
        /// <summary>
        /// Server is closed and won't accept connections
        /// </summary>
        GSS_Closed,
        /// <summary>
        /// Server is down
        /// </summary>
        GSS_Down,
        /// <summary>
        /// Server is full, no more connections accepted
        /// </summary>
        GSS_Full,
        /// <summary>
        /// Unknown server status
        /// </summary>
        GSS_Unknown,
        /// <summary>
        /// Server is banned for the user
        /// </summary>
        GSS_Banned,
        /// <summary>
        /// User is not invited
        /// </summary>
        GSS_NotInvited,
        /// <summary>
        /// The count of server stati
        /// </summary>
        _GSS_Count,
    }

    /// <summary>
    /// The different game server types
    /// </summary>
    public enum eGameServerType
    {
        /// <summary>
        /// Normal server
        /// </summary>
        GST_Normal = 0,
        /// <summary>
        /// Test server
        /// </summary>
        GST_Test = 1,
        /// <summary>
        /// The count of server types
        /// </summary>
        _GST_Count = 2,
    }
    
    public static class Constants
    {
        /// <summary>
        /// The size of the send buffer for a client socket.
        /// </summary>
        public const int SendBufferSize = 16 * 1024;

        /// <summary>
        /// The size of the receive buffer for a client socket.
        /// </summary>
        public const int ReceiveBufferSize = 16 * 1024;

        /// <summary>
        /// Whether or not to disable the Nagle algorithm. (TCP_NODELAY)
        /// </summary>
        public const bool UseNoDelay = true;

        /// <summary>
        /// The default encoding to use for all string operations in packet writing or reading.
        /// </summary>
        public static readonly Encoding DefaultEncoding =  Encoding.UTF8;
    }
}