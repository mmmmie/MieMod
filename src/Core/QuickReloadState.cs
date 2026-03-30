namespace QuickReload;

internal static class QuickReloadState
{
    private static readonly object Sync = new();

    private static bool _pendingRestart;
    private static ulong _pendingPlayerId;
    private static bool _pendingAutoReady;
    private static bool _pendingVisualRecovery;

    public static void SetPendingRestart(ulong playerId)
    {
        lock (Sync)
        {
            _pendingRestart = true;
            _pendingPlayerId = playerId;
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

    public static bool TryConsumePendingRestart(out ulong playerId)
    {
        lock (Sync)
        {
            if (!_pendingRestart)
            {
                playerId = 0;
                return false;
            }

            playerId = _pendingPlayerId;

            _pendingRestart = false;
            _pendingPlayerId = 0;
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
            _pendingPlayerId = 0;
            _pendingAutoReady = false;
        }
    }

    public static void SetPendingVisualRecovery()
    {
        lock (Sync)
        {
            _pendingVisualRecovery = true;
        }
    }

    public static bool TryConsumePendingVisualRecovery()
    {
        lock (Sync)
        {
            if (!_pendingVisualRecovery)
            {
                return false;
            }

            _pendingVisualRecovery = false;
            return true;
        }
    }

    public static void ClearPendingVisualRecovery()
    {
        lock (Sync)
        {
            _pendingVisualRecovery = false;
        }
    }
}
