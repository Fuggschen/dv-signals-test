using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Signals.Game
{
    /// <summary>
    /// Attached as a child GameObject to each signal.
    /// Applies emergency brakes to any train that passes through a signal
    /// whose active aspect has <see cref="Signals.Common.Aspects.AspectBaseDefinition.DisallowPassing"/> set.
    /// </summary>
    /// <remarks>
    /// Only uncoupled end-couplers of a trainset trigger (coupled couplers between cars have
    /// no active collider). This gives exactly two events per trainset pass:
    /// <list type="number">
    ///   <item><b>First coupler</b> (leading end) enters the zone — the signal state is read
    ///         and, if restrictive with the correct approach direction, emergency brakes are
    ///         applied immediately.</item>
    ///   <item><b>Second coupler</b> (trailing end) enters — clears the tracked trainset so
    ///         it can be evaluated again on a subsequent pass.</item>
    /// </list>
    /// Direction is determined by checking which side of the signal the coupler entered from
    /// (dot product of signal forward vs coupler-to-signal offset).
    /// </remarks>
    [RequireComponent(typeof(BoxCollider))]
    internal class SignalPassingDetector : MonoBehaviour
    {
        private const float ColliderWidth = 4f;
        private const float ColliderHeight = 4f;
        private const float ColliderDepth = 2f;
        private const float BrakeDurationSeconds = 10f;

        private const string CouplerFrontName = "[coupler front]";
        private const string CouplerRearName = "[coupler rear]";

        private Controllers.BasicSignalController _signal = null!;
        private BoxCollider _collider = null!;
        private Coroutine? _brakeCoroutine;

        // Trainsets currently being tracked. Present = first coupler has been seen.
        // Cleared when the second coupler enters (so the trainset can trigger again
        // on a subsequent pass) or when the signal returns to a non-restrictive aspect.
        private readonly HashSet<Trainset> _trackedTrainsets = new HashSet<Trainset>();

        internal static SignalPassingDetector Attach(Controllers.BasicSignalController signal)
        {
            var go = new GameObject("SignalPassingDetector");
            go.transform.SetParent(signal.Definition.transform, worldPositionStays: false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            // Must be on layer 0 (Default) so it interacts with coupler colliders
            // in the physics collision matrix.
            go.layer = 0;

            var detector = go.AddComponent<SignalPassingDetector>();
            detector._signal = signal;

            var col = go.GetComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(ColliderWidth, ColliderHeight, ColliderDepth);
            col.center = Vector3.zero;
            detector._collider = col;

            signal.AspectChanged += detector.OnAspectChanged;

            return detector;
        }

        private void OnDestroy()
        {
            if (_signal != null)
                _signal.AspectChanged -= OnAspectChanged;

            _trackedTrainsets.Clear();
        }

        private void OnAspectChanged(Aspects.AspectBase? newAspect)
        {
            bool isNowRestrictive = newAspect?.Definition.DisallowPassing ?? false;

            // When the signal returns to a non-restrictive aspect (train cleared the zone),
            // clear all tracked state so the next approaching train starts fresh.
            if (!isNowRestrictive && _trackedTrainsets.Count > 0)
            {
                SignalsMod.LogVerbose($"[Enforcement] Signal '{_signal.Name}' is no longer restrictive — clearing {_trackedTrainsets.Count} tracked trainset(s).");
                _trackedTrainsets.Clear();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Only react to coupler colliders.
            var name = other.name;
            if (name != CouplerFrontName && name != CouplerRearName) return;

            if (!SignalsMod.Settings.EnableSignalEnforcement) return;

            // Resolve train car via attached Rigidbody.
            var rb = other.attachedRigidbody;
            if (rb == null) return;
            if (!rb.TryGetComponent<TrainCar>(out var trainCar)) return;

            var trainset = trainCar.trainset;
            if (trainset == null) return;

            SignalsMod.LogVerbose($"[Enforcement] Coupler '{name}' of '{trainCar.ID}' entered zone on signal '{_signal.Name}'.");

            // --- Already tracking this trainset? This is the second (trailing) coupler. ---
            if (!_trackedTrainsets.Add(trainset))
            {
                // Second coupler: clear the trainset so it can trigger again on a subsequent pass.
                _trackedTrainsets.Remove(trainset);
                SignalsMod.LogVerbose($"[Enforcement] Second coupler (trailing end) of trainset — clearing tracking for signal '{_signal.Name}'.");
                return;
            }

            // --- First coupler (leading end): the "balise read" moment. ---

            // Direction check: the coupler just entered the zone edge. Its position relative
            // to the signal center tells us which side it came from. The signal's forward
            // points toward the approaching track, so a coupler on that side gives a positive dot.
            Vector3 couplerOffset = other.transform.position - transform.position;
            float dot = Vector3.Dot(transform.forward, couplerOffset.normalized);
            SignalsMod.LogVerbose($"[Enforcement] Direction dot: {dot:F2} (coupler at {other.transform.position}, signal at {transform.position}).");

            if (dot <= 0f)
            {
                SignalsMod.LogVerbose($"[Enforcement] Coupler approached from behind signal '{_signal.Name}' — ignoring.");
                return;
            }

            // Read the signal: check if the current aspect disallows passing.
            if (!CurrentAspectDisallowsPassing())
            {
                SignalsMod.LogVerbose($"[Enforcement] Signal '{_signal.Name}' is not restrictive — trainset may pass freely.");
                return;
            }

            SignalsMod.LogVerbose($"[Enforcement] Signal '{_signal.Name}' read as RESTRICTIVE — applying brakes.");
            ApplyEmergencyBrake(trainCar);
        }

        private bool CurrentAspectDisallowsPassing()
        {
            var aspects = _signal.AllAspects;
            int index = _signal.CurrentAspectIndex;

            if (aspects == null || index < 0 || index >= aspects.Length) return false;

            return aspects[index].Definition.DisallowPassing;
        }

        private void ApplyEmergencyBrake(TrainCar trainCar)
        {
            SignalsMod.Log($"[Enforcement] {trainCar.ID} passed signal '{_signal.Name}' at danger — applying emergency brakes for {BrakeDurationSeconds}s.");

            if (_brakeCoroutine != null)
                StopCoroutine(_brakeCoroutine);

            var trainset = trainCar.trainset;
            _brakeCoroutine = StartCoroutine(SustainBrake(trainset));
        }

        private IEnumerator SustainBrake(Trainset? trainset)
        {
            float elapsed = 0f;

            while (elapsed < BrakeDurationSeconds)
            {
                if (trainset != null)
                {
                    foreach (var car in trainset.cars)
                        SetBrakePressureZero(car);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            _brakeCoroutine = null;
        }

        private static void SetBrakePressureZero(TrainCar car)
        {
            var brakes = car.brakeSystem;
            if (brakes == null) return;

            brakes.SetBrakePipePressure(0f);
        }

    }
}
