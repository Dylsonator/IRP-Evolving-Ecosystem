using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// Extra IRP logger for niche count, diversity, brain complexity and environment response.
// Writes the IRP CSV with diversity, niches, brain complexity and environment response.
public class EvolutionResearchMetricsRecorder : MonoBehaviour
{
    public EvolutionEcosystemManager Manager;
    public bool WriteCsv = true;
    public string CsvFileName = "IRP_EcosystemResearchMetrics_V3.csv";
    public bool LogEveryGeneration = true;
    public bool LogWhenGenerationChanges = true;
    [Tooltip("When true, the manager records completed-generation metrics before creatures are cleared. This avoids misleading zero-fitness rows during respawn.")]
    public bool PreferCompletedGenerationSnapshots = true;

    [Header("Experiment Context")]
    public string CurrentRunId = "UnlabelledRun";
    public string CurrentExperimentPhase = "Uncontrolled";

    private int lastRecordedGeneration = -1;
    private string csvPath;

    // Sets up cached references and safe starting values before the sim runs
    private void Awake()
    {
        if (Manager == null)
        {
            Manager = EvolutionEcosystemManager.Instance != null ? EvolutionEcosystemManager.Instance : FindFirstObjectByType<EvolutionEcosystemManager>();
        }

        csvPath = Path.Combine(Application.persistentDataPath, CsvFileName);
        EnsureHeader();
    }

    // Runs the normal frame checks and timers
    private void Update()
    {
        if (!WriteCsv || !LogWhenGenerationChanges || Manager == null || PreferCompletedGenerationSnapshots)
        {
            return;
        }

        if (Manager.CurrentGeneration != lastRecordedGeneration)
        {
            RecordSnapshot("GenerationChanged");
        }
    }

    // Records snapshot now.
    [ContextMenu("Record Research Snapshot")]
    // Records a manual metrics snapshot from the current population
    public void RecordSnapshotNow()
    {
        RecordSnapshot("Manual");
    }

