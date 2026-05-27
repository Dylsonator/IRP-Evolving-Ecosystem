using System;
using UnityEngine;

// Small evolvable neural network used by fish to bias decisions without hand-authoring every action.
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

    // Creates a new random network with the requested input, hidden and output counts
    public static SimpleNeuralNetwork CreateRandom(int inputCount, int hiddenCount, int outputCount)
    {
        SimpleNeuralNetwork network = new SimpleNeuralNetwork();
        network.InputCount = Mathf.Max(1, inputCount);
        network.HiddenCount = Mathf.Max(1, hiddenCount);
        network.OutputCount = Mathf.Max(1, outputCount);

        network.InputHiddenWeights = new float[network.InputCount * network.HiddenCount];
        network.HiddenOutputWeights = new float[network.HiddenCount * network.OutputCount];
        network.HiddenBiases = new float[network.HiddenCount];
        network.OutputBiases = new float[network.OutputCount];

        network.Randomise(network.InputHiddenWeights);
        network.Randomise(network.HiddenOutputWeights);
        network.Randomise(network.HiddenBiases);
        network.Randomise(network.OutputBiases);

        return network;
    }

    // Runs the network and returns a new output array
    public float[] Evaluate(float[] inputs)
    {
        float[] outputs = new float[Mathf.Max(1, OutputCount)];
        float[] hidden = new float[Mathf.Max(1, HiddenCount)];
        EvaluateNonAlloc(inputs, outputs, hidden);
        return outputs;
    }

    // Runs the network into supplied buffers so the sim avoids garbage allocations
    public bool EvaluateNonAlloc(float[] inputs, float[] outputs, float[] hiddenScratch)
    {
        if (!ValidateNetwork())
        {
            return false;
        }

        if (inputs == null || inputs.Length != InputCount || outputs == null || outputs.Length < OutputCount || hiddenScratch == null || hiddenScratch.Length < HiddenCount)
        {
            // Do not spam warnings during simulation. Invalid buffers can happen during hot reload / old saved assets.
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

            hiddenScratch[h] = FastTanh(sum);
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

            outputs[o] = Mathf.Clamp(FastTanh(sum), -1f, 1f);
        }

        return true;
    }

    // Copies the network and mutates weights, biases and sometimes structure
    public SimpleNeuralNetwork CreateMutatedCopy(float mutationRate, float mutationStrength)
    {
        return CreateMutatedCopy(mutationRate, mutationStrength, 0f, HiddenCount);
    }

    // Copies the network and mutates weights, biases and sometimes structure
    public SimpleNeuralNetwork CreateMutatedCopy(float mutationRate, float mutationStrength, float structuralMutationRate, int maxHiddenCount)
    {
        if (!ValidateNetwork())
        {
            return CreateRandom(Mathf.Max(1, InputCount), Mathf.Max(1, HiddenCount), Mathf.Max(1, OutputCount));
        }

        SimpleNeuralNetwork copy = CopyStructure();

        copy.InputHiddenWeights = CopyAndMutate(InputHiddenWeights, mutationRate, mutationStrength);
        copy.HiddenOutputWeights = CopyAndMutate(HiddenOutputWeights, mutationRate, mutationStrength);
        copy.HiddenBiases = CopyAndMutate(HiddenBiases, mutationRate, mutationStrength);
        copy.OutputBiases = CopyAndMutate(OutputBiases, mutationRate, mutationStrength);

        maxHiddenCount = Mathf.Max(copy.HiddenCount, maxHiddenCount);
        if (structuralMutationRate > 0f && copy.HiddenCount < maxHiddenCount && UnityEngine.Random.value <= structuralMutationRate)
        {
            copy = copy.CreateWithAdditionalHiddenNode();
        }

        return copy;
    }

    // Resizes a network while keeping as many old weights as possible
    public SimpleNeuralNetwork ResizeTo(int inputCount, int hiddenCount, int outputCount)
    {
        inputCount = Mathf.Max(1, inputCount);
        hiddenCount = Mathf.Max(1, hiddenCount);
        outputCount = Mathf.Max(1, outputCount);

        SimpleNeuralNetwork resized = CreateRandom(inputCount, hiddenCount, outputCount);

        int inputMin = Mathf.Min(InputCount, inputCount);
        int hiddenMin = Mathf.Min(HiddenCount, hiddenCount);
        int outputMin = Mathf.Min(OutputCount, outputCount);

        for (int i = 0; i < inputMin; i++)
        {
            for (int h = 0; h < hiddenMin; h++)
            {
                resized.InputHiddenWeights[i * hiddenCount + h] = InputHiddenWeights[i * HiddenCount + h];
            }
        }

        for (int h = 0; h < hiddenMin; h++)
        {
            resized.HiddenBiases[h] = HiddenBiases[h];
            for (int o = 0; o < outputMin; o++)
            {
                resized.HiddenOutputWeights[h * outputCount + o] = HiddenOutputWeights[h * OutputCount + o];
            }
        }

        for (int o = 0; o < outputMin; o++)
        {
            resized.OutputBiases[o] = OutputBiases[o];
        }

        return resized;
    }

    // Counts active weights so brain complexity can be logged
    public int GetConnectionCount()
    {
        return Mathf.Max(0, InputCount * HiddenCount + HiddenCount * OutputCount);
    }

    // Copies network arrays without changing their values
    private SimpleNeuralNetwork CopyStructure()
    {
        SimpleNeuralNetwork copy = new SimpleNeuralNetwork();
        copy.InputCount = InputCount;
        copy.HiddenCount = HiddenCount;
        copy.OutputCount = OutputCount;
        return copy;
    }

    // Adds one hidden node while keeping old connections where possible
    private SimpleNeuralNetwork CreateWithAdditionalHiddenNode()
    {
        int newHiddenCount = HiddenCount + 1;
        SimpleNeuralNetwork expanded = CreateRandom(InputCount, newHiddenCount, OutputCount);

        for (int i = 0; i < InputCount; i++)
        {
            for (int h = 0; h < HiddenCount; h++)
            {
                expanded.InputHiddenWeights[i * newHiddenCount + h] = InputHiddenWeights[i * HiddenCount + h];
            }

            expanded.InputHiddenWeights[i * newHiddenCount + HiddenCount] = UnityEngine.Random.Range(-0.2f, 0.2f);
        }

        for (int h = 0; h < HiddenCount; h++)
        {
            expanded.HiddenBiases[h] = HiddenBiases[h];
            for (int o = 0; o < OutputCount; o++)
            {
                expanded.HiddenOutputWeights[h * OutputCount + o] = HiddenOutputWeights[h * OutputCount + o];
            }
        }

        expanded.HiddenBiases[HiddenCount] = UnityEngine.Random.Range(-0.1f, 0.1f);
        for (int o = 0; o < OutputCount; o++)
        {
            expanded.OutputBiases[o] = OutputBiases[o];
            expanded.HiddenOutputWeights[HiddenCount * OutputCount + o] = UnityEngine.Random.Range(-0.2f, 0.2f);
        }

        return expanded;
    }

    // Copies an array and randomly nudges values within a safe range
    private float[] CopyAndMutate(float[] source, float mutationRate, float mutationStrength)
    {
        if (source == null)
        {
            return new float[0];
        }

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

    // Fills a weight array with random starting values
    private void Randomise(float[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = UnityEngine.Random.Range(-1f, 1f);
        }
    }

    // Makes sure arrays exist and match the expected network size
    private bool ValidateNetwork()
    {
        return InputCount > 0
            && HiddenCount > 0
            && OutputCount > 0
            && InputHiddenWeights != null
            && HiddenOutputWeights != null
            && HiddenBiases != null
            && OutputBiases != null
            && InputHiddenWeights.Length == InputCount * HiddenCount
            && HiddenOutputWeights.Length == HiddenCount * OutputCount
            && HiddenBiases.Length == HiddenCount
            && OutputBiases.Length == OutputCount;
    }

    // Uses a cheap tanh-style clamp for brain output
    private float FastTanh(float value)
    {
        return (float)Math.Tanh(value);
    }
}
