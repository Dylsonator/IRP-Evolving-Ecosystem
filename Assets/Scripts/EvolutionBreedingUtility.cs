using System.Reflection;
using UnityEngine;

public static class EvolutionBreedingUtility
{
    public static EvolutionGenome CreateChildGenome(EvolutionGenome mother, EvolutionGenome father, float mutationMultiplier)
    {
        if (mother == null && father == null)
        {
            return EvolutionGenome.CreateRandom();
        }

        if (mother == null)
        {
            return father.CreateMutatedCopy(mutationMultiplier);
        }

        if (father == null)
        {
            return mother.CreateMutatedCopy(mutationMultiplier);
        }

        EvolutionGenome baseParent = Random.value < 0.5f ? mother : father;
        EvolutionGenome child = baseParent.CreateMutatedCopy(mutationMultiplier);

        FieldInfo[] fields = typeof(EvolutionGenome).GetFields(BindingFlags.Instance | BindingFlags.Public);
        for (int i = 0; i < fields.Length; i++)
        {
            FieldInfo field = fields[i];
            if (field.FieldType == typeof(float))
            {
                float a = (float)field.GetValue(mother);
                float b = (float)field.GetValue(father);
                float blend = Random.Range(0.35f, 0.65f);
                field.SetValue(child, Mathf.Lerp(a, b, blend));
            }
            else if (field.FieldType == typeof(string))
            {
                string chosen = Random.value < 0.5f ? (string)field.GetValue(mother) : (string)field.GetValue(father);
                field.SetValue(child, chosen);
            }
            else if (field.FieldType == typeof(bool))
            {
                bool chosen = Random.value < 0.5f ? (bool)field.GetValue(mother) : (bool)field.GetValue(father);
                field.SetValue(child, chosen);
            }
        }

        child.SexGene = Random.value;
        child.Brain = baseParent.Brain != null
            ? baseParent.Brain.CreateMutatedCopy(child.MutationRate, child.MutationStrength * mutationMultiplier, child.BrainStructuralMutationRate * mutationMultiplier, EvolutionGenome.BrainMaxHiddenCount)
            : SimpleNeuralNetwork.CreateRandom(EvolutionGenome.BrainInputCount, EvolutionGenome.BrainHiddenCount, EvolutionGenome.BrainOutputCount);

        child.ClampValues();
        return child;
    }
}
