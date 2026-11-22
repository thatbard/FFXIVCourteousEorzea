using System;
using System.Collections.Generic;
using System.IO;
using Dalamud.Configuration;
using Newtonsoft.Json;

namespace CourteousEorzea;

public enum GreetTrigger
{
    /// <summary>Send as soon as you have loaded into the duty (before the countdown).</summary>
    OnLoad = 0,

    /// <summary>Send when the duty actually starts (barrier drops / "Duty commenced").</summary>
    OnDutyStart = 1,
}

public enum ChatTarget
{
    /// <summary>Alliance chat when in an alliance, otherwise party chat.</summary>
    Auto = 0,
    Party = 1,
    Alliance = 2,
    Say = 3,
}

[Serializable]
public class Configuration : IPluginConfiguration
{
    /// <summary>Bump when a migration is added in <see cref="Migrate"/>.</summary>
    public const int CurrentVersion = 2;

    public int Version { get; set; } = CurrentVersion;

    /// <summary>Master switch for automatic greetings and farewells.</summary>
    public bool AutoMode { get; set; } = false;

    public bool AutoGreet { get; set; } = true;
    public bool AutoFarewell { get; set; } = true;

    public GreetTrigger GreetTrigger { get; set; } = GreetTrigger.OnLoad;

    /// <summary>Random delay range (seconds) before an automatic greeting is sent.</summary>
    public float GreetDelayMin { get; set; } = 1.5f;
    public float GreetDelayMax { get; set; } = 4f;

    /// <summary>Random delay range (seconds) before an automatic farewell is sent.</summary>
    public float FarewellDelayMin { get; set; } = 2f;
    public float FarewellDelayMax { get; set; } = 6f;

    /// <summary>Where messages go when no explicit channel is given.</summary>
    public ChatTarget DefaultTarget { get; set; } = ChatTarget.Auto;

    /// <summary>Do not auto-send when you are not in a party (solo instances, unsynced solo runs).</summary>
    public bool SkipWhenSolo { get; set; } = true;

    /// <summary>Do not auto-send in PvP duties.</summary>
    public bool SkipPvP { get; set; } = false;

    /// <summary>Pick messages at random; when false, cycle through the list in order.</summary>
    public bool RandomOrder { get; set; } = true;

    /// <summary>Echo what the plugin did (or why it skipped) to your own chat log.</summary>
    public bool ShowFeedback { get; set; } = true;

    // Dalamud deserializes with Newtonsoft.Json, whose default ObjectCreationHandling.Auto reuses a
    // collection created by an initializer/constructor and appends the saved entries onto it. That made
    // deleted default messages reappear on every load (v0.1.2 and earlier). Every collection here and in
    // MessageProfile uses Replace so the saved data wins outright.

    /// <summary>Message profiles. Always holds at least one (see <see cref="EnsureValid"/>).</summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<MessageProfile> Profiles { get; set; }

    /// <summary>Profile used by any character without an assignment.</summary>
    public Guid DefaultProfileId { get; set; }

    /// <summary>Per-character profile, keyed by the character's content id.</summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public Dictionary<ulong, CharacterAssignment> CharacterProfiles { get; set; } = new();

