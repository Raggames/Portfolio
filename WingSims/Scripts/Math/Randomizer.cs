using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class Randomizer
{
    public static float badLuckAccumulator = 0f;

    public static float Rand(float min, float max)
    {
        return UnityEngine.Random.Range(min, max);
    }

    public static int Rand(int min, int max)
    {
        return UnityEngine.Random.Range(min, max);
    }

    public static float BadLuckCompensedRand(float min, float max, float compensationRatio = .4f, float averageValue = .5f)
    {
        float rand = UnityEngine.Random.Range(min, max);
        float uncompensatedRand = rand;
        if (rand < averageValue)
        {
            if (badLuckAccumulator < 0)
            {
                float randomBadLuckCompensate = UnityEngine.Random.Range(0, Math.Abs((int)badLuckAccumulator));

                rand += randomBadLuckCompensate;
                badLuckAccumulator += randomBadLuckCompensate;
            }
            badLuckAccumulator += (uncompensatedRand - averageValue) * compensationRatio;
        }   

        rand = Mathf.Clamp(rand, min, max);
        return rand;
    }

    public static void InitRandom()
    {
       UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
    }

    /// <summary>
    /// Returns a random index of the list based on the probabilities given
    /// </summary>
    /// <param name="probabilities">List of probabilities</param>
    /// <returns>Index based on th probabilities</returns>
    public static int Index(List<float> probabilities, bool initSeed = false)
    {
        if (initSeed)
        {
            UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
        }

        float totalProbabilities = 0f;
        for (int i = 0; i < probabilities.Count; ++i)
        {
            totalProbabilities += probabilities[i];
        }

        float rand = UnityEngine.Random.Range(0f, totalProbabilities);

        float sum = 0f;
        for (int i = 0; i < probabilities.Count; ++i)
        {
            sum += probabilities[i];
            if (sum > rand)
            {
                return i;
            }
        }

        return -1;
    }


}

