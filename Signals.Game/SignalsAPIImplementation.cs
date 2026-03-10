using Signals.API;
using Signals.Game.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signals.Game
{
    internal class SignalsAPIImplementation : ISignalsAPI
    {
        private readonly SignalManager _manager;

        // Backing fields for the events.
        private Action<SignalState>? _signalAspectChanged;
        private Action<string, SignalMode>? _signalModeChanged;

        // Track which signals we've subscribed to, so we can clean up.
        private readonly HashSet<BasicSignalController> _subscribedSignals = new HashSet<BasicSignalController>();

        public event Action<SignalState>? SignalAspectChanged
        {
            add => _signalAspectChanged += value;
            remove => _signalAspectChanged -= value;
        }

        public event Action<string, SignalMode>? SignalModeChanged
        {
            add => _signalModeChanged += value;
            remove => _signalModeChanged -= value;
        }

        public SignalsAPIImplementation(SignalManager manager)
        {
            _manager = manager;

            // Subscribe to all existing signals.
            foreach (var signal in _manager.AllSignals)
            {
                SubscribeToSignal(signal);
            }
        }

        public IReadOnlyList<SignalState> GetAllSignals()
        {
            return _manager.AllSignals
                .Where(s => s.Exists)
                .Select(CreateSnapshot)
                .ToList();
        }

        public SignalState? GetSignal(string signalId)
        {
            if (_manager.TryGetSignal(signalId, out var signal) && signal != null && signal.Exists)
            {
                return CreateSnapshot(signal);
            }

            return null;
        }

        public bool SetSignalAspect(string signalId, string aspectId)
        {
            if (!_manager.TryGetSignal(signalId, out var signal) || signal == null || !signal.Exists)
            {
                return false;
            }

            return signal.SetAspectById(aspectId);
        }

        public bool SetSignalMode(string signalId, SignalMode mode)
        {
            if (!_manager.TryGetSignal(signalId, out var signal) || signal == null || !signal.Exists)
            {
                return false;
            }

            return signal.SetMode(mode);
        }

        public bool TurnOffSignal(string signalId)
        {
            if (!_manager.TryGetSignal(signalId, out var signal) || signal == null || !signal.Exists)
            {
                return false;
            }

            signal.SetMode(SignalMode.Manual);
            return signal.TurnOff();
        }

        internal void SubscribeToSignal(BasicSignalController signal)
        {
            if (!_subscribedSignals.Add(signal)) return;

            signal.AspectChanged += _ => OnAspectChanged(signal);
            signal.ModeChanged += OnModeChanged;
            signal.Destroyed += OnSignalDestroyed;
        }

        internal void Dispose()
        {
            foreach (var signal in _subscribedSignals)
            {
                if (signal.Exists)
                {
                    signal.ModeChanged -= OnModeChanged;
                    signal.Destroyed -= OnSignalDestroyed;
                }
            }

            _subscribedSignals.Clear();
            _signalAspectChanged = null;
            _signalModeChanged = null;
        }

        private void OnAspectChanged(BasicSignalController signal)
        {
            if (!signal.Exists) return;
            _signalAspectChanged?.Invoke(CreateSnapshot(signal));
        }

        private void OnModeChanged(string signalId, SignalMode mode)
        {
            _signalModeChanged?.Invoke(signalId, mode);
        }

        private void OnSignalDestroyed(BasicSignalController signal)
        {
            signal.ModeChanged -= OnModeChanged;
            signal.Destroyed -= OnSignalDestroyed;
            _subscribedSignals.Remove(signal);
        }

        private static SignalState CreateSnapshot(BasicSignalController signal)
        {
            return new SignalState(
                signal.Name,
                signal.Position,
                signal.CurrentAspect?.Id,
                signal.Mode
            );
        }
    }
}