    // Settings saved before v2 (and Polite Eorzea's) kept one flat list of each. They are read here
    // only so Migrate can move them into a profile, and are never written back.
    [JsonProperty("Greetings", ObjectCreationHandling = ObjectCreationHandling.Replace, NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? LegacyGreetings { get; set; }

    [JsonProperty("Farewells", ObjectCreationHandling = ObjectCreationHandling.Replace, NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? LegacyFarewells { get; set; }

    public Configuration()
    {
        var profile = MessageProfile.CreateDefault();
        Profiles = [profile];
        DefaultProfileId = profile.Id;
    }

    public MessageProfile? GetProfile(Guid id) => Profiles.Find(p => p.Id == id);

    [JsonIgnore]
    public MessageProfile DefaultProfile => GetProfile(DefaultProfileId) ?? Profiles[0];

    /// <summary>The profile assigned to this character, or the default profile.</summary>
    public MessageProfile ProfileFor(ulong contentId, out bool assigned)
    {
        if (contentId != 0 && CharacterProfiles.TryGetValue(contentId, out var a) && GetProfile(a.ProfileId) is { } p)
        {
            assigned = true;
            return p;
        }
        assigned = false;
        return DefaultProfile;
    }

    /// <summary>Assigns a profile to a character; null removes the assignment (the default is used).</summary>
    public void AssignProfile(ulong contentId, string characterName, string world, Guid? profileId)
    {
        if (profileId is not { } id)
        {
            CharacterProfiles.Remove(contentId);
            return;
        }
        CharacterProfiles[contentId] = new CharacterAssignment { ProfileId = id, CharacterName = characterName, World = world };
    }

    /// <summary>Deletes a profile. The last remaining profile cannot be deleted.</summary>
    public bool DeleteProfile(Guid id)
    {
        if (Profiles.Count <= 1 || GetProfile(id) is not { } profile)
            return false;

        Profiles.Remove(profile);
        if (DefaultProfileId == id)
            DefaultProfileId = Profiles[0].Id;

        // Characters that used it fall back to the default profile.
        var orphaned = new List<ulong>();
        foreach (var (cid, a) in CharacterProfiles)
        {
            if (a.ProfileId == id)
                orphaned.Add(cid);
        }
        foreach (var cid in orphaned)
            CharacterProfiles.Remove(cid);
        return true;
    }

    public string UniqueProfileName(string baseName)
    {
        var name = baseName;
        for (var n = 2; Profiles.Exists(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)); n++)
            name = $"{baseName} {n}";
        return name;
    }

    /// <summary>
    /// First run only: reads settings left behind by the original Polite Eorzea plugin
    /// (pluginConfigs/PoliteEorzea.json) so people switching over keep their messages.
    /// The old file is left untouched. Returns null when there is nothing usable to import.
    /// </summary>
    public static Configuration? ImportFromPoliteEorzea(string? configDirectory)
    {
        if (string.IsNullOrEmpty(configDirectory))
            return null;

        var path = Path.Combine(configDirectory, "PoliteEorzea.json");
        if (!File.Exists(path))
            return null;

        try
        {
            // The file's "$type" names the old PoliteEorzea assembly; TypeNameHandling.None ignores it
            // and maps the properties onto this class. Its Version (0) then runs the v1 and v2 migrations.
            var imported = JsonConvert.DeserializeObject<Configuration>(
                File.ReadAllText(path),
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None });
            if (imported != null)
                Plugin.Log.Information("Imported settings from Polite Eorzea ({Path}).", path);
            return imported;
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning(ex, "Could not import Polite Eorzea settings from {Path}; starting fresh.", path);
            return null;
        }
    }

    /// <summary>Upgrades an older saved config in place. Returns true when something changed.</summary>
    public bool Migrate()
    {
        if (Version >= CurrentVersion)
            return false;

        if (Version < 1)
        {
            // Configs saved before v1 were hit by the list-append bug, so they can hold the default
            // messages repeated once per load/save cycle. Collapse exact duplicates.
            LegacyGreetings = Dedupe(LegacyGreetings);
            LegacyFarewells = Dedupe(LegacyFarewells);
        }

        if (Version < 2)
        {
            // v2 introduced profiles: the flat lists become the "Default" profile, which the
            // constructor already created (with the built-in messages) since the file had no Profiles.
            var profile = MessageProfile.FromLists("Default", LegacyGreetings ?? [], LegacyFarewells ?? []);
            if (LegacyGreetings == null && LegacyFarewells == null)
                profile = MessageProfile.CreateDefault();
            Profiles = [profile];
            DefaultProfileId = profile.Id;
            CharacterProfiles = new();
            LegacyGreetings = null;
            LegacyFarewells = null;
        }

        Version = CurrentVersion;
        return true;
    }

    /// <summary>
    /// Repairs anything a hand-edited or partially written file could leave inconsistent.
    /// Returns true when something changed.
    /// </summary>
    public bool EnsureValid()
    {
        var changed = false;

        Profiles ??= [];
        Profiles.RemoveAll(p => p == null);
        CharacterProfiles ??= new();

        if (Profiles.Count == 0)
        {
            Profiles.Add(MessageProfile.CreateDefault());
            changed = true;
        }

        var profileIds = new HashSet<Guid>();
        foreach (var p in Profiles)
        {
            if (p.Id == Guid.Empty || !profileIds.Add(p.Id))
            {
                p.Id = Guid.NewGuid();
                profileIds.Add(p.Id);
                changed = true;
            }
            p.Name ??= "Profile";
            p.Greetings ??= [];
            p.Farewells ??= [];
            p.Greetings.RemoveAll(e => e == null);
            p.Farewells.RemoveAll(e => e == null);

            var messageIds = new HashSet<Guid>();
            foreach (var e in p.Greetings)
            {
                e.Text ??= string.Empty;
                if (e.Id == Guid.Empty || !messageIds.Add(e.Id)) { e.Id = Guid.NewGuid(); messageIds.Add(e.Id); changed = true; }
            }
            foreach (var e in p.Farewells)
            {
                e.Text ??= string.Empty;
                e.PairedFarewellId = null;
                if (e.Id == Guid.Empty || !messageIds.Add(e.Id)) { e.Id = Guid.NewGuid(); messageIds.Add(e.Id); changed = true; }
            }
            foreach (var g in p.Greetings)
            {
                if (g.PairedFarewellId != null && p.FindFarewell(g.PairedFarewellId) == null)
                {
                    g.PairedFarewellId = null;
                    changed = true;
                }
            }
        }

        if (GetProfile(DefaultProfileId) == null)
        {
            DefaultProfileId = Profiles[0].Id;
            changed = true;
        }

        var stale = new List<ulong>();
        foreach (var (cid, a) in CharacterProfiles)
        {
            if (a == null || GetProfile(a.ProfileId) == null)
                stale.Add(cid);
        }
        foreach (var cid in stale)
        {
            CharacterProfiles.Remove(cid);
            changed = true;
        }

        return changed;
    }

    private static List<string> Dedupe(List<string>? list)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<string>();
        if (list == null)
            return result;
        foreach (var s in list)
        {
            if (s != null && seen.Add(s))
                result.Add(s);
        }
        return result;
    }

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
