using System;
using UnityEngine;

[Serializable]
public class SimpleNeuralNetwork
{
    public int InputCount;
    public int HiddenCount;
    public int OutputCount;

    public float[] InputHiddenWeights;
    public float[] HiddenOutputWeights;
    public float[] HiddenBiases;
    public float[] OutputBiases;

    public static SimpleNeuralNetwork CreateRandom(int inputCount, int hiddenCount, int outputCount)
    {
        SimpleNeuralNetwork network = new SimpleNeuralNetwork();
        network.InputCount = inputCount;
        network.HiddenCount = hiddenCount;
        network.OutputCount = outputCount;

        network.InputHiddenWeights = new float[inputCount * hiddenCount];
        network.HiddenOutputWeights = new float[hiddenCount * outputCount];
        network.HiddenBiases = new float[hiddenCount];
        network.OutputBiases = new float[outputCount];

        network.Randomise(network.InputHiddenWeights);
        network.Randomise(network.HiddenOutputWeights);
        network.Randomise(network.HiddenBiases);
        network.Randomise(network.OutputBiases);

        return network;
    }

    public float[] Evaluate(float[] inputs)
    {
        float[] outputs = new float[OutputCount];
        float[] hidden = new float[HiddenCount];
        EvaluateNonAlloc(inputs, outputs, hidden);
        return outputs;
    }

    public bool EvaluateNonAlloc(float[] inputs, float[] outputs, float[] hiddenScratch)
    {
        if (inputs == null || inputs.Length != InputCount || outputs == null || outputs.Length < OutputCount || hiddenScratch == null || hiddenScratch.Length < HiddenCount)
        {
            Debug.LogWarning("SimpleNeuralNetwork received invalid input/output buffers.");
            return false;
        }

        for (int h = 0; h < HiddenCount; h++)
        {
            float sum = HiddenBiases[h];
            int weightOffset = h;

            for (int i = 0; i < InputCount; i++)
            {
                sum += inputs[i] * InputHiddenWeights[weightOffset];
                weightOffset += HiddenCount;
            }

            hiddenScratch[h] = (float)Math.Tanh(sum);
        }

        for (int o = 0; o < OutputCount; o++)
        {
            float sum = OutputBiases[o];
            int weightOffset = o;

            for (int h = 0; h < HiddenCount; h++)
            {
                sum += hiddenScratch[h] * HiddenOutputWeights[weightOffset];
                weightOffset += OutputCount;
            }

            outputs[o] = Mathf.Clamp((float)Math.Tanh(sum), -1f, 1f);
        }

        return true;
    }

    public SimpleNeuralNetwork CreateMutatedCopy(float mutationRate, float mutationStrength)
    {
        SimpleNeuralNetwork copy = new SimpleNeuralNetwork();
        copy.InputCount = InputCount;
        copy.HiddenCount = HiddenCount;
        copy.OutputCount = OutputCount;

        copy.InputHiddenWeights = CopyAndMutate(InputHiddenWeights, mutationRate, mutationStrength);
        copy.HiddenOutputWeights = CopyAndMutate(HiddenOutputWeights, mutationRate, mutationStrength);
        copy.HiddenBiases = CopyAndMutate(HiddenBiases, mutationRate, mutationStrength);
        copy.OutputBiases = CopyAndMutate(OutputBiases, mutationRate, mutationStrength);

        return copy;
    }

    private float[] CopyAndMutate(float[] source, float mutationRate, float mutationStrength)
    {
        float[] result = new float[source.Length];

        for (int i = 0; i < source.Length; i++)
        {
            result[i] = source[i];

            if (UnityEngine.Random.value <= mutationRate)
            {
                result[i] += UnityEngine.Random.Range(-0.35f, 0.35f) * mutationStrength;
                result[i] = Mathf.Clamp(result[i], -3f, 3f);
            }
        }

        return result;
    }

    private void Randomise(float[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = UnityEngine.Random.Range(-1f, 1f);
        }
    }
}
