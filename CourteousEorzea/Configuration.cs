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
    public const int CurrentVersion = 1;

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

    // Dalamud deserializes with Newtonsoft.Json, whose default ObjectCreationHandling.Auto reuses the
    // list created by the initializer below and appends the saved entries onto it. That made deleted
    // default messages reappear (and duplicates pile up) on every load. Replace makes the saved list
    // win outright, so the defaults are only used for a brand-new config.
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<string> Greetings { get; set; } =
    [
        "Hello! o/",
        "Hi everyone! :)",
        "Hey all, good luck and have fun!",
        "o/ Hello, hello!",
        "Hi! Thanks for having me.",
    ];

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<string> Farewells { get; set; } =
    [
        "Thanks for the run! o/",
        "GG, thank you all!",
        "Thanks everyone, have a great day!",
        "ty for the party! o/",
        "Well played, thanks all!",
    ];

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
            // and maps the properties onto this class. Its Version (0) then runs the v1 cleanup in Migrate.
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
            // Configs saved before v1 were hit by the list-append bug above, so they can hold the
            // default messages repeated once per load/save cycle. Collapse exact duplicates.
            Greetings = Dedupe(Greetings);
            Farewells = Dedupe(Farewells);
        }

        Version = CurrentVersion;
        return true;
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
