using System.Collections.Generic;
using UnityEngine;

// Puts active fish under readable scene folders by role/species.
public class CreatureSpeciesParentOrganizer : MonoBehaviour
{
    [Header("Organisation")]
    public bool AutoOrganiseCreatures = true;
    public bool UseManagerCreatureList = true;
    public bool GroupByRoleAndMorphology = true;
    public string RootParentName = "Runtime Creature Species Groups";
    public float RefreshInterval = 0.5f;

    [Header("Debug")]
    public bool LogCreatedGroups;

    private readonly Dictionary<string, Transform> groupParents = new Dictionary<string, Transform>();
    private Transform rootParent;
    private float refreshTimer;

    // Sets up cached references and safe starting values before the sim runs
    private void Awake()
    {
        EnsureRootParent();
    }

    // Runs the normal frame checks and timers
    private void Update()
    {
        if (!AutoOrganiseCreatures)
        {
            return;
        }

        refreshTimer += Time.deltaTime;

        if (refreshTimer < RefreshInterval)
        {
            return;
        }

        refreshTimer = 0f;
        OrganiseNow();
    }

    [ContextMenu("Organise Now")]
    // Handles organise now
    public void OrganiseNow()
    {
        EnsureRootParent();
        List<MarineCreatureAgent> creatures = GetCreatures();

        for (int i = 0; i < creatures.Count; i++)
        {
            MarineCreatureAgent creature = creatures[i];

            if (creature == null)
            {
                continue;
            }

            string groupName = "Uninitialised Group";

            if (creature.Candidate != null && creature.Candidate.Genome != null)
            {
                CreatureBehaviourType type = CreatureDebugTypeUtility.GetBehaviourType(creature.Candidate.Genome);
                creature.DebugBehaviourType = type;

                groupName = GroupByRoleAndMorphology
                    ? CreatureDebugTypeUtility.GetSpeciesGroupName(creature.Candidate.Genome) + " Group"
                    : CreatureDebugTypeUtility.GetTypeName(type) + " Group";
            }

            Transform groupParent = GetOrCreateGroupParent(groupName);

            if (creature.transform.parent != groupParent)
            {
                creature.transform.SetParent(groupParent, true);
            }
        }
    }

    // Gets the creatures used by the sim
    private List<MarineCreatureAgent> GetCreatures()
    {
        if (UseManagerCreatureList && EvolutionEcosystemManager.Instance != null)
        {
            return EvolutionEcosystemManager.Instance.GetActiveCreatures();
        }

        MarineCreatureAgent[] found = FindObjectsByType<MarineCreatureAgent>(FindObjectsSortMode.None);
        return new List<MarineCreatureAgent>(found);
    }

    // Handles ensure root parent
    private void EnsureRootParent()
    {
        if (rootParent != null)
        {
            return;
        }

        Transform existing = transform.Find(RootParentName);

        if (existing != null)
        {
            rootParent = existing;
            return;
        }

        GameObject rootObject = new GameObject(RootParentName);
        rootObject.transform.SetParent(transform, false);
        rootParent = rootObject.transform;
    }

    // Gets the or create group parent used by the sim
    private Transform GetOrCreateGroupParent(string groupName)
    {
        if (groupParents.TryGetValue(groupName, out Transform parent) && parent != null)
        {
            return parent;
        }

        EnsureRootParent();
        Transform existing = rootParent.Find(groupName);

        if (existing != null)
        {
            groupParents[groupName] = existing;
            return existing;
        }

        GameObject groupObject = new GameObject(groupName);
        groupObject.transform.SetParent(rootParent, false);
        parent = groupObject.transform;
        groupParents[groupName] = parent;

        if (LogCreatedGroups)
        {
            Debug.Log("Created species group parent: " + groupName);
        }

        return parent;
    }
}
