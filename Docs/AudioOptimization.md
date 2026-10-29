# Audio import optimization

Scope: 18 project SFX clips and one cutscene BGM. Settings were applied through Unity's AudioImporter and SaveAndReimport, then read back from the live Editor. All 19 source-file SHA-256 hashes are unchanged.

| Setting | Before | After |
| --- | --- | --- |
| SFX channels | Stereo | Force To Mono |
| SFX sample rate setting | Preserve | Override 22,050 Hz |
| SFX Vorbis quality | 100% | 70% |
| SFX load type | Decompress On Load | Decompress On Load |
| SFX preload | Off | On |
| BGM Vorbis quality | 100% | 75% |
| BGM channels / sample rate | Stereo / 44,100 Hz | Stereo / 44,100 Hz |
| BGM default load type | Streaming | Streaming |
| BGM background loading | Off | On |
| BGM WebGL override | None | Compressed In Memory, preload on |

Ambisonic remains off. Active target: WebGL. One active AudioListener and no scene AudioSources or AudioMixer assets were detected during the audit.

## Measured imported audio data

These are sums of Unity import artifact `.resource` files, not final build sizes or source-file sizes.

| Group | Before (bytes) | After (bytes) | Reduction |
| --- | ---: | ---: | ---: |
| SFX | 2,246,066 | 1,566,307 | 30.3% |
| BGM | 6,109,919 | 2,584,297 | 57.7% |
| Total | 8,355,985 | 4,150,604 | 50.3% |

Some individual short clips grew after re-encoding, while the combined SFX data decreased.

## Validation limits

Importer settings and resulting mono/stereo channels were verified after reimport. SFX remain decompressed at load time to avoid repeated decoding during playback. DSP CPU and frame time have not been measured in a device/browser build.

The Editor still reports AudioClip.frequency as 44,100 Hz despite the 22,050 Hz importer override, so a 75% runtime-memory reduction is not claimed.

WebGL BGM uses Compressed In Memory in accordance with the [Unity Web audio documentation](https://docs.unity3d.com/cn/6000.0/Manual/webgl-audio.html).
