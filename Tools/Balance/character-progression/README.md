# Character progression — provisional first pass

Source of runtime tuning: `Assets/Resources/CharacterProgressionSettings.asset` (Unity Inspector).
Run the offline model with `python3 tools/balance/character-progression/simulate.py`.
Keep `settings.json` in sync with the Inspector values before comparing forecasts.

## Wave-based gold override

Stage 1 uses the Stage spreadsheet’s `웨이브 골드` tab, exported through `Master` into column J of `TextData/StageData.csv`.
The C column is the total gold paid on either result after reaching that numbered wave.
Values are not summed. Before wave 1, gold is 0; missing rows retain the last configured total.
The initial 122 entries rise to 300 gold. Spreadsheet values override fixed victory/defeat gold.
The forecast below describes the **fixed-reward fallback only**, not wave-dependent defeats.
Shard rewards and enhancement costs are unchanged.

## Rules and assumptions

- Two initially owned characters: swordsman (1) and mage (2). Locked roster slots receive no rewards.
- Each completed run grants gold and shards for exactly one uniformly random owned character.
- Victory: 300 gold + 6 shards; defeat: 100 gold + 3 shards. No payment for quitting mid-run.
- Results are saved immediately and at most once per run ID; retry creates a new run ID.
- Level `L` costs `200 + 100L` gold and `3 + 2L` shards, with `L=0..9`.
- Maximum +10. Each level adds 5% of the original maximum HP and 2 attack bonus percentage points.
- Gold is shared; shards and enhancement levels belong to the recipient character. Battle upgrades reset independently.
- Model spends only on one chosen character; other characters' shards accumulate. Results are runs, not minutes.

## Targets and evidence

| Target | Audience / horizon | Target range |
|---|---|---|
| First enhancement of one chosen character | New player, two owned characters | Mean 1–4 runs; p95 at most 6 |
| Permanent power cap | One character at +10 | +50% original HP, +20 attack bonus points |
| Currency conservation | Every purchase | No negative resources; exact cost deducted |

The simulator uses 2,000 deterministic seeds per scenario and validates the initial cost,
level-10 cost, full costs (6,500 gold + 120 shards), cap reachability and nonnegative stocks.
`results.json` records all-win, mixed and all-defeat forecasts. With the accepted values,
first enhancement averages 2.0–2.5 runs (p95 5); one chosen character reaches +10 in
40.15 all-win runs, 53.83 mixed runs or 80.18 all-defeat runs on average.

Random shard distribution means no individual character is guaranteed shards each run.
The result screen identifies the actual recipient and amount; no implied pity rule is used.
There is no paid economy, trading, currency conversion or reward-growth feedback loop.
Gold accumulation after maximizing the roster is intentional for this first pass; no unrequested sink was added.

These are initial design values, not a playtest-validated economy. Measure run length,
first-enhancement timing and UI comprehension in playtests before tuning the pacing.
