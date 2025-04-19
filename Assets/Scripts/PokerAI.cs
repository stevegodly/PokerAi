using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using Unity.Barracuda;
using System.Linq;

public static class PokerAI
{
    private static readonly string RANKS = "23456789TJKQA";
    private static readonly string[] actions = { "fold", "call", "bet" };

    private static NNModel modelAsset;
    private static Model runtimeModel;
    private static IWorker worker;

    private static bool initialized = false;

    public static void LoadStrategy()
    {
        if (initialized) return;

        modelAsset = Resources.Load<NNModel>("Models/poker_policy");

        if (modelAsset == null)
        {
            Debug.LogError("Model not found! Ensure it's in Resources/Models and named correctly.");
            return;
        }

        runtimeModel = ModelLoader.Load(modelAsset);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, runtimeModel);
        initialized = true;

        Application.quitting += Cleanup;

        Debug.Log("PokerAI initialized.");
    }

    public static string DecideAction(List<string> hand, out float prob, int pot = 2)
    {
        if (!initialized)
        {
            LoadStrategy();
            if (!initialized)
            {
                prob = 0f;
                return "fold";
            }
        }

        float[] infoSet = GetInfoSet(hand, pot);

        using var input = new Tensor(1, infoSet.Length, infoSet);
        worker.Execute(input);

        using Tensor output = worker.PeekOutput();
        float[] result = output.ToReadOnlyArray();

        return SelectAction(result.ToList(), out prob);
    }

    private static float[] GetInfoSet(List<string> hand, int pot)
    {
        GetHandRank(hand, out int rank, out List<int> tiebreakers, out _);

        float[] input = new float[7];
        input[0] = rank;
        input[1] = pot;

        for (int i = 0; i < tiebreakers.Count && (2 + i) < input.Length; i++)
        {
            input[2 + i] = tiebreakers[i];
        }

        return input;
    }

    public static void GetHandRank(List<string> hand, out int rank, out List<int> tiebreakers, out List<int> discardIndices)
    {
        List<int> ranks = hand.Select(card =>
        {
            string rankStr = card[..^1];
            string normalizedRank = rankStr == "10" ? "T" : rankStr;
            return RANKS.IndexOf(normalizedRank);
        }).ToList();

        List<char> suits = hand.Select(card => card[^1]).ToList();
        var rankCounts = ranks.GroupBy(r => r).ToDictionary(g => g.Key, g => g.Count());
        var suitCounts = suits.GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());

        bool isFlush = suitCounts.Values.Max() == 5;
        bool isStraight = IsStraight(ranks.OrderByDescending(x => x).ToList());

        discardIndices = new List<int>();

        if (isFlush && isStraight)
        {
            rank = 9;
            tiebreakers = ranks.OrderByDescending(x => x).ToList();
        }
        else if (rankCounts.Values.Contains(4))
        {
            rank = 8;
            int fourRank = rankCounts.First(x => x.Value == 4).Key;
            int kicker = rankCounts.First(x => x.Value == 1).Key;
            tiebreakers = new List<int> { fourRank, kicker };
            discardIndices = GetIndicesNotMatchingRanks(ranks, new List<int> { fourRank });
        }
        else if (rankCounts.Values.Contains(3) && rankCounts.Values.Contains(2))
        {
            rank = 7;
            int threeRank = rankCounts.First(x => x.Value == 3).Key;
            int pairRank = rankCounts.First(x => x.Value == 2).Key;
            tiebreakers = new List<int> { threeRank, pairRank };
            discardIndices = GetIndicesNotMatchingRanks(ranks, new List<int> { threeRank, pairRank });
        }
        else if (isFlush)
        {
            rank = 6;
            tiebreakers = ranks.OrderByDescending(x => x).ToList();
        }
        else if (isStraight)
        {
            rank = 5;
            tiebreakers = ranks.OrderByDescending(x => x).ToList();
        }
        else if (rankCounts.Values.Contains(3))
        {
            rank = 4;
            int threeRank = rankCounts.First(x => x.Value == 3).Key;
            List<int> kickers = ranks.Where(r => r != threeRank).OrderByDescending(x => x).ToList();
            tiebreakers = new List<int> { threeRank }.Concat(kickers).ToList();
            discardIndices = GetIndicesNotMatchingRanks(ranks, new List<int> { threeRank });
        }
        else if (rankCounts.Values.Count(x => x == 2) == 2)
        {
            rank = 3;
            List<int> pairs = rankCounts.Where(x => x.Value == 2).Select(x => x.Key).OrderByDescending(x => x).ToList();
            int kicker = ranks.First(x => !pairs.Contains(x));
            tiebreakers = pairs.Concat(new List<int> { kicker }).ToList();
            discardIndices = GetIndicesNotMatchingRanks(ranks, pairs);
        }
        else if (rankCounts.Values.Contains(2))
        {
            rank = 2;
            int pairRank = rankCounts.First(x => x.Value == 2).Key;
            List<int> kickers = ranks.Where(r => r != pairRank).OrderByDescending(x => x).ToList();
            tiebreakers = new List<int> { pairRank }.Concat(kickers).ToList();
            discardIndices = GetIndicesNotMatchingRanks(ranks, new List<int> { pairRank });
        }
        else
        {
            rank = 1;
            tiebreakers = ranks.OrderByDescending(x => x).ToList();
            int maxRank = ranks.Max();
            discardIndices = GetIndicesNotMatchingRanks(ranks, new List<int> { maxRank });
        }
    }

    private static List<int> GetIndicesNotMatchingRanks(List<int> ranks, List<int> keepRanks)
    {
        var indices = new List<int>();
        for (int i = 0; i < ranks.Count; i++)
        {
            if (!keepRanks.Contains(ranks[i]))
                indices.Add(i);
        }
        return indices;
    }

    private static bool IsStraight(List<int> ranks)
    {
        return ranks.SequenceEqual(Enumerable.Range(ranks[0], 5).ToList()) ||
               ranks.SequenceEqual(new List<int> { 12, 3, 2, 1, 0 });
    }

    private static string SelectAction(List<float> actionProbabilities, out float prob)
    {
        int maxIndex = actionProbabilities.IndexOf(actionProbabilities.Max());
        prob = actionProbabilities[maxIndex];
        return actions[maxIndex];
    }

    private static void Cleanup()
    {
        worker?.Dispose();
        worker = null;
        initialized = false;
        Debug.Log("PokerAI resources cleaned up.");
    }
}