    // Writes one research snapshot from the current manager state
    public void RecordSnapshot(string reason)
    {
        if (!WriteCsv || Manager == null)
        {
            return;
        }

        EnsureHeader();
        lastRecordedGeneration = Manager.CurrentGeneration;

        List<MarineCreatureAgent> creatures = Manager.GetActiveCreatures();
        int population = 0;
        int grazers = 0;
        int predators = 0;
        int scavengers = 0;
        int ambushers = 0;
        int schoolers = 0;
        int eggGuardians = 0;
        int defensive = 0;
        int matureCount = 0;
        int lowHealthCount = 0;
        int hungryCount = 0;
        float totalFitness = 0f;
        float totalHidden = 0f;
        float totalConnections = 0f;
        float totalPlant = 0f;
        float totalMeat = 0f;
        float totalCarrion = 0f;
        float totalSurvival = 0f;
        float totalReproduction = 0f;
        float totalPreyKills = 0f;
        float totalPreyBites = 0f;
        float totalFood = 0f;
        float totalBrainFoodBias = 0f;
        float totalBrainHuntBias = 0f;
        float totalBrainFleeBias = 0f;
        float totalBrainMateSocialBias = 0f;
        float totalBrainRestBias = 0f;
        float totalBrainSprintBias = 0f;
        float totalSurvivalEmergencyTime = 0f;
        float totalSurvivalEmergencyActivations = 0f;
        float totalEnergy = 0f;
        float totalHealthRatio = 0f;
        float totalStomach = 0f;
        HashSet<string> niches = new HashSet<string>();
        Dictionary<string, int> coreNicheCounts = new Dictionary<string, int>();
        List<EvolutionCandidate> evaluatedCandidates = new List<EvolutionCandidate>();

        for (int i = 0; i < creatures.Count; i++)
        {
            MarineCreatureAgent creature = creatures[i];
            if (creature == null || creature.Candidate == null || creature.Candidate.Genome == null)
            {
                continue;
            }

            population++;
            EvolutionCandidate candidate = creature.Candidate;
            EvolutionGenome genome = candidate.Genome;
            evaluatedCandidates.Add(candidate);
            totalFitness += candidate.GetFitness();
            totalPlant += genome.PlantDiet;
            totalMeat += genome.MeatDiet;
            totalCarrion += genome.CarrionDiet;
            totalSurvival += candidate.SurvivalTime;
            totalReproduction += candidate.ReproductionCount + candidate.EggsLaid + candidate.EggsHatched;
            totalPreyKills += candidate.PreyKills;
            totalPreyBites += candidate.PreyBites;
            totalFood += candidate.FoodEaten + candidate.CarrionEaten;
            totalBrainFoodBias += candidate.AverageBrainFoodBias;
            totalBrainHuntBias += candidate.AverageBrainHuntBias;
            totalBrainFleeBias += candidate.AverageBrainFleeBias;
            totalBrainMateSocialBias += candidate.AverageBrainMateSocialBias;
            totalBrainRestBias += candidate.AverageBrainRestBias;
            totalBrainSprintBias += candidate.AverageBrainSprintBias;
            totalSurvivalEmergencyTime += candidate.SurvivalEmergencyTime;
            totalSurvivalEmergencyActivations += candidate.SurvivalEmergencyActivations;
            totalEnergy += creature.GetEffectiveEnergyRatio();
            totalHealthRatio += creature.GetHealthRatio();
            totalStomach += creature.GetStomachFullness01();

            if (creature.IsMatureForMating()) matureCount++;
            if (creature.GetHealthRatio() < 0.35f) lowHealthCount++;
            if (creature.GetStomachFullness01() < 0.30f) hungryCount++;

            if (genome.Brain != null)
            {
                totalHidden += genome.Brain.HiddenCount;
                totalConnections += genome.Brain.GetConnectionCount();
            }

            string niche = EvolutionNicheUtility.BuildNicheKey(candidate);
            niches.Add(niche);
            string coreNiche = EvolutionNicheUtility.BuildCoreNicheKey(candidate);
            if (!coreNicheCounts.ContainsKey(coreNiche))
            {
                coreNicheCounts[coreNiche] = 0;
            }
            coreNicheCounts[coreNiche]++;

            CreatureBehaviourType type = CreatureDebugTypeUtility.GetBehaviourType(genome);
            if (type == CreatureBehaviourType.Grazer || type == CreatureBehaviourType.ArmouredGrazer || type == CreatureBehaviourType.DefensiveHerbivore) grazers++;
            if (type == CreatureBehaviourType.Predator || type == CreatureBehaviourType.StreamlinedHunter) predators++;
            if (type == CreatureBehaviourType.Scavenger || type == CreatureBehaviourType.SensorScavenger) scavengers++;
            if (EvolutionNicheUtility.IsLikelyAmbusher(genome)) ambushers++;
            if (genome.GroupingChance >= 0.55f || type == CreatureBehaviourType.Schooling) schoolers++;
            if (genome.EggProtection >= 0.55f && genome.NestingDrive >= 0.45f) eggGuardians++;
            if (genome.DangerFactor >= 0.75f || genome.Armour >= 0.55f || genome.SpikeSize >= 0.8f) defensive++;
        }

        int dominantCoreNicheCount = 0;
        string dominantCoreNiche = "None";
        foreach (KeyValuePair<string, int> pair in coreNicheCounts)
        {
            if (pair.Value > dominantCoreNicheCount)
            {
                dominantCoreNiche = pair.Key;
                dominantCoreNicheCount = pair.Value;
            }
        }

        float inv = population > 0 ? 1f / population : 0f;
        float dominantCoreFraction = population > 0 ? dominantCoreNicheCount / (float)population : 0f;
        float shannon = CalculateShannon(coreNicheCounts, population);
        float maxShannon = coreNicheCounts.Count > 1 ? Mathf.Log(coreNicheCounts.Count) : 0f;
        float evenness = maxShannon > 0f ? Mathf.Clamp01(shannon / maxShannon) : 0f;
        float populationStability = Manager.FixedPopulationSize > 0 ? Mathf.Clamp01(population / (float)Manager.FixedPopulationSize) : 0f;
        float reproductionRate = population > 0 ? (Manager.GetOffspringPool().Count + totalReproduction) / Mathf.Max(1f, population) : 0f;
        float predatorFraction = population > 0 ? predators / (float)population : 0f;
        float naturalFeatureSpread = EvolutionBehaviourDescriptorUtility.CalculateFeatureSpread(evaluatedCandidates);
        EvolutionDiversityArchive diversityArchive = Manager.GetComponent<EvolutionDiversityArchive>();
        float noveltyArchiveSize = diversityArchive != null ? diversityArchive.Count : 0f;
        float noveltyArchiveSpread = diversityArchive != null ? diversityArchive.GetArchiveSpread() : 0f;
        float pressureZoneCount = Manager.GetActivePressureZones() != null ? Manager.GetActivePressureZones().Count : 0;
        float activeFoodCount = Manager.GetActiveFood() != null ? Manager.GetActiveFood().Count : 0;
        float activeCarrionCount = Manager.GetActiveCarrion() != null ? Manager.GetActiveCarrion().Count : 0;
        float eggCount = Manager.GetActiveEggClusters() != null ? Manager.GetActiveEggClusters().Count : 0;
        float adaptationScore = CalculateAdaptationScore(populationStability, evenness, dominantCoreFraction, reproductionRate, totalHealthRatio * inv, totalStomach * inv);

        SeasonalEnvironment env = Manager.Environment;
        string season = env != null ? env.CurrentSeason.ToString() : "None";
        float foodMultiplier = env != null ? env.FoodSpawnMultiplier : 1f;
        float drainMultiplier = env != null ? env.EnergyDrainMultiplier : 1f;
        float mutationMultiplier = env != null ? env.MutationMultiplier : 1f;

        StringBuilder line = new StringBuilder();
        line.Append(Safe(CurrentRunId)).Append(',');
        line.Append(Safe(CurrentExperimentPhase)).Append(',');
        line.Append(Manager.CurrentGeneration).Append(',');
        line.Append(Manager.GenerationTimer.ToString("F2")).Append(',');
        line.Append(population).Append(',');
        line.Append(Manager.GetOffspringPool().Count).Append(',');
        line.Append(activeFoodCount.ToString("F0")).Append(',');
        line.Append(activeCarrionCount.ToString("F0")).Append(',');
        line.Append(eggCount.ToString("F0")).Append(',');
        line.Append(pressureZoneCount.ToString("F0")).Append(',');
        line.Append(Safe(season)).Append(',');
        line.Append(foodMultiplier.ToString("F3")).Append(',');
        line.Append(drainMultiplier.ToString("F3")).Append(',');
        line.Append(mutationMultiplier.ToString("F3")).Append(',');
        line.Append(niches.Count).Append(',');
        line.Append(coreNicheCounts.Count).Append(',');
        line.Append(Safe(dominantCoreNiche)).Append(',');
        line.Append(dominantCoreFraction.ToString("F3")).Append(',');
        line.Append(shannon.ToString("F3")).Append(',');
        line.Append(evenness.ToString("F3")).Append(',');
        line.Append(populationStability.ToString("F3")).Append(',');
        line.Append(reproductionRate.ToString("F3")).Append(',');
        line.Append(adaptationScore.ToString("F3")).Append(',');
        line.Append((totalFitness * inv).ToString("F3")).Append(',');
        line.Append((totalSurvival * inv).ToString("F3")).Append(',');
        line.Append((totalHidden * inv).ToString("F3")).Append(',');
        line.Append((totalConnections * inv).ToString("F3")).Append(',');
        line.Append(naturalFeatureSpread.ToString("F3")).Append(',');
        line.Append(noveltyArchiveSize.ToString("F0")).Append(',');
        line.Append(noveltyArchiveSpread.ToString("F3")).Append(',');
        line.Append((totalPlant * inv).ToString("F3")).Append(',');
        line.Append((totalMeat * inv).ToString("F3")).Append(',');
        line.Append((totalCarrion * inv).ToString("F3")).Append(',');
        line.Append((totalEnergy * inv).ToString("F3")).Append(',');
        line.Append((totalHealthRatio * inv).ToString("F3")).Append(',');
        line.Append((totalStomach * inv).ToString("F3")).Append(',');
        line.Append((totalPreyBites * inv).ToString("F3")).Append(',');
        line.Append((totalPreyKills * inv).ToString("F3")).Append(',');
        line.Append((totalFood * inv).ToString("F3")).Append(',');
        line.Append(grazers).Append(',');
        line.Append(predators).Append(',');
        line.Append(scavengers).Append(',');
        line.Append(ambushers).Append(',');
        line.Append(schoolers).Append(',');
        line.Append(eggGuardians).Append(',');
        line.Append(defensive).Append(',');
        line.Append(matureCount).Append(',');
        line.Append(lowHealthCount).Append(',');
        line.Append(hungryCount).Append(',');
        line.Append(predatorFraction.ToString("F3")).Append(',');
        line.Append((totalBrainFoodBias * inv).ToString("F3")).Append(',');
        line.Append((totalBrainHuntBias * inv).ToString("F3")).Append(',');
        line.Append((totalBrainFleeBias * inv).ToString("F3")).Append(',');
        line.Append((totalBrainMateSocialBias * inv).ToString("F3")).Append(',');
        line.Append((totalBrainRestBias * inv).ToString("F3")).Append(',');
        line.Append((totalBrainSprintBias * inv).ToString("F3")).Append(',');
        line.Append((totalSurvivalEmergencyTime * inv).ToString("F3")).Append(',');
        line.Append((totalSurvivalEmergencyActivations * inv).ToString("F3")).Append(',');
        line.Append(Safe(reason));

        File.AppendAllText(csvPath, line.ToString() + "\n");
    }


