using UnityEngine;

[System.Serializable]
public class CreatureEffectiveStats
{
    public float Speed;
    public float Acceleration;
    public float TurnRate;
    public float VerticalControl;
    public float BodySize;
    public float VisionRange;
    public float EnergyCapacity;
    public float EnergyDrainMultiplier = 1f;
    public float MouthRadiusMultiplier = 1f;
    public float MouthForwardOffsetMultiplier = 1f;
    public float BiteDamage;
    public float Defence;
    public float DangerFactor;
    public float PlantDiet;
    public float MeatDiet;
    public float CarrionDiet;
    public float Aggression;
    public float RiskTolerance;
    public float GroupingChance;
    public float ThreatRange;

    public string MorphSummary;

    public static CreatureEffectiveStats Build(EvolutionGenome genome, CreatureMorphLibrary library)
    {
        CreatureEffectiveStats stats = new CreatureEffectiveStats();

        if (genome == null)
        {
            return stats;
        }

        genome.ClampValues();

        stats.Speed = genome.Speed;
        stats.Acceleration = genome.Acceleration;
        stats.TurnRate = genome.TurnRate;
        stats.VerticalControl = genome.VerticalControl;
        stats.BodySize = genome.BodySize;
        stats.VisionRange = genome.VisionRange;
        stats.EnergyCapacity = genome.EnergyCapacity;
        stats.BiteDamage = 1f + genome.JawSize * 2.5f + genome.Muscle * 1.5f;
        stats.Defence = genome.Armour;
        stats.DangerFactor = genome.DangerFactor;
        stats.PlantDiet = genome.PlantDiet;
        stats.MeatDiet = genome.MeatDiet;
        stats.CarrionDiet = genome.CarrionDiet;
        stats.Aggression = genome.Aggression;
        stats.RiskTolerance = genome.RiskTolerance;
        stats.GroupingChance = genome.GroupingChance;
        stats.ThreatRange = genome.ThreatRange;

        stats.ApplyMorphSlot(library, CreatureMorphSlot.Body, genome.BodyMorphId, genome.BodySize, genome.BodyLength, genome.BodyWidth);
        stats.ApplyMorphSlot(library, CreatureMorphSlot.Tail, genome.TailMorphId, genome.TailSize, genome.TailLength, genome.TailWidth);
        stats.ApplyMorphSlot(library, CreatureMorphSlot.Fins, genome.FinMorphId, genome.FinSize, genome.FinLength, genome.FinWidth);
        stats.ApplyMorphSlot(library, CreatureMorphSlot.Jaw, genome.JawMorphId, genome.JawSize, genome.JawLength, genome.JawWidth);
        stats.ApplyMorphSlot(library, CreatureMorphSlot.Sensors, genome.SensorMorphId, genome.SensorSize, genome.SensorSize, genome.SensorSize);
        stats.ApplyMorphSlot(library, CreatureMorphSlot.Armour, genome.ArmourMorphId, Mathf.Max(0.1f, genome.Armour + 0.35f), genome.BodyLength, genome.BodyWidth);
        stats.ApplyMorphSlot(library, CreatureMorphSlot.DorsalFin, genome.DorsalFinMorphId, genome.DorsalFinSize, genome.DorsalFinSize, genome.DorsalFinSize);
        stats.ApplyMorphSlot(library, CreatureMorphSlot.Spikes, genome.SpikeMorphId, genome.SpikeSize, genome.SpikeLength, genome.SpikeSize);
        stats.ApplyMorphSlot(library, CreatureMorphSlot.Gills, genome.GillMorphId, genome.GillSize, genome.GillSize, genome.GillSize);

        // Continuous body modifiers. These create small/large/long/wide variation on top of the part type.
        stats.Speed += (genome.TailLength - 1f) * 1.35f;
        stats.Speed += (genome.BodyLength - 1f) * 0.85f;
        stats.Speed -= (genome.BodyWidth - 1f) * 0.8f;
        stats.Speed -= genome.Armour * 0.95f;

        stats.Acceleration += (genome.Muscle - 1f) * 2.4f;
        stats.TurnRate += (genome.FinWidth - 1f) * 45f;
        stats.TurnRate -= (genome.BodyLength - 1f) * 35f;
        stats.VerticalControl += (genome.FinSize - 1f) * 0.65f;
        stats.VerticalControl += (genome.DorsalFinSize - 1f) * 0.35f;

        stats.VisionRange += (genome.SensorSize - 1f) * 8f;
        stats.EnergyCapacity += (genome.BodySize - 1f) * 45f;
        stats.EnergyCapacity += (genome.Muscle - 1f) * 12f;

        stats.MouthRadiusMultiplier += (genome.JawSize - 1f) * 0.38f;
        stats.MouthForwardOffsetMultiplier += (genome.JawLength - 1f) * 0.25f;
        stats.BiteDamage += (genome.JawSize - 1f) * 5f;
        stats.BiteDamage += (genome.JawLength - 1f) * 2.5f;
        stats.BiteDamage += (genome.SpikeSize - 1f) * 1.5f;

        stats.Defence += genome.Armour * 1.18f;
        stats.Defence += Mathf.Max(0f, genome.SpikeSize - 1f) * 0.25f;
        stats.DangerFactor += genome.SpikeSize * 0.35f;
        // Armour should deter other species, but it must be costly so it cannot dominate for free.
        stats.DangerFactor += genome.Armour * 0.42f;
        stats.DangerFactor += genome.JawSize * genome.MeatDiet * 0.2f;
        stats.DangerFactor += genome.BodySize * 0.1f;

        stats.EnergyDrainMultiplier *= genome.GetEnergyDrainMultiplier();
        stats.EnergyDrainMultiplier *= 1f + Mathf.Max(0f, genome.SpikeSize - 1f) * 0.08f;
        stats.EnergyDrainMultiplier *= 1f + Mathf.Max(0f, genome.Armour) * 0.22f;
        stats.EnergyDrainMultiplier *= 1f - Mathf.Clamp01((genome.GillSize - 1f) * 0.08f);

        stats.Clamp();
        stats.MorphSummary = BuildMorphSummary(genome, library);
        return stats;
    }

