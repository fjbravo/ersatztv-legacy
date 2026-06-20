# BYLab Jellyfin Media Segments MVP Handoff

Date: 2026-06-19
Repo: `/Users/fjbravo/repos/ersatztv-legacy`
Branch: `feat/jellyfin-media-segment-skip`
Remote: `github.com/fjbravo/ersatztv-legacy`

## User request
Implement a working MVP for Jellyfin Media Segment / Intro Skipper integration in ErsatzTV Legacy. Use subagents where useful to avoid context bloat. Codex model preference: `gpt-5.5`, but prior local Codex attempts were unreliable; continue with Hermes tools/subagents if needed.

## Product decisions
- Target ErsatzTV Legacy, not the Rust rewrite.
- Use Jellyfin native MediaSegments API as authoritative source.
- Prefer Jellyfin-source items for MVP; path matching can be later.
- Preserve cold opens by splitting playback ranges around intro segments.
- Per-show policy, not only global/per-channel.
- Gilmore Girls default should keep intros (`TrimIntros=false`) unless explicitly enabled later.
- GHCR/GitHub Actions image pipeline is the deployment route.
- Do not replace production ErsatzTV on docker-03 until a cloned test channel is validated.

## Previously completed and pushed
Two commits already pushed to origin:
- `6c585f98 feat: add Jellyfin media segment primitives`
- `03e52da2 ci: disable NuGet audit during docker restore`

GitHub Actions previously passed:
- https://github.com/fjbravo/ersatztv-legacy/actions/runs/27846117633

Image tags previously produced:
- `ghcr.io/fjbravo/ersatztv-legacy:feat-jellyfin-media-segment-skip`
- `ghcr.io/fjbravo/ersatztv-legacy:jellyfin-segments-mvp-03e52da`

Local verification previously passed:
- `dotnet build ErsatzTV.sln /p:NuGetAudit=false`
- `dotnet test ErsatzTV.Core.Tests/ErsatzTV.Core.Tests.csproj --filter "FullyQualifiedName~MediaSegment" /p:NuGetAudit=false --no-build`
- Result for MediaSegment tests: 27 passed.

## Current uncommitted state
`git status --short --branch` showed:

```text
## feat/jellyfin-media-segment-skip...origin/feat/jellyfin-media-segment-skip
 M ErsatzTV.Application/Playouts/Commands/BuildPlayoutHandler.cs
 M ErsatzTV.Core/Domain/ConfigElementKey.cs
 M ErsatzTV.Core/Scheduling/BlockScheduling/BlockPlayoutBuilder.cs
 M ErsatzTV.Core/Scheduling/Engine/SchedulingEngine.cs
 M ErsatzTV.Core/Scheduling/PlayoutBuilder.cs
 M ErsatzTV.Core/Scheduling/PlayoutBuilderState.cs
 M ErsatzTV.Core/Scheduling/PlayoutModeSchedulerBase.cs
 M ErsatzTV.Core/Scheduling/PlayoutModeSchedulerDuration.cs
 M ErsatzTV.Core/Scheduling/PlayoutModeSchedulerFlood.cs
 M ErsatzTV.Core/Scheduling/PlayoutModeSchedulerMultiple.cs
 M ErsatzTV.Core/Scheduling/PlayoutModeSchedulerOne.cs
 M ErsatzTV.Core/Scheduling/PlayoutReferenceData.cs
?? ErsatzTV.Core/MediaSegments/MediaSegmentPlayoutConfiguration.cs
?? ErsatzTV.Core/MediaSegments/MediaSegmentPlayoutPlanner.cs
?? ErsatzTV.Core/MediaSegments/MediaSegmentPolicyConfiguration.cs
```

Important: these edits are not verified after the most recent changes. Start by inspecting them and running a build.

## Implemented in current uncommitted state
The current edit pass started wiring segment playback ranges into scheduling:

### `PlayoutBuilderState`
- Added `MediaSegmentPlaybackRanges` as `IReadOnlyDictionary<int, IReadOnlyList<PlaybackRange>>`.
- Defaulted null to an empty dictionary.

### `PlayoutBuilder`
- Passes `referenceData.MediaSegmentPlaybackRanges` into `PlayoutBuilderState`.

