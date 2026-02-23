# Relative Burn Rate Chart

Powders ordered from fastest (1) to slowest. Lower number = faster burn = higher pressure for a given charge weight. Use this when creating PowderDefinition ScriptableObjects.

## Burn Rate Index

| Index | Powder | Typical Use |
|-------|--------|-------------|
| 10 | Alliant Red Dot | Shotgun, light pistol |
| 15 | Alliant Bullseye | Pistol (target) |
| 20 | Hodgdon Titegroup | Pistol (all-purpose) |
| 30 | Alliant Unique | Pistol / light rifle |
| 40 | Alliant Power Pistol | Magnum pistol |
| 50 | Hodgdon H110 / Winchester 296 | Magnum revolver, .300 BLK |
| 60 | Alliant 2400 | Magnum pistol, .22 Hornet |
| 70 | IMR 4198 | .222, .223, small capacity rifle |
| 80 | Hodgdon Benchmark | .223, 6mm BR |
| 90 | Hodgdon Varget | .223, .308, 6.5 CM (extremely popular) |
| 95 | IMR 4064 | .308, .30-06, versatile medium rifle |
| 100 | Hodgdon H4350 | 6.5 CM, .270, .25-06 (top precision choice) |
| 110 | IMR 4451 | 6.5 CM, .308 |
| 120 | Hodgdon H4831 | .270, .30-06, medium magnums |
| 130 | Alliant Reloder 22 | .300 WM, 7mm Rem Mag |
| 140 | Hodgdon H1000 | .300 WM, .338 Lapua |
| 150 | Alliant Reloder 26 | 6.5 PRC, .300 PRC |
| 160 | Hodgdon Retumbo | Large magnums, .338 Lapua |
| 170 | Hodgdon H50BMG | .50 BMG |
| 180 | Vihtavuori 20N29 | .50 BMG, 20mm |

## Usage Rules

- **Fast powders (10-60):** Pistol and revolver. Using a fast powder in a rifle cartridge = extreme pressure spike = catastrophic.
- **Medium powders (70-110):** Small and medium rifle cartridges. The sweet spot for .223 through .308.
- **Slow powders (120-160):** Magnum rifle cartridges. Need large case volume to burn completely.
- **Very slow powders (160+):** Large magnums and .50 BMG. Would barely ignite in a small case.

## Matching Powder to Caliber

General rule: case capacity and bore diameter determine appropriate burn rate range.

| Cartridge Class | Burn Rate Range | Example Powders |
|----------------|-----------------|-----------------|
| Pistol (9mm, .45) | 10-50 | Titegroup, Unique, Power Pistol |
| Magnum revolver (.357, .44) | 40-60 | H110, 2400 |
| Small rifle (.223) | 70-95 | Varget, Benchmark, IMR 4198 |
| Medium rifle (.308, 6.5 CM) | 90-120 | Varget, H4350, IMR 4064 |
| Magnum rifle (.300 WM) | 120-150 | Reloder 22, H1000 |
| Large magnum (.338 Lapua) | 140-170 | Retumbo, H1000 |
| .50 BMG | 165-180 | H50BMG, VV 20N29 |

## In-Game Notes

- Wrong burn rate for caliber should have dramatic consequences. A pistol powder in a rifle case = catastrophic overpressure.
- Each powder has an optimal charge window per caliber. Below minimum = incomplete burn, poor accuracy, possible squib. Above maximum = overpressure.
- Temperature sensitivity varies by powder. Hodgdon Extreme series (Varget, H4350, etc.) is temperature-insensitive — good for hunting in varied conditions. Others shift pressure with temperature.