    private void ApplyMorphSlot(CreatureMorphLibrary library, CreatureMorphSlot slot, string partId, float scale, float length, float width)
    {
        CreatureMorphPartData part = library != null ? library.FindPart(slot, partId) : null;

        if (part == null)
        {
            return;
        }

        if (!CreatureMorphLibrary.IsNonePartId(part.PartId) && part.PartPrefab == null)
        {
            return;
        }

        ApplyPartData(part, scale, length, width);
    }

    private void ApplyPartData(CreatureMorphPartData part, float scale, float length, float width)
    {
        float shapeStrength = Mathf.Clamp((scale + length + width) / 3f, 0.35f, 2.5f);

        Speed += part.SpeedModifier * shapeStrength;
        Acceleration += part.AccelerationModifier * shapeStrength;
        TurnRate += part.TurnRateModifier * shapeStrength;
        VerticalControl += part.VerticalControlModifier * shapeStrength;
        BodySize += part.BodySizeModifier * shapeStrength;
        VisionRange += part.VisionRangeModifier * shapeStrength;
        EnergyCapacity += part.EnergyCapacityModifier * shapeStrength;
        BiteDamage += part.BiteDamageModifier * shapeStrength;
        MouthRadiusMultiplier += part.MouthRadiusModifier * shapeStrength;
        Defence += part.DefenceModifier * shapeStrength;
        DangerFactor += part.DangerFactorModifier * shapeStrength;

        EnergyDrainMultiplier *= Mathf.Max(0.15f, part.EnergyDrainMultiplier);
        Speed *= Mathf.Max(0.1f, part.SpeedMultiplier);
        TurnRate *= Mathf.Max(0.1f, part.TurnRateMultiplier);
        BiteDamage *= Mathf.Max(0.1f, part.BiteDamageMultiplier);
        VisionRange *= Mathf.Max(0.1f, part.VisionRangeMultiplier);

        PlantDiet += part.PlantDietBias;
        MeatDiet += part.MeatDietBias;
        CarrionDiet += part.CarrionDietBias;
        Aggression += part.AggressionModifier;
        RiskTolerance += part.RiskToleranceModifier;
        GroupingChance += part.GroupingModifier;
        ThreatRange += part.ThreatModifier;
    }

