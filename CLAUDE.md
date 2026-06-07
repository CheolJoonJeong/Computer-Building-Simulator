# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity C# project: a PC-assembly simulation game. Players select hardware parts, snap them into case slots, route/connect internal cables (with realistic Verlet-physics simulation), and clean up cable routing. There's also a separate "Estimate" feature that uses the Gemini API to recommend PC builds based on budget/purpose.

## Development

This is a standard Unity project (open via Unity Editor / Unity Hub, see `ProjectSettings/`). There is no separate CLI build/test/lint pipeline checked into the repo — builds, play-mode testing, and iteration happen through the Unity Editor (Play button, Build Settings). When making script changes, verify by entering Play mode in the Editor rather than expecting a CLI test runner.

C# scripts live under `Assets/Scripts/`, organized into:
- `Assembly/` — part selection, snapping, and assembly-completion logic
- `Assembly/Cable/` — cable spawning, routing, physics simulation, and cleanup mini-game
- `Assembly/Cable/Editor/` — editor-only tooling (prefab scaffolding, scene setup, collider fixes)
- `Estimate/` — Gemini-API-backed PC build recommendation feature

## Architecture

### Part Selection & Snapping (`Assembly/`)

- **`PartData`** (ScriptableObject) defines a part's name and `PartType` (CPU/GPU/RAM/Mainboard/PSU/SSD/HDD/Cooler/Case/FrontPanel/BackPanel/PSUCover). **`PartInfo`** attaches a `PartData` reference to a part's root GameObject and is located via `GetComponentInParent` — this is how other systems (e.g. `CableOverlapChecker`) resolve "which part" a collider belongs to.
- **`PartSelectionManager`** is a pure static state holder (`SelectedPart`/`SelectedSlot`/`SelectedButton`, plus `Clear()`) — not an instanced singleton.
- **`PartSelector`** (on UI buttons) toggles selection, manages assembled/unassembled visuals, and on detach calls `SnapZone.ForceDetach()` and `CableOverlapChecker.OnPartDetached`. Detach/selection are blocked when `CableOverlapChecker.Instance.IsBlocked` or when the part has connected cables (`HasConnectedCable`).
- **`SnapZone`** (on slots) is the click target: validates the selected part's `PartType` against `acceptType`, reparents/positions it, switches its layer to `AssembledPart` (keeps colliders but stops raycasts via `SetLayerRecursively`), then triggers `CableOverlapChecker.RunCheckForPart` and `AssemblyCompletionChecker.CheckCompletion`. Supports pre-placed parts via `startOccupied`/`startPart`.
- **`AssemblyCompletionChecker`** (singleton) polls all `SnapZone.isOccupied` + `CableSpawner.IsAssembled`; when complete, shows the result panel and may invoke `CableAreaChecker` for a cable-routing grade.

**Flow**: button click sets static selection → click a `SnapZone` → type-check via `PartInfo.data` → snap/reparent/relayer → notify completion & overlap checkers.

### Cable System (`Assembly/Cable/`)

Cable prefabs (scaffolded by the editor tool `CableBuilder`) follow a fixed structure: root GameObject has `LineRenderer` + `CableComponent` + `CableInteraction`, with child `StartPoint`/`EndPoint` objects each holding a `CableConnector` (`isEndPoint` distinguishes them). `CableType` enumerates the supported connectors (ATX24Pin, CPU8Pin, PCIe8Pin, FanHeader, PWRSW, RESET, PLED, HDD_LED, FrontUSB3).

