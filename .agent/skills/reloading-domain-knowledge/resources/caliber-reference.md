# Caliber Reference Data

Use this when creating CaliberDefinition ScriptableObjects. Values are real-world SAAMI specifications.

## Rimfire

Rimfire calibers are buy-only in current gameplay scope and should be authored with `isReloadable=false` on `CaliberDefinition`.

| Caliber | Bullet Dia | Case Length | Max OAL | Max Pressure | Bolt Face | Primer |
|---------|-----------|-------------|---------|-------------|-----------|--------|
| .22 LR | .223" | .613" | 1.000" | 24,000 PSI | Rimfire | Rimfire |
| .22 WMR | .224" | 1.055" | 1.350" | 24,000 PSI | Rimfire | Rimfire |
| .17 HMR | .172" | 1.058" | 1.349" | 26,000 PSI | Rimfire | Rimfire |

## Pistol

| Caliber | Bullet Dia | Case Length | Max OAL | Max Pressure | Bolt Face | Primer |
|---------|-----------|-------------|---------|-------------|-----------|--------|
| 9mm Luger | .355" | .754" | 1.169" | 35,000 PSI | Small | Small Pistol |
| .40 S&W | .400" | .850" | 1.135" | 35,000 PSI | Small | Small Pistol |
| .45 ACP | .452" | .898" | 1.275" | 21,000 PSI | Large | Large Pistol |
| 10mm Auto | .400" | .992" | 1.260" | 37,500 PSI | Small | Large Pistol |
| .357 Magnum | .357" | 1.290" | 1.590" | 35,000 PSI | Small | Small Pistol |
| .44 Magnum | .429" | 1.285" | 1.610" | 36,000 PSI | Large | Large Pistol |

## Rifle — Small Caliber

| Caliber | Bullet Dia | Case Length | Max OAL | Max Pressure | Bolt Face | Primer |
|---------|-----------|-------------|---------|-------------|-----------|--------|
| .223 Remington | .224" | 1.760" | 2.260" | 55,000 PSI | Small | Small Rifle |
| 5.56 NATO | .224" | 1.760" | 2.260" | 62,366 PSI | Small | Small Rifle |
| .22-250 Rem | .224" | 1.912" | 2.350" | 65,000 PSI | Small | Large Rifle |
| .204 Ruger | .204" | 1.850" | 2.260" | 57,500 PSI | Small | Small Rifle |

## Rifle — Medium Caliber

| Caliber | Bullet Dia | Case Length | Max OAL | Max Pressure | Bolt Face | Primer |
|---------|-----------|-------------|---------|-------------|-----------|--------|
| 6.5 Creedmoor | .264" | 1.920" | 2.825" | 62,000 PSI | Small | Large Rifle |
| 6.5 PRC | .264" | 2.030" | 2.955" | 65,000 PSI | Magnum | Large Rifle |
| .243 Winchester | .243" | 2.045" | 2.710" | 60,000 PSI | Small | Large Rifle |
| .260 Remington | .264" | 2.035" | 2.800" | 60,000 PSI | Small | Large Rifle |
| 6mm BR | .243" | 1.560" | 2.350" | 52,000 PSI | Small | Small Rifle |

## Rifle — .30 Caliber

| Caliber | Bullet Dia | Case Length | Max OAL | Max Pressure | Bolt Face | Primer |
|---------|-----------|-------------|---------|-------------|-----------|--------|
| .308 Winchester | .308" | 2.015" | 2.810" | 62,000 PSI | Small | Large Rifle |
| .30-06 Springfield | .308" | 2.494" | 3.340" | 60,000 PSI | Small | Large Rifle |
| .300 Win Mag | .308" | 2.620" | 3.340" | 64,000 PSI | Magnum | Large Rifle Mag |
| .300 PRC | .308" | 2.580" | 3.700" | 65,000 PSI | Magnum | Large Rifle Mag |
| .30-30 Winchester | .308" | 2.039" | 2.550" | 42,000 PSI | Small | Large Rifle |
| .300 Blackout | .308" | 1.368" | 2.260" | 55,000 PSI | Small | Small Rifle |

## Rifle — Magnum & Large

| Caliber | Bullet Dia | Case Length | Max OAL | Max Pressure | Bolt Face | Primer |
|---------|-----------|-------------|---------|-------------|-----------|--------|
| .338 Lapua Mag | .338" | 2.724" | 3.681" | 61,000 PSI | Magnum | Large Rifle Mag |
| .375 H&H Mag | .375" | 2.850" | 3.600" | 62,000 PSI | Magnum | Large Rifle Mag |
| .50 BMG | .510" | 3.910" | 5.450" | 55,000 PSI | .50 BMG | .50 BMG Primer |

## Notes

- Pressure values are SAAMI maximum average pressure (MAP). In-game these serve as REFERENCE points for consequence calculation, not hard limits.
- 5.56 NATO operates at higher pressure than .223 Rem. A .223 chamber may not safely fire 5.56 ammo (thinner brass at case head). Important for the game's consequence system.
- Wildcat calibers won't have SAAMI specs. Use the parent case data as a starting point and adjust based on the specific wildcat's intended performance.
- Bolt face sizes: Small (~.473"), Large (same as Small for rifles, separate for pistols), Magnum (~.532"), .50 BMG (~.804"), Rimfire. The `boltFace` field on CaliberDefinition matches these.
- Primer types now use three fields: `primerSize` (Small/Large/.50BMG), `primerApplication` (Pistol/Rifle), `isMagnum` (bool). Examples: "Small Rifle" = Small + Rifle + false. "Large Rifle Mag" = Large + Rifle + true. ".50 BMG Primer" = .50BMG + Rifle + false.
- Rows showing "Large Rifle Mag" (.300 Win Mag, .300 PRC, .338 Lapua Mag, .375 H&H Mag) map to primerSize=Large, primerApplication=Rifle, isMagnum=true.
