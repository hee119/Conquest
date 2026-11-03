# macOS resolution build verification

Build: `Builds/MacResolutionTest/Terraformer.app` (Development, universal x86_64 + arm64).

- 1280×720, 1366×768, 1600×900, 1920×1080, 2560×1440, 3840×2160.
- Windowed, MaximizedWindow, FullScreenWindow: all 18 combinations matched requested dimensions and saved values.
- Settings reentry and six Stage1 settings checks: 25 total UI samples; dropdown bounds and raycast checks passed.
- Stage1 center, four edges, four corners at six resolutions: 54 camera positions, 77 viewport floor samples per position; no empty tile samples.
- Actual screenshots inspected: settings at 3840×2160 and map at center/corner.
- No runtime exceptions recorded. Original resolution/mode preference keys restored.

Fixed a race where an immediate resolution change canceled the pending screen-mode change by using the saved requested mode rather than the previous frame's Screen.fullScreenMode. Also fixed editor-only OnValidate override compilation in ParabolicDottedRoute.

Probe source is stored here, outside Assets; it is not included in future builds. The test app activates it only with `--resolution-test`. Run without that argument for normal gameplay.

Manual OS-window interaction was unavailable while the Mac was locked. Automated tests drove the same dropdown events and checked Unity's live rendered dimensions and UI raycasts. This is not an exhaustive platform certification.
