# Approved upgrade artwork — 2026-10-29

Applied the 12 individually approved designs to both level-up cards
(`Resources/UpgradeImageScrip`) and acquired-skill icons (`Resources/SkillUpgradeIcon`).

- FireBall, Lightning, Burn, FireStorm
- Shuriken, LightningShuriken
- Poison, Mold, Snail, Missile, Heatwave, LavaField

`ApprovedUpgradeCards.json` records each approved generated-image ID. The PNGs are
copied unchanged into `Assets/Sprite/ApprovedUpgradeCardIcons`.

Unity imports these monochrome black-background previews with **Alpha Source:
From Gray Scale**, **Alpha Is Transparency**, uncompressed Sprite/Single,
Bilinear filtering, no mipmaps, Full Rect mesh and CPU readability disabled.
Thus black becomes transparent without modifying the approved source drawings.

The card and HUD references share one sprite per design, avoiding duplicate
texture memory. The HUD retains its existing 76×76 entry and aspect-preserving
inset image. No runtime styling or layout override was added.

## Reapply / restore

Run `ApplyApprovedUpgradeCards.cs`, then `ApplyApprovedHudIcons.cs` using the
Unity Pipeline `eval_file` command. These files are one-shot editor operations,
not scripts under Assets or runtime code.

The `*Before_2026-10-29.tsv` files retain previous sprite GUIDs and local IDs.
The verification TSVs record imported pixel/alpha counts and matching card/HUD IDs.

Verified all 12 imported sprites contain both transparent and opaque pixels,
all 24 saved references point to the approved sprites, and Unity reports no
script compilation errors. Play-mode gameplay was not exercised for this asset swap.
