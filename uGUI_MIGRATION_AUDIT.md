# uGUI migration audit — 2026-07-15

1. Shell/input: standalone adaptive uGUI, manual hit testing, visible software cursor, smooth retained scroll — PASS
2. Player/Party: identity, gender/pronouns, levels/stats/HP/XP, currencies, Genodriver, equipment summary, companions — PASS
3. Monsters: list/inspector, species/Chimera, traits with descriptions/quality colors/grid/paging, loyalty/fertility/pigments/stats — PASS
4. Inventory/Eggs: search/paging/multiselect/batch quantities/quality, full item catalog/add, incubator/hatching — PASS
5. Farm: level/breeding summary, upgrade, crops, harvest charges, Ultra Bread, barn maintenance — PASS
6. Quests: dynamic discovery/search/active filter, inspector/dependency planning/safe jump/step/full completion/restart/repair — PASS
7. World/Dungeons: date/time/season/weather, migrations/calendar, dungeon progress/floor targets — PASS
8. Combat/Gallery: god mode/win/units/HP/statuses/movement; scene catalog/search/status/individual/all unlock — PASS
9. Advanced/Diagnostics/Safety: generic runtime editor, API compatibility checks, hash, logs, reports, consistency audit, automatic backup before destructive operations — PASS
10. Build/install: legacy IMGUI excluded from compilation, release build successful, deployed DLL hash D90CFBD2AB437F06BEDB5942CE501FBE1B00AD117D3245EB2CC0F39173AE2329 — PASS

Legacy sources are retained only as reference and are not compiled or instantiated.
