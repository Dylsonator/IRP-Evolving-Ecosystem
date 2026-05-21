using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "IRP Evolution/Creature Morph Library", fileName = "CreatureMorphLibrary")]
public class CreatureMorphLibrary : ScriptableObject
{
    private static CreatureMorphLibrary activeLibrary;

    [Header("Body Part Prefab Library")]
    public List<CreatureMorphPartData> Parts = new List<CreatureMorphPartData>();
    public List<CreatureMorphModifierData> Modifiers = new List<CreatureMorphModifierData>();

    public static void SetActiveLibrary(CreatureMorphLibrary library)
    {
        activeLibrary = library;
    }

    public static CreatureMorphLibrary ActiveLibrary
    {
        get { return activeLibrary; }
    }

    public CreatureMorphPartData FindPart(CreatureMorphSlot slot, string partId)
    {
        if (string.IsNullOrEmpty(partId))
        {
            return null;
        }

        for (int i = 0; i < Parts.Count; i++)
        {
            CreatureMorphPartData part = Parts[i];

            if (part == null)
            {
                continue;
            }

            if (part.Slot == slot && part.PartId == partId)
            {
                return part;
            }
        }

        return null;
    }

    public CreatureMorphPartData FindAnyPart(string partId)
    {
        if (string.IsNullOrEmpty(partId))
        {
            return null;
        }

        for (int i = 0; i < Parts.Count; i++)
        {
            if (Parts[i] != null && Parts[i].PartId == partId)
            {
                return Parts[i];
            }
        }

        return null;
    }

    public string GetRandomPartId(CreatureMorphSlot slot, string currentId)
    {
        List<CreatureMorphPartData> validParts = new List<CreatureMorphPartData>();
        float totalWeight = 0f;

        for (int i = 0; i < Parts.Count; i++)
        {
            CreatureMorphPartData part = Parts[i];

            if (part == null || part.Slot != slot || string.IsNullOrEmpty(part.PartId))
            {
                continue;
            }

            validParts.Add(part);
            totalWeight += Mathf.Max(0.01f, part.MutationWeight);
        }

        if (validParts.Count == 0)
        {
            return GetFallbackRandomPartId(slot, currentId);
        }

        float roll = Random.Range(0f, totalWeight);
        float running = 0f;

        for (int i = 0; i < validParts.Count; i++)
        {
            running += Mathf.Max(0.01f, validParts[i].MutationWeight);

            if (roll <= running)
            {
                return validParts[i].PartId;
            }
        }

        return validParts[validParts.Count - 1].PartId;
    }

    public static string GetRandomPartIdFromActive(CreatureMorphSlot slot, string currentId)
    {
        if (activeLibrary != null)
        {
            return activeLibrary.GetRandomPartId(slot, currentId);
        }

        return GetFallbackRandomPartId(slot, currentId);
    }

    public static string GetFallbackRandomPartId(CreatureMorphSlot slot, string currentId)
    {
        string[] ids = GetFallbackPartIds(slot);

        if (ids.Length == 0)
        {
            return currentId;
        }

        if (ids.Length == 1)
        {
            return ids[0];
        }

        string chosen = currentId;
        int safety = 0;

        while (chosen == currentId && safety < 12)
        {
            chosen = ids[Random.Range(0, ids.Length)];
            safety++;
        }

        return chosen;
    }

    public static string[] GetFallbackPartIds(CreatureMorphSlot slot)
    {
        switch (slot)
        {
            case CreatureMorphSlot.Body:
                return new[] { "body_basic", "body_streamlined", "body_armoured", "body_bulky", "body_soft" };
            case CreatureMorphSlot.Tail:
                return new[] { "tail_basic", "tail_forked", "tail_whip", "tail_flat" };
            case CreatureMorphSlot.Fins:
                return new[] { "fins_basic", "fins_wide", "fins_thin", "fins_stabiliser" };
            case CreatureMorphSlot.Jaw:
                return new[] { "jaw_filter", "jaw_predator", "jaw_small", "jaw_crushing" };
            case CreatureMorphSlot.Sensors:
                return new[] { "sensors_basic", "sensors_large_eyes", "sensors_antenna", "sensors_side" };
            case CreatureMorphSlot.Armour:
                return new[] { "armour_none", "armour_light", "armour_plated", "armour_shell" };
            case CreatureMorphSlot.DorsalFin:
                return new[] { "dorsal_none", "dorsal_small", "dorsal_large", "dorsal_sail" };
            case CreatureMorphSlot.Spikes:
                return new[] { "spikes_none", "spikes_small", "spikes_long", "spikes_barbed" };
            case CreatureMorphSlot.Gills:
                return new[] { "gills_basic", "gills_large", "gills_efficient", "gills_reduced" };
            default:
                return new string[0];
        }
    }

    public static string GetFallbackDisplayName(string partId)
    {
        if (string.IsNullOrEmpty(partId))
        {
            return "None";
        }

        return partId.Replace('_', ' ');
    }
}
