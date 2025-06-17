using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistributionUtils : MonoBehaviour
{
    public static Dictionary<T, float> CombineDistributions<T>(
        Dictionary<T, float>[] distributions,
        float[] weights = null
    )
    {
        if (distributions == null || distributions.Length == 0)
            return new Dictionary<T, float>();

        // If weights are not provided, use equal weights
        if (weights == null || weights.Length != distributions.Length)
        {
            weights = new float[distributions.Length];
            float equalWeight = 1f / distributions.Length;
            for (int i = 0; i < weights.Length; i++)
                weights[i] = equalWeight;
        }

        var combinedDistribution = new Dictionary<T, float>();

        // Get all unique keys from all distributions
        var allKeys = new HashSet<T>();
        foreach (var distribution in distributions)
        {
            if (distribution != null)
            {
                foreach (var key in distribution.Keys)
                    allKeys.Add(key);
            }
        }

        // Combine the distributions with weighted averaging
        foreach (var key in allKeys)
        {
            float combinedValue = 0f;
            for (int i = 0; i < distributions.Length; i++)
            {
                float value =
                    distributions[i] != null && distributions[i].ContainsKey(key)
                        ? distributions[i][key]
                        : 0f;
                combinedValue += value * weights[i];
            }
            combinedDistribution[key] = combinedValue;
        }

        return combinedDistribution;
    }

    public static T SampleDistribution<T>(Dictionary<T, float> distribution, float temperature)
    {
        if (temperature <= 0f)
        {
            throw new Exception("Temperature is less than or equal to 0");
        }

        if (distribution == null || distribution.Count == 0)
            return default(T);

        // Apply temperature to weights (similar to softmax with temperature)
        var adjustedWeights = new Dictionary<T, float>();
        float maxWeight = float.NegativeInfinity;

        foreach (var kvp in distribution)
        {
            if (kvp.Value > maxWeight)
                maxWeight = kvp.Value;
        }

        foreach (var kvp in distribution)
        {
            // Apply temperature scaling and convert to probabilities
            float adjustedWeight = Mathf.Exp((kvp.Value - maxWeight) / temperature);
            adjustedWeights[kvp.Key] = adjustedWeight;
        }

        // Calculate total weight for normalization
        float totalWeight = 0f;
        foreach (var weight in adjustedWeights.Values)
        {
            totalWeight += weight;
        }

        // Sample from the distribution
        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var kvp in adjustedWeights)
        {
            currentWeight += kvp.Value;
            if (randomValue <= currentWeight)
            {
                return kvp.Key;
            }
        }

        // Fallback (shouldn't reach here, but just in case)
        var fallbackItems = new List<T>(distribution.Keys);
        return fallbackItems[0];
    }

    public static Dictionary<T, float> NormalizeDistribution<T>(
        Dictionary<T, float> distribution,
        float min = 0f,
        float max = 1f
    )
    {
        if (distribution == null || distribution.Count == 0)
            return new Dictionary<T, float>();

        var normalizedDistribution = new Dictionary<T, float>();

        // Find the current min and max values
        float currentMin = float.MaxValue;
        float currentMax = float.MinValue;

        foreach (var kvp in distribution)
        {
            if (kvp.Value < currentMin)
                currentMin = kvp.Value;
            if (kvp.Value > currentMax)
                currentMax = kvp.Value;
        }

        // Avoid division by zero if all values are the same
        if (Mathf.Approximately(currentMin, currentMax))
        {
            foreach (var kvp in distribution)
            {
                normalizedDistribution[kvp.Key] = min;
            }
            return normalizedDistribution;
        }

        // Normalize each value to the target range
        foreach (var kvp in distribution)
        {
            float normalizedValue = Mathf.Lerp(
                min,
                max,
                (kvp.Value - currentMin) / (currentMax - currentMin)
            );
            normalizedDistribution[kvp.Key] = normalizedValue;
        }

        return normalizedDistribution;
    }
}
