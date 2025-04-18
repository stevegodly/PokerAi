using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;

using System.Linq;

public static class PokerAI
{
    private static string RANKS = "23456789TJKQA";
    private static Dictionary<string, List<float>> strategy;
    private static string[] actions = { "fold", "call", "bet" };

    public static void LoadStrategy()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("new_cfr_strategy1");
        if (jsonFile != null)
        {
            strategy = JsonConvert.DeserializeObject<Dictionary<string, List<float>>>(jsonFile.text);
            Debug.Log("Strategy loaded successfully!");
        }
        else
        {
            Debug.LogError("Failed to load strategy file.");
        }
    }

    public static string DecideAction(List<string> hand,out float prob)
    {
        string infoSet = GetInfoSet(hand); 
        prob = 1.0f; // Default probability

        if (strategy != null && strategy.ContainsKey(infoSet))
        {
            List<float> actionProbabilities = strategy[infoSet];  
            return SelectAction(actionProbabilities,out prob);  
        }
        else
        {
            Debug.LogWarning("InfoSet not found, choosing random action.");
            return actions[Random.Range(0, actions.Length)]; 
        }
    }

    private static string GetInfoSet(List<string> hand)
    {
        int rank;
        List<int> tiebreakers;
        List<int> discardIndices;
        GetHandRank(hand, out rank, out tiebreakers, out discardIndices);
    
        string rankString = rank + "-" + string.Join("-", tiebreakers.Select(t => t.ToString()).ToArray());
        return rankString;
    }

    public static void GetHandRank(List<string> hand, out int rank, out List<int> tiebreakers, out List<int> discardIndices)
    {
        List<int> ranks = hand.Select(card =>
        {
            string rankStr = card.Substring(0, card.Length - 1);
            string normalizedRank = rankStr == "10" ? "T" : rankStr;
            return RANKS.IndexOf(normalizedRank);
        }).ToList();
        List<char> suits = hand.Select(card => card.Last()).ToList();
        Dictionary<int, int> rankCounts = ranks.GroupBy(r => r).ToDictionary(g => g.Key, g => g.Count());
        Dictionary<char, int> suitCounts = suits.GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());

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

            // Keep the highest card only
            int maxRank = ranks.Max();
            discardIndices = GetIndicesNotMatchingRanks(ranks, new List<int> { maxRank });
        }
    }
    
    private static List<int> GetIndicesNotMatchingRanks(List<int> ranks, List<int> keepRanks)
    {
        List<int> indices = new List<int>();
        for (int i = 0; i < ranks.Count; i++)
        {
            if (!keepRanks.Contains(ranks[i])) indices.Add(i);
        }
        return indices;
    }

    
    private static bool IsStraight(List<int> ranks)
    {
        return ranks.SequenceEqual(Enumerable.Range(ranks[0], 5).ToList()) ||
            ranks.SequenceEqual(new List<int> { 12, 3, 2, 1, 0 });  
    }

    
    private static string SelectAction(List<float> actionProbabilities,out float prob)
    {
       
        int maxIndex = actionProbabilities.IndexOf(actionProbabilities.Max());
        prob=actionProbabilities[maxIndex];
        
        return actions[maxIndex];
    }

    
}
