# Live Experience Tracker

A small client-side Valheim mod that shows your skill XP as you earn it, in the bottom-left corner just above your health and food.

![Skill rows above the health panel](https://raw.githubusercontent.com/dsoltyka/ValheimLiveExperienceTracker/main/docs/screenshot.png)

Every time an action grants skill XP a row appears for that skill, stacking upward from the health panel:

```
Swords  [██████░░░░░░]  (7.35 / 12.02)  +1.00
Run     [██░░░░░░░░░░]  (0.41 / 2.33)   +0.02
```

* **Skill name** in yellow, **progress bar** to the next level, **current / needed XP** in white, and the **latest gain** in green.
* Keep doing the same thing and the row updates live with each gain.
* Stop, and the row fades out after a few seconds (5 by default, configurable).
* Several skills at once give several rows, each with its own timer, always sorted alphabetically.

Works with vanilla skills and with custom skills added through Jötunn.

## Installation

Install with r2modman / Thunderstore Mod Manager, or drop `LiveExperienceTracker.dll` into `BepInEx/plugins`.

Requires [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) and [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/). Client-side only: it does not need to be on the server and does nothing on a dedicated server.

## Configuration

`BepInEx/config/dsoltyka.LiveExperienceTracker.cfg` is created on first launch. All settings apply immediately, and everything is editable in-game with [Configuration Manager](https://thunderstore.io/c/valheim/p/Azumatt/Official_BepInEx_ConfigurationManager/).

| Section | Setting | Default | Meaning |
| --- | --- | --- | --- |
| General | `DisplaySeconds` | 5 | Seconds a row stays after its last XP gain |
| General | `FadeSeconds` | 0.5 | Fade-out length at the end of that time (0 = pop out) |
| General | `DecimalPlaces` | 2 | Decimals for the XP numbers |
| Layout | `FontSize` | 16 | Text size |
| Layout | `BarWidth` / `BarHeight` | 120 / 10 | Progress bar size in pixels |
| Layout | `ElementSpacing` | 6 | Gap between name, bar and numbers |
| Layout | `RowSpacing` | 2 | Gap between rows |
| Layout | `OffsetX` / `OffsetY` | 0 / 4 | Nudge from the health panel's top-left corner |
| Colors | `SkillName`, `Values`, `Gain`, `BarFill`, `BarBackground` | | Row colors |

## How it works

The game applies every skill gain through `Skills.Skill.Raise`. The mod postfixes that with Harmony, reads the skill's accumulator and next-level requirement, and updates the matching row. The UI is plain uGUI + TextMeshPro parented to the HUD root, using the HUD's own font so it matches the rest of the interface. It hides with the HUD (Ctrl+F3) and is anchored to the health panel, so it follows GUI scale changes and the panel growing with food.

## Source

Code is on GitHub at [dsoltyka/ValheimLiveExperienceTracker](https://github.com/dsoltyka/ValheimLiveExperienceTracker). Build instructions are in [BUILDING.md](https://github.com/dsoltyka/ValheimLiveExperienceTracker/blob/main/BUILDING.md).

## License

MIT. See [LICENSE](https://github.com/dsoltyka/ValheimLiveExperienceTracker/blob/main/LICENSE).
