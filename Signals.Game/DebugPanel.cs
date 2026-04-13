#if DEBUG
using Signals.API;
using UnityEngine;
using UnityModManagerNet;

namespace Signals.Game
{
    internal static class DebugPanel
    {
        private static string _signalId = string.Empty;
        private static string _aspectId = string.Empty;
        private static string _status = string.Empty;
        private static bool _eventsSubscribed;

        public static void Draw(UnityModManager.ModEntry modEntry)
        {
            GUILayout.Space(8);
            GUILayout.Label("<b>--- Signals API Debug Panel ---</b>", new GUIStyle(GUI.skin.label) { richText = true });

            if (!SignalsAPI.IsLoaded)
            {
                GUILayout.Label("API not loaded yet (load a save first).");
                return;
            }

            // Get All Signals
            if (GUILayout.Button("Get All Signals"))
            {
                var all = SignalsAPI.GetAllSignals();
                if (all == null || all.Count == 0)
                {
                    _status = "No signals found.";
                }
                else
                {
                    _status = $"Found {all.Count} signals (see log)";
                    SignalsMod.Log($"[API Debug] === All Signals ({all.Count}) ===");
                    foreach (var s in all)
                    {
                        SignalsMod.Log($"  {s.Id}  pos={s.Position}  aspect={s.CurrentAspectId ?? "OFF"}  mode={s.Mode}");
                    }
                }
            }

            GUILayout.Space(4);

            // Signal ID input
            GUILayout.BeginHorizontal();
            GUILayout.Label("Signal ID:", GUILayout.Width(70));
            _signalId = GUILayout.TextField(_signalId, GUILayout.Width(300));
            GUILayout.EndHorizontal();

            // Get Signal
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Get Signal", GUILayout.Width(120)))
            {
                var s = SignalsAPI.GetSignal(_signalId);
                if (s == null)
                {
                    _status = $"Signal '{_signalId}' not found.";
                }
                else
                {
                    _status = $"{s.Id}: aspect={s.CurrentAspectId ?? "OFF"}, mode={s.Mode}, pos={s.Position}";
                }
                SignalsMod.Log($"[API Debug] GetSignal: {_status}");
            }

            if (GUILayout.Button("Turn Off", GUILayout.Width(120)))
            {
                var result = SignalsAPI.TurnOffSignal(_signalId);
                _status = result ? $"Turned off '{_signalId}'." : $"Failed to turn off '{_signalId}'.";
                SignalsMod.Log($"[API Debug] TurnOff: {_status}");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            // Aspect ID input
            GUILayout.BeginHorizontal();
            GUILayout.Label("Aspect ID:", GUILayout.Width(70));
            _aspectId = GUILayout.TextField(_aspectId, GUILayout.Width(300));
            GUILayout.EndHorizontal();

            // Set Aspect
            if (GUILayout.Button("Set Aspect (enters Manual mode)", GUILayout.Width(250)))
            {
                var result = SignalsAPI.SetSignalAspect(_signalId, _aspectId);
                _status = result
                    ? $"Set '{_signalId}' to aspect '{_aspectId}' (Manual)."
                    : $"Failed to set aspect on '{_signalId}'.";
                SignalsMod.Log($"[API Debug] SetAspect: {_status}");
            }

            GUILayout.Space(4);

            // Mode buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Set Automatic", GUILayout.Width(150)))
            {
                var result = SignalsAPI.SetSignalMode(_signalId, SignalMode.Automatic);
                _status = result ? $"'{_signalId}' → Automatic." : $"Failed to set mode on '{_signalId}'.";
                SignalsMod.Log($"[API Debug] SetMode: {_status}");
            }

            if (GUILayout.Button("Set Manual", GUILayout.Width(150)))
            {
                var result = SignalsAPI.SetSignalMode(_signalId, SignalMode.Manual);
                _status = result ? $"'{_signalId}' → Manual." : $"Failed to set mode on '{_signalId}'.";
                SignalsMod.Log($"[API Debug] SetMode: {_status}");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            // Track occupancy check
            if (GUILayout.Button("Check Track Occupancy (for Signal ID)", GUILayout.Width(300)))
            {
                if (!SignalManager.Instance.TryGetSignal(_signalId, out var sig) || sig == null)
                {
                    _status = $"Signal '{_signalId}' not found for track check.";
                }
                else if (sig.Block?.Tracks == null || sig.Block.Tracks.Length == 0)
                {
                    _status = $"Signal '{_signalId}' has no track info.";
                }
                else
                {
                    var tracks = sig.Block.Tracks;
                    _status = $"Checking {tracks.Length} track(s) for '{_signalId}':";
                    SignalsMod.Log($"[API Debug] === Track Occupancy for '{_signalId}' ({tracks.Length} tracks) ===");
                    for (int i = 0; i < tracks.Length; i++)
                    {
                        bool occupied = SignalsAPI.IsTrackOccupied(tracks[i]);
                        var line = $"  [{i}] {tracks[i].name}: {(occupied ? "OCCUPIED" : "clear")}";
                        SignalsMod.Log($"[API Debug] {line}");
                        if (occupied)
                            _status += $"\n  [{i}] {tracks[i].name}: OCCUPIED";
                    }
                }
                SignalsMod.Log($"[API Debug] TrackOccupancy: {_status}");
            }

            GUILayout.Space(4);

            // Event subscription toggle
            var newSub = GUILayout.Toggle(_eventsSubscribed, "Subscribe to API events (logs changes)");
            if (newSub != _eventsSubscribed)
            {
                _eventsSubscribed = newSub;
                if (_eventsSubscribed)
                {
                    SignalsAPI.Instance!.SignalAspectChanged += OnAspectChanged;
                    SignalsAPI.Instance!.SignalModeChanged += OnModeChanged;
                    _status = "Subscribed to events.";
                }
                else
                {
                    SignalsAPI.Instance!.SignalAspectChanged -= OnAspectChanged;
                    SignalsAPI.Instance!.SignalModeChanged -= OnModeChanged;
                    _status = "Unsubscribed from events.";
                }
                SignalsMod.Log($"[API Debug] {_status}");
            }

            GUILayout.Space(4);

            // Status label
            if (!string.IsNullOrEmpty(_status))
            {
                GUILayout.Label($"<i>{_status}</i>", new GUIStyle(GUI.skin.label) { richText = true });
            }
        }

        public static void Cleanup()
        {
            if (_eventsSubscribed && SignalsAPI.Instance != null)
            {
                SignalsAPI.Instance.SignalAspectChanged -= OnAspectChanged;
                SignalsAPI.Instance.SignalModeChanged -= OnModeChanged;
                _eventsSubscribed = false;
            }
        }

        private static void OnAspectChanged(SignalState state)
        {
            SignalsMod.Log($"[API Event] AspectChanged: {state.Id} → {state.CurrentAspectId ?? "OFF"} (mode={state.Mode})");
        }

        private static void OnModeChanged(string signalId, SignalMode mode)
        {
            SignalsMod.Log($"[API Event] ModeChanged: {signalId} → {mode}");
        }
    }
}
#endif
