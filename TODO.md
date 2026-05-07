# TODO

## Per-dialogue ABM cap
- Add `maxConversationABM` setting (default 1), independent of `maxABMInjectionRounds`
- In `ABMCollector.Collect()`, skip conversation entries after reaching the cap, continue collecting non-conversation entries
- Files: `ABMCollector.cs`, `RimTalkSettings.cs`