    // Writes metrics using completed generation data before respawn clears it
    public void RecordCompletedGenerationSnapshot(string reason, int generation, List<EvolutionCandidate> evaluatedCandidates, int populationAtEnd, int offspringAtEnd)
    {
        if (!WriteCsv || Manager == null || evaluatedCandidates == null || evaluatedCandidates.Count == 0)
        {
            return;
        }

        EnsureHeader();
        lastRecordedGeneration = generation;

        int evaluatedCount = 0;
        int grazers = 0;
        int predators = 0;
        int scavengers = 0;
        int ambushers = 0;
        int schoolers = 0;
        int eggGuardians = 0;
        int defensive = 0;
        int matureCount = 0;
        int lowHealthCount = 0;
        int hungryCount = 0;
        float totalFitness = 0f;
        float totalHidden = 0f;
        float totalConnections = 0f;
        float totalPlant = 0f;
        float totalMeat = 0f;
        float totalCarrion = 0f;
        float totalSurvival = 0f;
        float totalReproduction = 0f;
        float totalPreyKills = 0f;
        float totalPreyBites = 0f;
        float totalFood = 0f;
        float totalBrainFoodBias = 0f;
        float totalBrainHuntBias = 0f;
        float totalBrainFleeBias = 0f;
        float totalBrainMateSocialBias = 0f;
        float totalBrainRestBias = 0f;
        float totalBrainSprintBias = 0f;
        float totalSurvivalEmergencyTime = 0f;
        float totalSurvivalEmergencyActivations = 0f;
        float totalEnergy = 0f;
        float totalHealthRatio = 0f;
        float totalStomach = 0f;
        HashSet<string> niches = new HashSet<string>();
        Dictionary<string, int> coreNicheCounts = new Dictionary<string, int>();
        List<EvolutionCandidate> validCandidates = new List<EvolutionCandidate>();

        for (int i = 0; i < evaluatedCandidates.Count; i++)
        {
            EvolutionCandidate candidate = evaluatedCandidates[i];
            if (candidate == null || candidate.Genome == null)
            {
                continue;
            }

            evaluatedCount++;
            validCandidates.Add(candidate);
            EvolutionGenome genome = candidate.Genome;
            totalFitness += candidate.GetFitness();
            totalPlant += genome.PlantDiet;
            totalMeat += genome.MeatDiet;
            totalCarrion += genome.CarrionDiet;
            totalSurvival += candidate.SurvivalTime;
            totalReproduction += candidate.ReproductionCount + candidate.EggsLaid + candidate.EggsHatched;
            totalPreyKills += candidate.PreyKills;
            totalPreyBites += candidate.PreyBites;
            totalFood += candidate.FoodEaten + candidate.CarrionEaten;
            totalBrainFoodBias += candidate.AverageBrainFoodBias;
            totalBrainHuntBias += candidate.AverageBrainHuntBias;
            totalBrainFleeBias += candidate.AverageBrainFleeBias;
            totalBrainMateSocialBias += candidate.AverageBrainMateSocialBias;
            totalBrainRestBias += candidate.AverageBrainRestBias;
            totalBrainSprintBias += candidate.AverageBrainSprintBias;
            totalSurvivalEmergencyTime += candidate.SurvivalEmergencyTime;
            totalSurvivalEmergencyActivations += candidate.SurvivalEmergencyActivations;

            float energyCapacity = Mathf.Max(1f, genome.EnergyCapacity);
            totalEnergy += Mathf.Clamp01(candidate.FinalEnergy / energyCapacity);
            float estimatedMaxHealth = Mathf.Max(1f, 100f * Mathf.Max(0.35f, genome.BodySize));
            float healthRatio = Mathf.Clamp01(candidate.FinalHealth / estimatedMaxHealth);
            totalHealthRatio += healthRatio;
            totalStomach += Mathf.Clamp01(candidate.FinalStomachFullness);

            if (candidate.SurvivalTime >= 55f) matureCount++;
            if (healthRatio < 0.35f) lowHealthCount++;
            if (candidate.FinalStomachFullness < 0.30f) hungryCount++;

            if (genome.Brain != null)
            {
                totalHidden += genome.Brain.HiddenCount;
                totalConnections += genome.Brain.GetConnectionCount();
            }

            string niche = EvolutionNicheUtility.BuildNicheKey(candidate);
            niches.Add(niche);
            string coreNiche = EvolutionNicheUtility.BuildCoreNicheKey(candidate);
            if (!coreNicheCounts.ContainsKey(coreNiche))
            {
                coreNicheCounts[coreNiche] = 0;
            }
            coreNicheCounts[coreNiche]++;

            CreatureBehaviourType type = CreatureDebugTypeUtility.GetBehaviourType(genome);
            if (type == CreatureBehaviourType.Grazer || type == CreatureBehaviourType.ArmouredGrazer || type == CreatureBehaviourType.DefensiveHerbivore) grazers++;
            if (type == CreatureBehaviourType.Predator || type == CreatureBehaviourType.StreamlinedHunter) predators++;
            if (type == CreatureBehaviourType.Scavenger || type == CreatureBehaviourType.SensorScavenger) scavengers++;
            if (EvolutionNicheUtility.IsLikelyAmbusher(genome)) ambushers++;
            if (genome.GroupingChance >= 0.55f || type == CreatureBehaviourType.Schooling) schoolers++;
            if (genome.EggProtection >= 0.55f && genome.NestingDrive >= 0.45f) eggGuardians++;
            if (genome.DangerFactor >= 0.75f || genome.Armour >= 0.55f || genome.SpikeSize >= 0.8f) defensive++;
        }

        if (evaluatedCount <= 0)
        {
            return;
        }

        int dominantCoreNicheCount = 0;
        string dominantCoreNiche = "None";
        foreach (KeyValuePair<string, int> pair in coreNicheCounts)
        {
            if (pair.Value > dominantCoreNicheCount)
            {
                dominantCoreNiche = pair.Key;
                dominantCoreNicheCount = pair.Value;
            }
        }

        float inv = 1f / evaluatedCount;
        float dominantCoreFraction = dominantCoreNicheCount / (float)evaluatedCount;
        float shannon = CalculateShannon(coreNicheCounts, evaluatedCount);
        float maxShannon = coreNicheCounts.Count > 1 ? Mathf.Log(coreNicheCounts.Count) : 0f;
        float evenness = maxShannon > 0f ? Mathf.Clamp01(shannon / maxShannon) : 0f;
        float populationStability = Manager.FixedPopulationSize > 0 ? Mathf.Clamp01(populationAtEnd / (float)Manager.FixedPopulationSize) : 0f;
        float reproductionRate = (offspringAtEnd + totalReproduction) / Mathf.Max(1f, evaluatedCount);
        float predatorFraction = predators / (float)evaluatedCount;
        float naturalFeatureSpread = EvolutionBehaviourDescriptorUtility.CalculateFeatureSpread(validCandidates);
        EvolutionDiversityArchive diversityArchive = Manager.GetComponent<EvolutionDiversityArchive>();
        float noveltyArchiveSize = diversityArchive != null ? diversityArchive.Count : 0f;
        float noveltyArchiveSpread = diversityArchive != null ? diversityArchive.GetArchiveSpread() : 0f;
        float pressureZoneCount = Manager.GetActivePressureZones() != null ? Manager.GetActivePressureZones().Count : 0;
        float activeFoodCount = Manager.GetActiveFood() != null ? Manager.GetActiveFood().Count : 0;
        float activeCarrionCount = Manager.GetActiveCarrion() != null ? Manager.GetActiveCarrion().Count : 0;
        float eggCount = Manager.GetActiveEggClusters() != null ? Manager.GetActiveEggClusters().Count : 0;
        float adaptationScore = CalculateAdaptationScore(populationStability, evenness, dominantCoreFraction, reproductionRate, totalHealthRatio * inv, totalStomach * inv);

        SeasonalEnvironment env = Manager.Environment;
        string season = env != null ? env.CurrentSeason.ToString() : "None";
        float foodMultiplier = env != null ? env.FoodSpawnMultiplier : 1f;
        float drainMultiplier = env != null ? env.EnergyDrainMultiplier : 1f;
        float mutationMultiplier = env != null ? env.MutationMultiplier : 1f;

        StringBuilder line = new StringBuilder();
        line.Append(Safe(CurrentRunId)).Append(',');
        line.Append(Safe(CurrentExperimentPhase)).Append(',');
        line.Append(generation).Append(',');
        line.Append(Manager.GenerationTimer.ToString("F2")).Append(',');
        line.Append(populationAtEnd).Append(',');
        line.Append(offspringAtEnd).Append(',');
        line.Append(activeFoodCount.ToString("F0")).Append(',');
        line.Append(activeCarrionCount.ToString("F0")).Append(',');
        line.Append(eggCount.ToString("F0")).Append(',');
        line.Append(pressureZoneCount.ToString("F0")).Append(',');
        line.Append(Safe(season)).Append(',');
        line.Append(foodMultiplier.ToString("F3")).Append(',');
        line.Append(drainMultiplier.ToString("F3")).Append(',');
        line.Append(mutationMultiplier.ToString("F3")).Append(',');
        line.Append(niches.Count).Append(',');
        line.Append(coreNicheCounts.Count).Append(',');
        line.Append(Safe(dominantCoreNiche)).Append(',');
        line.Append(dominantCoreFraction.ToString("F3")).Append(',');
        line.Append(shannon.ToString("F3")).Append(',');
        line.Append(evenness.ToString("F3")).Append(',');
        line.Append(populationStability.ToString("F3")).Append(',');
        line.Append(reproductionRate.ToString("F3")).Append(',');
        line.Append(adaptationScore.ToString("F3")).Append(',');
        line.Append((totalFitness * inv).ToString("F3")).Append(',');
        line.Append((totalSurvival * inv).ToString("F3")).Append(',');
        line.Append((totalHidden * inv).ToString("F3")).Append(',');
        line.Append((totalConnections * inv).ToString("F3")).Append(',');
        line.Append(naturalFeatureSpread.ToString("F3")).Append(',');
        line.Append(noveltyArchiveSize.ToString("F0")).Append(',');
        line.Append(noveltyArchiveSpread.ToString("F3")).Append(',');
        line.Append((totalPlant * inv).ToString("F3")).Append(',');
        line.Append((totalMeat * inv).ToString("F3")).Append(',');
        line.Append((totalCarrion * inv).ToString("F3")).Append(',');
        line.Append((totalEnergy * inv).ToString("F3")).Append(',');
        line.Append((totalHealthRatio * inv).ToString("F3")).Append(',');
        line.Append((totalStomach * inv).ToString("F3")).Append(',');
        line.Append((totalPreyBites * inv).ToString("F3")).Append(',');
        line.Append((totalPreyKills * inv).ToString("F3")).Append(',');
        line.Append((totalFood * inv).ToString("F3")).Append(',');
        line.Append(grazers).Append(',');
        line.Append(predators).Append(',');
        line.Append(scavengers).Append(',');
        line.Append(ambushers).Append(',');
        line.Append(schoolers).Append(',');
        line.Append(eggGuardians).Append(',');
        line.Append(defensive).Append(',');
        line.Append(matureCount).Append(',');
        line.Append(lowHealthCount).Append(',');
        line.Append(hungryCount).Append(',');
        line.Append(predatorFraction.ToString("F3")).Append(',');
        line.Append((totalBrainFoodBias * inv).ToString("F3")).Append(',');
        line.Append((totalBrainHuntBias * inv).ToString("F3")).Append(',');
        line.Append((totalBrainFleeBias * inv).ToString("F3")).Append(',');
        line.Append((totalBrainMateSocialBias * inv).ToString("F3")).Append(',');
        line.Append((totalBrainRestBias * inv).ToString("F3")).Append(',');
        line.Append((totalBrainSprintBias * inv).ToString("F3")).Append(',');
        line.Append((totalSurvivalEmergencyTime * inv).ToString("F3")).Append(',');
        line.Append((totalSurvivalEmergencyActivations * inv).ToString("F3")).Append(',');
        line.Append(Safe(reason));

        File.AppendAllText(csvPath, line.ToString() + "\n");
    }

