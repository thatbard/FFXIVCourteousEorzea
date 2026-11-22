using System;
using System.Collections.Generic;

namespace CourteousEorzea;

/// <summary>
/// Chooses the next greeting or farewell from a profile, randomly or in sequence, and remembers the
/// last greeting sent so its synced farewell can follow it.
/// </summary>
public sealed class MessagePicker
{
    private readonly Random random = new();
    private int greetIndex;
    private int byeIndex;
    private Guid? lastGreetId;
    private Guid? lastByeId;

    /// <summary>The greeting most recently sent, waiting for its farewell.</summary>
    private (Guid ProfileId, Guid GreetingId)? openGreeting;

    public MessageEntry? NextGreeting(MessageProfile profile, bool randomOrder) =>
        Next(profile.Greetings, randomOrder, ref greetIndex, ref lastGreetId);

    /// <summary>
    /// The farewell synced to the last greeting sent (from this profile), if it has one;
    /// otherwise the next farewell picked the usual way.
    /// </summary>
    public MessageEntry? NextFarewell(MessageProfile profile, bool randomOrder, out bool synced)
    {
        synced = false;
        if (openGreeting is { } open && open.ProfileId == profile.Id
            && profile.FindGreeting(open.GreetingId) is { } greeting
            && profile.FindFarewell(greeting.PairedFarewellId) is { } paired
            && !string.IsNullOrWhiteSpace(paired.Text))
        {
            synced = true;
            lastByeId = paired.Id;
            return paired;
        }

        return Next(profile.Farewells, randomOrder, ref byeIndex, ref lastByeId);
    }

    /// <summary>Call after a greeting actually went out.</summary>
    public void GreetingSent(MessageProfile profile, MessageEntry greeting) => openGreeting = (profile.Id, greeting.Id);

    /// <summary>Call after a farewell actually went out; the greeting it answered is closed.</summary>
    public void FarewellSent() => openGreeting = null;

    /// <summary>Forget the last greeting, e.g. on entering a new duty or logging in.</summary>
    public void ClearOpenGreeting() => openGreeting = null;

    private MessageEntry? Next(List<MessageEntry> list, bool randomOrder, ref int index, ref Guid? last)
    {
        var candidates = new List<MessageEntry>(list.Count);
        foreach (var e in list)
        {
            if (!string.IsNullOrWhiteSpace(e.Text))
                candidates.Add(e);
        }

        if (candidates.Count == 0)
            return null;

        MessageEntry chosen;
        if (randomOrder)
        {
            chosen = candidates[random.Next(candidates.Count)];

            // Avoid repeating the same line twice in a row when we have a choice.
            if (candidates.Count > 1 && chosen.Id == last)
                chosen = candidates[(candidates.IndexOf(chosen) + 1 + random.Next(candidates.Count - 1)) % candidates.Count];
        }
        else
        {
            if (index >= candidates.Count)
                index = 0;
            chosen = candidates[index];
            index = (index + 1) % candidates.Count;
        }

        last = chosen.Id;
        return chosen;
    }
}