    private void ApplyFallbackPart(CreatureMorphSlot slot, string partId, float scale, float length, float width)
    {
        string id = string.IsNullOrEmpty(partId) ? "" : partId;
        float s = Mathf.Clamp(scale, 0.35f, 2.5f);
        float l = Mathf.Clamp(length, 0.45f, 2.5f);
        float w = Mathf.Clamp(width, 0.45f, 2.5f);

        if (slot == CreatureMorphSlot.Body)
        {
            if (id.Contains("streamlined")) { Speed += 1.2f * l; EnergyDrainMultiplier *= 0.88f; Defence -= 0.15f; }
            else if (id.Contains("armoured")) { Defence += 0.9f * s; DangerFactor += 0.42f * s; Speed -= 0.75f * s; EnergyDrainMultiplier *= 1.28f; }
            else if (id.Contains("bulky")) { BodySize += 0.22f * s; EnergyCapacity += 25f * s; Speed -= 0.45f; TurnRate -= 18f; EnergyDrainMultiplier *= 1.12f; }
            else if (id.Contains("soft")) { TurnRate += 18f; EnergyDrainMultiplier *= 0.94f; Defence -= 0.2f; }
        }
        else if (slot == CreatureMorphSlot.Tail)
        {
            if (id.Contains("forked")) { Speed += 1.1f * s; Acceleration += 1.4f; }
            else if (id.Contains("whip")) { Speed += 0.7f * l; TurnRate += 18f; }
            else if (id.Contains("flat")) { VerticalControl += 0.55f * w; TurnRate += 25f; Speed -= 0.25f; }
        }
        else if (slot == CreatureMorphSlot.Fins)
        {
            if (id.Contains("wide")) { TurnRate += 45f * w; VerticalControl += 0.7f * w; Speed -= 0.25f; }
            else if (id.Contains("thin")) { Speed += 0.45f; TurnRate -= 10f; }
            else if (id.Contains("stabiliser")) { VerticalControl += 0.5f; TurnRate += 20f; EnergyDrainMultiplier *= 1.04f; }
        }
        else if (slot == CreatureMorphSlot.Jaw)
        {
            if (id.Contains("predator")) { BiteDamage += 5f * s; MouthRadiusMultiplier += 0.18f * s; MeatDiet += 0.08f; Aggression += 0.05f; EnergyDrainMultiplier *= 1.07f; }
            else if (id.Contains("filter")) { PlantDiet += 0.08f; BiteDamage -= 1.5f; EnergyDrainMultiplier *= 0.96f; }
            else if (id.Contains("crushing")) { BiteDamage += 3.5f * s; CarrionDiet += 0.06f; TurnRate -= 8f; EnergyDrainMultiplier *= 1.05f; }
            else if (id.Contains("small")) { BiteDamage -= 1.2f; MouthRadiusMultiplier -= 0.08f; EnergyDrainMultiplier *= 0.95f; }
        }
        else if (slot == CreatureMorphSlot.Sensors)
        {
            if (id.Contains("large")) { VisionRange += 8f * s; EnergyDrainMultiplier *= 1.07f; }
            else if (id.Contains("antenna")) { VisionRange += 5f * s; ThreatRange += 0.08f; CarrionDiet += 0.05f; }
            else if (id.Contains("side")) { VisionRange += 3f; RiskTolerance += 0.04f; }
        }
        else if (slot == CreatureMorphSlot.Armour)
        {
            if (id.Contains("light")) { Defence += 0.35f * s; DangerFactor += 0.12f; Speed -= 0.12f; }
            else if (id.Contains("plated")) { Defence += 0.85f * s; DangerFactor += 0.38f * s; Speed -= 0.62f; EnergyDrainMultiplier *= 1.23f; }
            else if (id.Contains("shell")) { Defence += 1.25f * s; DangerFactor += 0.55f * s; Speed -= 0.95f; TurnRate -= 32f; EnergyDrainMultiplier *= 1.34f; }
        }
        else if (slot == CreatureMorphSlot.DorsalFin)
        {
            if (id.Contains("large")) { VerticalControl += 0.35f; DangerFactor += 0.06f; }
            else if (id.Contains("sail")) { VerticalControl += 0.55f; TurnRate -= 8f; DangerFactor += 0.12f; }
        }
        else if (slot == CreatureMorphSlot.Spikes)
        {
            if (id.Contains("small")) { DangerFactor += 0.35f * s; Defence += 0.1f; }
            else if (id.Contains("long")) { DangerFactor += 0.7f * s; Defence += 0.18f; Speed -= 0.18f; EnergyDrainMultiplier *= 1.06f; }
            else if (id.Contains("barbed")) { DangerFactor += 0.9f * s; Defence += 0.25f; Aggression += 0.04f; EnergyDrainMultiplier *= 1.08f; }
        }
        else if (slot == CreatureMorphSlot.Gills)
        {
            if (id.Contains("large")) { EnergyDrainMultiplier *= 0.92f; Acceleration += 0.4f; }
            else if (id.Contains("efficient")) { EnergyDrainMultiplier *= 0.84f; Speed += 0.2f; }
            else if (id.Contains("reduced")) { EnergyDrainMultiplier *= 1.08f; BodySize -= 0.05f; }
        }
    }

