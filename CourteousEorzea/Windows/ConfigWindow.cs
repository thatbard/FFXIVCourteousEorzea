using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace CourteousEorzea.Windows;

public class ConfigWindow : Window, IDisposable
{
    private static readonly string[] TriggerNames = ["When I load into the duty", "When the duty starts (barrier drops)"];
    private static readonly string[] TargetNames = ["Auto (alliance if in one, else party)", "Party", "Alliance", "Say"];

    private readonly Plugin plugin;
    private string newGreeting = string.Empty;
    private string newFarewell = string.Empty;

    /// <summary>The profile whose messages the lists below are editing.</summary>
    private Guid editingProfileId;

    public ConfigWindow(Plugin plugin) : base("Courteous Eorzea###CourteousEorzeaConfig")
    {
        this.plugin = plugin;
        Size = new Vector2(620, 720);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(420, 300), MaximumSize = new Vector2(2000, 2000) };
    }

    public void Dispose() { }

    // Open on the profile that is in use, which is almost always the one people want to edit.
    public override void OnOpen() => editingProfileId = plugin.ActiveProfile.Id;

    private Configuration Config => plugin.Configuration;

    public override void Draw()
    {
        DrawAutoSection();
        ImGui.Separator();
        DrawSendingSection();
        ImGui.Separator();
        DrawQuickButtons();
        ImGui.Separator();

        if (ImGui.CollapsingHeader("Profiles###profiles", ImGuiTreeNodeFlags.DefaultOpen))
            DrawProfilesSection();

        var editing = EditingProfile;
        if (ImGui.CollapsingHeader($"Greetings: {ProfileLabel(editing)}###greetings", ImGuiTreeNodeFlags.DefaultOpen))
            DrawMessageList(editing, greetings: true, ref newGreeting);

        if (ImGui.CollapsingHeader($"Farewells: {ProfileLabel(editing)}###farewells", ImGuiTreeNodeFlags.DefaultOpen))
            DrawMessageList(editing, greetings: false, ref newFarewell);

        if (ImGui.CollapsingHeader("About & license"))
            DrawAbout();
    }

    // Appropriate Legal Notices (AGPL-3.0 section 5(d)): copyright, no warranty, license, source.
    private static void DrawAbout()
    {
        ImGui.TextWrapped("Courteous Eorzea is a modified version of Polite Eorzea by ahadev.");
        DrawLink("Original project", "https://github.com/AHAKuo/FFXIVPoliteEorzea");
        DrawLink("Source code of this version", "https://github.com/thatbard/FFXIVCourteousEorzea");
        ImGui.Spacing();
        ImGui.TextWrapped("Copyright (C) 2026 ahadev (Polite Eorzea)\nModifications copyright (C) 2026 thatbard");
        ImGui.Spacing();
        ImGui.TextWrapped("This program is free software: you can redistribute it and/or modify it under the terms of the " +
                          "GNU Affero General Public License as published by the Free Software Foundation, version 3. " +
                          "It comes with ABSOLUTELY NO WARRANTY; see the license for details.");
        DrawLink("GNU AGPL-3.0", "https://www.gnu.org/licenses/agpl-3.0.html");
    }

    private static void DrawLink(string label, string url)
    {
        ImGui.TextUnformatted($"{label}:");
        ImGui.SameLine();
        if (ImGui.SmallButton($"{url}##{label}"))
            Dalamud.Utility.Util.OpenLink(url);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Open in your browser");
    }

    private void DrawAutoSection()
    {
        var auto = Config.AutoMode;
        using (ImRaii.PushColor(ImGuiCol.Text, auto ? new Vector4(0.45f, 1f, 0.55f, 1f) : new Vector4(1f, 1f, 1f, 1f)))
        {
            if (ImGui.Checkbox("Auto mode", ref auto))
                plugin.SetAutoMode(auto);
        }
        Tooltip("Automatically greet when you enter an instanced duty (dungeon, trial, raid, alliance raid...)\nand say thanks when it completes. Toggle from chat with /courteous auto.");

        ImGui.SameLine();
        ImGui.TextDisabled(plugin.Watcher.Status);

        using var disabled = ImRaii.Disabled(!auto);
        ImGui.Indent();

        var greet = Config.AutoGreet;
        if (ImGui.Checkbox("Greet on enter", ref greet)) { Config.AutoGreet = greet; Config.Save(); }

        ImGui.SameLine(200);
        var bye = Config.AutoFarewell;
        if (ImGui.Checkbox("Farewell on completion", ref bye)) { Config.AutoFarewell = bye; Config.Save(); }

        var trigger = (int)Config.GreetTrigger;
        ImGui.SetNextItemWidth(300);
        if (ImGui.Combo("Greet timing", ref trigger, TriggerNames, TriggerNames.Length))
        {
            Config.GreetTrigger = (GreetTrigger)trigger;
            Config.Save();
        }
        Tooltip("\"Load\" says hi while everyone is gathering, before the countdown.\n\"Duty starts\" waits for the barrier to drop.");

        var gMin = Config.GreetDelayMin; var gMax = Config.GreetDelayMax;
        ImGui.SetNextItemWidth(300);
        if (ImGui.DragFloatRange2("Greeting delay (s)", ref gMin, ref gMax, 0.1f, 0f, 30f, "%.1f", "%.1f"))
        {
            Config.GreetDelayMin = gMin; Config.GreetDelayMax = gMax; Config.Save();
        }
        Tooltip("A random delay in this range, so it does not look robotic.");

        var fMin = Config.FarewellDelayMin; var fMax = Config.FarewellDelayMax;
        ImGui.SetNextItemWidth(300);
        if (ImGui.DragFloatRange2("Farewell delay (s)", ref fMin, ref fMax, 0.1f, 0f, 60f, "%.1f", "%.1f"))
        {
            Config.FarewellDelayMin = fMin; Config.FarewellDelayMax = fMax; Config.Save();
        }

        var skipSolo = Config.SkipWhenSolo;
        if (ImGui.Checkbox("Skip when not in a party", ref skipSolo)) { Config.SkipWhenSolo = skipSolo; Config.Save(); }
        Tooltip("Solo story instances and unsynced solo runs stay quiet.");

        ImGui.SameLine(200);
        var skipPvp = Config.SkipPvP;
        if (ImGui.Checkbox("Skip PvP duties", ref skipPvp)) { Config.SkipPvP = skipPvp; Config.Save(); }

        ImGui.Unindent();
    }

    private void DrawSendingSection()
    {
        var target = (int)Config.DefaultTarget;
        ImGui.SetNextItemWidth(300);
        if (ImGui.Combo("Send to", ref target, TargetNames, TargetNames.Length))
        {
            Config.DefaultTarget = (ChatTarget)target;
            Config.Save();
        }
        Tooltip("Used by Auto mode and by /hi and /bye without an argument.\n/hi p, /hi a, /hi s force party, alliance or say for one message.");

        var rnd = Config.RandomOrder;
        if (ImGui.Checkbox("Pick messages at random", ref rnd)) { Config.RandomOrder = rnd; Config.Save(); }
        Tooltip("Off: cycle through the list from top to bottom.");

        ImGui.SameLine(260);
        var fb = Config.ShowFeedback;
        if (ImGui.Checkbox("Show skip notes in my chat", ref fb)) { Config.ShowFeedback = fb; Config.Save(); }
        Tooltip("Prints a private note when Auto mode decides not to send (solo, PvP...).\nOnly you see these.");
    }

    private void DrawQuickButtons()
    {
        var inAlliance = Plugin.PartyList.IsAlliance;
        var partyText = Plugin.PartyList.Length <= 1 ? "not in a party" : $"party of {Plugin.PartyList.Length}";
        var allianceText = inAlliance ? ", in an alliance" : string.Empty;
        ImGui.TextUnformatted($"Right now: {partyText}{allianceText}. Messages go to {ChatSender.TargetName(Config.DefaultTarget, inAlliance)} chat.");

        if (ImGui.Button("Send greeting now (/hi)"))
            plugin.SendGreeting(Config.DefaultTarget, auto: false);
        ImGui.SameLine();
        if (ImGui.Button("Send farewell now (/bye)"))
            plugin.SendFarewell(Config.DefaultTarget, auto: false);
    }

    private MessageProfile EditingProfile
    {
        get
        {
            if (Config.GetProfile(editingProfileId) is { } p)
                return p;
            var fallback = plugin.ActiveProfile;
            editingProfileId = fallback.Id;
            return fallback;
        }
    }

    private static string ProfileLabel(MessageProfile p) => string.IsNullOrWhiteSpace(p.Name) ? "(unnamed)" : p.Name;

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

    private void DrawProfilesSection()
    {
        using var id = ImRaii.PushId("profiles");
        var scale = ImGuiHelpers.GlobalScale;

        // What is in use right now, and this character's assignment.
        var active = plugin.GetActiveProfile(out var assigned);
        ImGui.TextUnformatted($"In use now: {ProfileLabel(active)}");
        ImGui.SameLine();
        ImGui.TextDisabled(assigned ? "(assigned to this character)" : "(default profile)");

        if (Plugin.CurrentCharacter is { } me)
        {
            Config.CharacterProfiles.TryGetValue(me.ContentId, out var mine);
            ImGui.SetNextItemWidth(300 * scale);
            DrawAssignmentCombo("This character", me.ContentId, me.Name, me.World, mine);
            Tooltip($"{me.Name} @ {me.World} uses this profile. \"Default\" follows whichever profile is the default.");
        }
        else
        {
            ImGui.TextDisabled("Log in to assign a profile to the character you are playing.");
        }

        ImGui.Spacing();

        // Profile being edited + new / duplicate / delete.
        var editing = EditingProfile;
        ImGui.SetNextItemWidth(300 * scale);
        using (var combo = ImRaii.Combo("Edit profile", ProfileLabel(editing)))
        {
            if (combo)
            {
                foreach (var p in Config.Profiles)
                {
                    var tags = (p.Id == Config.DefaultProfileId ? "  (default)" : string.Empty)
                             + (p.Id == active.Id ? "  (in use)" : string.Empty);
                    if (ImGui.Selectable($"{ProfileLabel(p)}{tags}##{p.Id}", p.Id == editing.Id))
                        editingProfileId = p.Id;
                }
            }
        }
        Tooltip("Choose which profile's greetings and farewells to edit below.");

        ImGui.SameLine();
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
        {
            var p = new MessageProfile { Name = Config.UniqueProfileName("New profile") };
            Config.Profiles.Add(p);
            editingProfileId = p.Id;
            Config.Save();
        }
        Tooltip("New empty profile");

        ImGui.SameLine();
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Copy))
        {
            var p = editing.Duplicate(Config.UniqueProfileName($"{ProfileLabel(editing)} copy"));
            Config.Profiles.Add(p);
            editingProfileId = p.Id;
            Config.Save();
        }
        Tooltip("Duplicate this profile, including its synced farewells");

        ImGui.SameLine();
        var canDelete = Config.Profiles.Count > 1;
        using (ImRaii.Disabled(!canDelete || !ImGui.GetIO().KeyCtrl))
        {
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash) && Config.DeleteProfile(editing.Id))
            {
                editingProfileId = plugin.ActiveProfile.Id;
                Config.Save();
                return;
            }
        }
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(canDelete
                ? "Hold Ctrl and click to delete this profile.\nCharacters using it switch to the default profile."
                : "The last profile can't be deleted.");

        var name = editing.Name;
        ImGui.SetNextItemWidth(300 * scale);
        if (ImGui.InputText("Profile name", ref name, 64))
        {
            editing.Name = name;
            Config.Save();
        }

        var isDefault = editing.Id == Config.DefaultProfileId;
        using (ImRaii.Disabled(isDefault))
        {
            if (ImGui.Checkbox("Default profile", ref isDefault) && isDefault)
            {
                Config.DefaultProfileId = editing.Id;
                Config.Save();
            }
        }
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip("Used by every character that has no profile assigned.\nTo change it, edit another profile and tick this there.");

        // Every character with an assignment, so alts can be managed without logging in to them.
        if (Config.CharacterProfiles.Count > 0 && ImGui.TreeNode($"Character assignments ({Config.CharacterProfiles.Count})###assignments"))
        {
            var current = Plugin.CurrentCharacter?.ContentId ?? 0;
            foreach (var (cid, a) in new List<KeyValuePair<ulong, CharacterAssignment>>(Config.CharacterProfiles))
            {
                using var row = ImRaii.PushId(cid.ToString());
                ImGui.SetNextItemWidth(200 * scale);
                DrawAssignmentCombo(a.DisplayName + (cid == current ? " (you)" : string.Empty), cid, a.CharacterName, a.World, a);
                ImGui.SameLine();
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Times))
                {
                    Config.AssignProfile(cid, a.CharacterName, a.World, null);
                    Config.Save();
                }
                Tooltip("Remove this assignment (the character goes back to the default profile)");
            }
            ImGui.TreePop();
        }
    }

    /// <summary>Combo choosing the profile for one character: "Default" (no assignment) or a specific profile.</summary>
    private void DrawAssignmentCombo(string label, ulong contentId, string name, string world, CharacterAssignment? current)
    {
        var currentProfile = current != null ? Config.GetProfile(current.ProfileId) : null;
        var preview = currentProfile != null ? ProfileLabel(currentProfile) : $"Default ({ProfileLabel(Config.DefaultProfile)})";

        using var combo = ImRaii.Combo(label, preview);
        if (!combo)
            return;

        if (ImGui.Selectable($"Default ({ProfileLabel(Config.DefaultProfile)})##default", currentProfile == null))
        {
            Config.AssignProfile(contentId, name, world, null);
            Config.Save();
        }
        foreach (var p in Config.Profiles)
        {
            if (ImGui.Selectable($"{ProfileLabel(p)}##{p.Id}", currentProfile?.Id == p.Id))
            {
                Config.AssignProfile(contentId, name, world, p.Id);
                Config.Save();
            }
        }
    }

    private void DrawMessageList(MessageProfile profile, bool greetings, ref string newEntry)
    {
        var noun = greetings ? "greeting" : "farewell";
        var list = greetings ? profile.Greetings : profile.Farewells;
        using var id = ImRaii.PushId(noun);
        var scale = ImGuiHelpers.GlobalScale;
        var syncWidth = 170 * scale;

        if (greetings)
            ImGui.TextDisabled("The dropdown next to a greeting syncs a farewell to it: after that greeting, /bye and\nAuto mode send that farewell. \"Any farewell\" picks one as usual.");

        var removeAt = -1;
        for (var i = 0; i < list.Count; i++)
        {
            var entry = list[i];
            using var rowId = ImRaii.PushId(entry.Id.ToString());

            var text = entry.Text;
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 40 * scale - (greetings ? syncWidth + ImGui.GetStyle().ItemSpacing.X : 0));
            if (ImGui.InputText("##text", ref text, 400))
            {
                entry.Text = text;
                Config.Save();
            }

            if (greetings)
            {
                ImGui.SameLine();
                DrawSyncCombo(profile, entry, syncWidth);
            }

            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
                removeAt = i;
            Tooltip(greetings ? "Remove this greeting" : "Remove this farewell (greetings synced to it go back to any farewell)");
        }

        if (removeAt >= 0)
        {
            if (greetings) list.RemoveAt(removeAt);
            else profile.RemoveFarewellAt(removeAt);
            Config.Save();
        }

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 40 * scale);
        var submitted = ImGui.InputText("##new", ref newEntry, 400, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGui.SameLine();
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus) || submitted)
        {
            if (!string.IsNullOrWhiteSpace(newEntry))
            {
                list.Add(new MessageEntry(newEntry.Trim()));
                newEntry = string.Empty;
                Config.Save();
            }
        }
        Tooltip($"Add a new {noun} (Enter also adds)");

        if (list.Count == 0)
            ImGui.TextDisabled($"No {noun}s yet. Add one above.");
    }

    private void DrawSyncCombo(MessageProfile profile, MessageEntry greeting, float width)
    {
        var paired = profile.FindFarewell(greeting.PairedFarewellId);
        var preview = paired != null ? "→ " + Truncate(paired.Text, 20) : "Any farewell";

        ImGui.SetNextItemWidth(width);
        using (var combo = ImRaii.Combo("##sync", preview))
        {
            if (combo)
            {
                if (ImGui.Selectable("Any farewell", paired == null))
                {
                    greeting.PairedFarewellId = null;
                    Config.Save();
                }
                foreach (var f in profile.Farewells)
                {
                    if (string.IsNullOrWhiteSpace(f.Text))
                        continue;
                    if (ImGui.Selectable($"{Truncate(f.Text, 60)}##{f.Id}", paired?.Id == f.Id))
                    {
                        greeting.PairedFarewellId = f.Id;
                        Config.Save();
                    }
                }
                if (profile.Farewells.Count == 0)
                    ImGui.TextDisabled("Add farewells below first.");
            }
        }
        Tooltip(paired != null ? $"Synced farewell: {paired.Text}" : "No synced farewell: any farewell may follow this greeting.");
    }

    private static void Tooltip(string text)
    {
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(text);
    }
}
