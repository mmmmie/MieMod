namespace MieMod.QuickRestart;

internal static class QuickRestartState
{
    private static readonly object Sync = new();

    private static bool _pendingRestart;
    private static ulong _pendingLobbyId;
    private static bool _pendingAutoReady;

    public static void SetPendingRestart(ulong lobbyId)
    {
        lock (Sync)
        {
            _pendingRestart = true;
            _pendingLobbyId = lobbyId;
            _pendingAutoReady = false;
        }
    }

    public static void SetAutoReady(bool autoReady)
    {
        lock (Sync)
        {
            _pendingAutoReady = autoReady;
        }
    }

    public static bool TryConsumePendingRestart(out ulong lobbyId)
    {
        lock (Sync)
        {
            if (!_pendingRestart)
            {
                lobbyId = 0;
                return false;
            }

            lobbyId = _pendingLobbyId;

            _pendingRestart = false;
            _pendingLobbyId = 0;
            return true;
        }
    }

    public static bool TryConsumePendingAutoReady()
    {
        lock (Sync)
        {
            if (!_pendingAutoReady)
            {
                return false;
            }

            _pendingAutoReady = false;
            return true;
        }
    }

    public static void Clear()
    {
        lock (Sync)
        {
            _pendingRestart = false;
            _pendingLobbyId = 0;
            _pendingAutoReady = false;
        }
    }
}