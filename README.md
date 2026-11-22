# Courteous Eorzea

A small Dalamud plugin for FFXIV that says hello and thank you to your party or alliance.

Courteous Eorzea is a modified version of **[Polite Eorzea](https://github.com/AHAKuo/FFXIVPoliteEorzea) by ahadev ([AHAKuo](https://github.com/AHAKuo))**. All of the plugin's original design and features are their work. This fork fixes bugs that were reported upstream but not yet addressed. See [Changes from Polite Eorzea](#changes-from-polite-eorzea) below.

## Install

Courteous Eorzea is distributed through a custom plugin repository.

1. In game, type `/xlsettings` and open the **Experimental** tab.
2. Paste this URL into **Custom Plugin Repositories**, press the **+** button, then **Save**:

   ```
   https://raw.githubusercontent.com/thatbard/FFXIVCourteousEorzea/main/pluginmaster.json
   ```

3. Open `/xlplugins`, search for **Courteous Eorzea**, and install it.

**Coming from Polite Eorzea?** Disable or uninstall Polite Eorzea first. Both plugins register `/hi` and `/bye`, so only one should be enabled. On its first run, Courteous Eorzea copies your Polite Eorzea settings and messages. The old settings file is left untouched.

## Commands

| Command | What it does |
|---|---|
| `/hi` | Send a random greeting to party (or alliance when you are in one). |
| `/bye` | Send a random farewell the same way. |
| `/hi p` / `/hi a` / `/hi s` | Force party, alliance or say chat for that one message (same for `/bye`). |
| `/courteous` | Open the settings window. |
| `/courteous auto` | Toggle Auto mode. `/courteous on` and `/courteous off` also work. |

## Auto mode

When enabled, the plugin greets when you enter any Content Finder duty (dungeon, trial, raid, alliance raid, guildhest, deep dungeon, PvP if allowed) and sends a farewell when the duty completes. Both have a random delay range so they do not look instant. Solo instances are skipped by default.

Greeting timing can be either "when I load into the duty" or "when the duty starts" (barrier drops).

Messages go to alliance chat automatically when you are in an alliance, otherwise to party chat. You can pin a channel in the settings.

## Profiles

A profile is a named set of greetings and farewells. Manage them in the **Profiles** section of `/courteous`:

- **Edit profile** picks which profile the Greetings and Farewells lists below are editing. The buttons next to it create a new empty profile, duplicate the current one, or delete it (hold Ctrl and click).
- **Default profile** is used by every character that has no profile of its own.
- **This character** assigns a profile to the character you are logged in as. Characters are recognized by their character ID, so renames and world transfers keep their profile. **Character assignments** lists every assigned character, so you can change or remove an alt's profile without logging in to it.

## Synced farewells

Each greeting has a dropdown that can sync a farewell to it. After that greeting is sent (by `/hi` or Auto mode), the next farewell (by `/bye` or Auto mode) is the synced one, so a pair like "Hello! o/" and "Thanks for the run! o/" always goes out together. Greetings left on **Any farewell** work as before, with a farewell picked at random or in order.

A sync applies once: a second `/bye` picks normally. Entering a new duty or logging in also clears it, so a greeting from an earlier duty never decides a later farewell.

## Changes from Polite Eorzea

The full list, with dates, is in [CHANGELOG.md](CHANGELOG.md). In short:

- **Deleted default messages no longer come back** after a relog or plugin reload ([AHAKuo/FFXIVPoliteEorzea#1](https://github.com/AHAKuo/FFXIVPoliteEorzea/issues/1)). Saved message lists also no longer collect duplicate copies of the defaults, and existing duplicates are cleaned up once.
- Renamed to Courteous Eorzea (internal name `CourteousEorzea`, settings command `/courteous`) so it can't be mistaken for the original. Polite Eorzea settings are imported on first run.
- Added an "About & license" section to the settings window.
- Added message profiles, per-character profiles, and synced farewells (see above).
- New icon: the same speech-bubble design with "o7" (a salute) in place of "o/".

## Building

Requires the .NET 10 SDK and a Dalamud dev install (XIVLauncher). From the repo root:

```
dotnet build CourteousEorzea/CourteousEorzea.csproj -c Release
```

The plugin lands in `CourteousEorzea/bin/Release/CourteousEorzea.dll`. To load a local build, add that path under Dalamud Settings > Experimental > Dev Plugin Locations.

On Linux or WSL, point the build at the Dalamud files with `DALAMUD_HOME`, for example `DALAMUD_HOME=/mnt/c/Users/<you>/AppData/Roaming/XIVLauncher/addon/Hooks/dev/`.

## Releasing

1. Raise `<Version>` in `CourteousEorzea/CourteousEorzea.csproj` and add an entry to `CHANGELOG.md`.
2. Build in Release, then create a GitHub release tagged `vX.Y.Z` with `latest.zip` and `CourteousEorzea.json` from `CourteousEorzea/bin/Release/CourteousEorzea/`.
3. Update `pluginmaster.json`: copy in the fields from the new `CourteousEorzea.json`, point the three `DownloadLink*` URLs at the new release's `latest.zip`, and set `LastUpdate` to the current Unix time.

## Credits

- **ahadev ([AHAKuo](https://github.com/AHAKuo))**: author of [Polite Eorzea](https://github.com/AHAKuo/FFXIVPoliteEorzea), which this project is based on, and of the original icon design.
- **thatbard**: maintainer of this fork and its modifications. The current icon (`images/icon.png`, "o7" in place of "o/") was redrawn after ahadev's design.
- [believeinyuna](https://github.com/believeinyuna), for reporting the disappearing-messages bug upstream.

## License

Copyright (C) 2026 ahadev (Polite Eorzea)
Modifications copyright (C) 2026 thatbard

This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3 of the License, the same license as the original Polite Eorzea.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See [LICENSE](LICENSE) for the full text.
