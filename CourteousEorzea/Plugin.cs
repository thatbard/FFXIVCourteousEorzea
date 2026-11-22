using System;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using CourteousEorzea.Windows;

namespace CourteousEorzea;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static IDutyState DutyState { get; private set; } = null!;
    [PluginService] internal static IPartyList PartyList { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private const string MainCommand = "/courteous";
    private const string MainCommandAlias = "/courteouseorzea";
    private const string HiCommand = "/hi";
    private const string ByeCommand = "/bye";

    public Configuration Configuration { get; }
    public MessagePicker Picker { get; } = new();
    public DutyWatcher Watcher { get; }

    public readonly WindowSystem WindowSystem = new("CourteousEorzea");
    private ConfigWindow ConfigWindow { get; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration
                        ?? Configuration.ImportFromPoliteEorzea(PluginInterface.ConfigFile.DirectoryName)
                        ?? new Configuration();
        var migrated = Configuration.Migrate();
        if (Configuration.EnsureValid() | migrated)
            Configuration.Save();

        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);

        Watcher = new DutyWatcher(this);

        CommandManager.AddHandler(MainCommand, new CommandInfo(OnMainCommand)
        {
            HelpMessage = "Open Courteous Eorzea settings. \"/courteous auto\" toggles Auto mode, \"/courteous on|off\" sets it.",
        });
        CommandManager.AddHandler(MainCommandAlias, new CommandInfo(OnMainCommand) { ShowInHelp = false });
        CommandManager.AddHandler(HiCommand, new CommandInfo(OnHiCommand)
        {
            HelpMessage = "Send a random greeting to party/alliance. Optional: \"/hi p\", \"/hi a\", \"/hi s\" to force party, alliance or say.",
        });
        CommandManager.AddHandler(ByeCommand, new CommandInfo(OnByeCommand)
        {
            HelpMessage = "Send a random farewell to party/alliance. Optional: \"/bye p\", \"/bye a\", \"/bye s\".",
        });

        ClientState.Login += OnLogin;
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleConfigUi;

        Log.Information("Courteous Eorzea loaded. Auto mode: {Auto}", Configuration.AutoMode ? "on" : "off");
    }

    public void Dispose()
    {
        ClientState.Login -= OnLogin;
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleConfigUi;

        Watcher.Dispose();
        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();

        CommandManager.RemoveHandler(MainCommand);
        CommandManager.RemoveHandler(MainCommandAlias);
        CommandManager.RemoveHandler(HiCommand);
        CommandManager.RemoveHandler(ByeCommand);
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();

    // A greeting from a previous character should not pick this character's farewell.
    private void OnLogin() => Picker.ClearOpenGreeting();

    /// <summary>The logged-in character, or null on the title screen.</summary>
    public static (ulong ContentId, string Name, string World)? CurrentCharacter
    {
        get
        {
            if (!PlayerState.IsLoaded || PlayerState.ContentId == 0)
                return null;
            var world = PlayerState.HomeWorld.IsValid ? PlayerState.HomeWorld.Value.Name.ExtractText() : string.Empty;
            return (PlayerState.ContentId, PlayerState.CharacterName, world);
        }
    }

    /// <summary>The profile messages come from right now: this character's, or the default.</summary>
    public MessageProfile ActiveProfile => GetActiveProfile(out _);

    public MessageProfile GetActiveProfile(out bool assignedToCharacter)
    {
        // Keep the stored name/world fresh for the assignment list (renames, world transfers).
        if (CurrentCharacter is { } c && Configuration.CharacterProfiles.TryGetValue(c.ContentId, out var a))
        {
            a.CharacterName = c.Name;
            if (!string.IsNullOrEmpty(c.World)) a.World = c.World;
        }
        return Configuration.ProfileFor(CurrentCharacter?.ContentId ?? 0, out assignedToCharacter);
    }

    public void SetAutoMode(bool on)
    {
        Configuration.AutoMode = on;
        Configuration.Save();
        if (!on) Watcher.Cancel();
        Feedback($"Auto mode {(on ? "enabled" : "disabled")}.", always: true);
    }

    private void OnMainCommand(string command, string args)
    {
        switch (args.Trim().ToLowerInvariant())
        {
            case "auto":
            case "toggle":
                SetAutoMode(!Configuration.AutoMode);
                break;
            case "on":
                SetAutoMode(true);
                break;
            case "off":
                SetAutoMode(false);
                break;
            default:
                ToggleConfigUi();
                break;
        }
    }

    private void OnHiCommand(string command, string args) => SendGreeting(ParseTarget(args), auto: false);
    private void OnByeCommand(string command, string args) => SendFarewell(ParseTarget(args), auto: false);

    private ChatTarget ParseTarget(string args) => args.Trim().ToLowerInvariant() switch
    {
        "p" or "party" => ChatTarget.Party,
        "a" or "alliance" => ChatTarget.Alliance,
        "s" or "say" => ChatTarget.Say,
        _ => Configuration.DefaultTarget,
    };

    public void SendGreeting(ChatTarget target, bool auto)
    {
        var profile = ActiveProfile;
        var entry = Picker.NextGreeting(profile, Configuration.RandomOrder);
        if (entry == null)
        {
            Feedback($"No greetings in profile \"{profile.Name}\". Add some in /courteous.", always: true);
            return;
        }
        if (SendLine(entry.Text, target, auto ? "auto greeting" : "greeting"))
            Picker.GreetingSent(profile, entry);
    }

    public void SendFarewell(ChatTarget target, bool auto)
    {
        var profile = ActiveProfile;
        var entry = Picker.NextFarewell(profile, Configuration.RandomOrder, out var synced);
        if (entry == null)
        {
            Feedback($"No farewells in profile \"{profile.Name}\". Add some in /courteous.", always: true);
            return;
        }
        var what = (auto ? "auto farewell" : "farewell") + (synced ? " (synced)" : string.Empty);
        if (SendLine(entry.Text, target, what))
            Picker.FarewellSent();
    }

    /// <summary>Sends one chat line. Returns true when it went out.</summary>
    private bool SendLine(string text, ChatTarget target, string what)
    {
        if (!ClientState.IsLoggedIn)
        {
            Feedback("Not logged in.", always: true);
            return false;
        }

        var inAlliance = PartyList.IsAlliance;
        var prefix = ChatSender.Prefix(target, inAlliance);

        // Messages never get to smuggle their own command in front of the channel prefix.
        var body = text.TrimStart('/').Trim();
        var line = $"{prefix} {body}";

        var error = ChatSender.Send(line);
        if (error != null)
        {
            Feedback($"Could not send {what}: {error}", always: true);
            Log.Warning("Send failed ({What}): {Error}", what, error);
            return false;
        }

        Log.Information("Sent {What} to {Target}: {Text}", what, ChatSender.TargetName(target, inAlliance), body);
        return true;
    }

    /// <summary>Print a note to the local chat log (visible only to this player).</summary>
    public void Feedback(string message, bool always = false)
    {
        if (!always && !Configuration.ShowFeedback)
            return;
        ChatGui.Print(message, "Courteous Eorzea");
    }
}