- **`CableComponent`** runs a Verlet-physics simulation (particle array; `FixedUpdate` integrates, solves distance constraints, and does sphere-cast collision avoidance). It exposes a pin API (`PinParticle`/`UnpinParticle`/`SetEndAnchor`/`AddRouteAnchor`) mapping particle indices to Transforms; route anchors are spread proportionally along the cable via `RebuildRoutePins`.
- **`CableManager`** (singleton) is the central state machine driving routing: `Idle → TypeSelected → Routing → Idle`. It raycasts for `CableSocket`/`CablePassThrough` clicks, accumulates route anchors, and on final connection applies the destination socket's `EndRoute`, calls `cable.SetEndAnchor`, and notifies the spawner (`OnConnected`).
- **`CableSpawner`** (UI button) owns a `cableType`/`cablePrefab`/optional `defaultRoute`/`sourceSocket`/`initialEndPoint`; instantiates and initializes cables (`SpawnAt` → positions the end visual at `initialEndPoint` (or a default offset below the start) → `cable.Init` → `cable.HoldEndInPlace()`), or detaches an assembled cable. `HoldEndInPlace`/`ReleaseEnd` (in `CableComponent`) pin the end particle at its spawn position so it doesn't dangle as a free particle and get flung by overlapping socket colliders before routing begins; `CableManager` calls `ReleaseEnd()` once routing actually starts.
- **`CableSocket`** (on parts) validates type + occupancy in `TryConnect` and snaps the `CableConnector` to its `AnchorTransform`; may define forced `endRoute` waypoints. **`CableConnector`** is the end-cap that performs the actual `ConnectTo(socket)` snap. **`CablePassThrough`** is a clickable case-hole waypoint used mid-route.
- **Cleanup mini-game**: `CableInteraction` lets the player pin cable particles to **`CableTiePoint`**s (static `All` registry) or, via Shift-click + **`CableBundler`** (singleton), gather multiple cables' particles onto a shared `_BundleAnchor`. **`CableCleanChecker`** verifies a cable has reached `minTiePoints` bound points.
- **`CableAreaChecker`** is a trigger volume that counts particles inside it and grades clutter via sorted `Threshold[]` (used for the front-panel routing grade).
- **`CableOverlapChecker`** (singleton) is a global gate: on part assembly or cable connection it sphere-overlaps cable particles against colliders, resolves the owning part via `PartInfo`, and tracks conflicts in `conflictParts`. Its `IsBlocked` flag gates actions in `PartSelector`, `SnapZone`, and `CableManager`, and it shows warning/transient UI messages.

**Editor tools** (`Cable/Editor/`): `CableBuilder` scaffolds cable prefabs per type; `CableSetup` bulk-attaches `CableTiePoint`/`CablePassThrough` to scene objects matching naming conventions (`Tie_Point*`/`Pass_Through_Point*`); `FixAssembledColliders` re-enables colliders on the `AssembledPart` layer; `PlayModeTransformSaver` records/restores transforms across play sessions via `GlobalObjectId`.

**Big-picture cable flow**: spawner button → `CableManager` state machine → `SpawnAt` instantiates & inits `CableComponent` → source socket connects (anchors start) → user clicks pass-throughs (route anchors accumulate) → destination socket connects + `EndRoute` + `SetEndAnchor` finalizes physics pinning → spawner notified → completion/overlap checkers re-evaluate.

### Estimate System (`Estimate/`)

Gemini-API-backed PC build recommendation, in two stages:
1. **`PCRecommendationManager`** sends budget/purpose to **`GeminiClient`** (wraps `UnityWebRequest` to the Gemini `generateContent` REST endpoint, serializing via `JsonUtility`), parses a trailing JSON blob (`PickData`) out of the response by brace-extraction, and populates three `TMP_Dropdown`s with CPU/GPU/RAM picks.
2. The user finalizes CPU/GPU/RAM choices; the manager builds a **`QuoteData`** DTO (`budget`, `purpose`, `selected_parts: {cpu, gpu, ram}`) and sends a second prompt requesting a full compatible build (motherboard, RAM model, SSD, PSU, cooler, case), displaying the resulting quote text.

## Conventions & Patterns

- **Singletons**: `CableManager`, `CableOverlapChecker`, `CableBundler`, `AssemblyCompletionChecker` use static `Instance` with self-destruction of duplicates in `Awake`. `PartSelectionManager` is purely static (no instance).
- **Part identity**: code resolves "which part" a GameObject belongs to via `GetComponentInParent<PartInfo>()` — assume any collider/cable-particle could belong to a nested child and needs this upward lookup.
- **Layers**: assembled parts are moved to the `AssembledPart` layer to stop raycast hits while keeping physics colliders active.
- **Global blocking**: `CableOverlapChecker.IsBlocked` is a cross-cutting gate checked by selection, snapping, and cable-routing code — when adding new interactive actions, check whether they should respect it too.