### `PlayoutModeSchedulerBase`
- Added helpers:
  - `DurationForMediaItem(mediaItem, state)`
  - `PlaybackRangesForMediaItem(mediaItem, state)`
  - `HasMediaSegmentRanges(ranges, mediaDuration)`
  - `ApplyMediaSegmentRanges(playoutItems, template, ranges, mediaDuration)`
- `ApplyMediaSegmentRanges` replaces the original scheduled item with one or more split `PlayoutItem`s produced by `MediaSegmentPlayoutPlanner.ApplyRanges`.

### `PlayoutModeSchedulerOne`, `Multiple`, `Flood`, `Duration`
- Replaced raw media duration with segment-aware effective duration.
- Gets playback ranges from state.
- Disables chapter-derived filler when segment ranges are active by using empty chapter list.
- Applies segment ranges after `AddFiller`.

### `MediaSegmentPolicyConfiguration.cs`
- New config DTO created, but likely overlaps with `MediaSegmentPlayoutConfiguration.cs` and may be unnecessary.
- Reconcile before continuing.

### `BuildPlayoutHandler.cs`
- It already contains JSON config loading for `ConfigElementKey.JellyfinMediaSegments` and `GetMediaSegmentPlaybackRanges` near the end of the file.
- Most recent patch added constructor dependencies:
  - `IJellyfinApiClient`
  - `IJellyfinSecretStore`
- It added using statements:
  - `ErsatzTV.Core.Interfaces.Jellyfin`
  - `ErsatzTV.Core.Jellyfin`
- This file must be re-read before further edits because it appears prior partial edits already exist beyond what was shown in the latest turn.

## Known current concern
The uncommitted state is probably mid-edit and may not compile yet. In particular:
- `BuildPlayoutHandler` has newly injected services that may not yet be used.
- `MediaSegmentPolicyConfiguration.cs` may duplicate `ShowMediaSegmentSkipPolicy` in `MediaSegmentPlayoutConfiguration.cs`.
- `ConfigElementKey.cs` already has `JellyfinMediaSegments => new("jellyfin.media_segments")`; don't add a second key unless intentionally migrating.
- Need to inspect modifications in `BlockPlayoutBuilder.cs` and `SchedulingEngine.cs`; they were modified before/around the current pass and must be understood before deciding whether to keep or revert.
- `IReadOnlyList<PlaybackRange>` imports may need `using ErsatzTV.Core.MediaSegments;` where not already present.

## Recommended continuation steps
1. In a new session, start with:
   - `cd /Users/fjbravo/repos/ersatztv-legacy`
   - `git status --short --branch`
   - `git diff --stat`
   - inspect `git diff` for all listed files.
2. Run:
   - `dotnet build ErsatzTV.sln /p:NuGetAudit=false`
3. Fix compile errors first, no new feature expansion until build passes.
4. Run targeted tests:
   - `dotnet test ErsatzTV.Core.Tests/ErsatzTV.Core.Tests.csproj --filter "FullyQualifiedName~MediaSegment" /p:NuGetAudit=false`
5. Add or update tests for scheduler integration if feasible:
   - effective duration reflected in playout timing
   - cold-open split produces two PlayoutItems around intro
   - outro trims finish time
   - missing/no policy keeps full item
6. Only after local build/tests pass, commit and push.
7. Then run/monitor GitHub Actions and use GHCR image for a cloned BYLab test channel.

## Minimal next implementation decision
Prefer finishing a configuration-file/ConfigElement based MVP first, not a full UI/migration:
- `ConfigElementKey.JellyfinMediaSegments` stores JSON:
  - show policies
  - cached item segments
- `BuildPlayoutHandler.GetMediaSegmentPlaybackRanges` converts config into playback ranges.
- This is enough for a working setup if the config can be seeded manually for a cloned test channel.

A fuller follow-up can add UI/API and automatic Jellyfin segment refresh.

## Manual test setup idea
Seed `jellyfin.media_segments` config JSON with:
- show policy for test show ID
- cached `MediaItemId`, `JellyfinItemId`, `Type`, `StartTicks`, `EndTicks`
Then rebuild playout and inspect generated `PlayoutItems` for adjusted `InPoint`, `OutPoint`, `Start`, `Finish`.

## Don't do yet
- Do not deploy to docker-03 production.
- Do not modify production channels `101 - DannyGo TV` or `102 - Gilmore Girls TV`.
- Do not push/commit without build/test evidence.
