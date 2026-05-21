using System.Collections.Generic;
using UnityEngine;

public class CreatureSpeciesParentOrganizer : MonoBehaviour
{
    [Header("Organisation")]
    public bool AutoOrganiseCreatures = true;
    public bool UseManagerCreatureList = true;
    public string RootParentName = "Runtime Creature Species Groups";
    public float RefreshInterval = 0.5f;

    [Header("Debug")]
    public bool LogCreatedGroups;

    private readonly Dictionary<CreatureBehaviourType, Transform> groupParents = new Dictionary<CreatureBehaviourType, Transform>();
    private Transform rootParent;
    private float refreshTimer;

    private void Awake()
    {
        EnsureRootParent();
    }

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

            CreatureBehaviourType type = creature.DebugBehaviourType;

            if (creature.Candidate != null && creature.Candidate.Genome != null)
            {
                type = CreatureDebugTypeUtility.GetBehaviourType(creature.Candidate.Genome);
                creature.DebugBehaviourType = type;
            }

            Transform groupParent = GetOrCreateGroupParent(type);

            if (creature.transform.parent != groupParent)
            {
                creature.transform.SetParent(groupParent, true);
            }
        }
    }

    private List<MarineCreatureAgent> GetCreatures()
    {
        if (UseManagerCreatureList && EvolutionEcosystemManager.Instance != null)
        {
            return EvolutionEcosystemManager.Instance.GetActiveCreatures();
        }

        MarineCreatureAgent[] found = FindObjectsByType<MarineCreatureAgent>(FindObjectsSortMode.None);
        return new List<MarineCreatureAgent>(found);
    }

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

    private Transform GetOrCreateGroupParent(CreatureBehaviourType type)
    {
        if (groupParents.TryGetValue(type, out Transform parent) && parent != null)
        {
            return parent;
        }

        EnsureRootParent();

        string groupName = CreatureDebugTypeUtility.GetTypeName(type) + " Group";
        Transform existing = rootParent.Find(groupName);

        if (existing != null)
        {
            groupParents[type] = existing;
            return existing;
        }

        GameObject groupObject = new GameObject(groupName);
        groupObject.transform.SetParent(rootParent, false);
        parent = groupObject.transform;
        groupParents[type] = parent;

        if (LogCreatedGroups)
        {
            Debug.Log("Created species group parent: " + groupName);
        }

        return parent;
    }
}
