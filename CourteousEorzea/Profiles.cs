using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CourteousEorzea;

/// <summary>One greeting or farewell line. The Id stays stable when the text is edited or the list is reordered.</summary>
[Serializable]
public sealed class MessageEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Greetings only: the farewell to send after this greeting ("synced" farewell).
    /// Null means any farewell may be picked.
    /// </summary>
    public Guid? PairedFarewellId { get; set; }

    public MessageEntry() { }

    public MessageEntry(string text) => Text = text;
}

/// <summary>A named set of greetings and farewells. Characters can be assigned one.</summary>
[Serializable]
public sealed class MessageProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "Default";

    // Replace, not append, when loading (see the comment on Configuration.Profiles).
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<MessageEntry> Greetings { get; set; } = [];

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<MessageEntry> Farewells { get; set; } = [];

    public static readonly string[] DefaultGreetings =
    [
        "Hello! o/",
        "Hi everyone! :)",
        "Hey all, good luck and have fun!",
        "o/ Hello, hello!",
        "Hi! Thanks for having me.",
    ];

    public static readonly string[] DefaultFarewells =
    [
        "Thanks for the run! o/",
        "GG, thank you all!",
        "Thanks everyone, have a great day!",
        "ty for the party! o/",
        "Well played, thanks all!",
    ];

    /// <summary>A profile holding the built-in default messages.</summary>
    public static MessageProfile CreateDefault(string name = "Default") =>
        FromLists(name, DefaultGreetings, DefaultFarewells);

    public static MessageProfile FromLists(string name, IEnumerable<string> greetings, IEnumerable<string> farewells)
    {
        var profile = new MessageProfile { Name = name };
        foreach (var g in greetings) profile.Greetings.Add(new MessageEntry(g));
        foreach (var f in farewells) profile.Farewells.Add(new MessageEntry(f));
        return profile;
    }

    /// <summary>Deep copy with fresh ids; greeting/farewell links are carried over to the copies.</summary>
    public MessageProfile Duplicate(string newName)
    {
        var copy = new MessageProfile { Name = newName };
        var farewellIdMap = new Dictionary<Guid, Guid>();
        foreach (var f in Farewells)
        {
            var nf = new MessageEntry(f.Text);
            farewellIdMap[f.Id] = nf.Id;
            copy.Farewells.Add(nf);
        }
        foreach (var g in Greetings)
        {
            var ng = new MessageEntry(g.Text);
            if (g.PairedFarewellId is { } pid && farewellIdMap.TryGetValue(pid, out var newPid))
                ng.PairedFarewellId = newPid;
            copy.Greetings.Add(ng);
        }
        return copy;
    }

    public MessageEntry? FindGreeting(Guid id) => Greetings.Find(e => e.Id == id);

    public MessageEntry? FindFarewell(Guid? id) => id is { } v ? Farewells.Find(e => e.Id == v) : null;

    /// <summary>Removes a farewell and unlinks any greeting that was synced to it.</summary>
    public void RemoveFarewellAt(int index)
    {
        var id = Farewells[index].Id;
        Farewells.RemoveAt(index);
        foreach (var g in Greetings)
        {
            if (g.PairedFarewellId == id)
                g.PairedFarewellId = null;
        }
    }
}

/// <summary>Which profile a character uses. Name and world are only kept for display.</summary>
[Serializable]
public sealed class CharacterAssignment
{
    public Guid ProfileId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public string World { get; set; } = string.Empty;

    [JsonIgnore]
    public string DisplayName => string.IsNullOrEmpty(World) ? CharacterName : $"{CharacterName} @ {World}";
}
