# Changelog

Courteous Eorzea is a modified version of [Polite Eorzea](https://github.com/AHAKuo/FFXIVPoliteEorzea) by ahadev. This file records the changes made in this fork, with dates, as AGPL-3.0 section 5(a) requires. Everything before 0.1.3 is the original author's work.

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
