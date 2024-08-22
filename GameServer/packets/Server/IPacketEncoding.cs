﻿namespace Project.GS.PacketHandler;

public enum eEncryptionState
{
    NotEncrypted = 0,
    RSAEncrypted = 1,
    PseudoRC4Encrypted = 2
}

public interface IPacketEncoding
{
    eEncryptionState EncryptionState { get; set; }
    byte[] DecryptPacket(byte[] content, int offset, bool udpPacket);
    byte[] EncryptPacket(byte[] content, int offset, bool udpPacket);
    byte[] SBox { get; set; }
}