    private static string BuildMorphSummary(EvolutionGenome genome, CreatureMorphLibrary library)
    {
        string body = GetPartName(library, CreatureMorphSlot.Body, genome.BodyMorphId);
        string tail = GetPartName(library, CreatureMorphSlot.Tail, genome.TailMorphId);
        string jaw = GetPartName(library, CreatureMorphSlot.Jaw, genome.JawMorphId);
        string extra = "";

        if (!string.IsNullOrEmpty(genome.ArmourMorphId) && !genome.ArmourMorphId.Contains("none"))
        {
            extra = ", " + GetPartName(library, CreatureMorphSlot.Armour, genome.ArmourMorphId);
        }

        if (!string.IsNullOrEmpty(genome.SpikeMorphId) && !genome.SpikeMorphId.Contains("none"))
        {
            extra += ", " + GetPartName(library, CreatureMorphSlot.Spikes, genome.SpikeMorphId);
        }

        return body + " / " + tail + " / " + jaw + extra;
    }

    private static string GetPartName(CreatureMorphLibrary library, CreatureMorphSlot slot, string id)
    {
        CreatureMorphPartData part = library != null ? library.FindPart(slot, id) : null;
        if (part != null && !string.IsNullOrEmpty(part.DisplayName))
        {
            return part.DisplayName;
        }

        return string.IsNullOrEmpty(id) ? "None" : CreatureMorphLibrary.NormalisePartIdForSlot(slot, id).Replace('_', ' ');
    }

    public void Clamp()
    {
        Speed = Mathf.Clamp(Speed, 0.5f, 18f);
        Acceleration = Mathf.Clamp(Acceleration, 1f, 40f);
        TurnRate = Mathf.Clamp(TurnRate, 25f, 620f);
        VerticalControl = Mathf.Clamp(VerticalControl, 0.15f, 4f);
        BodySize = Mathf.Clamp(BodySize, 0.3f, 3.5f);
        VisionRange = Mathf.Clamp(VisionRange, 2f, 70f);
        EnergyCapacity = Mathf.Clamp(EnergyCapacity, 30f, 350f);
        EnergyDrainMultiplier = Mathf.Clamp(EnergyDrainMultiplier, 0.1f, 4f);
        MouthRadiusMultiplier = Mathf.Clamp(MouthRadiusMultiplier, 0.35f, 2.8f);
        MouthForwardOffsetMultiplier = Mathf.Clamp(MouthForwardOffsetMultiplier, 0.5f, 2f);
        BiteDamage = Mathf.Clamp(BiteDamage, 0.25f, 80f);
        Defence = Mathf.Clamp(Defence, 0f, 5f);
        DangerFactor = Mathf.Clamp(DangerFactor, 0f, 5f);
        PlantDiet = Mathf.Clamp01(PlantDiet);
        MeatDiet = Mathf.Clamp01(MeatDiet);
        CarrionDiet = Mathf.Clamp01(CarrionDiet);
        Aggression = Mathf.Clamp01(Aggression);
        RiskTolerance = Mathf.Clamp01(RiskTolerance);
        GroupingChance = Mathf.Clamp01(GroupingChance);
        ThreatRange = Mathf.Clamp01(ThreatRange);
    }
}