    // Calculates Shannon diversity from niche counts
    private float CalculateShannon(Dictionary<string, int> counts, int population)
    {
        if (counts == null || counts.Count == 0 || population <= 0)
        {
            return 0f;
        }

        float shannon = 0f;
        foreach (KeyValuePair<string, int> pair in counts)
        {
            float p = pair.Value / (float)population;
            if (p > 0f)
            {
                shannon -= p * Mathf.Log(p);
            }
        }
        return shannon;
    }

    // Builds a simple score for how well the population is handling pressure
    private float CalculateAdaptationScore(float populationStability, float evenness, float dominantFraction, float reproductionRate, float healthRatio, float stomachRatio)
    {
        float diversityTerm = evenness * 0.30f + (1f - Mathf.Clamp01(dominantFraction)) * 0.20f;
        float stabilityTerm = populationStability * 0.22f;
        float reproductionTerm = Mathf.Clamp01(reproductionRate / 2.5f) * 0.16f;
        float conditionTerm = Mathf.Clamp01((healthRatio + stomachRatio) * 0.5f) * 0.12f;
        return Mathf.Clamp01(diversityTerm + stabilityTerm + reproductionTerm + conditionTerm);
    }

    // Creates the CSV header if the file is new
    private void EnsureHeader()
    {
        if (!WriteCsv || string.IsNullOrEmpty(csvPath) || File.Exists(csvPath))
        {
            return;
        }

        File.WriteAllText(csvPath,
            "RunId,ExperimentPhase,Generation,GenerationTimer,Population,OffspringPool,ActiveFood,ActiveCarrion,EggClusters,PressureZones,Season,FoodMultiplier,EnergyDrainMultiplier,MutationMultiplier,NicheCount,CoreNicheCount,DominantCoreNiche,DominantCoreFraction,CoreNicheShannon,CoreNicheEvenness,PopulationStability,ReproductionRate,AdaptationScore,AverageFitness,AverageSurvival,AverageBrainHidden,AverageBrainConnections,NaturalFeatureSpread,NoveltyArchiveSize,NoveltyArchiveSpread,AveragePlantDiet,AverageMeatDiet,AverageCarrionDiet,AverageEnergyRatio,AverageHealthRatio,AverageStomachRatio,AveragePreyBites,AveragePreyKills,AverageFoodEvents,Grazers,Predators,Scavengers,Ambushers,Schoolers,EggGuardians,Defensive,MatureCount,LowHealthCount,HungryCount,PredatorFraction,AverageBrainFoodBias,AverageBrainHuntBias,AverageBrainFleeBias,AverageBrainMateSocialBias,AverageBrainRestBias,AverageBrainSprintBias,AverageSurvivalEmergencyTime,AverageSurvivalEmergencyActivations,Reason\n");
    }

    // Cleans text so commas and nulls do not break CSV output
    private string Safe(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Replace(',', ';').Replace('\n', ' ').Replace('\r', ' ');
    }
}
