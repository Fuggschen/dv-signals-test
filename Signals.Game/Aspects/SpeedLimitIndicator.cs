using Signals.Common.Aspects;
using Signals.Game.Controllers;
using Signals.Game.SpeedLimits;

namespace Signals.Game.Aspects
{
    internal class SpeedLimitIndicator : AspectBase
    {
        private readonly SpeedLimitIndicatorDefinition _fullDef;
        private bool? _cachedResult;
        private RailTrack[]? _cachedTracks;

        public SpeedLimitIndicator(AspectBaseDefinition definition, BasicSignalController controller)
            : base(definition, controller)
        {
            _fullDef = (SpeedLimitIndicatorDefinition)definition;
        }

        public override bool MeetsConditions()
        {
            // Block is not available until the first UpdateBlock() call.
            if (ControllerTrackBlock == null)
                return false;

            var tracks = ControllerTrackBlock.Tracks;

            if (tracks == null || tracks.Length == 0)
            {
                _cachedResult = false;
                _cachedTracks = tracks;
                return false;
            }

            // Return cached result if the track set hasn't changed.
            if (_cachedResult.HasValue && !TracksChanged(tracks))
                return _cachedResult.Value;

            int? lowest = TrackSpeedLimitIndexer.GetLowestSpeedLimit(tracks);

            _cachedResult = lowest.HasValue && lowest.Value <= _fullDef.MaxSpeedLimit;
            _cachedTracks = tracks;

            SignalsMod.LogVerbose(
                $"SpeedLimitIndicator [{Controller.Name}]: lowest={lowest?.ToString() ?? "none"} km/h, " +
                $"threshold={_fullDef.MaxSpeedLimit} km/h, active={_cachedResult.Value}");

            return _cachedResult.Value;
        }

        /// <summary>
        /// Compares element-by-element to detect if the walked tracks changed (e.g. junction switched).
        /// </summary>
        private bool TracksChanged(RailTrack[] tracks)
        {
            if (_cachedTracks == null || _cachedTracks.Length != tracks.Length)
                return true;

            for (int i = 0; i < tracks.Length; i++)
            {
                if (_cachedTracks[i] != tracks[i])
                    return true;
            }

            return false;
        }
    }
}
