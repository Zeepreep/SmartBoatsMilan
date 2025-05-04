using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class DataAnalysis : MonoBehaviour
{
    [SerializeField] private string exportFileName = "SimulationData.csv";

    /// <summary>
    /// Collects and exports relevant data for the current generation.
    /// </summary>
    /// <param name="agents">List of agents to analyze.</param>
    /// <param name="generation">Current generation number.</param>
    public void ExportGenerationData(List<CowLogic> agents, int generation)
    {
        string filePath = Path.Combine(Application.dataPath, exportFileName);

        if (agents == null || agents.Count == 0) return;

        string[] headers =
        {
            "Type", "Generation", "AgentID", "FitnessScore (s)", "GasBoxesCollected",
            "SurvivalTime (s)", "Health", "BehavioralPatterns", "GeneticTraits"
        };

        List<string[]> rows = new List<string[]>();

        agents = agents.OrderByDescending(a => a.GetFitnessScore()).ToList();

        var winner = agents.First();
        rows.Add(new string[]
        {
            "Winner",
            generation.ToString(),
            winner.name,
            (winner.GetFitnessScore()).ToString("F2"),
            winner.GetGasBoxesCollected().ToString(),
            winner.GetSurvivalTimeInSeconds().ToString("F2"),
            winner.GetHealth().ToString("F2"),
            winner.GetQuantifiedBehavioralPatterns(),
            winner.GetGeneticTraits()
        });

        rows.Add(new string[]
        {
            "Median",
            generation.ToString(),
            "N/A",
            GetMedian(agents.Select(a => (float)a.GetFitnessScore()).ToList()).ToString("F2"),
            GetMedian(agents.Select(a => (float)a.GetGasBoxesCollected()).ToList()).ToString(),
            GetMedian(agents.Select(a => a.GetSurvivalTimeInSeconds()).ToList()).ToString("F2"),
            GetMedian(agents.Select(a => a.GetHealth()).ToList()).ToString("F2"),
            "N/A",
            "N/A"
        });

        rows.Add(new string[]
        {
            "Average",
            generation.ToString(),
            "N/A",
            agents.Average(a => a.GetFitnessScore()).ToString("F2"),
            agents.Average(a => a.GetGasBoxesCollected()).ToString(),
            agents.Average(a => a.GetSurvivalTimeInSeconds()).ToString("F2"),
            agents.Average(a => a.GetHealth()).ToString("F2"),
            "N/A",
            "N/A"
        });

        using (StreamWriter writer = new StreamWriter(filePath, true))
        {
            if (new FileInfo(filePath).Length == 0)
            {
                writer.WriteLine(string.Join(";", headers));
            }

            foreach (var row in rows)
            {
                writer.WriteLine(string.Join(";", row));
            }
        }

        Debug.Log($"Data exported to: {filePath}");
    }

    /// <summary>
    /// Calculates the median of a list of floats.
    /// </summary>
    private float GetMedian(List<float> values)
    {
        if (values == null || values.Count == 0) return 0;
        values.Sort();
        int mid = values.Count / 2;
        return values.Count % 2 == 0 ? (values[mid - 1] + values[mid]) / 2.0f : values[mid];
    }
}
