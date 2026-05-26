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
        string wantedId = NormalisePartIdForSlot(slot, partId);

        if (string.IsNullOrEmpty(wantedId))
        {
            return null;
        }

        for (int i = 0; i < Parts.Count; i++)
        {
            CreatureMorphPartData part = Parts[i];

            if (part == null || part.Slot != slot)
            {
                continue;
            }

            string candidateId = NormalisePartIdForSlot(slot, part.PartId);

            if (candidateId == wantedId)
            {
                return part;
            }
        }

        return null;
    }

    public bool HasUsablePart(CreatureMorphSlot slot, string partId)
    {
        CreatureMorphPartData part = FindPart(slot, partId);

        if (part == null)
        {
            return false;
        }

        if (IsNonePartId(part.PartId))
        {
            return true;
        }

        return part.PartPrefab != null;
    }

    public static bool IsNonePartId(string partId)
    {
        return NormaliseSharedPartId(partId) == "none";
    }

    public CreatureMorphPartData FindAnyPart(string partId)
    {
        if (string.IsNullOrEmpty(partId))
        {
            return null;
        }

        string wantedId = NormaliseSharedPartId(partId);

        for (int i = 0; i < Parts.Count; i++)
        {
            if (Parts[i] == null)
            {
                continue;
            }

            if (NormaliseSharedPartId(Parts[i].PartId) == wantedId)
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

            if (!IsNonePartId(part.PartId) && part.PartPrefab == null)
            {
                continue;
            }

            validParts.Add(part);
            totalWeight += Mathf.Max(0.01f, part.MutationWeight);
        }

        if (validParts.Count == 0)
        {
            return NormalisePartIdForSlot(slot, currentId);
        }

        float roll = Random.Range(0f, totalWeight);
        float running = 0f;

        for (int i = 0; i < validParts.Count; i++)
        {
            running += Mathf.Max(0.01f, validParts[i].MutationWeight);

            if (roll <= running)
            {
                return NormalisePartIdForSlot(slot, validParts[i].PartId);
            }
        }

        return NormalisePartIdForSlot(slot, validParts[validParts.Count - 1].PartId);
    }

    public static string GetRandomPartIdFromActive(CreatureMorphSlot slot, string currentId)
    {
        if (activeLibrary != null)
        {
            return activeLibrary.GetRandomPartId(slot, currentId);
        }

        return NormalisePartIdForSlot(slot, currentId);
    }

    public static string GetFallbackRandomPartId(CreatureMorphSlot slot, string currentId)
    {
        return NormalisePartIdForSlot(slot, currentId);
    }

    public static string[] GetFallbackPartIds(CreatureMorphSlot slot)
    {
        return new string[0];
    }

    public static string NormalisePartIdForSlot(CreatureMorphSlot slot, string partId)
    {
        if (string.IsNullOrEmpty(partId))
        {
            return string.Empty;
        }

        string id = NormaliseSharedPartId(partId);

        if (id == "filter" || id == "grazer")
        {
            return "basic";
        }

        if (id == "predator" || id == "sharp" || id == "forked" || id == "fast")
        {
            return "streamlined";
        }

        if (id == "crusher" || id == "crushing" || id == "heavy" || id == "shell" || id == "plated")
        {
            return "armoured";
        }

        if (id == "wide" || id == "side" || id == "stabiliser" || id == "stabilizer")
        {
            return "flat";
        }

        if (id == "large" || id == "large_eyes" || id == "antenna" || id == "antennae" || id == "efficient")
        {
            return "deep";
        }

        if (id == "small" || id == "thin" || id == "short" || id == "soft" || id == "light" || id == "reduced")
        {
            return "basic";
        }

        if (id == "none")
        {
            return "none";
        }

        return id;
    }

    private static string NormaliseSharedPartId(string partId)
    {
        string id = partId.Trim().ToLowerInvariant();

        id = RemoveSlotPrefix(id, "body_");
        id = RemoveSlotPrefix(id, "tail_");
        id = RemoveSlotPrefix(id, "fin_");
        id = RemoveSlotPrefix(id, "fins_");
        id = RemoveSlotPrefix(id, "jaw_");
        id = RemoveSlotPrefix(id, "mouth_");
        id = RemoveSlotPrefix(id, "sensor_");
        id = RemoveSlotPrefix(id, "sensors_");
        id = RemoveSlotPrefix(id, "armour_");
        id = RemoveSlotPrefix(id, "armor_");
        id = RemoveSlotPrefix(id, "dorsal_");
        id = RemoveSlotPrefix(id, "dorsalfin_");
        id = RemoveSlotPrefix(id, "spike_");
        id = RemoveSlotPrefix(id, "spikes_");
        id = RemoveSlotPrefix(id, "gill_");
        id = RemoveSlotPrefix(id, "gills_");

        if (id == "light_scales" || id == "light_scale")
        {
            id = "basic";
        }

        if (id == "head_plate" || id == "side_plates")
        {
            id = "armoured";
        }

        if (id == "low_light")
        {
            id = "deep";
        }

        return id;
    }

    private static string RemoveSlotPrefix(string id, string prefix)
    {
        if (id.StartsWith(prefix))
        {
            return id.Substring(prefix.Length);
        }

        return id;
    }

    public static string GetFallbackDisplayName(string partId)
    {
        if (string.IsNullOrEmpty(partId))
        {
            return "None";
        }

        string normalised = NormaliseSharedPartId(partId);
        if (string.IsNullOrEmpty(normalised))
        {
            return "None";
        }

        return normalised.Replace('_', ' ');
    }
}
