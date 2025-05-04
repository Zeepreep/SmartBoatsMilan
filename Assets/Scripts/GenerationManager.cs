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
    [SerializeField] private GenerateObjectsInArea cowGenerator;

    [Space(10)] [Header("Parenting and Mutation")] [SerializeField]
    private float mutationFactor;

    [SerializeField] private float mutationChance;
    [SerializeField] private int cowParentSize;

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

    [Space(10)] [Header("Debug Options")] [SerializeField, Tooltip("Disable cow spawning.")]
    private bool debugDisableCowSpawning;

    /// <summary>
    /// Those variables are used mostly for debugging in the inspector.
    /// </summary>
    [Header("Former winners")] [SerializeField]
    private AgentData lastCowWinnerData;

    private bool _runningSimulation;
    private List<CowLogic> _activeCows;
    private CowLogic[] _cowParents;

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

        if (simulationCount >= simulationTimer)
        {
            ++generationCount;
            MakeNewGeneration();
            simulationCount = 0;
        }
        else
        {
            simulationCount += Time.deltaTime;
        }
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
    /// Generates cows using the parents list.
    /// If no parents are used, then they are ignored and the cows are generated using the default prefab
    /// specified in their areas.
    /// </summary>
    /// <param name="cowParents"></param>
    public void GenerateObjects(CowLogic[] cowParents = null)
    {
        GenerateCows(cowParents);
    }

    /// <summary>
    /// Generates the list of cows using the parents list. The parent list can be null and, if so, it will be ignored.
    /// Newly created cows will go under mutation (MutationChances and MutationFactor will be applied).
    /// Newly create agents will be Awaken (calling AwakeUp()).
    /// </summary>
    /// <param name="cowParents"></param>
    private void GenerateCows(CowLogic[] cowParents)
    {
        if (debugDisableCowSpawning)
        {
            Debug.Log("Cow spawning is disabled via debug option.");
            return;
        }

        _activeCows = new List<CowLogic>();
        var objects = cowGenerator.RegenerateObjects();
        foreach (var cow in objects.Select(obj => obj.GetComponent<CowLogic>()).Where(cow => cow != null))
        {
            _activeCows.Add(cow);
            if (cowParents != null && cowParents.Length > 0)
            {
                var cowParent = cowParents[Random.Range(0, cowParents.Length)];
                cow.Birth(cowParent.GetData());
            }

            cow.Mutate(mutationFactor, mutationChance);
            cow.AwakeUp();
        }
    }

    /// <summary>
    /// Creates a new generation by using GenerateBoxes and GenerateCows.
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

        if (debugDisableCowSpawning)
        {
            GenerateObjects(null);
            return;
        }

        _activeCows ??= new List<CowLogic>();
        _activeCows.RemoveAll(item => item == null);
        _activeCows.Sort((a, b) =>
        {
            float aHealthFactor = Mathf.Clamp(a.GetHealth() / a.startHealth, 0.1f, 1.0f);
            float bHealthFactor = Mathf.Clamp(b.GetHealth() / b.startHealth, 0.1f, 1.0f);

            float aGasZoneScore = a.GetSurvivalTimeInSeconds() * aHealthFactor * 2.0f; // Higher weight for survival
            float bGasZoneScore = b.GetSurvivalTimeInSeconds() * bHealthFactor * 2.0f;

            float aPointScore = a.GetPoints() + (a.GetData().gasZoneSurvivalTime > 0 ? a.GetPoints() * 0.5f : 0);
            float bPointScore = b.GetPoints() + (b.GetData().gasZoneSurvivalTime > 0 ? b.GetPoints() * 0.5f : 0);

            float aRiskPenalty = a.GetData().gasZoneSurvivalTime > 0 && a.GetPoints() <= 0 ? -10.0f : 0.0f;
            float bRiskPenalty = b.GetData().gasZoneSurvivalTime > 0 && b.GetPoints() <= 0 ? -10.0f : 0.0f;

            float aScore = aGasZoneScore + aPointScore + aRiskPenalty;
            float bScore = bGasZoneScore + bPointScore + bRiskPenalty;

            return bScore.CompareTo(aScore);
        });
        
        if (_activeCows.Count == 0)
        {
            GenerateCows(_cowParents);
            _activeCows.RemoveAll(item => item == null);
            _activeCows.Sort();
        }

        int parentCount = Mathf.Min(cowParentSize, _activeCows.Count);
        _cowParents = new CowLogic[parentCount];
        for (int i = 0; i < parentCount; i++)
        {
            _cowParents[i] = _activeCows[i];
        }

        if (_activeCows.Count > 0)
        {
            var best = _activeCows[0];
            best.name += $"Gen-{generationCount}";
            lastCowWinnerData = best.GetData();
            PrefabUtility.SaveAsPrefabAsset(best.gameObject, $"{savePrefabsAt}{best.name}.prefab");
            Debug.Log($"Last winner had: {best.GetPoints()} points!");
        }

        foreach (var cow in _activeCows)
        {
            if (cow != null)
            {
                Destroy(cow.gameObject);
            }
        }

        var dataAnalysis = FindObjectOfType<DataAnalysis>();
        dataAnalysis.ExportGenerationData(_activeCows, generationCount);
        
        GenerateCows(_cowParents);

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
    /// Stops the count for the simulation. It also removes null (Destroyed) cows from the _activeCows list and sets
    /// all cows to Sleep.
    /// </summary>
    public void StopSimulation()
    {
        _runningSimulation = false;
        _activeCows?.RemoveAll(item => item == null);
        _activeCows?.ForEach(cow => cow.Sleep());
    }
}
