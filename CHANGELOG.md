# Changelog

Courteous Eorzea is a modified version of [Polite Eorzea](https://github.com/AHAKuo/FFXIVPoliteEorzea) by ahadev. This file records the changes made in this fork, with dates, as AGPL-3.0 section 5(a) requires. Everything before 0.1.3 is the original author's work.

## 0.3.0 (2026-11-22)

- **Profiles:** greetings and farewells now live in named profiles. Create, duplicate, rename and delete them in `/courteous`, and choose which one is the default.
- **Per-character profiles:** assign a profile to the character you're playing. Characters without one use the default profile. All assignments are listed so alts can be managed from any character.
- **Synced farewells:** each greeting can be linked to a farewell, which is then sent after that greeting instead of a random one.
- Settings format version 2: your existing greetings and farewells become a profile named "Default". Older Polite Eorzea and Courteous Eorzea settings both migrate automatically.

## Icon update (2026-11-22)

- Replaced the plugin icon with a redrawn version of the original design that shows "o7" instead of "o/". The icon is served from the repo, so this needed no new plugin version.

## 0.2.0 (2026-11-22)

- Renamed the plugin from Polite Eorzea to **Courteous Eorzea**. The internal name is now `CourteousEorzea` and the settings command is `/courteous` (alias `/courteouseorzea`), replacing `/polite` and `/politeeorzea`. `/hi` and `/bye` are unchanged.
- On first run, settings from an existing Polite Eorzea install (`pluginConfigs/PoliteEorzea.json`) are imported. The old file is not modified.
- Added an "About & license" section to the settings window with credits, source links, and the AGPL-3.0 notices.
- Repo, icon, and download links now point to this fork.

## 0.1.3 (2026-11-22)

- Fixed deleted default greetings and farewells coming back after every relog or plugin reload ([AHAKuo/FFXIVPoliteEorzea#1](https://github.com/AHAKuo/FFXIVPoliteEorzea/issues/1)). Dalamud's JSON loader added the saved lists onto the built-in defaults instead of replacing them. The lists now replace the defaults.
- Added a one-time settings migration that removes duplicate messages left behind by that bug.

## 0.1.0 to 0.1.2 (2026-09-27 to 2026-09-28)

Original releases of Polite Eorzea by ahadev. See the [upstream repository](https://github.com/AHAKuo/FFXIVPoliteEorzea).
