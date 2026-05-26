using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace QuickReload.Multiplayer;

public struct QuickReloadMessage :
    INetMessage,
    IPacketSerializable
{
    public ulong playerId;

    public bool ShouldBroadcast => true;

    public NetTransferMode Mode => NetTransferMode.Reliable;

    public LogLevel LogLevel => LogLevel.VeryDebug;

    public bool ShouldBuffer => false;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteULong(playerId);
    }

    public void Deserialize(PacketReader reader)
    {
        this.playerId = reader.ReadULong();
    }

    public override string ToString()
    {
        return $"[QUICKRELOAD]: QuickReloadMessage: playerId={playerId}";
    }
}
