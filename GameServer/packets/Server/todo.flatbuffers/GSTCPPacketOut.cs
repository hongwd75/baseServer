﻿using Project.Network;

namespace Project.GS.PacketHandler;

/// <summary>
/// An outgoing TCP packet
///  플랫 버퍼 형태로 변경할 예정임.
/// </summary>
public class GSTCPPacketOut : PacketOut
{
    private byte m_packetCode;
		
    /// <summary>
    /// This Packet Byte Handling Code
    /// </summary>
    public byte PacketCode {
        get { return m_packetCode; }
    }
		
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="packetCode">ID of the packet</param>
    public GSTCPPacketOut(byte packetCode)
    {
        m_packetCode = packetCode;
        base.WriteShort(0x00); //reserved for size
        base.WriteByte(packetCode);
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="packetCode">ID of the packet</param>
    public GSTCPPacketOut(byte packetCode, int startingSize) : base(startingSize + 3)
    {
        m_packetCode = packetCode;
        base.WriteShort(0x00); //reserved for size
        base.WriteByte(packetCode);
    }

    public override string ToString()
    {
        return base.ToString() + $": Size={Length - 3} ID=0x{m_packetCode:X2}";
    }
}