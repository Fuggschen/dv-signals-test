using MPAPI;
using MPAPI.Interfaces;
using MPAPI.Types;
using Signals.API;
using System;
using System.Linq;

namespace Signals.Multiplayer
{
    /// <summary>
    /// Manages multiplayer synchronisation of signal states.
    /// Only manual overrides are synced — automatic signals run locally on each client.
    /// </summary>
    internal static class SignalNetworkManager
    {
        private static IServer? _server;
        private static IClient? _client;
        private static bool _initialized;

        private static string _modId = string.Empty;
        private static Action<string> _log = _ => { };
        private static Action<string> _logVerbose = _ => { };

        public static void Initialize(string modId, Action<string> log, Action<string> logVerbose)
        {
            if (_initialized) return;
            if (!MultiplayerAPI.IsMultiplayerLoaded) return;

            _modId = modId;
            _log = log;
            _logVerbose = logVerbose;

            MultiplayerAPI.Instance.SetModCompatibility(modId, MultiplayerCompatibility.All);

            MultiplayerAPI.ServerStarted += OnServerStarted;
            MultiplayerAPI.ServerStopped += OnServerStopped;
            MultiplayerAPI.ClientStarted += OnClientStarted;
            MultiplayerAPI.ClientStopped += OnClientStopped;

            SignalsAPI.Loaded += OnSignalsAPILoaded;
            SignalsAPI.Unloaded += OnSignalsAPIUnloaded;

            _initialized = true;
            _log("[MP Sync] Multiplayer detected, signal sync enabled.");
        }

        public static void Teardown()
        {
            if (!_initialized) return;

            MultiplayerAPI.ServerStarted -= OnServerStarted;
            MultiplayerAPI.ServerStopped -= OnServerStopped;
            MultiplayerAPI.ClientStarted -= OnClientStarted;
            MultiplayerAPI.ClientStopped -= OnClientStopped;

            SignalsAPI.Loaded -= OnSignalsAPILoaded;
            SignalsAPI.Unloaded -= OnSignalsAPIUnloaded;

            CleanupServer();
            CleanupClient();

            _initialized = false;
        }

        #region Host

        private static void SubscribeToSignalEvents()
        {
            if (SignalsAPI.Instance == null) return;

            SignalsAPI.Instance.SignalModeChanged += OnHostModeChanged;
            SignalsAPI.Instance.SignalAspectChanged += OnHostAspectChanged;
            _log("[MP Sync] Subscribed to signal events.");
        }

        private static void UnsubscribeFromSignalEvents()
        {
            if (SignalsAPI.Instance == null) return;

            SignalsAPI.Instance.SignalModeChanged -= OnHostModeChanged;
            SignalsAPI.Instance.SignalAspectChanged -= OnHostAspectChanged;
        }

        private static void OnSignalsAPILoaded()
        {
            // API just became available — if we're already hosting, subscribe now.
            if (_server != null)
            {
                SubscribeToSignalEvents();
            }
        }

        private static void OnSignalsAPIUnloaded()
        {
            // API going away — nothing to unsubscribe from, instance is already null.
        }

        private static void OnServerStarted(IServer server)
        {
            _server = server;

            // Register packets on the server (no handler — host doesn't receive signal packets from clients).
            _server.RegisterPacket<SignalStatePacket>((_, __) => { });
            _server.RegisterSerializablePacket<SignalFullSyncPacket>((_, __) => { });

            // Send full sync to joining players.
            _server.OnPlayerReady += OnPlayerReady;

            // Subscribe to Signals API events to broadcast manual overrides.
            // If the API isn't loaded yet, OnSignalsAPILoaded will subscribe later.
            SubscribeToSignalEvents();

            _log("[MP Sync] Server started, signal sync active.");
        }

        private static void OnServerStopped()
        {
            CleanupServer();
            _log("[MP Sync] Server stopped, signal sync deactivated.");
        }

        private static void CleanupServer()
        {
            if (_server != null)
            {
                _server.OnPlayerReady -= OnPlayerReady;
                _server = null;
            }

            UnsubscribeFromSignalEvents();
        }

