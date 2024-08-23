using Project.GS.Events;
using Project.GS.PacketHandler;
using Project.Network;

namespace Project.GS;

public partial class GameClient : BaseClient, ICustomParamsValuable
{
    /// <summary>
    /// Called when a packet has been received.
    /// </summary>
    /// <param name="numBytes">The number of bytes received</param>
    /// <remarks>This function parses the incoming data into individual packets and then calls the appropriate handler.</remarks>
    protected override void OnReceive(int numBytes)
    {
        //This is the first received packet ...
        if (Version == eClientVersion.VersionNotChecked)
        {
            //Disconnect if the packet seems wrong
            if (numBytes < 17) // 17 is correct bytes count for 0xF4 packet
            {
                if (log.IsWarnEnabled)
                {
                    log.WarnFormat("Disconnected {0} in login phase because wrong packet size {1}", TcpEndpoint,
                        numBytes);
                    log.Warn("numBytes=" + numBytes);
                    log.Warn(Marshal.ToHexDump("packet buffer:", _pBuf, 0, numBytes));
                }

                GameServer.Instance.Disconnect(this);
                return;
            }

            int version;

            /// <summary>
            /// The First Packet Format Change after 1.115c
            /// If "numbytes" is below 19 we have a pre-1.115c packet !
            /// </summary>
            if (numBytes < 19)
            {
                //Currently, the version is sent with the first packet, no
                //matter what packet code it is
                version = (_pBuf[12] * 100) + (_pBuf[13] * 10) + _pBuf[14];

                // we force the versionning: 200 correspond to 1.100 (1100)
                // thus we could handle logically packets with version number based on the client version
                if (version >= 200) version += 900;
            }
            else
            {
                // post 1.115c
                // first byte is major (1), second byte is minor (1), third byte is version (15)
                // revision (c) is also coded in ascii after that, then a build number appear using two bytes (0x$$$$)
                version = _pBuf[11] * 1000 + _pBuf[12] * 100 + _pBuf[13];
            }

            eClientVersion ver;
            IPacketLib lib = AbstractPacketLib.CreatePacketLibForVersion(version, this, out ver);

            if (lib == null)
            {
                Version = eClientVersion.VersionUnknown;
                if (log.IsWarnEnabled)
                    log.Warn(TcpEndpointAddress + " client Version " + version + " not handled on this server!");
                GameServer.Instance.Disconnect(this);
            }
            else
            {
                log.Info("Incoming connection from " + TcpEndpointAddress + " using client version " + version);
                Version = ver;
                Out = lib;
                PacketProcessor = new PacketProcessor(this);
            }
        }

        if (Version != eClientVersion.VersionUnknown)
        {
            m_packetProcessor.ReceiveBytes(numBytes);
        }
    }

    /// <summary>
    /// Called when this client has been disconnected
    /// </summary>
    public override void OnDisconnect()
    {
        try
        {
            if (PacketProcessor != null)
                PacketProcessor.OnDisconnect();

            //If we went linkdead and we were inside the game
            //we don't let the player disappear!
            if (ClientState == eClientState.Playing)
            {
                OnLinkdeath();
                return;
            }

            if (ClientState == eClientState.WorldEnter && Player != null)
            {
                Player.SaveIntoDatabase();
            }
        }
        catch (Exception e)
        {
            if (log.IsErrorEnabled)
                log.Error("OnDisconnect", e);
        }
        finally
        {
            // Make sure the client is diconnected even on errors
            Quit();
        }
    }

    /// <summary>
    /// Called when this client has connected
    /// </summary>
    public override void OnConnect()
    {
        GameEventMgr.Notify(GameClientEvent.Connected, this);
    }

    /// <summary>
    /// Called when a player goes linkdead
    /// </summary>
    protected void OnLinkdeath()
    {
        if (log.IsDebugEnabled)
            log.Debug("Linkdeath called (" + Account.Name + ")  client state=" + ClientState);

        //If we have no sessionid we simply disconnect
        GamePlayer curPlayer = Player;
        if (m_sessionID == 0 || curPlayer == null)
        {
            Quit();
        }
        else
        {
            ClientState = eClientState.Linkdead;
            // If we have a good sessionid, we won't remove the client yet!
            // OnLinkdeath() can start a timer to remove the client "a bit later"
            curPlayer.OnLinkdeath();
        }
    }


    /// <summary>
    /// Quits a client from the world
    /// </summary>
    protected internal void Quit()
    {
        lock (this)
        {
            try
            {
                eClientState oldClientState = ClientState;
                if (m_sessionID != 0)
                {
                    if (oldClientState == eClientState.Playing || oldClientState == eClientState.WorldEnter ||
                        oldClientState == eClientState.Linkdead)
                    {
                        try
                        {
                            if (Player != null)
                                Player.Quit(true); //calls delete
                            //m_player.Delete(true);
                        }
                        catch (Exception e)
                        {
                            log.Error("player cleanup on client quit", e);
                        }
                    }

                    try
                    {
                        //Now free our objid and sessionid again
                        WorldMgr.RemoveClient(this); //calls RemoveSessionID -> player.Delete
                    }
                    catch (Exception e)
                    {
                        log.Error("client cleanup on quit", e);
                    }
                }

                ClientState = eClientState.Disconnected;
                Player = null;

                GameEventMgr.Notify(GameClientEvent.Disconnected, this);

                if (Account != null)
                {
                    if (log.IsInfoEnabled)
                    { 
                        log.Info("(" + TcpEndpoint + ") " + Account.Name + " just disconnected!");
                    }

                    // log disconnect
                    AuditMgr.AddAuditEntry(this, AuditType.Account, AuditSubtype.AccountLogout, "", Account.Name);
                }
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error("Quit", e);
            }
        }
    }
}