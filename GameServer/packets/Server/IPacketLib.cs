namespace Project.GS.PacketHandler;

public enum eChatLoc : byte
{
    CL_ChatWindow = 0x0,
    CL_PopupWindow = 0x1,
    CL_SystemWindow = 0x2
} ;

/// <summary>
/// Types of chat messages
/// </summary>
public enum eChatType : byte
{
    CT_System = 0x00,
    CT_Say = 0x01,
}

public interface IPacketLib
{
    byte GetPacketCode(eServerPackets packetCode);
    void SendTCP(GSTCPPacketOut packet);
    void SendTCP(byte[] buf);
    void SendMessage(string msg, eChatType type, eChatLoc loc);
}