        private static void OnPlayerReady(IPlayer player)
        {
            if (_server == null || SignalsAPI.Instance == null) return;

            // Build a sync packet containing only manually-controlled signals.
            var allSignals = SignalsAPI.Instance.GetAllSignals();
            var packet = new SignalFullSyncPacket();

            foreach (var signal in allSignals)
            {
                if (signal.Mode == SignalMode.Manual)
                {
                    packet.Signals.Add(new SignalFullSyncPacket.SignalEntry
                    {
                        SignalId = signal.Id,
                        AspectId = signal.CurrentAspectId ?? string.Empty,
                        Mode = (byte)signal.Mode
                    });
                }
            }

            if (packet.Signals.Count > 0)
            {
                _server.SendSerializablePacketToPlayer(packet, player);
                _logVerbose($"[MP Sync] Sent full sync to {player.Username}: {packet.Signals.Count} manual signal(s).");
            }
        }

        private static void OnHostModeChanged(string signalId, SignalMode mode)
        {
            if (_server == null || SignalsAPI.Instance == null) return;

            // When mode changes, broadcast the new mode and current aspect to all clients.
            var signal = SignalsAPI.Instance.GetSignal(signalId);
            if (signal == null) return;

            var packet = new SignalStatePacket
            {
                SignalId = signalId,
                AspectId = signal.CurrentAspectId ?? string.Empty,
                Mode = (byte)mode
            };

            _server.SendPacketToAll(packet, reliable: true, excludeSelf: true);
            _logVerbose($"[MP Sync] Broadcast mode change: {signalId} → {mode}");
        }

        private static void OnHostAspectChanged(SignalState state)
        {
            if (_server == null) return;

            // Only broadcast aspect changes for signals in Manual mode.
            if (state.Mode != SignalMode.Manual) return;

            var packet = new SignalStatePacket
            {
                SignalId = state.Id,
                AspectId = state.CurrentAspectId ?? string.Empty,
                Mode = (byte)SignalMode.Manual
            };

            _server.SendPacketToAll(packet, reliable: true, excludeSelf: true);
            _logVerbose($"[MP Sync] Broadcast manual aspect: {state.Id} → {state.CurrentAspectId ?? "OFF"}");
        }

        #endregion

        #region Client

        private static void OnClientStarted(IClient client)
        {
            _client = client;

            // Register packet handlers.
            _client.RegisterPacket<SignalStatePacket>(OnClientReceivedState);
            _client.RegisterSerializablePacket<SignalFullSyncPacket>(OnClientReceivedFullSync);

            _log("[MP Sync] Client started, signal sync active.");
        }

        private static void OnClientStopped()
        {
            CleanupClient();
            _log("[MP Sync] Client stopped, signal sync deactivated.");
        }

        private static void CleanupClient()
        {
            if (_client == null) return;

            // Restore any remaining manual signals back to automatic on disconnect.
            if (SignalsAPI.Instance != null)
            {
                var allSignals = SignalsAPI.Instance.GetAllSignals();
                if (allSignals != null)
                {
                    foreach (var signal in allSignals.Where(s => s.Mode == SignalMode.Manual))
                    {
                        SignalsAPI.Instance.SetSignalMode(signal.Id, SignalMode.Automatic);
                    }
                }
            }

            _client = null;
        }

        private static void OnClientReceivedState(SignalStatePacket packet)
        {
            if (SignalsAPI.Instance == null) return;

            var mode = (SignalMode)packet.Mode;

            if (mode == SignalMode.Manual)
            {
                if (string.IsNullOrEmpty(packet.AspectId))
                {
                    // Signal is off but in manual mode.
                    SignalsAPI.Instance.TurnOffSignal(packet.SignalId);
                }
                else
                {
                    // Set aspect (this also enters Manual mode).
                    SignalsAPI.Instance.SetSignalAspect(packet.SignalId, packet.AspectId);
                }
            }
            else
            {
                // Host released signal back to Automatic — resume local logic.
                SignalsAPI.Instance.SetSignalMode(packet.SignalId, SignalMode.Automatic);
            }

            _logVerbose($"[MP Sync] Received state: {packet.SignalId} → {(mode == SignalMode.Manual ? packet.AspectId : "AUTO")}");
        }

        private static void OnClientReceivedFullSync(SignalFullSyncPacket packet)
        {
            if (SignalsAPI.Instance == null) return;

            _log($"[MP Sync] Received full sync: {packet.Signals.Count} manual signal(s).");

            foreach (var entry in packet.Signals)
            {
                if (string.IsNullOrEmpty(entry.AspectId))
                {
                    SignalsAPI.Instance.TurnOffSignal(entry.SignalId);
                }
                else
                {
                    SignalsAPI.Instance.SetSignalAspect(entry.SignalId, entry.AspectId);
                }
            }
        }

        #endregion
    }
}
