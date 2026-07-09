using MegaCrit.Sts2.Core.Multiplayer.Game;
using PartyRace.Core.Network;

namespace PartyRace.Sts2Adapter;

public sealed class Sts2TransportAdapter : IPartyRaceTransport, IDisposable
{
    private readonly MessageHandlerDelegate<PartyRaceNetEnvelope> _handler;
    private bool _isDisposed;

    public Sts2TransportAdapter(INetGameService netGameService)
    {
        NetGameService = netGameService;
        _handler = HandleEnvelope;
        NetGameService.RegisterMessageHandler(_handler);
    }

    public event Action<RaceMessage, ulong>? MessageReceived;

    public INetGameService NetGameService { get; }

    public void SendToHost(RaceMessage message)
    {
        ThrowIfDisposed();
        NetGameService.SendMessage(PartyRaceNetEnvelope.FromMessage(
            message,
            shouldBroadcast: false,
            shouldBuffer: false));
    }

    public void BroadcastFromHost(RaceMessage message)
    {
        ThrowIfDisposed();
        NetGameService.SendMessage(PartyRaceNetEnvelope.FromMessage(
            message,
            shouldBroadcast: true,
            shouldBuffer: true));
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        NetGameService.UnregisterMessageHandler(_handler);
    }

    private void HandleEnvelope(PartyRaceNetEnvelope envelope, ulong senderId)
    {
        MessageReceived?.Invoke(envelope.ToMessage(), senderId);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }
}
