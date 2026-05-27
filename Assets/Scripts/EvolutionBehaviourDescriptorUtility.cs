using System.Collections.Generic;
using UnityEngine;

// Converts a fish into behaviour values for novelty/QD without hard role quotas.
public static class EvolutionBehaviourDescriptorUtility
{
    public const int DescriptorLength = 16;

    // Builds a behaviour descriptor used for novelty and diversity checks
    public static float[] BuildDescriptor(EvolutionCandidate candidate)
    {
        float[] descriptor = new float[DescriptorLength];
        FillDescriptor(candidate, descriptor);
        return descriptor;
    }

    // Fills descriptor values from diet, movement, defence and behaviour metrics
    public static void FillDescriptor(EvolutionCandidate candidate, float[] descriptor)
    {
        if (descriptor == null || descriptor.Length < DescriptorLength)
        {
            return;
        }

        for (int i = 0; i < DescriptorLength; i++)
        {
            descriptor[i] = 0f;
        }

        if (candidate == null || candidate.Genome == null)
        {
            return;
        }

        EvolutionGenome g = candidate.Genome;
        float totalModeTime = Mathf.Max(1f,
            candidate.RestingTime + candidate.ExploringTime + candidate.SchoolingTime + candidate.ForagingTime +
            candidate.FeedingTime + candidate.MateSeekingTime + candidate.HuntingTime + candidate.FleeingTime + candidate.RecoveryTime);

        descriptor[0] = Mathf.Clamp01(g.PlantDiet);
        descriptor[1] = Mathf.Clamp01(g.MeatDiet);
        descriptor[2] = Mathf.Clamp01(g.CarrionDiet);
        descriptor[3] = Mathf.Clamp01(g.Speed / 12f);
        descriptor[4] = Mathf.Clamp01(g.BodySize / 2.5f);
        descriptor[5] = Mathf.Clamp01(g.Armour / 2f);
        descriptor[6] = Mathf.Clamp01(g.DangerFactor / 3f);
        descriptor[7] = Mathf.Clamp01(g.GroupingChance);
        descriptor[8] = Mathf.Clamp01(g.Aggression);
        descriptor[9] = Mathf.Clamp01(g.RiskTolerance);
        descriptor[10] = Mathf.Clamp01((candidate.PreyBites + candidate.PreyKills * 2f) / 12f);
        descriptor[11] = Mathf.Clamp01((candidate.FoodEaten + candidate.CarrionEaten) / 18f);
        descriptor[12] = Mathf.Clamp01((candidate.ReproductionCount + candidate.EggsLaid + candidate.EggsHatched) / 8f);
        descriptor[13] = Mathf.Clamp01(candidate.HuntingTime / totalModeTime);
        descriptor[14] = Mathf.Clamp01(candidate.SchoolingTime / totalModeTime);
        descriptor[15] = Mathf.Clamp01(candidate.FleeingTime / totalModeTime);
    }

    // Returns the distance between two behaviour descriptors
    public static float Distance(EvolutionCandidate a, EvolutionCandidate b)
    {
        if (a == null || b == null)
        {
            return 1f;
        }

        float[] da = BuildDescriptor(a);
        float[] db = BuildDescriptor(b);
        return Distance(da, db);
    }

    // Returns the distance between two behaviour descriptors
    public static float Distance(float[] a, float[] b)
    {
        if (a == null || b == null)
        {
            return 1f;
        }

        int count = Mathf.Min(a.Length, b.Length);
        if (count <= 0)
        {
            return 1f;
        }

        float sum = 0f;
        for (int i = 0; i < count; i++)
        {
            float d = Mathf.Clamp01(a[i]) - Mathf.Clamp01(b[i]);
            sum += d * d;
        }

        return Mathf.Sqrt(sum / count);
    }

    // Measures how spread out a set of descriptors is
    public static float CalculateFeatureSpread(List<EvolutionCandidate> candidates)
    {
        if (candidates == null || candidates.Count <= 1)
        {
            return 0f;
        }

        float[] average = new float[DescriptorLength];
        int valid = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i] == null || candidates[i].Genome == null)
            {
                continue;
            }

            float[] d = BuildDescriptor(candidates[i]);
            for (int k = 0; k < DescriptorLength; k++)
            {
                average[k] += d[k];
            }
            valid++;
        }

        if (valid <= 1)
        {
            return 0f;
        }

        for (int k = 0; k < DescriptorLength; k++)
        {
            average[k] /= valid;
        }

        float spread = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i] == null || candidates[i].Genome == null)
            {
                continue;
            }

            spread += Distance(BuildDescriptor(candidates[i]), average);
        }

        return spread / valid;
    }
}
