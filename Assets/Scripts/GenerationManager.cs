using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class GenerationManager : MonoBehaviour
{
    [Header("Generators")] [SerializeField]
    private GenerateObjectsInArea[] boxGenerators;
    
    [SerializeField] private GasSphere[] gasSpheres;

    [Space(10)]
    [SerializeField] private GenerateObjectsInArea boatGenerator;

    [Space(10)] [Header("Parenting and Mutation")] [SerializeField]
    private float mutationFactor;

    [SerializeField] private float mutationChance;
    [SerializeField] private int boatParentSize;

    [Space(10)] [Header("Simulation Controls")] [SerializeField, Tooltip("Time per simulation (in seconds).")]
    private float simulationTimer;

    [SerializeField, Tooltip("Current time spent on this simulation.")]
    private float simulationCount;

    [SerializeField, Tooltip("Automatically starts the simulation on Play.")]
    private bool runOnStart;

    [SerializeField, Tooltip("Initial count for the simulation. Used for the Prefabs naming.")]
    private int generationCount;

    [Space(10)] [Header("Prefab Saving")] [SerializeField]
    private string savePrefabsAt;

    [Space(10)] [Header("Debug Options")] [SerializeField, Tooltip("Disable boat spawning.")]
    private bool debugDisableBoatSpawning;

    /// <summary>
    /// Those variables are used mostly for debugging in the inspector.
    /// </summary>
    [Header("Former winners")] [SerializeField]
    private AgentData lastBoatWinnerData;

    private bool _runningSimulation;
    private List<CowLogic> _activeBoats;
    private CowLogic[] _boatParents;

    private void Awake()
    {
        Random.InitState(6);
    }

    private void Start()
    {
        if (runOnStart)
        {
            StartSimulation();
        }
    }

    private void Update()
    {
        if (!_runningSimulation) return;
        //Creates a new generation.
        if (simulationCount >= simulationTimer)
        {
            ++generationCount;
            MakeNewGeneration();
            simulationCount = -Time.deltaTime;
        }

        simulationCount += Time.deltaTime;
    }

    /// <summary>
    /// Generates the boxes on all box areas.
    /// </summary>
    public void GenerateBoxes()
    {
        foreach (var generateObjectsInArea in boxGenerators)
        {
            generateObjectsInArea.RegenerateObjects();
        }
    }

    /// <summary>
    /// Generates boats using the parents list.
    /// If no parents are used, then they are ignored and the boats are generated using the default prefab
    /// specified in their areas.
    /// </summary>
    /// <param name="boatParents"></param>
    public void GenerateObjects(CowLogic[] boatParents = null)
    {
        GenerateBoats(boatParents);
    }

    /// <summary>
    /// Generates the list of boats using the parents list. The parent list can be null and, if so, it will be ignored.
    /// Newly created boats will go under mutation (MutationChances and MutationFactor will be applied).
    /// Newly create agents will be Awaken (calling AwakeUp()).
    /// </summary>
    /// <param name="boatParents"></param>
    private void GenerateBoats(CowLogic[] boatParents)
    {
        if (debugDisableBoatSpawning)
        {
            Debug.Log("Boat spawning is disabled via debug option.");
            return;
        }

        _activeBoats = new List<CowLogic>();
        var objects = boatGenerator.RegenerateObjects();
        foreach (var boat in objects.Select(obj => obj.GetComponent<CowLogic>()).Where(boat => boat != null))
        {
            _activeBoats.Add(boat);
            if (boatParents != null && boatParents.Length > 0)
            {
                var boatParent = boatParents[Random.Range(0, boatParents.Length)];
                boat.Birth(boatParent.GetData());
            }

            boat.Mutate(mutationFactor, mutationChance);
            boat.AwakeUp();
        }
    }

    /// <summary>
    /// Creates a new generation by using GenerateBoxes and GenerateBoats.
    /// Previous generations will be removed and the best parents will be selected and used to create the new generation.
    /// The best parents (top 1) of the generation will be stored as a Prefab in the [savePrefabsAt] folder. Their name
    /// will use the [generationCount] as an identifier.
    /// </summary>
    private void MakeNewGeneration()
    {
        Random.InitState(6);

        GenerateBoxes();

        foreach (GasSphere _gS in gasSpheres)
        {
            _gS.CheckCowsInSphere();
        }

        if (debugDisableBoatSpawning)
        {
            GenerateObjects(null);
            return;
        }

        _activeBoats ??= new List<CowLogic>();
        _activeBoats.RemoveAll(item => item == null);
        _activeBoats.Sort((a, b) =>
        {
            // Health factor (scaled and clamped)
            float aHealthFactor = Mathf.Clamp(a.GetHealth() / a.startHealth, 0.1f, 1.0f);
            float bHealthFactor = Mathf.Clamp(b.GetHealth() / b.startHealth, 0.1f, 1.0f);

            // Gas zone survival time (reward scaled by health factor)
            float aGasZoneScore = a.GetSurvivalTimeInSeconds() * aHealthFactor * 2.0f; // Higher weight for survival
            float bGasZoneScore = b.GetSurvivalTimeInSeconds() * bHealthFactor * 2.0f;

            // Points collected (with bonus for gas zone points)
            float aPointScore = a.GetPoints() + (a.GetData().gasZoneSurvivalTime > 0 ? a.GetPoints() * 0.5f : 0);
            float bPointScore = b.GetPoints() + (b.GetData().gasZoneSurvivalTime > 0 ? b.GetPoints() * 0.5f : 0);

            // Risk factor (penalty for staying in gas zone too long without progress)
            float aRiskPenalty = a.GetData().gasZoneSurvivalTime > 0 && a.GetPoints() <= 0 ? -10.0f : 0.0f;
            float bRiskPenalty = b.GetData().gasZoneSurvivalTime > 0 && b.GetPoints() <= 0 ? -10.0f : 0.0f;

            // Final score calculation
            float aScore = aGasZoneScore + aPointScore + aRiskPenalty;
            float bScore = bGasZoneScore + bPointScore + bRiskPenalty;

            return bScore.CompareTo(aScore);
        });
        
        if (_activeBoats.Count == 0)
        {
            GenerateBoats(_boatParents);
            _activeBoats.RemoveAll(item => item == null);
            _activeBoats.Sort();
        }

        // Select top parents
        int parentCount = Mathf.Min(boatParentSize, _activeBoats.Count);
        _boatParents = new CowLogic[parentCount];
        for (int i = 0; i < parentCount; i++)
        {
            _boatParents[i] = _activeBoats[i];
        }

        // Save best agent as prefab
        if (_activeBoats.Count > 0)
        {
            var best = _activeBoats[0];
            best.name += $"Gen-{generationCount}";
            lastBoatWinnerData = best.GetData();
            PrefabUtility.SaveAsPrefabAsset(best.gameObject, $"{savePrefabsAt}{best.name}.prefab");
            Debug.Log($"Last winner had: {best.GetPoints()} points!");
        }

        // Destroy all boats from the previous generation
        foreach (var boat in _activeBoats)
        {
            if (boat != null)
            {
                Destroy(boat.gameObject);
            }
        }

        var dataAnalysis = FindObjectOfType<DataAnalysis>();
        dataAnalysis.ExportGenerationData(_activeBoats, generationCount);
        
        // Create new generation from selected parents
        GenerateBoats(_boatParents);

        ++generationCount;
    }


    /// <summary>
    /// Starts a new simulation. It does not call MakeNewGeneration. It calls both GenerateBoxes and GenerateObjects and
    /// then sets the _runningSimulation flag to true.
    /// </summary>
    public void StartSimulation()
    {
        Random.InitState(6);

        GenerateBoxes();
        GenerateObjects();
        _runningSimulation = true;
    }

    /// <summary>
    /// Continues the simulation. It calls MakeNewGeneration to use the previous state of the simulation and continue it.
    /// It sets the _runningSimulation flag to true.
    /// </summary>
    public void ContinueSimulation()
    {
        MakeNewGeneration();
        _runningSimulation = true;
    }

    /// <summary>
    /// Stops the count for the simulation. It also removes null (Destroyed) boats from the _activeBoats list and sets
    /// all boats to Sleep.
    /// </summary>
    public void StopSimulation()
    {
        _runningSimulation = false;
        _activeBoats?.RemoveAll(item => item == null);
        _activeBoats?.ForEach(boat => boat.Sleep());
    }
}
