using Signals.Game.Curves;
using UnityEngine;

namespace Signals.Game.SpeedLimits
{
    /// <summary>
    /// Calculates speed limits for tracks based on their bezier curve geometry.
    /// Speed is derived from curvature radius using the same formula as DV's speed signs.
    /// </summary>
    internal static class TrackSpeedLimitIndexer
    {
        private const int CurvatureSamples = 20;
        private const int YardSpeedCap = 50;

        /// <summary>
        /// Gets the lowest speed limit across all given tracks, calculated from
        /// bezier curve geometry. Returns null if all tracks are straight (120 km/h).
        /// </summary>
        public static int? GetLowestSpeedLimit(RailTrack[] tracks)
        {
            int? lowest = null;

            foreach (var track in tracks)
            {
                int speed = GetTrackSpeedLimit(track);

                if (lowest == null || speed < lowest.Value)
                    lowest = speed;
            }

            return lowest;
        }

        /// <summary>
        /// Calculates the posted speed limit for a single track from its curvature.
        /// </summary>
        public static int GetTrackSpeedLimit(RailTrack track)
        {
            var curve = track.curve;

            if (curve == null || curve.pointCount < 2)
                return 120;

            bool isYard = track.name.StartsWith("[Y]") || track.name.StartsWith("[#]");
            bool isTurntable = track.name.Contains("Turntable Track");
            int lowestSpeed = 120;

            for (int i = 0; i < curve.pointCount - 1; i++)
            {
                var segment = CubicBezier.FromBezierCurve(curve, i);
                float avgCurvature = EstimateCurvature(segment);

                if (avgCurvature <= 0.0000001f)
                    continue;

                float radius = 1f / avgCurvature;
                int speed = RadiusToSpeed(radius);

                if (isYard || isTurntable)
                    speed = Mathf.Min(speed, YardSpeedCap);

                if (speed < lowestSpeed)
                    lowestSpeed = speed;
            }

            return lowestSpeed;
        }

        /// <summary>
        /// Estimates average curvature of a cubic bezier segment by sampling.
        /// Uses the formula: κ = |v × a| / |v|³
        /// </summary>
        private static float EstimateCurvature(CubicBezier seg)
        {
            float totalCurvature = 0f;
            int validSamples = 0;

            for (int i = 0; i < CurvatureSamples; i++)
            {
                float t = (float)i / CurvatureSamples;

                var vel = EvaluateVelocity(seg, t);
                var acc = EvaluateAcceleration(seg, t);

                float velMag = vel.magnitude;

                if (velMag < 0.0001f)
                    continue;

                float curvature = Vector3.Cross(vel, acc).magnitude / (velMag * velMag * velMag);

                if (!float.IsNaN(curvature))
                {
                    totalCurvature += curvature;
                    validSamples++;
                }
            }

            if (validSamples == 0)
                return 0f;

            return totalCurvature / validSamples;
        }

        /// <summary>
        /// First derivative of cubic bezier at parameter t.
        /// </summary>
        private static Vector3 EvaluateVelocity(CubicBezier seg, float t)
        {
            float t2 = t * t;
            float p0v = -3f * t2 + 6f * t - 3f;
            float p1v = 9f * t2 - 12f * t + 3f;
            float p2v = -9f * t2 + 6f * t;
            float p3v = 3f * t2;

            return seg.P0 * p0v + seg.P1 * p1v + seg.P2 * p2v + seg.P3 * p3v;
        }

        /// <summary>
        /// Second derivative of cubic bezier at parameter t.
        /// </summary>
        private static Vector3 EvaluateAcceleration(CubicBezier seg, float t)
        {
            float p0v = -6f * t + 6f;
            float p1v = 18f * t - 12f;
            float p2v = -18f * t + 6f;
            float p3v = 6f * t;

            return seg.P0 * p0v + seg.P1 * p1v + seg.P2 * p2v + seg.P3 * p3v;
        }

        /// <summary>
        /// Converts curve radius (meters) to posted speed limit (km/h).
        /// Matches DV's speed sign placement logic.
        /// </summary>
        private static int RadiusToSpeed(float radius)
        {
            if (radius < 50f) return 10;
            if (radius < 70f) return 20;
            if (radius < 95f) return 30;
            if (radius < 130f) return 40;
            if (radius < 170f) return 50;
            if (radius < 230f) return 60;
            if (radius < 360f) return 70;
            if (radius < 700f) return 80;
            if (radius < 900f) return 90;
            if (radius < 1200f) return 100;
            return 120;
        }
    }
}
