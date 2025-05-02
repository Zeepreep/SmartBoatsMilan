﻿using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class GenerationManager : MonoBehaviour
{
    [Header("Generators")] [SerializeField]
    private GenerateObjectsInArea[] boxGenerators;

    [SerializeField] private GenerateObjectsInArea boatGenerator;
    [SerializeField] private GenerateObjectsInArea pirateGenerator;

    [Space(10)] [Header("Parenting and Mutation")] [SerializeField]
    private float mutationFactor;

    [SerializeField] private float mutationChance;
    [SerializeField] private int boatParentSize;
    [SerializeField] private int pirateParentSize;

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

    /// <summary>
    /// Those variables are used mostly for debugging in the inspector.
    /// </summary>
    [Header("Former winners")] [SerializeField]
    private AgentData lastBoatWinnerData;

    [SerializeField] private AgentData lastPirateWinnerData;

    private bool _runningSimulation;
    private List<CowLogic> _activeBoats;
    private List<PirateLogic> _activePirates;
    private CowLogic[] _boatParents;
    private PirateLogic[] _pirateParents;

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
    /// Generates boats and pirates using the parents list.
    /// If no parents are used, then they are ignored and the boats/pirates are generated using the default prefab
    /// specified in their areas.
    /// </summary>
    /// <param name="boatParents"></param>
    /// <param name="pirateParents"></param>
    public void GenerateObjects(CowLogic[] boatParents = null, PirateLogic[] pirateParents = null)
    {
        GenerateBoats(boatParents);
        GeneratePirates(pirateParents);
    }

    /// <summary>
    /// Generates the list of pirates using the parents list. The parent list can be null and, if so, it will be ignored.
    /// Newly created pirates will go under mutation (MutationChances and MutationFactor will be applied).
    /// Newly create agents will be Awaken (calling AwakeUp()).
    /// </summary>
    /// <param name="pirateParents"></param>
    private void GeneratePirates(PirateLogic[] pirateParents)
    {
        _activePirates = new List<PirateLogic>();
        var objects = pirateGenerator.RegenerateObjects();
        foreach (var pirate in objects.Select(obj => obj.GetComponent<PirateLogic>()).Where(pirate => pirate != null))
        {
            _activePirates.Add(pirate);
            if (pirateParents != null)
            {
                var pirateParent = pirateParents[Random.Range(0, pirateParents.Length)];
                pirate.Birth(pirateParent.GetData());
            }

            pirate.Mutate(mutationFactor, mutationChance);
            pirate.AwakeUp();
        }
    }

    /// <summary>
    /// Generates the list of boats using the parents list. The parent list can be null and, if so, it will be ignored.
    /// Newly created boats will go under mutation (MutationChances and MutationFactor will be applied).
    /// /// Newly create agents will be Awaken (calling AwakeUp()).
    /// </summary>
    /// <param name="boatParents"></param>
    private void GenerateBoats(CowLogic[] boatParents)
    {
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
    /// Creates a new generation by using GenerateBoxes and GenerateBoats/Pirates.
    /// Previous generations will be removed and the best parents will be selected and used to create the new generation.
    /// The best parents (top 1) of the generation will be stored as a Prefab in the [savePrefabsAt] folder. Their name
    /// will use the [generationCount] as an identifier.
    /// </summary>
    private void MakeNewGeneration()
    {
        Random.InitState(6);

        GenerateBoxes();

        //Fetch parents
        _activeBoats.RemoveAll(item => item == null);
        _activeBoats.Sort();
        if (_activeBoats.Count == 0)
        {
            GenerateBoats(_boatParents);
        }

        int parentCount = Mathf.Min(boatParentSize, _activeBoats.Count);
        _boatParents = new CowLogic[parentCount];
        for (int i = 0; i < parentCount; i++)
        {
            _boatParents[i] = _activeBoats[i];
        }


        var lastBoatWinner = _activeBoats[0];
        lastBoatWinner.name += "Gen-" + generationCount;
        lastBoatWinnerData = lastBoatWinner.GetData();
        PrefabUtility.SaveAsPrefabAsset(lastBoatWinner.gameObject, savePrefabsAt + lastBoatWinner.name + ".prefab");

        _activePirates.RemoveAll(item => item == null);
        _activePirates.Sort();
        
        parentCount = Mathf.Min(pirateParentSize, _activePirates.Count);
        _pirateParents = new PirateLogic[parentCount];
        for (var i = 0; i < parentCount; i++)
        {
            _pirateParents[i] = _activePirates[i];
        }

        var lastPirateWinner = _activePirates[0];
        lastPirateWinner.name += "Gen-" + generationCount;
        lastPirateWinnerData = lastPirateWinner.GetData();
        PrefabUtility.SaveAsPrefabAsset(lastPirateWinner.gameObject, savePrefabsAt + lastPirateWinner.name + ".prefab");

        //Winners:
        Debug.Log("Last winner boat had: " + lastBoatWinner.GetPoints() + " points!" + " Last winner pirate had: " +
                  lastPirateWinner.GetPoints() + " points!");

        GenerateObjects(_boatParents, _pirateParents);
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
    /// all boats and pirates to Sleep.
    /// </summary>
    public void StopSimulation()
    {
        _runningSimulation = false;
        _activeBoats.RemoveAll(item => item == null);
        _activeBoats.ForEach(boat => boat.Sleep());
        _activePirates.ForEach(pirate => pirate.Sleep());
    }
}