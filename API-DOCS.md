# Signals API Reference

> **Assembly:** `Signals.API.dll`  
> **Namespace:** `Signals.API`  
> **Thread Safety:** All methods must be called from the Unity main thread.

---

## Table of Contents

- [Getting Started](#getting-started)
- [Static Entry Point — `SignalsAPI`](#static-entry-point--signalsapi)
  - [Properties](#signalsapi-properties)
  - [Methods](#signalsapi-methods)
  - [Events](#signalsapi-events)
- [Interface — `ISignalsAPI`](#interface--isignalsapi)
  - [Methods](#isignalsapi-methods)
  - [Events](#isignalsapi-events)
- [Models](#models)
  - [`SignalState`](#signalstate)
- [Enumerations](#enumerations)
  - [`SignalMode`](#signalmode)
  - [`SignalType`](#signaltype)
  - [`SignalDirection`](#signaldirection)

---

## Getting Started

**Reference** the `Signals.API.dll` assembly in your project. All public types are in the `Signals.API` namespace.

> **Do not create a hard dependency on `Signals.API.dll`.** If your mod references the assembly directly, it will crash when the Signals mod is not installed.
> Instead, create a small **shim class** that wraps all Signals API calls behind a reflection or soft-reference layer. Only call into the shim when Signals is actually loaded. This keeps your mod functional even without Signals installed.

### Shim Example

```csharp
// In your mod's project — no compile-time reference to Signals.API needed.
public static class SignalsShim
{
    private static bool _checked;
    private static bool _available;

    public static bool IsAvailable
    {
        get
        {
            if (!_checked)
            {
                _available = Type.GetType("Signals.API.SignalsAPI, Signals.API") != null;
                _checked = true;
            }
            return _available;
        }
    }
}

// Then guard all Signals calls:
if (SignalsShim.IsAvailable)
{
    // Safe to call Signals.API types here.
}
```

Alternatively, if you **do** reference the assembly at compile time, mark it as an optional dependency and wrap calls in a try/catch or type-check so your mod still loads cleanly when Signals is absent.

```csharp
using Signals.API;
```

The API is not available immediately at game start. Wait for the `Loaded` event or check `IsLoaded` before calling any methods.

### Initialization Example

```csharp
// Option 1: Event-based
SignalsAPI.Loaded += OnSignalsLoaded;

void OnSignalsLoaded()
{
    var signals = SignalsAPI.GetAllSignals();
    // ...
}

// Option 2: Polling
if (SignalsAPI.IsLoaded)
{
    var signal = SignalsAPI.GetSignal("S-0370-MF-T");
}
```

---

## Static Entry Point — `SignalsAPI`

`public static class SignalsAPI`

Static facade that delegates to the underlying `ISignalsAPI` implementation. All methods return safe defaults (`null` or `false`) when the API is not yet loaded.

---

### SignalsAPI Properties

| Property | Type | Description |
|---|---|---|
| `Instance` | `ISignalsAPI?` | The backing API instance. `null` until the mod has finished loading. |
| `IsLoaded` | `bool` | `true` when `Instance` is available and the API is ready to use. |

---

### SignalsAPI Methods

#### `GetAllSignals`

```csharp
public static IReadOnlyList<SignalState>? GetAllSignals()
```

Returns an immutable snapshot list of **all** currently registered signals in the world.

| | |
|---|---|
| **Returns** | `IReadOnlyList<SignalState>?` — All signal snapshots, or `null` if the API is not loaded. |

**Example**

```csharp
var signals = SignalsAPI.GetAllSignals();
if (signals != null)
{
    foreach (var s in signals)
        Debug.Log($"{s.Id}: {s.CurrentAspectId}");
}
```

---

#### `GetSignal`

```csharp
public static SignalState? GetSignal(string signalId)
```

Returns a snapshot of a single signal identified by its unique name.

| Parameter | Type | Required | Description |
|---|---|---|---|
| `signalId` | `string` | Yes | The unique name of the signal (e.g. `"S-0370-MF-T"`). |

| | |
|---|---|
| **Returns** | `SignalState?` — The signal snapshot, or `null` if the signal does not exist or the API is not loaded. |

**Example**

```csharp
var signal = SignalsAPI.GetSignal("S-0370-MF-T");
if (signal != null)
    Debug.Log($"Aspect: {signal.CurrentAspectId}, Mode: {signal.Mode}");
```

---

#### `SetSignalAspect`

```csharp
public static bool SetSignalAspect(string signalId, string aspectId)
```

Sets the active aspect of a signal and switches it to **Manual** mode. The signal will hold this aspect until the mode is changed back to Automatic.

| Parameter | Type | Required | Description |
|---|---|---|---|
| `signalId` | `string` | Yes | The unique name of the signal. |
| `aspectId` | `string` | Yes | The aspect to display (e.g. `"OPEN"`, `"STOP"`, `"CAUTION"`). |

| | |
|---|---|
| **Returns** | `bool` — `true` if the aspect was set successfully; `false` if the signal was not found or the API is not loaded. |

**Side Effects**
- The signal's mode changes to `SignalMode.Manual`.
- Fires `SignalAspectChanged` with the updated state.
- Fires `SignalModeChanged` if the signal was previously in Automatic mode.

**Example**

```csharp
bool success = SignalsAPI.SetSignalAspect("S-0370-MF-T", "STOP");
```

---

#### `SetSignalMode`

```csharp
public static bool SetSignalMode(string signalId, SignalMode mode)
```

Switches the operating mode of a signal. When switching to `Automatic`, the signal immediately re-evaluates its aspect based on current track/junction conditions.

| Parameter | Type | Required | Description |
|---|---|---|---|
| `signalId` | `string` | Yes | The unique name of the signal. |
| `mode` | `SignalMode` | Yes | The mode to set (`Automatic` or `Manual`). |

| | |
|---|---|
| **Returns** | `bool` — `true` if the mode was changed; `false` if the signal was not found or the API is not loaded. |

**Side Effects**
- Fires `SignalModeChanged` with the signal ID and new mode.
- If switching to `Automatic`, fires `SignalAspectChanged` after re-evaluation.

**Example**

```csharp
// Lock a signal to its current aspect
SignalsAPI.SetSignalMode("S-0370-MF-T", SignalMode.Manual);

// Release back to automatic control
SignalsAPI.SetSignalMode("S-0370-MF-T", SignalMode.Automatic);
```

---

#### `TurnOffSignal`

```csharp
public static bool TurnOffSignal(string signalId)
```

Turns off a signal (no active aspect is displayed). The signal enters **Manual** mode.

| Parameter | Type | Required | Description |
|---|---|---|---|
| `signalId` | `string` | Yes | The unique name of the signal. |

| | |
|---|---|
| **Returns** | `bool` — `true` if the signal was turned off; `false` if the signal was not found or the API is not loaded. |

**Side Effects**
- `SignalState.CurrentAspectId` becomes `null`.
- `SignalState.IsOn` becomes `false`.
- The signal's mode changes to `SignalMode.Manual`.
- Fires `SignalAspectChanged` and `SignalModeChanged`.

**Example**

```csharp
SignalsAPI.TurnOffSignal("S-0370-MF-T");
```

---

#### `IsTrackOccupied`

```csharp
public static bool IsTrackOccupied(RailTrack track)
```

Checks whether the given track has any trains physically on it. This only detects **real occupancy** (train bogies on the rail). It does not include virtual/pseudo-occupancy used internally by the signal system (e.g. misaligned junction blocking).

| Parameter | Type | Required | Description |
|---|---|---|---|
| `track` | `RailTrack` | Yes | The track to check. |

| | |
|---|---|
| **Returns** | `bool` — `true` if at least one train bogie is on the track; `false` otherwise. |

**Example**

```csharp
RailTrack track = /* obtain a RailTrack reference */;
bool occupied = SignalsAPI.IsTrackOccupied(track);
Debug.Log($"Track occupied: {occupied}");
```

---

### SignalsAPI Events

#### `Loaded`

```csharp
public static event Action? Loaded
```

Fired **once** when the Signals mod has finished loading and the API becomes available. Safe to subscribe before the mod loads.

---

#### `Unloaded`

```csharp
public static event Action? Unloaded
```

Fired when the API is being torn down (mod unload). After this event, `Instance` is `null` and all methods return defaults.

---

## Interface — `ISignalsAPI`

`public interface ISignalsAPI`

The full API contract. Obtain an instance via `SignalsAPI.Instance`.

---

### ISignalsAPI Methods

| Method | Signature | Description |
|---|---|---|
| `GetAllSignals` | `IReadOnlyList<SignalState> GetAllSignals()` | Returns snapshots of all registered signals. |
| `GetSignal` | `SignalState? GetSignal(string signalId)` | Returns a snapshot of a single signal, or `null`. |
| `SetSignalAspect` | `bool SetSignalAspect(string signalId, string aspectId)` | Sets a signal's aspect and enters Manual mode. |
| `SetSignalMode` | `bool SetSignalMode(string signalId, SignalMode mode)` | Changes a signal's operating mode. |
| `TurnOffSignal` | `bool TurnOffSignal(string signalId)` | Turns off a signal (enters Manual mode). |
| `IsTrackOccupied` | `bool IsTrackOccupied(RailTrack track)` | Checks whether a track has any trains physically on it. |

### ISignalsAPI Events

| Event | Signature | Description |
|---|---|---|
| `SignalAspectChanged` | `event Action<SignalState>?` | Fired when any signal's aspect changes. Payload is the post-change snapshot. |
| `SignalModeChanged` | `event Action<string, SignalMode>?` | Fired when a signal's mode changes. Parameters: signal ID, new mode. |

**Event Example**

```csharp
SignalsAPI.Instance!.SignalAspectChanged += state =>
{
    Debug.Log($"Signal {state.Id} changed to {state.CurrentAspectId}");
};

SignalsAPI.Instance!.SignalModeChanged += (id, mode) =>
{
    Debug.Log($"Signal {id} is now {mode}");
};
```

---

## Models

### `SignalState`

`public sealed class SignalState`

An **immutable** snapshot of a signal's state at the moment of capture. Instances are created by the API — you cannot construct them yourself.

#### Properties

| Property | Type | Nullable | Description |
|---|---|---|---|
| `Id` | `string` | No | Unique signal name (e.g. `"S-0370-MF-T"`). |
| `Position` | `Vector3` | No | World-space position of the signal. |
| `CurrentAspectId` | `string?` | Yes | Active aspect ID (e.g. `"OPEN"`, `"STOP"`), or `null` if the signal is off. |
| `IsOn` | `bool` | No | Computed: `true` when `CurrentAspectId` is not `null`. |
| `Mode` | `SignalMode` | No | Current operating mode (`Automatic` or `Manual`). |
| `Type` | `SignalType` | No | The role of this signal in the network. |
| `Direction` | `SignalDirection` | No | Junction orientation, or `None` if not a junction signal. |
| `JunctionId` | `string?` | Yes | Associated junction identifier (e.g. `"ST-J-01"`), or `null`. |
| `SelectedBranch` | `int?` | Yes | 0-based index of the currently selected junction branch, or `null`. |
| `YardId` | `string?` | Yes | Yard/station name of the next track (e.g. `"SteelMill"`), or `null`. |
| `TrackId` | `string?` | Yes | Track identifier including type (e.g. `"M01"`), or `null`. |

#### Property Details

##### `Id`
```csharp
public string Id { get; }
```
The unique signal name used throughout the API as the primary key. Pass this value to `GetSignal()`, `SetSignalAspect()`, `SetSignalMode()`, and `TurnOffSignal()`.

##### `Position`
```csharp
public Vector3 Position { get; }
```
The world-space position of the signal object. Useful for distance calculations or spatial queries.

##### `CurrentAspectId`
```csharp
public string? CurrentAspectId { get; }
```
The currently displayed aspect. Common values include `"OPEN"`, `"STOP"`, `"CAUTION"`. Is `null` when the signal has been turned off via `TurnOffSignal()`.

##### `IsOn`
```csharp
public bool IsOn { get; }  // => CurrentAspectId != null
```
Convenience property. `true` if the signal is displaying any aspect.

##### `Mode`
```csharp
public SignalMode Mode { get; }
```
Indicates whether the signal is under automatic control or manually overridden. See [`SignalMode`](#signalmode).

##### `Type`
```csharp
public SignalType Type { get; }
```
The functional role of this signal. See [`SignalType`](#signaltype).

##### `Direction`
```csharp
public SignalDirection Direction { get; }
```
Indicates whether the signal faces the diverging or converging side of its junction. `None` for non-junction signals. See [`SignalDirection`](#signaldirection).

##### `JunctionId`
```csharp
public string? JunctionId { get; }
```
The identifier of the junction this signal controls (e.g. `"ST-J-01"`). `null` for non-junction signals.

##### `SelectedBranch`
```csharp
public int? SelectedBranch { get; }
```
The 0-based branch index currently selected on the associated junction. Branch `0` is typically the through/main route. `null` for non-junction signals.

##### `YardId`
```csharp
public string? YardId { get; }
```
The yard or station name for the track ahead of this signal (e.g. `"SteelMill"`, `"HarborA"`). May be `null` if the signal has no forward track info or the track is not in a named area.

##### `TrackId`
```csharp
public string? TrackId { get; }
```
A short track identifier including its type prefix (e.g. `"M01"` for mainline track 1, `"Y03"` for yard track 3). May be `null` if track info is unavailable.

---

## Enumerations

### `SignalMode`

`public enum SignalMode`

The operating mode of a signal.

| Value | Integer | Description |
|---|---|---|
| `Automatic` | `0` | Aspect is determined by internal logic (track occupancy, junction state, next-signal lookahead, etc.). This is the default. |
| `Manual` | `1` | Aspect is locked to an externally-set value. Internal logic will not change it until the mode is switched back to `Automatic`. |

---

### `SignalType`

`public enum SignalType`

The functional role of a signal in the railway network.

| Value | Integer | Description |
|---|---|---|
| `NotSet` | `0` | Type has not been assigned. Treat as unknown. |
| `Mainline` | `1` | Controls mainline traffic between stations. |
| `IntoYard` | `2` | Controls entry into a yard or station area. |
| `Shunting` | `3` | Controls shunting/switching movements within a yard. |
| `Distant` | `4` | Advance warning signal — repeats the aspect of the next main signal ahead. |
| `Other` | `5` | Any signal type not covered by the above categories. |

---

### `SignalDirection`

`public enum SignalDirection`

The orientation of a junction signal relative to its junction.

| Value | Integer | Description |
|---|---|---|
| `None` | `0` | The signal is not associated with a junction. |
| `Out` | `1` | The signal faces the **diverging** (outbound) branches. Trains pass this signal before the point where tracks split. |
| `In` | `2` | The signal faces the **converging** (inbound) track. Trains pass this signal after the point where tracks merge. |

---

## Usage Recipes

### List all junction signals and their current branch

```csharp
var signals = SignalsAPI.GetAllSignals();
if (signals == null) return;

foreach (var s in signals)
{
    if (s.JunctionId != null)
    {
        Debug.Log($"Signal {s.Id} at junction {s.JunctionId} " +
                  $"(branch {s.SelectedBranch}, dir={s.Direction})");
    }
}
```

### Set a signal to STOP and release it after 30 seconds

```csharp
IEnumerator HoldSignal(string signalId)
{
    SignalsAPI.SetSignalAspect(signalId, "STOP");
    yield return new WaitForSeconds(30f);
    SignalsAPI.SetSignalMode(signalId, SignalMode.Automatic);
}
```

### Listen for any signal turning red

```csharp
SignalsAPI.Loaded += () =>
{
    SignalsAPI.Instance!.SignalAspectChanged += state =>
    {
        if (state.CurrentAspectId == "STOP")
            Debug.Log($"Signal {state.Id} turned red!");
    };
};
```

### Query a signal's full context

```csharp
var s = SignalsAPI.GetSignal("S-0370-MF-T");
if (s != null)
{
    Debug.Log($"Signal:    {s.Id}");
    Debug.Log($"Position:  {s.Position}");
    Debug.Log($"Aspect:    {s.CurrentAspectId ?? "(off)"}");
    Debug.Log($"Mode:      {s.Mode}");
    Debug.Log($"Type:      {s.Type}");
    Debug.Log($"Direction: {s.Direction}");
    Debug.Log($"Junction:  {s.JunctionId ?? "N/A"}");
    Debug.Log($"Branch:    {s.SelectedBranch?.ToString() ?? "N/A"}");
    Debug.Log($"Yard:      {s.YardId ?? "N/A"}");
    Debug.Log($"Track:     {s.TrackId ?? "N/A"}");
}
```
