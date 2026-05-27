using System.Collections.Generic;
using UnityEngine;

// Main fish script. Most of the creature behaviour runs through here.
public enum FishAutonomousBehaviourMode
{
    Resting,
    Sleeping,
    Exploring,
    Schooling,
    Foraging,
    Feeding,
    FollowingLeader,
    SeekingMate,
    Courtship,
    Nesting,
    GuardingEggs,
    Hunting,
    Ambushing,
    MobbingPredator,
    Fleeing,
    Recovering
}

[RequireComponent(typeof(Rigidbody))]
public class MarineCreatureAgent : MonoBehaviour
{
    private static int labelFrame = -1;
    private static int labelsDrawnThisFrame;
    private static readonly Collider[] predatorBiteOverlapBuffer = new Collider[40];

    [Header("Runtime")]
    public EvolutionCandidate Candidate;
    public CreatureEffectiveStats EffectiveStats;
    public CreatureMorphLibrary MorphLibrary;
    public float CurrentEnergy;
    public float CurrentHealth;
    public float StomachPlant;
    public float StomachMeat;
    public float StomachCarrion;
    public float AgeSeconds;
    public bool IsJuvenile;
    public string DebugName;
    public CreatureBehaviourType DebugBehaviourType;

    // Extra vision values so fish notice danger before it is too late.
    [Header("Awareness / Runtime Vision")]
    [Tooltip("Runtime multiplier applied to evolved vision. This helps fish see threats, food and terrain earlier without changing saved genomes.")]
    public float RuntimeVisionRangeMultiplier = 1.45f;
    public float MinimumRuntimeVisionRange = 24f;
    public float FleeThreatSenseMultiplier = 1.35f;

    // Base survival values before genes/morph parts change them.
    [Header("Energy / Survival")]
    public float BaseEnergyDrainPerSecond = 0.45f;
    public float ReproductionCooldown = 9f;
    public float LowEnergyRatio = 0.35f;
    public float BaseHealth = 100f;
    public float StarvationHealthDamagePerSecond = 2.4f;
    public float HealthRecoveryPerSecond = 2.2f;
    public float BaseStomachCapacity = 64f;
    public float BaseDigestionPerSecond = 4.9f;
    public float BaseBiteMass = 4.2f;
    public float FullStomachSlowdown = 0.18f;

    [Header("Ecology Balance Safeguards")]
    [Tooltip("Keeps old inspector values from causing starvation-heavy simulations after the ecosystem systems were added.")]
    public bool UseEcologyBalanceSafeguards = true;
    public float PlantEnergyGainMultiplier = 2.15f;
    public float MeatEnergyGainMultiplier = 2.35f;
    public float CarrionEnergyGainMultiplier = 1.95f;
    public float EnergyRecoveredPerStoredStomachMass = 0.28f;

    // Healing only happens when the fish actually gets a safe rest.
    [Header("Rest / Healing")]
    [Tooltip("When true, fish only heal while resting or sleeping. They no longer regenerate while being chased or bitten.")]
    public bool HealOnlyWhileRestingOrSleeping = true;
    [Tooltip("Seconds after taking damage before healing can begin again.")]
    public float HealDelayAfterDamage = 5.0f;
    [Tooltip("Resting heals slowly. Sleeping heals much faster.")]
    public float RestingHealMultiplier = 0.35f;
    public float SleepingHealMultiplier = 2.4f;
    public float SleepHealthRatioThreshold = 0.78f;
    public float SleepMinimumEnergyRatio = 0.32f;
    public float SleepMinimumStomachRatio = 0.18f;

    // Short burst speed for chasing and fleeing, but it costs extra energy.
    [Header("Sprint / Burst Swimming")]
    public bool UseSprintBursts = true;
    [Tooltip("Sprint multiplies speed, but energy drain is also multiplied. This creates proper chase/flee bursts instead of free speed.")]
    public float SprintSpeedMultiplier = 1.55f;
    public float SprintEnergyCostMultiplier = 2.0f;
    public float SprintMinimumEnergyRatio = 0.18f;
    public float SprintMinimumStomachRatio = 0.08f;
    public float SprintChaseDistance = 8.0f;
    public float SprintFleeHealthRatio = 0.72f;
    public float SprintCooldownAfterExhaustion = 1.4f;

    // If the fish is close to dying, survival takes over normal behaviour.
    [Header("Survival Priority")]
    [Tooltip("When health is critical, survival overrides mating, feeding, hunting and social behaviour.")]
    public bool UseSurvivalPriority = true;
    [Range(0f, 1f)] public float SurvivalCriticalHealthRatio = 0.24f;
    [Range(0f, 1f)] public float SurvivalWarningHealthRatio = 0.42f;
    [Range(0f, 1f)] public float SurvivalCriticalEnergyRatio = 0.12f;
    [Range(0f, 1f)] public float SurvivalCriticalStomachRatio = 0.08f;
    public float SurvivalFleeThreatRadius = 13f;
    public float SurvivalFleeWeight = 9.5f;
    public float SurvivalModeLockTime = 2.6f;
    public float SurvivalRecentDamagePanicTime = 4.0f;
    public bool SurvivalCanOverrideBrainConservation = true;
    public bool SurvivalSuppressNonEssentialBehaviour = true;

    [Header("Urgent Survival Tuning")]
    public bool UseUrgentSurvivalPriority = true;
    [Range(0f, 1f)] public float InjuredSurvivalHealthRatio = 0.50f;
    [Range(0f, 1f)] public float CriticalStarvationEnergyRatio = 0.16f;
    [Range(0f, 1f)] public float CriticalStarvationStomachRatio = 0.08f;
    public float SurvivalFleeDesireBoost = 1.4f;
    public float SurvivalFoodDesireBoost = 1.15f;
    public float SurvivalRestDesireBoost = 0.9f;
    public float SafeRestSearchRadius = 12f;
    public float SafeRestMemoryTime = 16f;
    public float SafeRestThreatCheckRadius = 11f;

    [Header("Group Defence / Counter-Attack")]
    public bool EnableGroupDefenceCounterAttacks = true;
    public float GroupCounterSupportRadius = 8f;
    public int GroupCounterMinimumAllies = 2;
    public float GroupCounterDamageMultiplier = 0.32f;
    public float GroupCounterMaxDamage = 9f;
    public float GroupCounterMobbingPressureTime = 4.5f;
    public float PredatorTypeAvoidanceTime = 70f;
    public float PredatorTypeThreatBonus = 0.38f;

    [Header("Predator Bite Reliability")]
    public bool UsePredatorOverlapBiteCheck = true;
    public float PredatorBiteScanRadiusMultiplier = 1.85f;
    public float PredatorContactBiteBodyPadding = 0.72f;
    public float PredatorContactBiteMouthPadding = 0.45f;
    [Tooltip("Predators gain some meat from every successful bite, not only from final kills. This stops hunters starving before they can finish prey.")]
    public float PredatorBiteMeatFromDamageMultiplier = 0.26f;
    [Tooltip("Extra meat chunk taken on successful predator bites. This makes hunting viable without needing dozens of bites.")]
    public float PredatorBiteBaseMeatReward = 1.45f;
    [Tooltip("Bite damage multiplier for committed hunters so prey cannot simply out-heal repeated attacks.")]
    public float PredatorCommittedBiteDamageMultiplier = 1.18f;
    [Tooltip("Predators below this energy ratio are willing to attack more dangerous prey instead of giving up.")]
    [Range(0f, 1f)] public float HungryPredatorCommitEnergyRatio = 0.42f;
    [Tooltip("Strong meat/aggression hunters can attack similar morphs rather than refusing most of the population.")]
    [Range(0f, 1f)] public float CommittedPredatorMorphAttackThreshold = 0.52f;
    [Tooltip("After seeing or biting prey, hunters keep focus instead of instantly switching back to plants.")]
    public float HunterPreyFocusTime = 6.5f;
    [Tooltip("Successful bites create stronger focus so predators finish weak/injured prey instead of circling away.")]
    public float HunterBiteFocusTime = 10.0f;
    [Tooltip("Predators above this energy/stomach level stop suppressing plants and may opportunistically feed again.")]
    [Range(0f, 1f)] public float HunterPlantSuppressionStopRatio = 0.82f;
    [Tooltip("Extra chase pull while locked onto wounded prey.")]
    public float HunterStickyChaseWeight = 5.2f;
    [Tooltip("Damage scaling from predator/prey size difference. Large predators kill small prey quickly, small predators need repeated bites.")]
    public float PredatorSizeDamageScale = 0.62f;
    [Tooltip("Plant-led grazers below this meat value are treated as peaceful feeders and should not keep hunting schoolmates.")]
    [Range(0f, 1f)] public float GrazerHuntBlockMeatThreshold = 0.55f;
    [Tooltip("A creature needs at least this meat value or a clearly meat-dominant diet to behave as an ecological predator.")]
    [Range(0f, 1f)] public float EcologicalPredatorMeatThreshold = 0.52f;
    [Tooltip("Predators above these reserves stop chaining hunts and return to resting/schooling/exploring until hungry again.")]
    [Range(0f, 1f)] public float HunterChillEnergyRatio = 0.66f;
    [Range(0f, 1f)] public float HunterChillStomachRatio = 0.42f;

    [Header("Predator Diet Priority")]
    [Tooltip("Meat-biased predator bodies above this meat value refuse plants unless in absolute emergency.")]
    [Range(0f, 1f)] public float PredatorPlantBlockMeatThreshold = 0.42f;
    [Tooltip("Only below this energy can a predator use plants as a last-resort emergency fallback.")]
    [Range(0f, 1f)] public float PredatorPlantEmergencyEnergyRatio = 0.18f;
    [Tooltip("Only below this stomach ratio can a predator use plants as a last-resort emergency fallback.")]
    [Range(0f, 1f)] public float PredatorPlantEmergencyStomachRatio = 0.09f;
    [Tooltip("When true, predator-like fish prioritise carrion/prey and clear plant targets instead of grazing between hunts.")]
    public bool StrictPredatorMeatPriority = true;

    [Header("Predator Realism / Wounds")]
    public bool UseRealisticInjuryMovement = true;
    [Tooltip("Health ratio where wounds first start to affect swimming. Recent bites also apply a small temporary injury even above this value.")]
    [Range(0f, 1f)] public float InjuryMovementStartHealthRatio = 0.72f;
    [Range(0f, 1f)] public float InjuryMaxSpeedLoss = 0.24f;
    [Range(0f, 1f)] public float InjuryMaxTurnLoss = 0.20f;
    [Range(0f, 1f)] public float InjuryMaxAccelerationLoss = 0.18f;
    [Tooltip("Extra sprint energy multiplier from wounds. Injured prey can still burst, but it is expensive.")]
    public float InjurySprintEnergyExtraMultiplier = 0.22f;
    [Tooltip("How long a bitten fish leaves a useful wounded scent/memory for predators.")]
    public float WoundedScentDuration = 9f;
    public float WoundedScentPredatorBonus = 0.58f;

    public bool UsePredatorHuntCommitment = true;
    [Tooltip("Shark-like predators commit to a chase briefly, then give up if they cannot land a bite.")]
    public float PredatorFailedChaseGiveUpTime = 4.8f;
    public float PredatorExhaustionAfterFailedChase = 4.2f;
    public float PredatorExhaustionAfterEnoughBites = 5.5f;
    [Range(0f, 1f)] public float PredatorSatisfiedAfterBiteEnergyRatio = 0.58f;
    [Range(0f, 1f)] public float PredatorSatisfiedAfterBiteStomachRatio = 0.35f;
    public float PredatorCloseChaseDistance = 5.5f;
    public float PredatorWoundedPreyCommitBonus = 1.03f;
    public float SharkLikeStrikeDamageMultiplier = 1.04f;

    [Tooltip("Cached group support still discourages attacks in emergency performance mode without doing an expensive fresh scan.")]
    public float CachedGroupDefenceThreatWeight = 0.32f;

    [Header("Predator Kill Feeding")]
    [Tooltip("When a predator kills prey, it claims the fresh carrion and keeps eating it instead of instantly switching to plants.")]
    public bool PrioritiseFreshKills = true;
    public float FreshKillClaimRadius = 8.0f;
    public float FreshKillPriorityTime = 42f;
    [Range(0f, 1f)] public float FreshKillStopEnergyRatio = 0.98f;
    [Range(0f, 1f)] public float FreshKillStopStomachRatio = 0.96f;
    [Tooltip("How strongly a claimed kill overrides normal plant/carrion target choice.")]
    public float FreshKillFeedingBias = 4.0f;

    [Header("Current Escape Steering")]
    public bool UseCurrentEscapeSteering = true;
    [Tooltip("Current/pressure stress where fish start treating the area as dangerous.")]
    public float CurrentEscapeStressThreshold = 0.34f;
    public float CurrentEscapeFlowThreshold = 1.15f;
    public float CurrentEscapeWeight = 6.4f;
    public float CurrentEscapeMemoryTime = 11.0f;
    public float CurrentEscapeCentreBias = 1.25f;
    public float CurrentEscapeSideBias = 1.15f;
    public float CurrentEscapeAgainstFlowBias = 0.12f;
    public float CurrentEscapeBoundaryBoost = 1.75f;
    public float CurrentEscapeStuckBoost = 1.45f;
    [Range(0f, 1f)] public float CurrentEscapeCurrentDriftMultiplier = 0.18f;
    public float CurrentEscapeSprintStress = 0.48f;

    [Header("Flee Terrain Safety")]
    public bool UseFleeTerrainSafety = true;
    [Tooltip("Extra terrain avoidance while fleeing/recovering so panicking fish do not swim straight into walls.")]
    public float FleeTerrainAvoidanceMultiplier = 3.75f;
    [Tooltip("Extra terrain look-ahead while fleeing/current-escaping. Panic steering needs earlier wall detection than normal cruising.")]
    public float FleeTerrainLookAheadMultiplier = 2.25f;
    public float MinimumFleeTerrainLookAhead = 9.0f;
    [Tooltip("When threatened, fish ignore static food unless they are critically starving. Stops prey freezing on food while being chased.")]
    public bool IgnoreStaticFoodWhileThreatened = true;
    [Tooltip("Threat memory duration used to stop fish re-locking onto food immediately after spotting a predator.")]
    public float ThreatFoodSuppressionMemoryTime = 3.0f;
    [Tooltip("Maximum downward velocity while fleeing near the floor. Prevents panic dives into terrain.")]
    public float FleeFloorMaxDownVelocity = 0.05f;
    [Tooltip("Maximum upward velocity as a ratio of speed when a fleeing fish is pressing into terrain. Stops wall-climbing launch bugs.")]
    public float FleeWallVerticalVelocityLimit = 0.38f;
    public float FleeWallSideStepWeight = 3.2f;
    public float FleeWallBackOffWeight = 4.8f;
    [Tooltip("When fleeing near the floor, downward panic steering is suppressed so fish do not dive into terrain and die.")]
    public float FleeFloorAvoidanceClearance = 2.25f;
    public float FleeFloorExtraLiftWeight = 2.8f;
    [Range(0f, 1f)] public float FleeDownwardClampNearFloor = 0.08f;

    [Header("Mobbing Safety")]
    public bool RequireGroupForMobbing = true;
    public int MinimumNearbyAlliesToMob = 2;
    public float MobbingAllySupportRadius = 7.5f;

    [Header("Threat Awareness")]
    [Tooltip("Threats only become panic threats when they are close, recently attacked this fish, or are visibly approaching from the fish's forward/sides vision cone.")]
    public bool UseCloseLineOfSightThreats = true;
    public float ClosePredatorThreatRange = 7.5f;
    [Range(30f, 170f)] public float ClosePredatorThreatHalfAngle = 105f;
    public float EarlyGroupPredatorWarningRange = 20f;
    [Range(30f, 175f)] public float EarlyGroupPredatorWarningHalfAngle = 120f;
    public float GroupPredatorWarningRadius = 13f;
    public float GroupPredatorWarningDuration = 3.4f;
    public float GroupPredatorWarningCooldown = 1.2f;
    public float PredatorApproachDotForThreat = 0.18f;
    public bool AllowEscapeTowardPredatorWhenTerrainBlocked = true;
    public float TerrainBlockedThreatEscapeDamping = 0.42f;
    public float TerrainBlockedSafePathWeight = 4.4f;

    [Header("Predator Reproduction Restraint")]
    [Tooltip("Predators with enough reserves stop treating similar opposite-sex predators as prey, giving them a risky mating window.")]
    public bool AllowPredatorMateRiskWindow = true;
    [Range(0f, 1f)] public float PredatorMateRiskEnergyRatio = 0.52f;
    [Range(0f, 1f)] public float PredatorMateRiskStomachRatio = 0.20f;
    [Range(0f, 1f)] public float PredatorMateSimilarity = 0.62f;

    [Header("Predator Population Restraint")]
    [Tooltip("Natural selection pressure against predator takeover. This is not a forced cap; it reduces fitness when predators dominate the evaluated population.")]
    public float PredatorDominanceFitnessPenalty = 0.0f;

    [Header("Predator Social Restraint")]
    public bool ProtectSimilarPredatorsFromSpawnKilling = true;
    [Tooltip("Predators with similar morphology treat each other as same-niche rivals/mates rather than immediate prey.")]
    [Range(0f, 1f)] public float SimilarPredatorPeaceSimilarity = 0.74f;
    [Range(0f, 1f)] public float SimilarPredatorAttackEnergyRatio = 0.16f;
    [Range(0f, 1f)] public float SimilarPredatorAttackStomachRatio = 0.06f;

    [Header("Final Balance Tuners")]
    [Tooltip("Satisfied predators fully stop hunting until they need food again.")]
    public bool SatisfiedPredatorsStopHunting = true;
    [Tooltip("Predator damage is damped against healthy grouped prey so hunters remain viable without wiping the ecosystem.")]
    public float HealthyGroupedPreyDamageDamping = 0.58f;

    [Header("Evolved Movement Model")]
    [Tooltip("Minimum swim speed as a ratio of evolved speed. Fish should rarely hard-stop.")]
    public float MinimumCruiseSpeedScale = 0.28f;
    public float NormalCruiseSpeedScale = 0.78f;
    public float HungryCruiseSpeedScale = 1.08f;
    public float SharpTurnSpeedLoss = 0.55f;
    public float SteeringAcceleration = 7.5f;
    public float RotationResponsiveness = 4.5f;
    public float MaxBankAngle = 34f;
    public float WanderStrength = 0.16f;
    public float WanderRefreshMin = 2.5f;
    public float WanderRefreshMax = 6.5f;
    public float BrainInfluence = 0.08f;
    [Tooltip("How strongly evolved neural outputs can bias high-level choices such as feeding, hunting, mating and exploration.")]
    public float BrainDecisionInfluence = 0.18f;
    public bool ShouldLeaveCurrentResource;

    [Header("Autonomous Behaviour Brain")]
    [Tooltip("How often the fish re-evaluates its high-level behaviour. Higher values make behaviour calmer and less twitchy.")]
    public float BrainDecisionInterval = 0.65f;
    [Tooltip("Minimum time before the fish willingly changes between major behaviours unless survival is at risk.")]
    public float MinimumBehaviourHoldTime = 1.35f;
    [Tooltip("How much health pressure pushes food seeking and survival behaviour.")]
    public float HealthNeedWeight = 0.85f;
    [Tooltip("How full the fish wants to be before mating becomes more important than feeding.")]
    [Range(0f, 1f)] public float MateSatietyBias = 0.68f;
    [Tooltip("How strongly bravery and exploration drive casual roaming when the fish is safe and fed.")]
    public float ExplorationBehaviourWeight = 1.05f;
    [Tooltip("Extra reluctance to switch away from feeding once a fish has chosen a nearby food item.")]
    public float FeedingDecisionStickiness = 0.55f;
    [Tooltip("How quickly the high-level brain blends its chosen pull into movement instead of snapping.")]
    public float BehaviourBlendSpeed = 2.8f;
    [Tooltip("Resting fish still cruise slowly, but mostly around their home area instead of searching for food.")]
    public float RestingWanderMultiplier = 0.55f;
    [Tooltip("Exploring fish wander further and rely more on memory and terrain than schooling.")]
    public float ExploringWanderMultiplier = 1.45f;

    [Header("Schooling From Genome")]
    public float SchoolPerceptionRadius = 10f;
    public float FriendlySeparationRadius = 1.55f;
    public int MaxSchoolNeighbours = 34;
    public float SeparationWeight = 1.35f;
    public float AlignmentWeight = 1.10f;
    public float CohesionWeight = 1.05f;
    public float DifferentMorphSpacingWeight = 0.45f;
    public float FoodPrioritySchoolReduction = 0.18f;
    public float MorphSimilarityForSchool = 0.72f;
    [Tooltip("How much friendly separation is allowed to move vertically. Keep this low to stop schools bobbing when crowded.")]
    [Range(0f, 1f)] public float HorizontalSeparationBias = 0.04f;
    [Tooltip("Extra soft spacing radius used when many creatures form a clump.")]
    public float DenseCrowdRadius = 2.65f;
    public float DenseCrowdHorizontalWeight = 3.25f;
    [Range(0f, 1f)] public float DenseCrowdVerticalInfluence = 0.04f;
    [Tooltip("When a static food/carrion target has several fish around it, non-contact fish spread sideways instead of stacking vertically.")]
    public float FeedingCrowdRadius = 3.25f;
    public float FeedingCrowdSideStepWeight = 2.15f;
    public int FeedingCrowdSoftLimit = 3;
    [Tooltip("Upper clamp for any vertical contribution coming from purely social steering.")]
    public float MaxSocialVerticalComponent = 0.025f;
    public float GroupDangerRadius = 8f;
    public float GroupDangerWeight = 0.18f;

    [Header("Feeding")]
    public float FoodEatDistance = 1.0f;
    public float CloseTargetSlowdownDistance = 2.8f;
    public float CloseTargetPull = 2.6f;
    public float FoodQueueRadius = 2.05f;
    public float FoodQueueStrength = 0.25f;
    public float MouthForwardOffset = 0.58f;
    public float MouthRadius = 0.38f;
    [Range(5f, 180f)] public float MouthAngle = 120f;
    public float CloseContactEatForgiveness = 0.45f;
    [Tooltip("When a fish is this close to an edible target, schooling/wander/depth steering are suppressed so the fish commits to feeding instead of stalling near the target.")]
    public float FeedingCommitDistance = 4.0f;
    [Tooltip("Extra body-centre forgiveness for eating static food/carrion. This prevents fish from starving while visually touching food but failing a strict mouth angle check.")]
    public float StaticFoodContactConsumeDistance = 1.45f;
    [Tooltip("Close feeding uses direct velocity towards the target. Rotation still follows movement, but the fish no longer has to complete a wide turn before making progress.")]
    public float CloseFeedingDirectMotionDistance = 4.5f;
    [Range(0f, 1f)] public float FeedingSocialSuppression = 0.04f;
    [Tooltip("Hard override used only when a static food/carrion item is close. It prevents slot/social/depth steering from cancelling the final bite/eat approach.")]
    public float DirectStaticFeedingPullWeight = 7.5f;
    [Tooltip("If a static food/carrion item is this close to the creature body, it is consumed. Static resources are not enemies, so they should not require perfect mouth alignment.")]
    public float StaticBodyContactConsumeDistance = 2.15f;
    [Tooltip("If the mouth is this close to static food/carrion, consume even if the fish's angle is slightly wrong.")]
    public float StaticMouthContactConsumeDistance = 1.65f;
    public float TargetRetainTime = 2f;

    [Header("Feeding Pace / Satiety")]
    public float FeedingHoldAfterBite = 0.32f;
    public float FeedingHoldDamping = 9.5f;
    public float FeedingTurnResponsiveness = 1.35f;
    [Range(0f, 1f)] public float LeaveResourceEnergyRatio = 0.70f;
    [Range(0f, 1f)] public float LeaveResourceStomachRatio = 0.50f;
    public float LeaveResourceIgnoreTime = 5.5f;
    [Tooltip("Extra space used only while multiple fish are feeding. This stops them from sitting inside each other while nibbling.")]
    public float FeedingPersonalSpaceBoost = 1.35f;

    [Header("Autonomous Resource Distribution")]
    [Tooltip("Radius used to judge whether too many creatures are already feeding around the same resource.")]
    public float ResourceCrowdRadius = 5.25f;
    [Tooltip("Low-food-sharing creatures prefer less crowded resources.")]
    public int MinComfortableFeeders = 1;
    [Tooltip("High-food-sharing creatures tolerate more feeders around the same resource.")]
    public int MaxComfortableFeeders = 5;
    [Tooltip("Crowd penalty used by solitary/territorial creatures when picking food.")]
    public float ResourceCrowdPenaltyLowSharing = 32f;
    [Tooltip("Crowd penalty used by social/food-sharing creatures when picking food.")]
    public float ResourceCrowdPenaltyHighSharing = 7f;
    [Tooltip("When a resource is crowded, fish aim for their own approach slot first rather than every fish pushing into the centre.")]
    public float ResourceApproachSlotRadius = 1.85f;
    [Tooltip("If the fish makes no progress near a resource, it temporarily abandons that resource and searches elsewhere.")]
    public float ResourceAbandonDuration = 5.0f;
    public float StuckResourceProgressTime = 1.65f;
    public float StuckResourceDistanceEpsilon = 0.08f;
    [Tooltip("Extra sideways escape force used when a fish is stuck around a target or inside a dense school.")]
    public float HardUnstickSideStepWeight = 5.5f;
    [Tooltip("If crowding around a target is high, friendly schooling is almost fully ignored so fish can split toward different food.")]
    public float CrowdedFeedingSchoolSuppression = 0.02f;

    [Header("School Food Memory / Hunger Leadership")]
    public float HungryLeaderSearchRadius = 14f;
    public float HungryLeaderPullWeight = 1.35f;
    public float FoodMemoryDuration = 70f;
    public float FoodMemoryAreaRadius = 8f;
    public float BadFoodMemoryDuration = 18f;
    public float NotHungryFoodPullSuppression = 0.06f;
    public float DesperateHealthRatio = 0.35f;

    [Header("Habitat Memory")]
    public float HomeMemoryStrength = 0.75f;
    public float HomeAttractionDistance = 12f;
    public float HomeAttractionWeight = 0.9f;
    public float SafeHomeLearningRate = 0.035f;
    public float UnsafeHomeDecayRate = 0.08f;
    public float DangerMemoryDuration = 80f;
    public float DangerMemoryAvoidanceWeight = 1.15f;


    [Header("Mating / Eggs")]
    public float MaturityAgeSeconds = 35f;
    public float JuvenileGrowTime = 35f;
    public float JuvenileStartScale = 0.35f;
    public float MateSearchRadius = 26f;
    public float RequiredMateMorphSimilarity = 0.48f;
    public float MateEnergyRatioRequired = 0.42f;
    public float EggLayCooldown = 20f;
    public float EggLayEnergyCost = 9f;
    public int MinimumEggsPerClutch = 2;
    public int MaximumEggsPerClutch = 8;
    public float EggHatchTime = 38f;
    public float EggHealthPerBodySize = 12f;
    public float EggMassPerEgg = 7f;
    public float NestSearchRadius = 18f;
    public int NestSearchSamples = 12;

    [Header("Predation / Danger")]
    public float BiteCooldown = 1.9f;
    public float BaseBiteDamage = 6f;
    public float ArmourDamageReductionPerPoint = 0.13f;
    public float MissedAttackEnergyCost = 1.4f;
    [Range(0f, 1f)] public float SameMorphAttackAggressionRequired = 0.56f;
    [Range(0f, 1f)] public float SameMorphAttackMeatRequired = 0.54f;
    [Range(0f, 1f)] public float StarvingAttackEnergyRatio = 0.26f;
    [Range(0f, 1f)] public float StarvingAttackAggressionRequired = 0.24f;
    [Range(0f, 1f)] public float StarvingAttackMeatRequired = 0.46f;

    [Header("Advanced Fish Behaviours")]
    public bool EnableAdvancedFishBehaviours = true;

    [Tooltip("Stealth/meat/aggression based predators can wait near food-rich areas instead of always chasing.")]
    public float AmbushBehaviourThreshold = 0.48f;
    public float AmbushAnchorRefreshTime = 5.5f;
    public float AmbushHoldRadius = 2.6f;
    public float AmbushStrikeRange = 7.5f;
    public float AmbushPullWeight = 1.8f;

    [Tooltip("Short sudden escape burst when a fish switches into fleeing.")]
    public float CStartBurstDuration = 0.55f;
    public float CStartBurstWeight = 8.5f;
    public float CStartSideBias = 0.65f;

    [Tooltip("Parents with enough egg-protection/territoriality stay near eggs and intercept predators.")]
    public float EggGuardSearchRadius = 22f;
    public float EggGuardBehaviourThreshold = 0.36f;
    public float EggGuardPullWeight = 2.4f;
    public float EggGuardThreatInterceptWeight = 3.2f;
    public float EggGuardScanInterval = 1.25f;

    [Tooltip("Brave/social prey can harass nearby predators instead of only fleeing.")]
    public float MobbingBehaviourThreshold = 0.56f;
    public float MobbingSearchRadius = 15f;
    public float MobbingIdealDistance = 4.4f;
    public float MobbingPullWeight = 2.2f;
    public float MobbingPressureDuration = 1.4f;

    [Tooltip("Close fed mates slow down and circle before laying eggs, so reproduction is visible.")]
    public float CourtshipStartDistance = 8f;
    public float CourtshipDuration = 2.4f;
    public float CourtshipCircleWeight = 1.6f;

    [Header("Habitat / Depth")]
    public float DepthPreferenceStrength = 0.95f;
    public float DepthPadding = 3f;
    public float BoundaryAvoidanceDistance = 8f;
    public float BoundaryAvoidanceStrength = 5.5f;
    public float BoundaryHardStopMargin = 0.55f;
    public float BoundaryVelocityDamping = 0.65f;

    [Header("Upright Swimming")]
    [Tooltip("Maximum pitch used for normal steering. Vertical targets can still be reached, but the fish avoids flipping straight up/down.")]
    public float MaxNormalPitchAngle = 58f;
    [Tooltip("Additional pitch allowance when actively feeding or hunting a target above/below.")]
    public float FeedingPitchAllowance = 18f;
    [Tooltip("Roll/bank is reduced in dense groups to stop fish rolling upside down while crowded.")]
    [Range(0f, 1f)] public float CrowdedBankReduction = 0.35f;
    [Tooltip("Hard reset angular velocity each tick so physics cannot accumulate unwanted spins.")]
    public bool SuppressPhysicsSpin = true;
    [Tooltip("Use kinematic steering instead of Rigidbody collision forces. This keeps motion autonomous but prevents physics piles from bouncing fish up/down.")]
    public bool UseKinematicSwimming = true;
    [Tooltip("How strongly the fish returns to an upright world-up posture. Bank still happens while turning, but the fish will not keep rolling over.")]
    public float UprightCorrectionStrength = 5.5f;

    [Header("Anti-Stuck / Collision Avoidance")]
    public bool UseRootLogicCollider = true;
    public bool RootColliderIsTrigger = true;
    [Tooltip("Local-space root collider radius. Keep this small because the creature root is already scaled by body size at runtime.")]
    public float RootColliderRadius = 0.35f;
    [Tooltip("Maximum local-space radius allowed at runtime. This fixes old prefab Inspector values that kept the previous oversized collider radius.")]
    public float MaxRuntimeRootColliderRadius = 0.35f;
    [Tooltip("Predators are allowed to push closer to their current prey so soft spacing does not stop hunting/bites.")]
    [Range(0.05f, 1f)] public float HuntingContactSpaceMultiplier = 0.25f;
    public bool DisableVisualColliders = true;
    public float PersonalSpaceMultiplier = 1.25f;
    public float EmergencyUnstickRadiusMultiplier = 0.85f;
    public float EmergencyUnstickWeight = 2.75f;
    public float StuckSpeedThreshold = 0.12f;
    public float StuckCheckTime = 1.2f;
    public float StuckEscapeDuration = 1.8f;
    public float StuckEscapeWeight = 2.4f;
    public float StuckPositionDeltaThreshold = 0.12f;

    [Header("Soft Body Spacing")]
    public int KinematicOverlapIterations = 2;
    public float KinematicOverlapResolveStrength = 0.65f;
    public float KinematicOverlapVerticalInfluence = 0.03f;
    public float FeedingOverlapResolveStrength = 1.05f;
    public float OverlapSearchRadius = 3.5f;

    // Terrain checks keep fleeing and chasing fish from swimming into floors or walls.
    [Header("Terrain Navigation")]
    public bool UseTerrainAvoidance = true;
    public float TerrainLookAhead = 4.5f;
    public float TerrainSideLookAhead = 3.25f;
    public float TerrainAvoidanceWeight = 5.0f;
    public float TerrainFloorClearance = 1.15f;
    public float TerrainFloorLiftWeight = 3.2f;
    public float TerrainWallSlideWeight = 2.4f;
    public float TerrainProbeForwardHeight = 0.35f;

    [Header("Mate Seeking")]
    public float MateSeekingWeight = 2.0f;
    public float MateSeekingEnergyRatio = 0.42f;
    public float MateSeekingStomachRatio = 0.16f;
    public float MatePairDistance = 4.25f;
    public float MateTargetRefreshTime = 2.5f;
    public float MateSearchWanderSuppression = 0.35f;
    public float MateSearchSchoolSuppression = 0.55f;

    [Header("Evolution Feedback")]
    public float DietLearningRate = 0.028f;
    public float BehaviourDecayRate = 0.0015f;

    [Header("Visuals / Debug")]
    public bool ApplyTypeColour = true;
    public bool LocalDebugRays;
    public bool LocalDebugLabels;
    public bool ScaleCreatureRootByEffectiveBodySize = true;
    public bool DisableLegacyPhenotypeVisuals = true;

    private Rigidbody rb;
    private Renderer[] cachedRenderers;
    private MaterialPropertyBlock materialBlock;

    [Header("Performance")]
    [Tooltip("Emergency mode keeps the simulation alive by heavily throttling expensive per-fish logic. Turn this off later only after profiling.")]
    public bool EmergencyPerformanceMode = true;
    [Tooltip("1 = every physics tick. 2 = every other physics tick. Use 2 while debugging heavy scenes.")]
    public int EmergencyFixedUpdateStride = 2;
    [Tooltip("Caches expensive all-creature social/danger scans. Lower values are more accurate but slower.")]
    public float SocialScanInterval = 0.75f;
    [Tooltip("Caches terrain ray checks. Lower values are more accurate but slower.")]
    public float TerrainScanInterval = 0.45f;

    private int emergencyFixedUpdateCounter;
    private float socialScanTimer;
    private float dangerScanTimer;
    private float crowdScanTimer;
    private float terrainScanTimer;
    private Vector3 cachedSchoolPull;
    private Vector3 cachedDangerPull;
    private Vector3 cachedCrowdPull;
    private Vector3 cachedTerrainPull;
    private float[] brainInputs;
    private float[] brainOutputs;
    private float[] brainHiddenScratch;
    private int brainEvaluationFrame = -1;
    private float brainOutputFoodBias;
    private float brainOutputHuntBias;
    private float brainOutputFleeBias;
    private float brainOutputMateSocialBias;
    private float brainOutputExploreHomeBias;
    private float brainOutputRestBias;
    private float brainOutputSprintBias;
    private float brainMemoryFood;
    private float brainMemoryPrey;
    private float brainMemoryThreat;
    private float brainMemoryEnergyStress;

    private FoodSource nearestFood;
    private CarrionSource nearestCarrion;
    private MarineCreatureAgent nearestCreature;
    private MarineCreatureAgent nearestPrey;
    private MarineCreatureAgent focusedPrey;
    private float hunterPreyFocusTimer;
    private float predatorChaseTimer;
    private float predatorTimeSinceLastBite;
    private float predatorExhaustionTimer;
    private float woundedScentTimer;

    private FoodSource retainedFood;
    private CarrionSource retainedCarrion;
    private MarineCreatureAgent retainedPrey;
    private CarrionSource claimedFreshKillCarrion;
    private float freshKillPriorityTimer;
    private Vector3 freshKillSearchPosition;
    private bool hasFreshKillSearchPosition;
    private MarineCreatureAgent currentMateTarget;
    private float mateTargetTimer;
    private FishEggCluster guardedEggCluster;
    private MarineCreatureAgent currentEggThreat;
    private MarineCreatureAgent currentMobbingTarget;
    private Vector3 ambushAnchor;
    private Vector3 cStartBurstDirection;
    private Vector3 mobbingPressureDirection;
    private bool hasAmbushAnchor;
    private float eggGuardScanTimer;
    private float ambushAnchorTimer;
    private float cStartBurstTimer;
    private float courtshipTimer;
    private float mobbingPressureTimer;
    private Vector3 lastTerrainAvoidance;
    private float retainedTargetTimer;
    private FoodSource temporarilyIgnoredFood;
    private CarrionSource temporarilyIgnoredCarrion;
    private float ignoredResourceTimer;
    private Vector3 lastPrimaryStaticTargetPosition;
    private float lastPrimaryStaticTargetDistance = float.MaxValue;
    private float staticTargetNoProgressTimer;
    private bool hasFoodMemory;
    private Vector3 rememberedFoodArea;
    private float foodMemoryTimer;
    private bool rememberedFoodWasBad;
    private MarineCreatureAgent currentHungryLeader;
    private Vector3 lastLeaderPull;

    private FishAutonomousBehaviourMode currentBrainMode = FishAutonomousBehaviourMode.Resting;
    private FishAutonomousBehaviourMode previousBrainMode = FishAutonomousBehaviourMode.Resting;
    private float brainDecisionTimer;
    private float behaviourHoldTimer;
    private float brainFoodDesire;
    private float brainMateDesire;
    private float brainSchoolDesire;
    private float brainExploreDesire;
    private float brainHomeDesire;
    private float brainHuntDesire;
    private float brainFleeDesire;
    private float brainRestDesire;
    private float brainNestingDesire;
    private bool brainWantsFood;
    private bool brainWantsMate;
    private bool brainWantsHunt;
    private bool brainWantsFlee;
    private Vector3 behaviourBlendVector;
    private string brainReason = "Initialising";

    private float reproductionTimer;
    private float eggLayTimer;
    private float biteTimer;
    private float aliveTimer;
    private float recentDamageHealLockTimer;
    private bool isSprintingThisTick;
    private float sprintCooldownTimer;
    private float survivalEmergencyTimer;
    private float survivalRecentDamageTimer;
    private Vector3 survivalEscapeDirection;
    private MarineCreatureAgent survivalThreatTarget;
    private float threatFoodSuppressionTimer;
    private float groupPredatorWarningTimer;
    private Vector3 groupPredatorWarningDirection;
    private float groupPredatorWarningCooldownTimer;
    private MarineCreatureAgent groupWarningPredator;
    private float senseTimer;
    private float senseInterval;
    private float lowProgressTimer;
    private float stuckEscapeTimer;

    private Vector3 lastPosition;
    private Vector3 wanderDirection;
    private float wanderTimer;
    private Vector3 stuckEscapeDirection;
    private Vector3 currentVelocity;
    private Vector3 currentEscapeDirection;
    private float currentEscapeTimer;
    private float lastCurrentStress;
    private Vector3 lastCurrentFlow;

    private Vector3 wantedDirection;
    private Vector3 lastFoodDirection;
    private Vector3 lastCarrionDirection;
    private Vector3 lastCreatureDirection;
    private Vector3 lastPreyDirection;
    private Vector3 lastBoundaryPush;
    private Vector3 lastSchoolCohesion;
    private Vector3 lastSchoolAlignment;
    private Vector3 lastSchoolSeparation;
    private Vector3 lastThreatAvoidance;
    private Vector3 lastEmergencyUnstick;
    private int lastFriendlyCount;
    private int lastThreatCount;
    private int lastDifferentCount;
    private int lastDenseCrowdCount;
    private int lastFeedingCrowdCount;
    private string debugMoveState = "Initialising";
    private string debugVerticalReason = "Initialising";

    private float feedingHoldTimer;
    private Vector3 currentStaticFeedingPoint;
    private bool hasStaticFeedingPoint;
    private Vector3 homeArea;
    private float homeConfidence;
    private bool hasHomeArea;
    private Vector3 rememberedDangerArea;
    private float dangerMemoryTimer;

    // Pass 17 survival-memory state. These were referenced by the survival patch
    // but were missing as fields in the previous zip, causing compile errors.
    private Vector3 safeRestArea;
    private bool hasSafeRestArea;
    private float safeRestAreaTimer;
    private string rememberedPredatorSignature = "";
    private float rememberedPredatorTypeTimer;

    // Sets up this fish from its candidate, stats, body, timers and starting needs
    public void Initialise(EvolutionCandidate candidate)
    {
        Candidate = candidate ?? new EvolutionCandidate(EvolutionGenome.CreateBaseline());
        if (Candidate.Genome == null)
        {
            Candidate.Genome = EvolutionGenome.CreateBaseline();
        }

        if (MorphLibrary == null)
        {
            MorphLibrary = EvolutionEcosystemManager.Instance != null ? EvolutionEcosystemManager.Instance.MorphLibrary : CreatureMorphLibrary.ActiveLibrary;
        }

        ApplyRuntimeEcologyBalanceSafeguards();

        Candidate.Genome.ClampValues();
        EffectiveStats = CreatureEffectiveStats.Build(Candidate.Genome, MorphLibrary);
        ApplyRuntimeAwarenessBoost();
        ClampRuntimeColliderSettings();
        Candidate.RefreshDebugIdentity();

        DebugName = Candidate.DisplayName;
        DebugBehaviourType = Candidate.BehaviourType;
        gameObject.name = "Creature_" + DebugName;

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = UseKinematicSwimming;
        rb.linearDamping = 1.85f;
        rb.angularDamping = 5.0f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        transform.localScale = ScaleCreatureRootByEffectiveBodySize ? Vector3.one * EffectiveStats.BodySize : Vector3.one;
        CurrentEnergy = EffectiveStats.EnergyCapacity * 0.82f;
        CurrentHealth = GetMaxHealth();
        AgeSeconds = 0f;
        IsJuvenile = false;
        StomachPlant = 0f;
        StomachMeat = 0f;
        StomachCarrion = 0f;
        hasFoodMemory = false;
        foodMemoryTimer = 0f;
        rememberedFoodWasBad = false;
        currentHungryLeader = null;
        guardedEggCluster = null;
        currentEggThreat = null;
        currentMobbingTarget = null;
        hasAmbushAnchor = false;
        ambushAnchor = transform.position;
        ambushAnchorTimer = Random.Range(0f, Mathf.Max(0.2f, AmbushAnchorRefreshTime));
        eggGuardScanTimer = Random.Range(0f, Mathf.Max(0.2f, EggGuardScanInterval));
        cStartBurstTimer = 0f;
        courtshipTimer = 0f;
        mobbingPressureTimer = 0f;
        cStartBurstDirection = Vector3.zero;
        mobbingPressureDirection = Vector3.zero;
        feedingHoldTimer = 0f;
        hasStaticFeedingPoint = false;
        currentStaticFeedingPoint = transform.position;
        hasHomeArea = true;
        homeArea = transform.position;
        homeConfidence = 0.55f;
        rememberedDangerArea = transform.position;
        dangerMemoryTimer = 0f;
        recentDamageHealLockTimer = 0f;
        isSprintingThisTick = false;
        sprintCooldownTimer = 0f;
        survivalEmergencyTimer = 0f;
        survivalRecentDamageTimer = 0f;
        survivalEscapeDirection = Vector3.zero;
        survivalThreatTarget = null;
        threatFoodSuppressionTimer = 0f;
        groupPredatorWarningTimer = 0f;
        groupPredatorWarningDirection = Vector3.zero;
        groupPredatorWarningCooldownTimer = 0f;
        groupWarningPredator = null;

        aliveTimer = 0f;
        lowProgressTimer = 0f;
        retainedTargetTimer = 0f;
        predatorChaseTimer = 0f;
        predatorTimeSinceLastBite = 999f;
        predatorExhaustionTimer = 0f;
        woundedScentTimer = 0f;
        ignoredResourceTimer = 0f;
        temporarilyIgnoredFood = null;
        temporarilyIgnoredCarrion = null;
        claimedFreshKillCarrion = null;
        freshKillPriorityTimer = 0f;
        hasFreshKillSearchPosition = false;
        currentEscapeDirection = Vector3.zero;
        currentEscapeTimer = 0f;
        lastCurrentStress = 0f;
        lastCurrentFlow = Vector3.zero;
        lastPrimaryStaticTargetDistance = float.MaxValue;
        staticTargetNoProgressTimer = 0f;
        reproductionTimer = Random.Range(1f, ReproductionCooldown);
        eggLayTimer = Random.Range(3f, EggLayCooldown);
        biteTimer = Random.Range(0f, BiteCooldown);
        senseInterval = Random.Range(0.35f, 0.75f);
        senseTimer = Random.Range(0f, senseInterval);
        ApplyEmergencyPerformanceSafeguards();
        socialScanTimer = Random.Range(0f, Mathf.Max(0.02f, SocialScanInterval));
        dangerScanTimer = Random.Range(0f, Mathf.Max(0.02f, SocialScanInterval));
        crowdScanTimer = Random.Range(0f, Mathf.Max(0.02f, SocialScanInterval));
        terrainScanTimer = Random.Range(0f, Mathf.Max(0.02f, TerrainScanInterval));
        lastPosition = transform.position;
        currentVelocity = Vector3.zero;
        currentBrainMode = FishAutonomousBehaviourMode.Resting;
        previousBrainMode = currentBrainMode;
        brainDecisionTimer = Random.Range(0f, Mathf.Max(0.05f, BrainDecisionInterval));
        behaviourHoldTimer = Random.Range(0.25f, Mathf.Max(0.3f, MinimumBehaviourHoldTime));
        behaviourBlendVector = transform.forward;
        brainReason = "Fresh spawn";
        brainEvaluationFrame = -1;
        brainOutputFoodBias = 0f;
        brainOutputHuntBias = 0f;
        brainOutputFleeBias = 0f;
        brainOutputMateSocialBias = 0f;
        brainOutputExploreHomeBias = 0f;
        brainOutputRestBias = 0f;
        brainOutputSprintBias = 0f;
        brainMemoryFood = 0f;
        brainMemoryPrey = 0f;
        brainMemoryThreat = 0f;
        brainMemoryEnergyStress = 0f;
        brainWantsFood = false;
        brainWantsMate = false;
        brainWantsHunt = false;
        brainWantsFlee = false;
        PickNewWanderDirection();

        ApplyMorphVisuals();

        if (DisableVisualColliders)
        {
            DisableBlockingCollidersOnVisuals();
        }

        if (UseRootLogicCollider)
        {
            EnsureRootLogicCollider();
        }

        CacheRenderers(true);
        ApplyDebugColour();
    }


    // Applies safe runtime values so old prefab settings do not break balance
    private void ApplyRuntimeEcologyBalanceSafeguards()
    {
        if (!UseEcologyBalanceSafeguards)
        {
            return;
        }

        // Keep creatures alive long enough to do more than panic-feed, while still
        // allowing starvation if they make poor choices for a full 150s generation.
        BaseEnergyDrainPerSecond = Mathf.Min(BaseEnergyDrainPerSecond, 0.48f);
        StarvationHealthDamagePerSecond = Mathf.Min(StarvationHealthDamagePerSecond, 2.4f);
        HealthRecoveryPerSecond = Mathf.Max(HealthRecoveryPerSecond, 2.0f);
        HealDelayAfterDamage = Mathf.Clamp(HealDelayAfterDamage, 3.5f, 8.0f);
        RestingHealMultiplier = Mathf.Clamp(RestingHealMultiplier, 0.20f, 0.55f);
        SleepingHealMultiplier = Mathf.Clamp(SleepingHealMultiplier, 1.75f, 3.25f);
        SprintSpeedMultiplier = Mathf.Clamp(SprintSpeedMultiplier, 1.25f, 1.85f);
        SprintEnergyCostMultiplier = Mathf.Max(2.0f, SprintEnergyCostMultiplier);
        HunterPreyFocusTime = Mathf.Clamp(HunterPreyFocusTime, 3.0f, 5.25f);
        HunterBiteFocusTime = Mathf.Clamp(HunterBiteFocusTime, HunterPreyFocusTime, 7.5f);
        PredatorFailedChaseGiveUpTime = Mathf.Clamp(PredatorFailedChaseGiveUpTime, 3.0f, 5.4f);
        PredatorExhaustionAfterFailedChase = Mathf.Clamp(PredatorExhaustionAfterFailedChase, 2.5f, 10.0f);
        PredatorExhaustionAfterEnoughBites = Mathf.Clamp(PredatorExhaustionAfterEnoughBites, 3.0f, 10.0f);
        GrazerHuntBlockMeatThreshold = Mathf.Clamp(GrazerHuntBlockMeatThreshold, 0.42f, 0.72f);
        EcologicalPredatorMeatThreshold = Mathf.Clamp(EcologicalPredatorMeatThreshold, 0.48f, 0.74f);
        HunterChillEnergyRatio = Mathf.Clamp(HunterChillEnergyRatio, 0.52f, 0.78f);
        HunterChillStomachRatio = Mathf.Clamp(HunterChillStomachRatio, 0.22f, 0.58f);
        HunterStickyChaseWeight = Mathf.Clamp(HunterStickyChaseWeight, 3.2f, 8.0f);
        PredatorCommittedBiteDamageMultiplier = Mathf.Clamp(PredatorCommittedBiteDamageMultiplier, 1.0f, 1.36f);
        PredatorSizeDamageScale = Mathf.Clamp(PredatorSizeDamageScale, 0.25f, 0.78f);
        PredatorBiteBaseMeatReward = Mathf.Clamp(PredatorBiteBaseMeatReward, 0.55f, 1.90f);
        PredatorBiteMeatFromDamageMultiplier = Mathf.Clamp(PredatorBiteMeatFromDamageMultiplier, 0.10f, 0.34f);
        InjuryMovementStartHealthRatio = Mathf.Clamp(InjuryMovementStartHealthRatio, 0.55f, 0.82f);
        InjuryMaxSpeedLoss = Mathf.Clamp(InjuryMaxSpeedLoss, 0.08f, 0.60f);
        InjuryMaxTurnLoss = Mathf.Clamp(InjuryMaxTurnLoss, 0.04f, 0.55f);
        InjuryMaxAccelerationLoss = Mathf.Clamp(InjuryMaxAccelerationLoss, 0.04f, 0.55f);
        WoundedScentDuration = Mathf.Clamp(WoundedScentDuration, 4f, 18f);
        WoundedScentPredatorBonus = Mathf.Clamp(WoundedScentPredatorBonus, 0.10f, 1.15f);
        SharkLikeStrikeDamageMultiplier = Mathf.Clamp(SharkLikeStrikeDamageMultiplier, 1.0f, 1.18f);
        BiteCooldown = Mathf.Max(BiteCooldown, 2.75f);
        SimilarPredatorPeaceSimilarity = Mathf.Clamp01(SimilarPredatorPeaceSimilarity);
        SimilarPredatorAttackEnergyRatio = Mathf.Clamp(SimilarPredatorAttackEnergyRatio, 0.05f, 0.28f);
        SimilarPredatorAttackStomachRatio = Mathf.Clamp(SimilarPredatorAttackStomachRatio, 0.01f, 0.16f);
        FleeTerrainAvoidanceMultiplier = Mathf.Clamp(FleeTerrainAvoidanceMultiplier, 1.2f, 4.2f);
        FleeWallVerticalVelocityLimit = Mathf.Clamp(FleeWallVerticalVelocityLimit, 0.08f, 0.75f);
        FleeFloorAvoidanceClearance = Mathf.Clamp(FleeFloorAvoidanceClearance, 1.25f, 4.0f);
        FleeFloorExtraLiftWeight = Mathf.Clamp(FleeFloorExtraLiftWeight, 1.0f, 6.0f);
        FleeDownwardClampNearFloor = Mathf.Clamp01(FleeDownwardClampNearFloor);
        MinimumNearbyAlliesToMob = Mathf.Clamp(MinimumNearbyAlliesToMob, 1, 5);
        MobbingAllySupportRadius = Mathf.Clamp(MobbingAllySupportRadius, 4.0f, 14.0f);
        ClosePredatorThreatRange = Mathf.Clamp(ClosePredatorThreatRange, 3.5f, 18f);
        ClosePredatorThreatHalfAngle = Mathf.Clamp(ClosePredatorThreatHalfAngle, 45f, 170f);
        EarlyGroupPredatorWarningRange = Mathf.Clamp(EarlyGroupPredatorWarningRange, ClosePredatorThreatRange, 34f);
        EarlyGroupPredatorWarningHalfAngle = Mathf.Clamp(EarlyGroupPredatorWarningHalfAngle, 45f, 175f);
        GroupPredatorWarningRadius = Mathf.Clamp(GroupPredatorWarningRadius, 4f, 24f);
        GroupPredatorWarningDuration = Mathf.Clamp(GroupPredatorWarningDuration, 0.5f, 8f);
        GroupPredatorWarningCooldown = Mathf.Clamp(GroupPredatorWarningCooldown, 0.2f, 8f);
        PredatorApproachDotForThreat = Mathf.Clamp(PredatorApproachDotForThreat, -0.2f, 0.85f);
        TerrainBlockedThreatEscapeDamping = Mathf.Clamp01(TerrainBlockedThreatEscapeDamping);
        TerrainBlockedSafePathWeight = Mathf.Clamp(TerrainBlockedSafePathWeight, 0.5f, 8f);
        PredatorMateRiskEnergyRatio = Mathf.Clamp01(PredatorMateRiskEnergyRatio);
        PredatorMateRiskStomachRatio = Mathf.Clamp01(PredatorMateRiskStomachRatio);
        PredatorMateSimilarity = Mathf.Clamp01(PredatorMateSimilarity);
        HealthyGroupedPreyDamageDamping = Mathf.Clamp(HealthyGroupedPreyDamageDamping, 0.38f, 0.82f);
        BiteCooldown = Mathf.Max(BiteCooldown, 2.75f);
        PredatorPlantBlockMeatThreshold = Mathf.Clamp(PredatorPlantBlockMeatThreshold, 0.30f, 0.70f);
        PredatorPlantEmergencyEnergyRatio = Mathf.Clamp(PredatorPlantEmergencyEnergyRatio, 0.04f, 0.25f);
        PredatorPlantEmergencyStomachRatio = Mathf.Clamp(PredatorPlantEmergencyStomachRatio, 0.02f, 0.16f);
        InjuredSurvivalHealthRatio = Mathf.Clamp(InjuredSurvivalHealthRatio, 0.35f, 0.65f);
        SurvivalWarningHealthRatio = Mathf.Max(SurvivalWarningHealthRatio, InjuredSurvivalHealthRatio);
        SurvivalCriticalHealthRatio = Mathf.Max(SurvivalCriticalHealthRatio, Mathf.Min(0.42f, InjuredSurvivalHealthRatio * 0.84f));
        CriticalStarvationEnergyRatio = Mathf.Clamp(CriticalStarvationEnergyRatio, 0.08f, 0.28f);
        CriticalStarvationStomachRatio = Mathf.Clamp(CriticalStarvationStomachRatio, 0.02f, 0.20f);
        GroupCounterSupportRadius = Mathf.Clamp(GroupCounterSupportRadius, 4f, 18f);
        GroupCounterMaxDamage = Mathf.Clamp(GroupCounterMaxDamage, 3f, 16f);
        CurrentEscapeWeight = Mathf.Clamp(CurrentEscapeWeight, 5.5f, 8.5f);
        CurrentEscapeMemoryTime = Mathf.Clamp(CurrentEscapeMemoryTime, 8.0f, 16.0f);
        CurrentEscapeCentreBias = Mathf.Clamp(CurrentEscapeCentreBias, 1.0f, 1.65f);
        CurrentEscapeSideBias = Mathf.Clamp(CurrentEscapeSideBias, 0.85f, 1.45f);
        CurrentEscapeAgainstFlowBias = Mathf.Clamp(CurrentEscapeAgainstFlowBias, 0.05f, 0.32f);
        CurrentEscapeCurrentDriftMultiplier = Mathf.Clamp01(CurrentEscapeCurrentDriftMultiplier * 0.75f);
        BaseStomachCapacity = Mathf.Clamp(BaseStomachCapacity, 54f, 76f);
        BaseDigestionPerSecond = Mathf.Clamp(BaseDigestionPerSecond, 4.2f, 6.2f);
        BaseBiteMass = Mathf.Clamp(BaseBiteMass, 3.4f, 5.0f);

        // Satisfied fish stop stripping plants and return to wandering/social/mating.
        LeaveResourceEnergyRatio = Mathf.Clamp(LeaveResourceEnergyRatio, 0.62f, 0.74f);
        LeaveResourceStomachRatio = Mathf.Clamp(LeaveResourceStomachRatio, 0.42f, 0.56f);
        LeaveResourceIgnoreTime = Mathf.Clamp(LeaveResourceIgnoreTime, 4.5f, 7.0f);
        FeedingHoldAfterBite = Mathf.Clamp(FeedingHoldAfterBite, 0.24f, 0.42f);
        ResourceAbandonDuration = Mathf.Max(ResourceAbandonDuration, 6.0f);

        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        float generationDuration = manager != null ? Mathf.Max(60f, manager.GenerationDuration) : 150f;

        MaturityAgeSeconds = Mathf.Min(MaturityAgeSeconds, generationDuration * 0.24f);
        JuvenileGrowTime = Mathf.Min(JuvenileGrowTime, generationDuration * 0.24f);
        MateEnergyRatioRequired = Mathf.Min(MateEnergyRatioRequired, 0.38f);
        MateSeekingEnergyRatio = Mathf.Min(MateSeekingEnergyRatio, 0.38f);
        MateSeekingStomachRatio = Mathf.Min(MateSeekingStomachRatio, 0.16f);
        RequiredMateMorphSimilarity = Mathf.Min(RequiredMateMorphSimilarity, 0.42f);
        MatePairDistance = Mathf.Max(MatePairDistance, 4.8f);
        EggLayCooldown = Mathf.Min(EggLayCooldown, Mathf.Min(16f, generationDuration * 0.13f));
        EggLayEnergyCost = Mathf.Min(EggLayEnergyCost, 8f);
        EggHatchTime = Mathf.Min(EggHatchTime, Mathf.Min(32f, generationDuration * 0.22f));
        MateSearchRadius = Mathf.Max(MateSearchRadius, 30f);
        MatePairDistance = Mathf.Max(MatePairDistance, 4.25f);
        MateTargetRefreshTime = Mathf.Min(MateTargetRefreshTime, 1.4f);

        PlantEnergyGainMultiplier = Mathf.Max(PlantEnergyGainMultiplier, 2.05f);
        MeatEnergyGainMultiplier = Mathf.Max(MeatEnergyGainMultiplier, 2.20f);
        CarrionEnergyGainMultiplier = Mathf.Max(CarrionEnergyGainMultiplier, 1.85f);
        EnergyRecoveredPerStoredStomachMass = Mathf.Max(EnergyRecoveredPerStoredStomachMass, 0.26f);
        FoodMemoryDuration = Mathf.Max(FoodMemoryDuration, 70f);
        DangerMemoryDuration = Mathf.Max(DangerMemoryDuration, 80f);
    }

    // Sets up cached references and safe starting values before the sim runs
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        CacheRenderers();
    }

    // Starts the setup that needs other scene objects to already exist
    private void Start()
    {
        if (Candidate == null)
        {
            Initialise(new EvolutionCandidate(EvolutionGenome.CreateBaseline()));
        }
    }

    // Runs the fish brain, movement, feeding, hunting, mating and survival checks each physics step
    private void FixedUpdate()
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return;
        }

        if (EffectiveStats == null)
        {
            EffectiveStats = CreatureEffectiveStats.Build(Candidate.Genome, MorphLibrary);
            ApplyRuntimeAwarenessBoost();
        }

        if (EmergencyPerformanceMode && EmergencyFixedUpdateStride > 1)
        {
            emergencyFixedUpdateCounter++;
            if (emergencyFixedUpdateCounter % EmergencyFixedUpdateStride != 0)
            {
                if (rb != null && SuppressPhysicsSpin && !rb.isKinematic)
                {
                    rb.angularVelocity = Vector3.zero;
                }
                return;
            }
        }

        aliveTimer += Time.fixedDeltaTime;
        AgeSeconds += Time.fixedDeltaTime;
        reproductionTimer -= Time.fixedDeltaTime;
        eggLayTimer -= Time.fixedDeltaTime;
        UpdateJuvenileGrowth();
        biteTimer -= Time.fixedDeltaTime;
        if (recentDamageHealLockTimer > 0f)
        {
            recentDamageHealLockTimer -= Time.fixedDeltaTime;
        }
        if (sprintCooldownTimer > 0f)
        {
            sprintCooldownTimer -= Time.fixedDeltaTime;
        }
        if (survivalEmergencyTimer > 0f)
        {
            survivalEmergencyTimer -= Time.fixedDeltaTime;
        }
        if (survivalRecentDamageTimer > 0f)
        {
            survivalRecentDamageTimer -= Time.fixedDeltaTime;
        }
        if (predatorExhaustionTimer > 0f)
        {
            predatorExhaustionTimer -= Time.fixedDeltaTime;
        }
        if (woundedScentTimer > 0f)
        {
            woundedScentTimer -= Time.fixedDeltaTime;
        }
        predatorTimeSinceLastBite += Time.fixedDeltaTime;
        isSprintingThisTick = false;
        senseTimer -= Time.fixedDeltaTime;
        retainedTargetTimer -= Time.fixedDeltaTime;
        if (hunterPreyFocusTimer > 0f)
        {
            hunterPreyFocusTimer -= Time.fixedDeltaTime;
            predatorChaseTimer += Time.fixedDeltaTime;
            if (hunterPreyFocusTimer <= 0f || focusedPrey == null || focusedPrey.CurrentHealth <= 0f || !CanAttackPrey(focusedPrey))
            {
                ClearFocusedPrey(false);
            }
            else if (ShouldAbandonFocusedPrey())
            {
                ClearFocusedPrey(true);
            }
        }
        ignoredResourceTimer -= Time.fixedDeltaTime;
        feedingHoldTimer -= Time.fixedDeltaTime;
        socialScanTimer -= Time.fixedDeltaTime;
        dangerScanTimer -= Time.fixedDeltaTime;
        crowdScanTimer -= Time.fixedDeltaTime;
        terrainScanTimer -= Time.fixedDeltaTime;
        if (feedingHoldTimer < 0f)
        {
            feedingHoldTimer = 0f;
        }
        if (freshKillPriorityTimer > 0f)
        {
            freshKillPriorityTimer -= Time.fixedDeltaTime;
            if (freshKillPriorityTimer <= 0f || claimedFreshKillCarrion == null || claimedFreshKillCarrion.IsConsumed || GetRemainingStomachSpace() <= 0.05f || !ShouldContinueEatingFreshKillCarrion())
            {
                ClearFreshKillClaim();
            }
        }
        if (currentEscapeTimer > 0f)
        {
            currentEscapeTimer -= Time.fixedDeltaTime;
            if (currentEscapeTimer <= 0f)
            {
                currentEscapeTimer = 0f;
                currentEscapeDirection = Vector3.zero;
            }
        }
        mateTargetTimer -= Time.fixedDeltaTime;
        eggGuardScanTimer -= Time.fixedDeltaTime;
        ambushAnchorTimer -= Time.fixedDeltaTime;
        if (cStartBurstTimer > 0f)
        {
            cStartBurstTimer -= Time.fixedDeltaTime;
        }
        if (courtshipTimer > 0f)
        {
            courtshipTimer -= Time.fixedDeltaTime;
        }
        if (mobbingPressureTimer > 0f)
        {
            mobbingPressureTimer -= Time.fixedDeltaTime;
        }
        if (dangerMemoryTimer > 0f)
        {
            dangerMemoryTimer -= Time.fixedDeltaTime;
        }
        if (safeRestAreaTimer > 0f)
        {
            safeRestAreaTimer -= Time.fixedDeltaTime;
        }
        if (safeRestAreaTimer <= 0f)
        {
            hasSafeRestArea = false;
        }
        if (rememberedPredatorTypeTimer > 0f)
        {
            rememberedPredatorTypeTimer -= Time.fixedDeltaTime;
            if (Candidate != null)
            {
                Candidate.PredatorTypeAvoidanceTime += Time.fixedDeltaTime;
            }
        }
        if (threatFoodSuppressionTimer > 0f)
        {
            threatFoodSuppressionTimer -= Time.fixedDeltaTime;
        }
        if (groupPredatorWarningTimer > 0f)
        {
            groupPredatorWarningTimer -= Time.fixedDeltaTime;
            if (groupPredatorWarningTimer <= 0f)
            {
                groupPredatorWarningTimer = 0f;
                groupPredatorWarningDirection = Vector3.zero;
                groupWarningPredator = null;
            }
        }
        if (groupPredatorWarningCooldownTimer > 0f)
        {
            groupPredatorWarningCooldownTimer -= Time.fixedDeltaTime;
        }
        if (ignoredResourceTimer <= 0f)
        {
            temporarilyIgnoredFood = null;
            temporarilyIgnoredCarrion = null;
            ignoredResourceTimer = 0f;
        }

        if (senseTimer <= 0f)
        {
            senseTimer = senseInterval;
            SenseEnvironment();
        }

        UpdateSurvivalPriorityState();
        UpdateFoodMemoryTimers();
        DigestStomach();
        UpdateResourceSatisfactionState();
        UpdateAdvancedBehaviourTargets();
        UpdateAutonomousBrain();
        RunEvolvedMovement();
        DrainEnergy();
        TryEatFood();
        TryEatCarrion();
        TryBitePrey();
        ApplyMobbingPressureIfClose();
        UpdateHabitatMemory();
        TryMateAndLayEggs();
        UpdateMetrics();
        DrawRuntimeDebugRays();

        if (CurrentHealth <= 0f)
        {
            Die(false);
        }
    }

    // Skips or slows heavy checks when emergency performance mode is enabled
    private void ApplyEmergencyPerformanceSafeguards()
    {
        if (!EmergencyPerformanceMode)
        {
            return;
        }

        EmergencyFixedUpdateStride = Mathf.Clamp(EmergencyFixedUpdateStride, 1, 4);
        BrainDecisionInterval = Mathf.Max(BrainDecisionInterval, 1.15f);
        MinimumBehaviourHoldTime = Mathf.Max(MinimumBehaviourHoldTime, 1.75f);
        SocialScanInterval = Mathf.Max(SocialScanInterval, 0.75f);
        TerrainScanInterval = Mathf.Max(TerrainScanInterval, 0.35f);
        TerrainLookAhead = Mathf.Max(TerrainLookAhead, 5.5f);
        TerrainSideLookAhead = Mathf.Max(TerrainSideLookAhead, 4.25f);
        senseInterval = Mathf.Max(senseInterval, Random.Range(0.45f, 0.85f));
        MateTargetRefreshTime = Mathf.Max(MateTargetRefreshTime, 4.0f);
        KinematicOverlapIterations = Mathf.Clamp(KinematicOverlapIterations, 0, 1);
        LocalDebugRays = false;
        LocalDebugLabels = false;
    }

    // Boosts awareness at runtime so fish can react before hitting terrain or predators
    private void ApplyRuntimeAwarenessBoost()
    {
        if (EffectiveStats == null)
        {
            return;
        }

        float multiplier = Mathf.Max(1f, RuntimeVisionRangeMultiplier);
        float minimum = Mathf.Max(1f, MinimumRuntimeVisionRange);
        EffectiveStats.VisionRange = Mathf.Max(minimum, EffectiveStats.VisionRange * multiplier);
    }

    // Builds the visible morph model from the current genome
    private void ApplyMorphVisuals()
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return;
        }

        if (DisableLegacyPhenotypeVisuals)
        {
            RemoveLegacyPhenotypeVisuals();
        }

        CreatureMorphBuilder builder = GetComponent<CreatureMorphBuilder>();
        if (builder == null)
        {
            builder = gameObject.AddComponent<CreatureMorphBuilder>();
        }

        builder.MorphLibrary = MorphLibrary;
        builder.UseTypeColour = ApplyTypeColour;
        builder.Build(Candidate.Genome, EffectiveStats, CreatureDebugTypeUtility.GetTypeColour(Candidate.BehaviourType));
    }

    // Caches renderers once so colour changes do not search children every frame
    private void CacheRenderers(bool forceRefresh = false)
    {
        if (forceRefresh || cachedRenderers == null || cachedRenderers.Length == 0)
        {
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
        }

        if (materialBlock == null)
        {
            materialBlock = new MaterialPropertyBlock();
        }
    }

    // Applies the behaviour colour to the fish renderers
    private void ApplyDebugColour()
    {
        if (!ApplyTypeColour || cachedRenderers == null || Candidate == null)
        {
            return;
        }

        Color colour = CreatureDebugTypeUtility.GetTypeColour(Candidate.BehaviourType);
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer rendererToColour = cachedRenderers[i];
            if (rendererToColour == null)
            {
                continue;
            }

            rendererToColour.GetPropertyBlock(materialBlock);
            materialBlock.SetColor("_BaseColor", colour);
            materialBlock.SetColor("_Color", colour);
            rendererToColour.SetPropertyBlock(materialBlock);
        }
    }

    // Turns off visual part colliders so only the logic collider drives behaviour
    private void DisableBlockingCollidersOnVisuals()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null || colliders[i].gameObject == gameObject)
            {
                continue;
            }

            CreatureHurtbox hurtbox = colliders[i].GetComponent<CreatureHurtbox>();
            if (hurtbox != null)
            {
                colliders[i].isTrigger = true;
                colliders[i].enabled = true;
                continue;
            }

            colliders[i].enabled = false;
        }
    }

    // Creates the small root trigger used for spacing and selection
    private void EnsureRootLogicCollider()
    {
        SphereCollider rootCollider = GetComponent<SphereCollider>();
        if (rootCollider == null)
        {
            rootCollider = gameObject.AddComponent<SphereCollider>();
        }

        // If the prefab has an old large root collider as well as the managed logic collider,
        // disable the extras. Child visual colliders are already disabled elsewhere.
        Collider[] rootColliders = GetComponents<Collider>();
        for (int i = 0; i < rootColliders.Length; i++)
        {
            Collider c = rootColliders[i];
            if (c != null && c != rootCollider)
            {
                c.enabled = false;
            }
        }

        // The creature root transform is already scaled by EffectiveStats.BodySize.
        // Do not multiply the SphereCollider radius by body size again or large fish get a double-sized collider
        // and predators cannot get close enough to bite.
        float localRadius = GetSafeRootColliderRadius();
        rootCollider.enabled = true;
        rootCollider.isTrigger = true;
        rootCollider.center = Vector3.zero;
        rootCollider.radius = localRadius;

        if (rb != null)
        {
            rb.detectCollisions = true;
        }
    }

    // Disables old generated visuals so the morph builder controls the model
    private void RemoveLegacyPhenotypeVisuals()
    {
        CreaturePhenotypeVisuals legacyVisuals = GetComponent<CreaturePhenotypeVisuals>();
        if (legacyVisuals != null)
        {
            legacyVisuals.AutoCreateParts = false;
            legacyVisuals.enabled = false;
        }

        string[] legacyNames =
        {
            "Phenotype_Body",
            "Phenotype_Tail",
            "Phenotype_LeftFin",
            "Phenotype_RightFin",
            "Phenotype_Mouth",
            "Phenotype_LeftSensor",
            "Phenotype_RightSensor"
        };

        for (int i = 0; i < legacyNames.Length; i++)
        {
            Transform child = transform.Find(legacyNames[i]);
            if (child == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    // Refreshes food, carrion, prey, mate and nearby creature targets on a timer
    private void SenseEnvironment()
    {
        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null)
        {
            return;
        }

        float senseRange = EffectiveStats != null ? EffectiveStats.VisionRange : Candidate.Genome.VisionRange;
        if (currentBrainMode == FishAutonomousBehaviourMode.Fleeing || survivalEmergencyTimer > 0f || CurrentHealth / Mathf.Max(0.01f, GetMaxHealth()) <= InjuredSurvivalHealthRatio)
        {
            senseRange *= Mathf.Max(1f, FleeThreatSenseMultiplier);
        }
        Vector3 mouth = GetMouthWorldPosition();

        int comfortableFeeders = GetComfortableFeederLimit();
        float crowdPenalty = GetResourceCrowdPenalty();

        nearestFood = manager.GetBestFoodForCreature(this, mouth, senseRange, ResourceCrowdRadius, comfortableFeeders, crowdPenalty);
        nearestCarrion = manager.GetBestCarrionForCreature(this, mouth, senseRange, ResourceCrowdRadius, comfortableFeeders, crowdPenalty);

        if (IsPredatorPlantBlockedRole() && !CanUsePlantAsPredatorEmergencyFallback())
        {
            nearestFood = null;
        }

        if (ignoredResourceTimer > 0f && nearestFood == temporarilyIgnoredFood)
        {
            nearestFood = null;
        }

        if (ignoredResourceTimer > 0f && nearestCarrion == temporarilyIgnoredCarrion)
        {
            nearestCarrion = null;
        }

        ApplyFreshKillCarrionPriority();

        nearestCreature = manager.GetNearestCreature(this, transform.position, senseRange);
        nearestPrey = manager.GetNearestPrey(this, transform.position, senseRange);
        ProcessEarlyGroupPredatorWarning(manager, senseRange);
        if (focusedPrey != null && hunterPreyFocusTimer > 0f && CanAttackPrey(focusedPrey))
        {
            nearestPrey = focusedPrey;
        }
        else if (nearestPrey != null && IsCommittedPredatorForTargeting())
        {
            FocusPrey(nearestPrey, HunterPreyFocusTime);
        }

        SuppressPlantTargetsWhileHunting();

        if (nearestFood != null && !nearestFood.IsConsumed)
        {
            retainedFood = nearestFood;
            retainedTargetTimer = TargetRetainTime;
            RememberFoodArea(nearestFood.transform.position, false);
        }

        if (nearestCarrion != null && !nearestCarrion.IsConsumed)
        {
            retainedCarrion = nearestCarrion;
            retainedTargetTimer = TargetRetainTime;
            RememberFoodArea(nearestCarrion.transform.position, false);
        }

        if (nearestPrey != null && CanAttackPrey(nearestPrey))
        {
            retainedPrey = nearestPrey;
            retainedTargetTimer = TargetRetainTime;
        }

        UpdateMateTarget();

        if (retainedTargetTimer <= 0f)
        {
            retainedFood = null;
            retainedCarrion = null;
            retainedPrey = null;
        }
    }

    // Checks if energy and stomach are low enough to risk desperate choices
    private bool IsCriticallyStarving()
    {
        return GetEffectiveEnergyRatio() <= CriticalStarvationEnergyRatio && GetStomachFullness01() <= CriticalStarvationStomachRatio;
    }

    // Checks for direct danger like recent attacks, close predators or harsh current
    private bool HasImmediateSurvivalThreat()
    {
        return lastThreatCount > 0
            || survivalRecentDamageTimer > 0f
            || mobbingPressureTimer > 0f
            || HasCloseVisiblePredatorThreat()
            || lastCurrentStress >= CurrentEscapeStressThreshold;
    }

    // Blocks normal food eating while chased, unless the fish is starving
    private bool ShouldIgnoreStaticFoodBecauseThreatened()
    {
        if (!IgnoreStaticFoodWhileThreatened)
        {
            return false;
        }

        bool threatened = currentBrainMode == FishAutonomousBehaviourMode.Fleeing
            || survivalEmergencyTimer > 0f
            || lastThreatCount > 0
            || survivalRecentDamageTimer > 0f
            || HasCloseVisiblePredatorThreat()
            || threatFoodSuppressionTimer > 0f;

        if (!threatened)
        {
            return false;
        }

        // If starvation is the immediate killer, risky food is still allowed.
        return !IsCriticallyStarving();
    }

    // Chooses fleeing, feeding or resting when the fish is in danger of dying
    private bool TryPickUrgentSurvivalMode(out FishAutonomousBehaviourMode mode)
    {
        mode = currentBrainMode;
        if (!UseUrgentSurvivalPriority || Candidate == null || Candidate.Genome == null)
        {
            return false;
        }

        float healthRatio = GetHealthRatio();
        bool injured = healthRatio <= InjuredSurvivalHealthRatio;
        bool starving = IsCriticallyStarving();
        bool immediateThreat = HasImmediateSurvivalThreat();

        if (!injured && !starving)
        {
            return false;
        }

        if (Candidate != null)
        {
            Candidate.SurvivalEmergencyTime += Time.fixedDeltaTime;
        }

        // If starvation is about to kill the fish, it takes risky food even if the area is not ideal.
        if (starving && (nearestFood != null || nearestCarrion != null || hasFoodMemory || ShouldContinueEatingFreshKillCarrion()))
        {
            mode = HasCloseFoodTarget() ? FishAutonomousBehaviourMode.Feeding : FishAutonomousBehaviourMode.Foraging;
            brainReason = immediateThreat ? "starving: taking risky food" : "starving: emergency food";
            return true;
        }

        if (immediateThreat)
        {
            threatFoodSuppressionTimer = Mathf.Max(threatFoodSuppressionTimer, ThreatFoodSuppressionMemoryTime);
            RefreshSafeRestArea();
            mode = FishAutonomousBehaviourMode.Fleeing;
            brainReason = "survival priority: flee";
            return true;
        }

        if (injured)
        {
            RefreshSafeRestArea();
            mode = ShouldSleepToHeal() ? FishAutonomousBehaviourMode.Sleeping : FishAutonomousBehaviourMode.Resting;
            brainReason = hasSafeRestArea ? "survival priority: safe rest" : "survival priority: rest";
            return true;
        }

        return false;
    }

    // Forces survival priorities into the brain scores during emergency states
    private void ApplyUrgentSurvivalDesires(ref float foodDesire, ref float huntDesire, ref float mateDesire, ref float schoolDesire, ref float exploreDesire, ref float homeDesire, ref float fleeDesire, ref float restDesire)
    {
        if (!UseUrgentSurvivalPriority)
        {
            return;
        }

        bool injured = GetHealthRatio() <= InjuredSurvivalHealthRatio;
        bool starving = IsCriticallyStarving();
        if (!injured && !starving)
        {
            return;
        }

        mateDesire = 0f;
        schoolDesire *= starving ? 0.25f : 0.10f;
        exploreDesire *= 0.15f;
        huntDesire *= starving ? 0.65f : 0.10f;

        if (starving)
        {
            foodDesire = Mathf.Clamp01(foodDesire + SurvivalFoodDesireBoost);
        }

        if ((injured && HasImmediateSurvivalThreat()) || (groupPredatorWarningTimer > 0f && !starving))
        {
            fleeDesire = Mathf.Clamp01(fleeDesire + SurvivalFleeDesireBoost);
            homeDesire = Mathf.Clamp01(homeDesire + 0.35f);
        }
        else if (injured)
        {
            restDesire = Mathf.Clamp01(restDesire + SurvivalRestDesireBoost);
            homeDesire = Mathf.Clamp01(homeDesire + 0.25f);
        }
    }

    // Looks for a safer nearby rest point when injured fish need to recover
    private void RefreshSafeRestArea()
    {
        if (safeRestAreaTimer > 0f && hasSafeRestArea)
        {
            return;
        }

        Vector3 origin = transform.position;
        Vector3 away = Vector3.zero;
        if (dangerMemoryTimer > 0f)
        {
            away += origin - rememberedDangerArea;
        }
        if (nearestCreature != null && IsPredatorThreateningThis(nearestCreature, ClosePredatorThreatRange, ClosePredatorThreatHalfAngle, true, false))
        {
            away += origin - nearestCreature.transform.position;
        }
        if (lastCurrentStress >= CurrentEscapeStressThreshold)
        {
            away += currentEscapeDirection.sqrMagnitude > 0.01f ? currentEscapeDirection : -lastCurrentFlow.normalized;
        }
        if (away.sqrMagnitude <= 0.01f && hasHomeArea)
        {
            away = homeArea - origin;
        }
        if (away.sqrMagnitude <= 0.01f)
        {
            away = GetFallbackSideDirection();
        }

        Vector3 candidate = origin + away.normalized * Mathf.Max(2f, SafeRestSearchRadius);
        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager != null)
        {
            candidate = manager.ClampToSimulationArea(candidate);
            candidate = manager.ProjectPointAboveTerrain(candidate, manager.FishTerrainClearance);
        }

        safeRestArea = candidate;
        hasSafeRestArea = true;
        safeRestAreaTimer = SafeRestMemoryTime;
    }

    // Scores feeding, hunting, fleeing, mating, resting and exploring needs
    private void UpdateAutonomousBrain()
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return;
        }

        brainDecisionTimer -= Time.fixedDeltaTime;
        behaviourHoldTimer -= Time.fixedDeltaTime;

        if (brainDecisionTimer > 0f && behaviourHoldTimer > 0f && !ShouldUseSurvivalFleePriority())
        {
            return;
        }

        brainDecisionTimer = Mathf.Max(0.05f, BrainDecisionInterval) * Random.Range(0.75f, 1.25f);

        float energyRatio = GetEffectiveEnergyRatio();
        float healthRatio = GetHealthRatio();
        float stomachRatio = GetStomachFullness01();
        float hungerPressure = GetHungerPressure();
        float lowHealthNeed = Mathf.Clamp01(1f - healthRatio);
        float stomachNeed = Mathf.Clamp01(1f - stomachRatio);
        float mature01 = IsMatureForMating() ? 1f : 0f;
        bool closeFood = HasCloseFoodTarget();
        bool hasStaticFood = GetPrimaryStaticFoodTargetPosition().HasValue;
        bool canHunt = (nearestPrey != null && CanAttackPrey(nearestPrey)) || (focusedPrey != null && hunterPreyFocusTimer > 0f && CanAttackPrey(focusedPrey));
        bool hasThreat = CountCurrentThreatsForBrain() > 0 || mobbingPressureTimer > 0f || groupPredatorWarningTimer > 0f || survivalRecentDamageTimer > 0f || HasCloseVisiblePredatorThreat();
        EvaluateEvolvedBrain(energyRatio, healthRatio, stomachRatio, hungerPressure);

        brainFoodDesire = Mathf.Clamp01(
            hungerPressure * 0.62f
            + stomachNeed * Candidate.Genome.HungerDrive * 0.34f
            + lowHealthNeed * HealthNeedWeight * 0.38f
            + (hasFoodMemory && !rememberedFoodWasBad ? Candidate.Genome.FoodMemoryStrength * 0.14f : 0f));

        if (ShouldLeaveCurrentResource)
        {
            brainFoodDesire *= 0.25f;
        }

        if (hasStaticFood && closeFood && hungerPressure > 0.05f)
        {
            brainFoodDesire = Mathf.Clamp01(brainFoodDesire + FeedingDecisionStickiness);
        }
        if (ShouldContinueEatingFreshKillCarrion())
        {
            brainFoodDesire = Mathf.Clamp01(brainFoodDesire + FreshKillFeedingBias * 0.25f);
        }

        float ambushPotential = EnableAdvancedFishBehaviours && hasAmbushAnchor
            ? Mathf.Clamp01(GetAmbushScore01() * (0.45f + hungerPressure * 0.65f + Candidate.Genome.Stealth * 0.20f))
            : 0f;
        brainHuntDesire = canHunt
            ? Mathf.Clamp01(Candidate.Genome.MeatDiet * 0.48f + Candidate.Genome.Aggression * 0.38f + hungerPressure * 0.32f + lowHealthNeed * 0.14f)
            : ambushPotential;
        if (hunterPreyFocusTimer > 0f && focusedPrey != null)
        {
            brainHuntDesire = Mathf.Clamp01(brainHuntDesire + 0.28f);
            brainFoodDesire *= Mathf.Lerp(0.35f, 0.78f, GetEffectiveEnergyRatio());
        }

        brainMateDesire = ShouldSeekMate()
            ? Mathf.Clamp01(Candidate.Genome.MateDrive * 0.45f + mature01 * 0.25f + energyRatio * 0.18f + stomachRatio * 0.18f - hungerPressure * 0.55f)
            : 0f;

        bool isFemale = Candidate.Genome.SexGene >= 0.5f;
        brainNestingDesire = isFemale && IsMatureForMating() && HasMatingEnergy()
            ? Mathf.Clamp01(Candidate.Genome.NestingDrive * 0.35f + Candidate.Genome.EggProtection * 0.25f + brainMateDesire * 0.35f - hungerPressure * 0.45f)
            : 0f;

        brainFleeDesire = hasThreat
            ? Mathf.Clamp01((1f - Candidate.Genome.RiskTolerance) * 0.45f + (1f - Candidate.Genome.Bravery) * 0.28f + lowHealthNeed * 0.35f)
            : 0f;

        float fedEnoughForSocial = Mathf.Clamp01(energyRatio * 0.55f + stomachRatio * 0.45f);

        brainSchoolDesire = Mathf.Clamp01(
            Candidate.Genome.GroupingChance * 0.35f
            + Candidate.Genome.SchoolTightness * 0.28f
            + Candidate.Genome.FoodSharing * 0.22f
            + (1f - Candidate.Genome.Selfishness) * 0.22f
            + fedEnoughForSocial * 0.12f
            - brainFoodDesire * 0.18f
            - brainMateDesire * 0.10f);

        brainHomeDesire = Mathf.Clamp01((hasHomeArea ? homeConfidence : 0.15f) * 0.42f + (1f - Candidate.Genome.ExplorationDrive) * 0.35f + healthRatio * 0.15f - hungerPressure * 0.42f);
        brainExploreDesire = Mathf.Clamp01(Candidate.Genome.ExplorationDrive * ExplorationBehaviourWeight * (0.35f + Candidate.Genome.Bravery * 0.65f) + fedEnoughForSocial * 0.14f - hungerPressure * 0.28f - brainMateDesire * 0.12f);
        brainRestDesire = Mathf.Clamp01(healthRatio * 0.24f + stomachRatio * 0.32f + energyRatio * 0.24f + brainHomeDesire * 0.20f - Candidate.Genome.ActivityCycle * 0.18f - hungerPressure * 0.45f);

        if (IsPeacefulGrazerRole())
        {
            brainHuntDesire = 0f;
            brainSchoolDesire = Mathf.Clamp01(brainSchoolDesire + 0.18f);
            brainFoodDesire = Mathf.Clamp01(brainFoodDesire + hungerPressure * 0.20f);
        }

        if (ShouldPredatorChillAfterFeeding())
        {
            brainHuntDesire = 0f;
            brainFoodDesire *= 0.18f;
            brainRestDesire = Mathf.Clamp01(brainRestDesire + 0.34f);
            brainExploreDesire = Mathf.Clamp01(brainExploreDesire + 0.10f);
            brainSchoolDesire = Mathf.Clamp01(brainSchoolDesire + Candidate.Genome.GroupingChance * 0.12f);
        }

        ApplyEvolvedBrainDecisionBiases(ref brainFoodDesire, ref brainHuntDesire, ref brainMateDesire, ref brainSchoolDesire, ref brainExploreDesire, ref brainHomeDesire, ref brainFleeDesire, ref brainRestDesire);
        ApplyUrgentSurvivalDesires(ref brainFoodDesire, ref brainHuntDesire, ref brainMateDesire, ref brainSchoolDesire, ref brainExploreDesire, ref brainHomeDesire, ref brainFleeDesire, ref brainRestDesire);
        ApplySurvivalPriorityDecisionOverride(ref brainFoodDesire, ref brainHuntDesire, ref brainMateDesire, ref brainSchoolDesire, ref brainExploreDesire, ref brainHomeDesire, ref brainFleeDesire, ref brainRestDesire);

        FishAutonomousBehaviourMode chosen = PickBrainMode();
        bool emergencySwitch = chosen == FishAutonomousBehaviourMode.Fleeing
            || chosen == FishAutonomousBehaviourMode.Feeding
            || chosen == FishAutonomousBehaviourMode.Hunting
            || chosen == FishAutonomousBehaviourMode.MobbingPredator
            || chosen == FishAutonomousBehaviourMode.GuardingEggs
            || currentBrainMode == FishAutonomousBehaviourMode.Recovering;
        if (behaviourHoldTimer > 0f && !emergencySwitch)
        {
            return;
        }

        SetBrainMode(chosen);
    }

    // Chooses the current high-level behaviour from the scored needs
    private FishAutonomousBehaviourMode PickBrainMode()
    {
        if (stuckEscapeTimer > 0f || lastEmergencyUnstick.sqrMagnitude > 0.35f)
        {
            brainReason = "recovering from crowding or terrain";
            return FishAutonomousBehaviourMode.Recovering;
        }

        if (UseCurrentEscapeSteering && currentEscapeTimer > 0f && (lastCurrentStress >= CurrentEscapeStressThreshold * 0.65f || lastTerrainAvoidance.sqrMagnitude > 0.05f))
        {
            brainReason = "escaping current or terrain trap";
            return FishAutonomousBehaviourMode.Recovering;
        }

        if (TryPickUrgentSurvivalMode(out FishAutonomousBehaviourMode urgentMode))
        {
            return urgentMode;
        }

        if (ShouldPredatorChillAfterFeeding())
        {
            brainReason = Candidate.Genome.GroupingChance > 0.48f && lastFriendlyCount > 0 ? "fed predator schooling" : "fed predator resting";
            return Candidate.Genome.GroupingChance > 0.48f && lastFriendlyCount > 0 ? FishAutonomousBehaviourMode.Schooling : FishAutonomousBehaviourMode.Resting;
        }

        if (ShouldUseSurvivalFleePriority())
        {
            brainReason = "critical survival override";
            return FishAutonomousBehaviourMode.Fleeing;
        }

        if (ShouldGuardEggs())
        {
            brainReason = currentEggThreat != null ? "guarding eggs from predator" : "guarding own eggs";
            return FishAutonomousBehaviourMode.GuardingEggs;
        }

        if (ShouldMobPredator())
        {
            brainReason = "brave group mobbing predator";
            return FishAutonomousBehaviourMode.MobbingPredator;
        }

        if (ShouldCourtMate())
        {
            brainReason = "courtship before mating";
            return FishAutonomousBehaviourMode.Courtship;
        }

        if (ShouldReactToGroupPredatorWarnings() && groupPredatorWarningTimer > 0f && !IsCriticallyStarving())
        {
            brainReason = "group predator warning";
            return FishAutonomousBehaviourMode.Fleeing;
        }

        if (brainFleeDesire > 0.52f && brainFleeDesire > brainFoodDesire * 0.85f)
        {
            brainReason = "close predator threat";
            return FishAutonomousBehaviourMode.Fleeing;
        }

        if (ShouldSleepToHeal())
        {
            brainReason = "sleeping to heal";
            return FishAutonomousBehaviourMode.Sleeping;
        }

        if (ShouldContinueEatingFreshKillCarrion())
        {
            brainReason = "feeding on own kill";
            return HasCloseFoodTarget() ? FishAutonomousBehaviourMode.Feeding : FishAutonomousBehaviourMode.Foraging;
        }

        if (hunterPreyFocusTimer > 0f && focusedPrey != null && CanAttackPrey(focusedPrey))
        {
            brainReason = "committed hunter finishing prey";
            nearestPrey = focusedPrey;
            return FishAutonomousBehaviourMode.Hunting;
        }

        if (brainHuntDesire > 0.48f && brainHuntDesire >= brainFoodDesire * 0.62f)
        {
            if (ShouldAmbushHunt())
            {
                brainReason = "stealth predator ambushing";
                return FishAutonomousBehaviourMode.Ambushing;
            }

            brainReason = "meat-biased hunting";
            return FishAutonomousBehaviourMode.Hunting;
        }

        if (brainFoodDesire > 0.50f && HasCloseFoodTarget())
        {
            brainReason = "close edible target";
            return FishAutonomousBehaviourMode.Feeding;
        }

        if (brainFoodDesire > 0.44f)
        {
            brainReason = "hunger and stomach need";
            return FishAutonomousBehaviourMode.Foraging;
        }

        if (brainMateDesire > 0.46f && brainMateDesire >= brainExploreDesire && brainMateDesire >= brainSchoolDesire * 0.75f)
        {
            brainReason = "mature and full enough";
            return FishAutonomousBehaviourMode.SeekingMate;
        }

        if (brainNestingDesire > 0.55f && brainNestingDesire >= brainExploreDesire)
        {
            brainReason = "safe nesting drive";
            return FishAutonomousBehaviourMode.Nesting;
        }

        if (currentHungryLeader != null && brainSchoolDesire > 0.35f && brainFoodDesire < 0.32f)
        {
            brainReason = "following hungry schoolmate";
            return FishAutonomousBehaviourMode.FollowingLeader;
        }

        if (brainSchoolDesire > 0.48f && lastFriendlyCount > 0 && brainSchoolDesire >= brainExploreDesire)
        {
            brainReason = "same-morph schooling";
            return FishAutonomousBehaviourMode.Schooling;
        }

        if (brainExploreDesire > brainRestDesire && brainExploreDesire > 0.28f)
        {
            brainReason = "curious roaming";
            return FishAutonomousBehaviourMode.Exploring;
        }

        brainReason = hasHomeArea ? "safe home cruising" : "low need cruising";
        return FishAutonomousBehaviourMode.Resting;
    }

    // Switches mode and sets the short hold timer so behaviour is not too twitchy
    private void SetBrainMode(FishAutonomousBehaviourMode nextMode)
    {
        if (nextMode != currentBrainMode)
        {
            previousBrainMode = currentBrainMode;
            currentBrainMode = nextMode;
            if (Candidate != null)
            {
                Candidate.BrainModeSwitches++;
            }
            float hold = MinimumBehaviourHoldTime;
            if (nextMode == FishAutonomousBehaviourMode.Feeding || nextMode == FishAutonomousBehaviourMode.Fleeing || nextMode == FishAutonomousBehaviourMode.Recovering)
            {
                hold *= 0.55f;
            }
            else if (nextMode == FishAutonomousBehaviourMode.Sleeping)
            {
                hold *= 1.65f;
            }
            else if (nextMode == FishAutonomousBehaviourMode.Resting || nextMode == FishAutonomousBehaviourMode.Exploring)
            {
                hold *= 1.35f;
            }
            else if (nextMode == FishAutonomousBehaviourMode.Ambushing || nextMode == FishAutonomousBehaviourMode.GuardingEggs)
            {
                hold *= 1.55f;
            }
            else if (nextMode == FishAutonomousBehaviourMode.Courtship)
            {
                hold *= 0.95f;
                courtshipTimer = Mathf.Max(courtshipTimer, CourtshipDuration);
            }

            if (nextMode == FishAutonomousBehaviourMode.Fleeing)
            {
                threatFoodSuppressionTimer = Mathf.Max(threatFoodSuppressionTimer, ThreatFoodSuppressionMemoryTime);
                StartCStartEscapeBurst();
                if (Candidate != null && UseUrgentSurvivalPriority && GetHealthRatio() <= InjuredSurvivalHealthRatio)
                {
                    Candidate.SurvivalEmergencyActivations++;
                }
            }

            behaviourHoldTimer = Mathf.Max(0.15f, hold * Random.Range(0.75f, 1.25f));
        }

        brainWantsFood = currentBrainMode == FishAutonomousBehaviourMode.Foraging
            || currentBrainMode == FishAutonomousBehaviourMode.Feeding
            || currentBrainMode == FishAutonomousBehaviourMode.Hunting
            || currentBrainMode == FishAutonomousBehaviourMode.Ambushing;
        brainWantsMate = currentBrainMode == FishAutonomousBehaviourMode.SeekingMate || currentBrainMode == FishAutonomousBehaviourMode.Courtship;
        brainWantsHunt = currentBrainMode == FishAutonomousBehaviourMode.Hunting || currentBrainMode == FishAutonomousBehaviourMode.Ambushing;
        brainWantsFlee = currentBrainMode == FishAutonomousBehaviourMode.Fleeing || survivalEmergencyTimer > 0f;
    }

    // Updates survival priority state using the current sim state
    private void UpdateSurvivalPriorityState()
    {
        if (!UseSurvivalPriority || Candidate == null || Candidate.Genome == null)
        {
            survivalThreatTarget = null;
            return;
        }

        float healthRatio = GetHealthRatio();
        float energyRatio = GetEffectiveEnergyRatio();
        float stomachRatio = GetStomachFullness01();
        bool criticalHealth = healthRatio <= SurvivalCriticalHealthRatio;
        bool warningHealthAfterDamage = healthRatio <= SurvivalWarningHealthRatio && survivalRecentDamageTimer > 0f;
        bool starvingAndWeak = energyRatio <= SurvivalCriticalEnergyRatio && stomachRatio <= SurvivalCriticalStomachRatio && healthRatio <= SurvivalWarningHealthRatio;
        bool harshCurrent = UseCurrentEscapeSteering && (lastCurrentStress >= CurrentEscapeStressThreshold || currentEscapeTimer > 0f);

        MarineCreatureAgent threat = FindImmediateSurvivalThreat();
        bool groupWarningApplies = ShouldReactToGroupPredatorWarnings() && groupPredatorWarningTimer > 0f;
        bool shouldFlee = (criticalHealth || warningHealthAfterDamage || starvingAndWeak) && (threat != null || harshCurrent || lastThreatCount > 0 || groupWarningApplies || survivalRecentDamageTimer > 0f);

        if (!shouldFlee)
        {
            if (survivalEmergencyTimer <= 0f)
            {
                survivalThreatTarget = null;
                survivalEscapeDirection = Vector3.zero;
            }
            return;
        }

        survivalThreatTarget = threat;
        Vector3 escape = BuildSurvivalEscapeDirection(threat, harshCurrent);
        if (escape.sqrMagnitude <= 0.001f)
        {
            escape = GetFallbackSideDirection();
        }

        bool newlyActivated = survivalEmergencyTimer <= 0f;
        survivalEscapeDirection = escape.normalized;
        survivalEmergencyTimer = Mathf.Max(survivalEmergencyTimer, SurvivalModeLockTime);
        brainWantsFlee = true;

        if (newlyActivated && Candidate != null)
        {
            Candidate.SurvivalEmergencyActivations++;
        }
    }

    // Finds immediate survival threat by checking the current options
    private MarineCreatureAgent FindImmediateSurvivalThreat()
    {
        if (survivalThreatTarget != null && survivalThreatTarget.CurrentHealth > 0f)
        {
            float keepRadius = Mathf.Max(2f, SurvivalFleeThreatRadius * 1.35f);
            if ((survivalThreatTarget.transform.position - transform.position).sqrMagnitude <= keepRadius * keepRadius)
            {
                return survivalThreatTarget;
            }
        }

        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null)
        {
            return null;
        }

        List<MarineCreatureAgent> nearby = manager.GetNearbyCreatures(transform.position, Mathf.Max(2f, SurvivalFleeThreatRadius));
        MarineCreatureAgent best = null;
        float bestScore = 0f;
        for (int i = 0; i < nearby.Count; i++)
        {
            MarineCreatureAgent other = nearby[i];
            if (other == null || other == this || other.CurrentHealth <= 0f || !IsPredatorThreateningThis(other, ClosePredatorThreatRange, ClosePredatorThreatHalfAngle, true, false))
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, other.transform.position);
            float distanceScore = 1f - Mathf.Clamp01(distance / Mathf.Max(0.1f, SurvivalFleeThreatRadius));
            float predatorScore = 0.35f;
            if (other.Candidate != null && other.Candidate.Genome != null)
            {
                predatorScore += other.Candidate.Genome.MeatDiet * 0.35f + other.Candidate.Genome.Aggression * 0.25f;
            }

            if (other.EffectiveStats != null)
            {
                predatorScore += Mathf.Clamp01(other.EffectiveStats.BiteDamage / 20f) * 0.25f;
            }

            float score = distanceScore + predatorScore;
            if (score > bestScore)
            {
                bestScore = score;
                best = other;
            }
        }

        return best;
    }

    // Builds the survival escape direction data from the current values
    private Vector3 BuildSurvivalEscapeDirection(MarineCreatureAgent threat, bool harshCurrent)
    {
        Vector3 escape = Vector3.zero;
        Vector3 position = transform.position;

        bool terrainBlocked = UseFleeTerrainSafety && lastTerrainAvoidance.sqrMagnitude > 0.05f;

        if (threat != null)
        {
            Vector3 awayFromThreat = position - threat.transform.position;
            awayFromThreat.y *= 0.18f;
            if (awayFromThreat.sqrMagnitude > 0.001f)
            {
                float threatWeight = terrainBlocked && AllowEscapeTowardPredatorWhenTerrainBlocked ? TerrainBlockedThreatEscapeDamping : 1.65f;
                escape += awayFromThreat.normalized * threatWeight;
            }
        }

        if (groupPredatorWarningTimer > 0f && groupPredatorWarningDirection.sqrMagnitude > 0.001f)
        {
            escape += groupPredatorWarningDirection.normalized * 1.05f;
        }

        if (terrainBlocked)
        {
            Vector3 terrainSafe = lastTerrainAvoidance;
            terrainSafe.y = Mathf.Max(0f, terrainSafe.y);
            if (terrainSafe.sqrMagnitude > 0.001f)
            {
                escape += terrainSafe.normalized * TerrainBlockedSafePathWeight;
            }
        }

        if (harshCurrent)
        {
            if (currentEscapeDirection.sqrMagnitude > 0.001f)
            {
                escape += currentEscapeDirection.normalized * 1.25f;
            }
            if (lastCurrentFlow.sqrMagnitude > 0.001f)
            {
                escape += -lastCurrentFlow.normalized * 0.55f;
            }
        }

        if (dangerMemoryTimer > 0f)
        {
            Vector3 away = position - rememberedDangerArea;
            if (away.sqrMagnitude > 0.001f)
            {
                escape += away.normalized * 0.8f;
            }
        }

        Vector3 centre = GetDirectionToSimulationCentre();
        if (centre.sqrMagnitude > 0.001f)
        {
            centre.y *= 0.35f;
            escape += centre.normalized * 0.55f;
        }

        if (escape.sqrMagnitude <= 0.001f)
        {
            escape = currentVelocity.sqrMagnitude > 0.01f ? -currentVelocity.normalized : GetFallbackSideDirection();
        }

        return escape;
    }

    // Checks if it should use survival flee priority right now
    private bool ShouldUseSurvivalFleePriority()
    {
        return UseSurvivalPriority && survivalEmergencyTimer > 0f && survivalEscapeDirection.sqrMagnitude > 0.001f;
    }

    private void ApplySurvivalPriorityDecisionOverride(
        ref float foodDesire,
        ref float huntDesire,
        ref float mateDesire,
        ref float schoolDesire,
        ref float exploreDesire,
        ref float homeDesire,
        ref float fleeDesire,
        ref float restDesire)
    {
        if (!UseSurvivalPriority || !SurvivalSuppressNonEssentialBehaviour)
        {
            return;
        }

        float energyRatio = GetEffectiveEnergyRatio();
        float stomachRatio = GetStomachFullness01();
        bool starving = energyRatio <= SurvivalCriticalEnergyRatio && stomachRatio <= SurvivalCriticalStomachRatio;

        if (ShouldUseSurvivalFleePriority())
        {
            fleeDesire = 1f;
            huntDesire *= 0.05f;
            mateDesire = 0f;
            schoolDesire *= 0.15f;
            exploreDesire = 0f;
            restDesire = 0f;
            foodDesire *= starving ? 0.35f : 0.05f;
            return;
        }

        if (starving && GetHealthRatio() <= SurvivalWarningHealthRatio)
        {
            foodDesire = Mathf.Max(foodDesire, 0.95f);
            huntDesire *= 0.25f;
            mateDesire = 0f;
            schoolDesire *= 0.35f;
            exploreDesire *= 0.25f;
            restDesire *= 0.20f;
        }
    }


    // Updates advanced behaviour targets using the current sim state
    private void UpdateAdvancedBehaviourTargets()
    {
        if (!EnableAdvancedFishBehaviours || Candidate == null || Candidate.Genome == null)
        {
            return;
        }

        if (mobbingPressureTimer <= 0f)
        {
            mobbingPressureDirection = Vector3.zero;
        }

        if (eggGuardScanTimer <= 0f)
        {
            eggGuardScanTimer = Mathf.Max(0.2f, EggGuardScanInterval) * Random.Range(0.85f, 1.15f);
            RefreshEggGuardTarget();
            RefreshMobbingTarget();
        }

        if (ambushAnchorTimer <= 0f || !hasAmbushAnchor)
        {
            ambushAnchorTimer = Mathf.Max(0.5f, AmbushAnchorRefreshTime) * Random.Range(0.75f, 1.25f);
            RefreshAmbushAnchor();
        }
    }

    // Handles refresh egg guard target
    private void RefreshEggGuardTarget()
    {
        guardedEggCluster = null;
        currentEggThreat = null;

        if (!CanCareForEggs())
        {
            return;
        }

        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null)
        {
            return;
        }

        List<FishEggCluster> eggs = manager.GetActiveEggClusters();
        if (eggs == null || eggs.Count == 0)
        {
            return;
        }

        float bestScore = float.NegativeInfinity;
        Vector3 position = transform.position;
        float maxRangeSqr = EggGuardSearchRadius * EggGuardSearchRadius;

        for (int i = 0; i < eggs.Count; i++)
        {
            FishEggCluster egg = eggs[i];
            if (egg == null || !egg.IsParentOrGuardian(this))
            {
                continue;
            }

            float sqr = (egg.transform.position - position).sqrMagnitude;
            if (sqr > maxRangeSqr)
            {
                continue;
            }

            float threatScore = FindThreatNearEgg(egg, out MarineCreatureAgent threat);
            float closeness = 1f - Mathf.Clamp01(Mathf.Sqrt(sqr) / Mathf.Max(0.01f, EggGuardSearchRadius));
            float score = threatScore * 2.0f + closeness + Candidate.Genome.EggProtection * 1.5f + Candidate.Genome.Territoriality;
            if (score > bestScore)
            {
                bestScore = score;
                guardedEggCluster = egg;
                currentEggThreat = threat;
            }
        }
    }

    // Finds threat near egg by checking the current options
    private float FindThreatNearEgg(FishEggCluster egg, out MarineCreatureAgent threat)
    {
        threat = null;
        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null || egg == null)
        {
            return 0f;
        }

        float bestScore = 0f;
        float radius = Mathf.Max(egg.ProtectionRadius, EggGuardSearchRadius * 0.55f);
        float radiusSqr = radius * radius;
        Vector3 eggPosition = egg.transform.position;
        List<MarineCreatureAgent> creatures = manager.GetNearbyCreatures(eggPosition, radius);

        for (int i = 0; i < creatures.Count; i++)
        {
            MarineCreatureAgent other = creatures[i];
            if (other == null || other == this || egg.IsParentOrGuardian(other))
            {
                continue;
            }

            float sqr = (other.transform.position - eggPosition).sqrMagnitude;
            if (sqr > radiusSqr)
            {
                continue;
            }

            float predatorScore = other.GetPredatorDrive01();
            if (predatorScore < 0.28f)
            {
                continue;
            }

            float closeness = 1f - Mathf.Clamp01(Mathf.Sqrt(sqr) / Mathf.Max(0.01f, radius));
            float score = predatorScore + closeness;
            if (score > bestScore)
            {
                bestScore = score;
                threat = other;
            }
        }

        return bestScore;
    }

    // Handles refresh mobbing target
    private void RefreshMobbingTarget()
    {
        currentMobbingTarget = null;
        if (!CanMobPredators())
        {
            return;
        }

        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null)
        {
            return;
        }

        float bestScore = 0f;
        Vector3 position = transform.position;
        float maxRangeSqr = MobbingSearchRadius * MobbingSearchRadius;
        List<MarineCreatureAgent> creatures = manager.GetNearbyCreatures(position, MobbingSearchRadius);

        for (int i = 0; i < creatures.Count; i++)
        {
            MarineCreatureAgent other = creatures[i];
            if (other == null || other == this)
            {
                continue;
            }

            float sqr = (other.transform.position - position).sqrMagnitude;
            if (sqr > maxRangeSqr)
            {
                continue;
            }

            float predatorScore = other.GetPredatorDrive01();
            if (predatorScore < 0.45f)
            {
                continue;
            }

            bool directThreat = other.CanAttackPrey(this);
            bool eggThreat = guardedEggCluster != null && currentEggThreat == other;
            if (!directThreat && !eggThreat && predatorScore < 0.68f)
            {
                continue;
            }

            float closeness = 1f - Mathf.Clamp01(Mathf.Sqrt(sqr) / Mathf.Max(0.01f, MobbingSearchRadius));
            float score = predatorScore + closeness + (directThreat ? 0.45f : 0f) + (eggThreat ? 0.35f : 0f);
            if (score > bestScore)
            {
                bestScore = score;
                currentMobbingTarget = other;
            }
        }
    }

    // Handles refresh ambush anchor
    private void RefreshAmbushAnchor()
    {
        Vector3? anchor = null;

        if (nearestFood != null && !nearestFood.IsConsumed)
        {
            anchor = nearestFood.transform.position;
        }
        else if (nearestCarrion != null && !nearestCarrion.IsConsumed)
        {
            anchor = nearestCarrion.transform.position;
        }
        else if (hasFoodMemory && !rememberedFoodWasBad)
        {
            anchor = rememberedFoodArea;
        }
        else if (hasHomeArea)
        {
            anchor = homeArea;
        }

        if (anchor.HasValue)
        {
            ambushAnchor = anchor.Value;
            hasAmbushAnchor = true;
        }
        else
        {
            ambushAnchor = transform.position;
            hasAmbushAnchor = false;
        }
    }

    // Checks if it should guard eggs right now
    private bool ShouldGuardEggs()
    {
        if (!EnableAdvancedFishBehaviours || guardedEggCluster == null || Candidate == null || Candidate.Genome == null)
        {
            return false;
        }

        if (!CanCareForEggs())
        {
            return false;
        }

        if (GetHungerPressure() > 0.76f && currentEggThreat == null)
        {
            return false;
        }

        float score = Candidate.Genome.EggProtection * 0.55f + Candidate.Genome.Territoriality * 0.30f + Candidate.Genome.Bravery * 0.15f;
        return score >= EggGuardBehaviourThreshold;
    }

    // Checks if it should mob predator right now
    private bool ShouldMobPredator()
    {
        if (!EnableAdvancedFishBehaviours || currentMobbingTarget == null || Candidate == null || Candidate.Genome == null)
        {
            return false;
        }

        if (!CanMobPredators())
        {
            return false;
        }

        if (brainFoodDesire > 0.62f || GetHealthRatio() < 0.45f)
        {
            return false;
        }

        if (RequireGroupForMobbing && CountNearbyMobbingAllies(currentMobbingTarget) < MinimumNearbyAlliesToMob)
        {
            return false;
        }

        return currentMobbingTarget.GetPredatorDrive01() > 0.42f;
    }

    // Counts nearby mobbing allies from nearby/current data
    private int CountNearbyMobbingAllies(MarineCreatureAgent predator)
    {
        if (predator == null || EvolutionEcosystemManager.Instance == null || Candidate == null || Candidate.Genome == null)
        {
            return 0;
        }

        int count = 0;
        List<MarineCreatureAgent> nearby = EvolutionEcosystemManager.Instance.GetNearbyCreatures(transform.position, MobbingAllySupportRadius);
        float radiusSqr = MobbingAllySupportRadius * MobbingAllySupportRadius;
        for (int i = 0; i < nearby.Count; i++)
        {
            MarineCreatureAgent ally = nearby[i];
            if (ally == null || ally == this || ally == predator || ally.Candidate == null || ally.Candidate.Genome == null)
            {
                continue;
            }

            if (!IsFriendlyByMorph(ally))
            {
                continue;
            }

            if ((ally.transform.position - transform.position).sqrMagnitude > radiusSqr)
            {
                continue;
            }

            if (ally.GetHealthRatio() < 0.35f || ally.GetPredatorDrive01() > 0.55f)
            {
                continue;
            }

            count++;
        }
        return count;
    }

    // Checks if it should court mate right now
    private bool ShouldCourtMate()
    {
        if (!EnableAdvancedFishBehaviours || currentMateTarget == null || Candidate == null || Candidate.Genome == null)
        {
            return false;
        }

        if (!ShouldSeekMate() || !currentMateTarget.HasMatingEnergy())
        {
            return false;
        }

        float distance = Vector3.Distance(transform.position, currentMateTarget.transform.position);
        return distance <= Mathf.Max(CourtshipStartDistance, MatePairDistance * 1.4f) && brainMateDesire > 0.35f && brainFoodDesire < 0.55f;
    }

    // Checks if it should ambush hunt right now
    private bool ShouldAmbushHunt()
    {
        if (!EnableAdvancedFishBehaviours || Candidate == null || Candidate.Genome == null)
        {
            return false;
        }

        if (currentEscapeTimer > 0f || lastCurrentStress >= CurrentEscapeStressThreshold * 0.65f || lastTerrainAvoidance.sqrMagnitude > 0.08f)
        {
            return false;
        }

        if (nearestPrey != null && CanAttackPrey(nearestPrey))
        {
            float preyDistance = Vector3.Distance(transform.position, nearestPrey.transform.position);
            return preyDistance > AmbushStrikeRange;
        }

        if (GetHungerPressure() > 0.86f)
        {
            return false;
        }

        float score = GetAmbushScore01();
        return score >= AmbushBehaviourThreshold && hasAmbushAnchor;
    }

    // Checks if it can care for eggs with the current state
    private bool CanCareForEggs()
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return false;
        }

        return Candidate.Genome.EggProtection * 0.65f + Candidate.Genome.NestingDrive * 0.25f + Candidate.Genome.Territoriality * 0.25f >= EggGuardBehaviourThreshold;
    }

    // Checks if it can mob predators with the current state
    private bool CanMobPredators()
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return false;
        }

        float predatorDrive = GetPredatorDrive01();
        if (predatorDrive > 0.55f)
        {
            return false;
        }

        float socialCourage = Candidate.Genome.GroupingChance * 0.32f + Candidate.Genome.SchoolTightness * 0.24f + Candidate.Genome.Bravery * 0.28f + Candidate.Genome.RiskTolerance * 0.16f;
        return socialCourage >= MobbingBehaviourThreshold;
    }

    // Gets the predator drive01 used by the sim
    private float GetPredatorDrive01()
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return 0f;
        }

        float meatLead = Mathf.Clamp01((Candidate.Genome.MeatDiet - Mathf.Max(Candidate.Genome.PlantDiet, Candidate.Genome.CarrionDiet) + 0.18f) / 0.55f);
        return Mathf.Clamp01(Candidate.Genome.MeatDiet * 0.46f + Candidate.Genome.Aggression * 0.30f + Candidate.Genome.JawSize * 0.05f + Candidate.Genome.RiskTolerance * 0.03f + meatLead * 0.20f);
    }

    // Gets the ambush score01 used by the sim
    private float GetAmbushScore01()
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return 0f;
        }

        float lowCruisePenalty = Mathf.InverseLerp(7.5f, 3.5f, EffectiveStats != null ? EffectiveStats.Speed : Candidate.Genome.Speed) * 0.12f;
        return Mathf.Clamp01(Candidate.Genome.MeatDiet * 0.38f + Candidate.Genome.Aggression * 0.24f + Candidate.Genome.Stealth * 0.28f + Candidate.Genome.Territoriality * 0.18f + lowCruisePenalty);
    }

    // Gets the advanced behaviour pull used by the sim
    private Vector3 GetAdvancedBehaviourPull(float hungerPressure)
    {
        if (!EnableAdvancedFishBehaviours)
        {
            return Vector3.zero;
        }

        Vector3 pull = Vector3.zero;

        if (currentBrainMode == FishAutonomousBehaviourMode.Fleeing && cStartBurstTimer > 0f && cStartBurstDirection.sqrMagnitude > 0.001f)
        {
            float t = Mathf.Clamp01(cStartBurstTimer / Mathf.Max(0.01f, CStartBurstDuration));
            pull += cStartBurstDirection.normalized * CStartBurstWeight * Mathf.Lerp(0.35f, 1f, t);
        }

        if (survivalEmergencyTimer > 0f && survivalEscapeDirection.sqrMagnitude > 0.001f)
        {
            float panicT = Mathf.Clamp01(survivalEmergencyTimer / Mathf.Max(0.01f, SurvivalModeLockTime));
            pull += survivalEscapeDirection.normalized * SurvivalFleeWeight * Mathf.Lerp(0.75f, 1.25f, panicT);
        }

        if (currentBrainMode == FishAutonomousBehaviourMode.GuardingEggs && guardedEggCluster != null)
        {
            Vector3 eggPosition = guardedEggCluster.transform.position;
            Vector3 target = eggPosition;
            float weight = EggGuardPullWeight;

            if (currentEggThreat != null)
            {
                target = Vector3.Lerp(eggPosition, currentEggThreat.transform.position, 0.72f);
                weight = EggGuardThreatInterceptWeight;
            }

            Vector3 toTarget = target - transform.position;
            if (toTarget.sqrMagnitude > 0.04f)
            {
                pull += toTarget.normalized * weight;
            }
        }

        if (currentBrainMode == FishAutonomousBehaviourMode.Hunting)
        {
            MarineCreatureAgent preyTarget = focusedPrey != null && hunterPreyFocusTimer > 0f && CanAttackPrey(focusedPrey) ? focusedPrey : nearestPrey;
            if (preyTarget != null && CanAttackPrey(preyTarget))
            {
                nearestPrey = preyTarget;
                Vector3 toPrey = preyTarget.GetBiteTargetPosition() - GetMouthWorldPosition();
                if (toPrey.sqrMagnitude > 0.001f)
                {
                    float distance = toPrey.magnitude;
                    float closeBoost = 1f - Mathf.Clamp01(distance / Mathf.Max(0.1f, SprintChaseDistance));
                    float sticky = hunterPreyFocusTimer > 0f ? HunterStickyChaseWeight : Mathf.Lerp(2.0f, 4.6f, closeBoost);
                    pull += toPrey.normalized * sticky * Mathf.Lerp(0.85f, 1.45f, Candidate.Genome.Aggression);
                }
            }
        }

        if (currentBrainMode == FishAutonomousBehaviourMode.MobbingPredator && currentMobbingTarget != null)
        {
            Vector3 toPredator = currentMobbingTarget.transform.position - transform.position;
            float distance = toPredator.magnitude;
            if (distance > 0.01f)
            {
                Vector3 direction = toPredator.normalized;
                Vector3 side = Vector3.Cross(Vector3.up, direction);
                if (side.sqrMagnitude <= 0.001f)
                {
                    side = transform.right;
                }

                float approach = distance > MobbingIdealDistance ? 1f : -0.45f;
                float orbit = Mathf.Sin((Time.time + Candidate.Id * 0.17f) * 3.5f) * 0.45f;
                pull += (direction * approach + side.normalized * orbit).normalized * MobbingPullWeight;
            }
        }

        if (currentBrainMode == FishAutonomousBehaviourMode.Courtship && currentMateTarget != null)
        {
            Vector3 toMate = currentMateTarget.transform.position - transform.position;
            if (toMate.sqrMagnitude > 0.04f)
            {
                Vector3 direction = toMate.normalized;
                Vector3 side = Vector3.Cross(Vector3.up, direction);
                if (side.sqrMagnitude <= 0.001f)
                {
                    side = transform.right;
                }

                float distance = toMate.magnitude;
                float approach = distance > MatePairDistance * 0.85f ? 0.55f : -0.15f;
                pull += (direction * approach + side.normalized * CourtshipCircleWeight).normalized * CourtshipCircleWeight;
            }
        }

        if (currentBrainMode == FishAutonomousBehaviourMode.Ambushing)
        {
            if (nearestPrey != null && CanAttackPrey(nearestPrey))
            {
                Vector3 toPrey = nearestPrey.GetBiteTargetPosition() - GetMouthWorldPosition();
                if (toPrey.sqrMagnitude <= AmbushStrikeRange * AmbushStrikeRange)
                {
                    pull += toPrey.normalized * AmbushPullWeight * 2.4f;
                }
            }
            else if (hasAmbushAnchor)
            {
                Vector3 toAnchor = ambushAnchor - transform.position;
                float distance = toAnchor.magnitude;
                if (distance > AmbushHoldRadius && distance > 0.01f)
                {
                    pull += toAnchor.normalized * AmbushPullWeight;
                }
                else
                {
                    Vector3 side = Vector3.Cross(Vector3.up, transform.forward);
                    if (side.sqrMagnitude <= 0.001f)
                    {
                        side = GetFallbackSideDirection();
                    }
                    pull += side.normalized * AmbushPullWeight * 0.25f;
                }
            }
        }

        return pull;
    }

    // Handles start cstart escape burst
    private void StartCStartEscapeBurst()
    {
        Vector3 away = Vector3.zero;

        if (nearestCreature != null && IsPredatorThreateningThis(nearestCreature, ClosePredatorThreatRange, ClosePredatorThreatHalfAngle, true, false))
        {
            away = transform.position - nearestCreature.transform.position;
        }
        else if (mobbingPressureTimer > 0f && mobbingPressureDirection.sqrMagnitude > 0.001f)
        {
            away = mobbingPressureDirection;
        }
        else if (dangerMemoryTimer > 0f)
        {
            away = transform.position - rememberedDangerArea;
        }

        away.y *= 0.10f;
        if (EvolutionEcosystemManager.Instance != null && EvolutionEcosystemManager.Instance.TryGetTerrainHeight(transform.position, out float fleeGroundY))
        {
            float clearance = transform.position.y - fleeGroundY;
            if (clearance < FleeFloorAvoidanceClearance)
            {
                away.y = Mathf.Max(away.y, 0.25f);
            }
        }
        if (away.sqrMagnitude <= 0.001f)
        {
            away = -transform.forward;
        }

        Vector3 side = Vector3.Cross(Vector3.up, away.normalized);
        if (side.sqrMagnitude <= 0.001f)
        {
            side = transform.right;
        }
        if (Random.value < 0.5f)
        {
            side = -side;
        }

        cStartBurstDirection = (away.normalized + side.normalized * CStartSideBias).normalized;
        cStartBurstTimer = Mathf.Max(0.05f, CStartBurstDuration);
    }

    // Applies mobbing pressure if close to the current object
    private void ApplyMobbingPressureIfClose()
    {
        if (!EnableAdvancedFishBehaviours || currentBrainMode != FishAutonomousBehaviourMode.MobbingPredator || currentMobbingTarget == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, currentMobbingTarget.transform.position);
        if (distance <= MobbingIdealDistance * 1.35f)
        {
            currentMobbingTarget.ReceiveMobbingPressure(this, transform.position);
        }
    }

    // Handles receive mobbing pressure
    public void ReceiveMobbingPressure(MarineCreatureAgent source, Vector3 sourcePosition)
    {
        if (source == null || source == this)
        {
            return;
        }

        Vector3 away = transform.position - sourcePosition;
        away.y *= 0.25f;
        if (away.sqrMagnitude <= 0.001f)
        {
            away = transform.forward;
        }

        mobbingPressureDirection = away.normalized;
        mobbingPressureTimer = Mathf.Max(mobbingPressureTimer, MobbingPressureDuration);
        RememberDangerArea(sourcePosition);
    }

    // Counts real nearby threats without making every predator freeze the sim
    private int CountCurrentThreatsForBrain()
    {
        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null)
        {
            return 0;
        }

        Vector3 position = transform.position;
        float scanRange = Mathf.Max(10f, GetThreatRange() * 2.2f);
        List<MarineCreatureAgent> creatures = manager.GetNearbyCreatures(position, scanRange);
        if (creatures == null || creatures.Count <= 1)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < creatures.Count; i++)
        {
            MarineCreatureAgent other = creatures[i];
            if (other == null || other == this || !IsPredatorThreateningThis(other, ClosePredatorThreatRange, ClosePredatorThreatHalfAngle, true, false))
            {
                continue;
            }

            count++;
        }

        return count;
    }

    // Scales steering pulls depending on whether the fish is feeding, fleeing, hunting or resting.
    private void ApplyBrainModeToPulls(
        float hungerPressure,
        ref Vector3 targetPull,
        ref Vector3 rawStaticFeedingPull,
        ref Vector3 schoolPull,
        ref Vector3 dangerPull,
        ref Vector3 depthPull,
        ref Vector3 closeTargetPull,
        ref Vector3 queuePull,
        ref Vector3 crowdPull,
        ref Vector3 brainPull,
        ref Vector3 wanderPull,
        ref Vector3 homePull,
        ref Vector3 matePull,
        ref Vector3 terrainPull,
        ref Vector3 emergencyPull)
    {
        switch (currentBrainMode)
        {
            case FishAutonomousBehaviourMode.GuardingEggs:
                targetPull *= 0.25f;
                rawStaticFeedingPull = Vector3.zero;
                schoolPull *= 0.35f;
                dangerPull *= 1.15f;
                closeTargetPull = Vector3.zero;
                queuePull = Vector3.zero;
                matePull = Vector3.zero;
                wanderPull *= 0.2f;
                homePull *= 0.25f;
                break;

            case FishAutonomousBehaviourMode.MobbingPredator:
                targetPull = Vector3.zero;
                rawStaticFeedingPull = Vector3.zero;
                schoolPull *= 0.75f;
                dangerPull *= 0.35f;
                closeTargetPull = Vector3.zero;
                queuePull = Vector3.zero;
                matePull = Vector3.zero;
                wanderPull *= 0.15f;
                homePull *= 0.15f;
                break;

            case FishAutonomousBehaviourMode.Courtship:
                targetPull = Vector3.zero;
                rawStaticFeedingPull = Vector3.zero;
                schoolPull *= 0.25f;
                depthPull *= 0.35f;
                closeTargetPull = Vector3.zero;
                queuePull = Vector3.zero;
                wanderPull *= 0.15f;
                homePull *= 0.25f;
                matePull *= 1.4f;
                break;

            case FishAutonomousBehaviourMode.Ambushing:
                targetPull *= currentEggThreat != null ? 1.0f : 0.25f;
                rawStaticFeedingPull = Vector3.zero;
                schoolPull *= 0.2f;
                dangerPull *= 0.65f;
                closeTargetPull = Vector3.zero;
                queuePull = Vector3.zero;
                matePull = Vector3.zero;
                wanderPull *= 0.08f;
                homePull *= 0.35f;
                break;

            case FishAutonomousBehaviourMode.Fleeing:
                targetPull *= 0.15f;
                rawStaticFeedingPull = Vector3.zero;
                schoolPull *= 0.35f;
                dangerPull *= 2.2f;
                closeTargetPull = Vector3.zero;
                queuePull = Vector3.zero;
                matePull = Vector3.zero;
                wanderPull *= 0.25f;
                homePull *= 0.25f;
                break;

            case FishAutonomousBehaviourMode.Hunting:
                targetPull *= Mathf.Lerp(1.25f, 1.95f, Candidate.Genome.Aggression);
                rawStaticFeedingPull *= ShouldSuppressPlantsForHunter() ? 0.0f : 0.35f;
                closeTargetPull *= ShouldSuppressPlantsForHunter() ? 0.25f : 0.65f;
                queuePull *= 0.25f;
                crowdPull *= 0.35f;
                schoolPull *= Mathf.Lerp(0.25f, 0.75f, Candidate.Genome.GroupingChance);
                matePull = Vector3.zero;
                homePull *= 0.2f;
                wanderPull *= 0.08f;
                break;

            case FishAutonomousBehaviourMode.Feeding:
                targetPull *= 1.75f;
                rawStaticFeedingPull *= 1.35f;
                schoolPull *= FeedingSocialSuppression;
                depthPull *= 0.15f;
                queuePull = Vector3.zero;
                crowdPull *= 0.45f;
                brainPull *= 0.05f;
                wanderPull = Vector3.zero;
                homePull = Vector3.zero;
                matePull = Vector3.zero;
                break;

            case FishAutonomousBehaviourMode.Foraging:
                targetPull *= Mathf.Lerp(0.85f, 1.45f, brainFoodDesire);
                schoolPull *= Mathf.Lerp(0.35f, 0.95f, Candidate.Genome.FoodSharing);
                wanderPull *= 0.45f;
                homePull *= 0.2f;
                matePull = Vector3.zero;
                break;

            case FishAutonomousBehaviourMode.SeekingMate:
                targetPull = Vector3.zero;
                rawStaticFeedingPull = Vector3.zero;
                matePull *= Mathf.Lerp(1.2f, 2.0f, Candidate.Genome.MateDrive);
                schoolPull *= Mathf.Clamp01(MateSearchSchoolSuppression * Mathf.Lerp(0.35f, 1f, Candidate.Genome.GroupingChance));
                wanderPull *= MateSearchWanderSuppression;
                homePull *= 0.35f;
                break;

            case FishAutonomousBehaviourMode.Nesting:
                targetPull = Vector3.zero;
                rawStaticFeedingPull = Vector3.zero;
                schoolPull *= 0.55f;
                matePull *= 0.65f;
                homePull *= 1.35f;
                wanderPull *= 0.85f;
                break;

            case FishAutonomousBehaviourMode.FollowingLeader:
                targetPull *= 0.2f;
                rawStaticFeedingPull = Vector3.zero;
                schoolPull *= 1.45f;
                matePull = Vector3.zero;
                wanderPull *= 0.25f;
                homePull *= 0.45f;
                break;

            case FishAutonomousBehaviourMode.Schooling:
                targetPull *= 0.2f;
                rawStaticFeedingPull = Vector3.zero;
                schoolPull *= 1.35f;
                wanderPull *= 0.55f;
                homePull *= 0.65f;
                matePull *= 0.35f;
                break;

            case FishAutonomousBehaviourMode.Exploring:
                targetPull *= 0.18f;
                rawStaticFeedingPull = Vector3.zero;
                schoolPull *= Mathf.Lerp(0.45f, 0.95f, Candidate.Genome.GroupingChance);
                wanderPull *= ExploringWanderMultiplier;
                homePull *= 0.35f;
                matePull *= 0.25f;
                break;

            case FishAutonomousBehaviourMode.Sleeping:
                targetPull = Vector3.zero;
                rawStaticFeedingPull = Vector3.zero;
                schoolPull *= 0.12f;
                dangerPull *= 1.15f;
                depthPull *= 0.45f;
                closeTargetPull = Vector3.zero;
                queuePull = Vector3.zero;
                matePull = Vector3.zero;
                wanderPull *= 0.10f;
                homePull *= 1.45f;
                brainPull = Vector3.zero;
                break;

            case FishAutonomousBehaviourMode.Recovering:
                targetPull *= 0.25f;
                rawStaticFeedingPull = Vector3.zero;
                schoolPull *= 0.1f;
                crowdPull *= 1.65f;
                emergencyPull *= 1.85f;
                wanderPull *= 0.35f;
                matePull = Vector3.zero;
                break;

            default:
                targetPull = Vector3.zero;
                rawStaticFeedingPull = Vector3.zero;
                schoolPull *= Mathf.Lerp(0.35f, 0.9f, Candidate.Genome.GroupingChance);
                wanderPull *= RestingWanderMultiplier;
                homePull *= 1.25f;
                matePull *= 0.25f;
                break;
        }
    }

    // Combines target, school, terrain, current and brain pulls into final movement
    private void RunEvolvedMovement()
    {
        float energyRatio = GetEffectiveEnergyRatio();
        float hungerPressure = GetHungerPressure();
        bool hungryEnough = brainWantsFood
            || currentBrainMode == FishAutonomousBehaviourMode.Feeding
            || currentBrainMode == FishAutonomousBehaviourMode.Foraging
            || currentBrainMode == FishAutonomousBehaviourMode.Hunting
            || currentBrainMode == FishAutonomousBehaviourMode.Ambushing;

        Vector3 targetPull = hungryEnough ? GetFeedingTargetPull(hungerPressure) : Vector3.zero;
        Vector3 rawStaticFeedingPull = hungryEnough ? GetRawStaticFeedingPull(hungerPressure) : Vector3.zero;
        Vector3 schoolPull = GetCachedSchoolingPull(hungerPressure);
        Vector3 dangerPull = GetCachedDangerAvoidancePull(hungerPressure);
        Vector3 depthPull = GetDepthPreferencePull(targetPull, dangerPull, hungerPressure);
        Vector3 boundaryPull = GetBoundaryAvoidanceDirection();
        Vector3 currentEscapePull = GetCurrentEscapePull(boundaryPull);
        Vector3 closeTargetPull = GetCloseTargetPull(hungerPressure);
        Vector3 queuePull = GetFoodQueuePull(hungerPressure);
        Vector3 crowdPull = GetCachedCrowdStabilisationPull(hungerPressure);
        Vector3 brainPull = GetBrainPull(energyRatio);
        Vector3 wanderPull = GetWanderPull(energyRatio);
        Vector3 homePull = GetHomeAreaPull(hungerPressure);
        Vector3 matePull = GetMateSeekingPull(hungerPressure);
        Vector3 terrainPull = GetCachedTerrainAvoidancePull();
        if (UseFleeTerrainSafety && (currentBrainMode == FishAutonomousBehaviourMode.Fleeing || currentBrainMode == FishAutonomousBehaviourMode.Recovering || survivalEmergencyTimer > 0f))
        {
            terrainPull *= Mathf.Max(1f, FleeTerrainAvoidanceMultiplier);
        }
        Vector3 emergencyPull = GetEmergencyUnstickPull();
        Vector3 advancedPull = GetAdvancedBehaviourPull(hungerPressure);

        ApplyBrainModeToPulls(hungerPressure,
            ref targetPull,
            ref rawStaticFeedingPull,
            ref schoolPull,
            ref dangerPull,
            ref depthPull,
            ref closeTargetPull,
            ref queuePull,
            ref crowdPull,
            ref brainPull,
            ref wanderPull,
            ref homePull,
            ref matePull,
            ref terrainPull,
            ref emergencyPull);

        if (currentEscapePull.sqrMagnitude > 0.001f)
        {
            // Harsh currents are treated like temporary environmental danger. Reduce distractions
            // so fish do not keep trying to feed/mate while being pushed into the edge of the map.
            targetPull *= 0.55f;
            rawStaticFeedingPull *= 0.45f;
            schoolPull *= 0.55f;
            matePull *= 0.35f;
            wanderPull *= 0.35f;
            dangerPull += currentEscapePull;
        }
        if (UseFleeTerrainSafety && (currentBrainMode == FishAutonomousBehaviourMode.Fleeing || survivalEmergencyTimer > 0f) && terrainPull.sqrMagnitude > 0.001f)
        {
            Vector3 horizontalTerrain = new Vector3(terrainPull.x, 0f, terrainPull.z);
            Vector3 sideStep = GetFallbackSideDirection() * FleeWallSideStepWeight;
            if (horizontalTerrain.sqrMagnitude > 0.001f)
            {
                sideStep += horizontalTerrain.normalized * FleeWallBackOffWeight;
            }
            dangerPull += sideStep;
            depthPull *= 0.35f;
        }

        bool feedingCommit = hungryEnough && ShouldCommitToStaticFeedingTarget(hungerPressure);
        if (feedingCommit)
        {
            float suppression = Mathf.Clamp01(FeedingSocialSuppression);
            schoolPull *= suppression;
            depthPull = Vector3.zero;
            queuePull = Vector3.zero;
            crowdPull *= 0.18f;
            brainPull *= 0.05f;
            wanderPull = Vector3.zero;
            matePull = Vector3.zero;
            closeTargetPull *= 0.35f;

            if (rawStaticFeedingPull.sqrMagnitude > 0.0001f)
            {
                targetPull = rawStaticFeedingPull * DirectStaticFeedingPullWeight;
            }
        }

        if (feedingHoldTimer > 0f)
        {
            schoolPull *= 0.05f;
            depthPull = Vector3.zero;
            queuePull = Vector3.zero;
            crowdPull *= 0.35f;
            brainPull = Vector3.zero;
            wanderPull = Vector3.zero;
            matePull = Vector3.zero;
            closeTargetPull = Vector3.zero;

            if (hasStaticFeedingPoint)
            {
                Vector3 toFood = currentStaticFeedingPoint - GetMouthWorldPosition();
                if (toFood.sqrMagnitude > 0.0001f)
                {
                    targetPull = toFood.normalized * DirectStaticFeedingPullWeight;
                }
            }
        }

        if (matePull.sqrMagnitude > 0.001f && !hungryEnough)
        {
            schoolPull *= Mathf.Clamp01(MateSearchSchoolSuppression);
            wanderPull *= Mathf.Clamp01(MateSearchWanderSuppression);
            homePull *= 0.45f;
        }

        lastBoundaryPush = boundaryPull;

        if (lastFeedingCrowdCount >= FeedingCrowdSoftLimit)
        {
            schoolPull *= CrowdedFeedingSchoolSuppression;
            queuePull *= 0.25f;
        }

        Vector3 combined = targetPull + schoolPull + dangerPull + depthPull + boundaryPull + currentEscapePull + closeTargetPull + queuePull + crowdPull + brainPull + wanderPull + homePull + matePull + terrainPull + emergencyPull + advancedPull;
        StabiliseVerticalSteering(ref combined, targetPull, dangerPull, boundaryPull, hungerPressure);
        if (stuckEscapeTimer > 0f)
        {
            stuckEscapeTimer -= Time.fixedDeltaTime;
            combined += stuckEscapeDirection * StuckEscapeWeight;
        }

        if (combined.sqrMagnitude < 0.025f)
        {
            combined = transform.forward.sqrMagnitude > 0.01f ? transform.forward : Vector3.forward;
        }

        PreventOutwardDirectionAtBounds(ref combined);
        if (combined.sqrMagnitude < 0.025f)
        {
            combined = GetDirectionToSimulationCentre();
        }

        behaviourBlendVector = Vector3.Slerp(behaviourBlendVector.sqrMagnitude > 0.001f ? behaviourBlendVector.normalized : transform.forward, combined.normalized, Mathf.Clamp01(BehaviourBlendSpeed * Time.fixedDeltaTime));
        wantedDirection = behaviourBlendVector.sqrMagnitude > 0.001f ? behaviourBlendVector.normalized : combined.normalized;
        UpdateDebugMovementState(hungerPressure, targetPull, dangerPull, schoolPull + crowdPull, boundaryPull, emergencyPull);
        MoveFish(hungerPressure);
        UpdateStuckDetection(combined);
    }

    // Gets the feeding target pull used by the sim
    private Vector3 GetFeedingTargetPull(float hungerPressure)
    {
        Vector3 pull = Vector3.zero;
        Vector3? targetPosition = null;
        string targetKind = GetPreferredFoodKind();

        if (targetKind == "Meat" && focusedPrey != null && hunterPreyFocusTimer > 0f && CanAttackPrey(focusedPrey))
        {
            targetPosition = focusedPrey.GetBiteTargetPosition();
        }
        else if (targetKind == "Meat" && nearestPrey != null && CanAttackPrey(nearestPrey))
        {
            targetPosition = nearestPrey.GetBiteTargetPosition();
        }
        else if (targetKind == "Meat" && HasUsableCarrionTarget())
        {
            CarrionSource carrionTarget = claimedFreshKillCarrion != null && !claimedFreshKillCarrion.IsConsumed ? claimedFreshKillCarrion : nearestCarrion;
            if (carrionTarget == null || carrionTarget.IsConsumed)
            {
                carrionTarget = retainedCarrion;
            }

            if (carrionTarget != null && !carrionTarget.IsConsumed)
            {
                targetPosition = GetMovementTargetForStaticResource(carrionTarget.transform.position);
            }
        }
        else if (targetKind == "Carrion" && nearestCarrion != null && !nearestCarrion.IsConsumed)
        {
            targetPosition = GetMovementTargetForStaticResource(nearestCarrion.transform.position);
        }
        else if (targetKind == "Plant" && nearestFood != null && !nearestFood.IsConsumed)
        {
            targetPosition = GetMovementTargetForStaticResource(nearestFood.transform.position);
        }

        if (!targetPosition.HasValue)
        {
            if (IsPredatorPlantBlockedRole() && !CanUsePlantAsPredatorEmergencyFallback())
            {
                ClearPlantTargetForPredator();

                if (nearestCarrion != null && !nearestCarrion.IsConsumed)
                {
                    targetPosition = GetMovementTargetForStaticResource(nearestCarrion.transform.position);
                }
                else if (retainedCarrion != null && !retainedCarrion.IsConsumed)
                {
                    targetPosition = GetMovementTargetForStaticResource(retainedCarrion.transform.position);
                }
                else if (nearestPrey != null && CanAttackPrey(nearestPrey))
                {
                    targetPosition = nearestPrey.GetBiteTargetPosition();
                }
            }
            else if (nearestFood != null && !nearestFood.IsConsumed && Candidate.Genome.PlantDiet >= 0.12f)
            {
                targetPosition = GetMovementTargetForStaticResource(nearestFood.transform.position);
            }
            else if (nearestCarrion != null && !nearestCarrion.IsConsumed && Candidate.Genome.CarrionDiet >= 0.12f)
            {
                targetPosition = GetMovementTargetForStaticResource(nearestCarrion.transform.position);
            }
            else if (nearestPrey != null && CanAttackPrey(nearestPrey))
            {
                targetPosition = nearestPrey.GetBiteTargetPosition();
            }
        }

        if (!targetPosition.HasValue && hasFoodMemory && !rememberedFoodWasBad)
        {
            targetPosition = rememberedFoodArea;
            if (Candidate != null)
            {
                Candidate.FoodMemoryUses++;
            }
        }

        if (targetPosition.HasValue)
        {
            Vector3 toTarget = targetPosition.Value - GetMouthWorldPosition();
            float distance = toTarget.magnitude;
            if (distance > 0.001f)
            {
                float hunger = Mathf.Lerp(0.55f, 3.1f, Mathf.Clamp01(hungerPressure));
                pull += toTarget.normalized * hunger * Mathf.Lerp(0.75f, 1.3f, Candidate.Genome.HungerDrive);
            }
        }

        lastFoodDirection = DirectionTo(nearestFood != null && !nearestFood.IsConsumed ? nearestFood.transform.position : (Vector3?)null);
        lastCarrionDirection = DirectionTo(nearestCarrion != null && !nearestCarrion.IsConsumed ? nearestCarrion.transform.position : (Vector3?)null);
        lastPreyDirection = DirectionTo(nearestPrey != null && CanAttackPrey(nearestPrey) ? nearestPrey.GetBiteTargetPosition() : (Vector3?)null);
        return pull;
    }

    // Gets the raw static feeding pull used by the sim
    private Vector3 GetRawStaticFeedingPull(float hungerPressure)
    {
        Vector3? targetPosition = GetPrimaryStaticFoodTargetPosition();
        if (!targetPosition.HasValue)
        {
            return Vector3.zero;
        }

        Vector3 toTarget = targetPosition.Value - GetMouthWorldPosition();
        if (toTarget.sqrMagnitude <= 0.0001f)
        {
            return transform.forward.sqrMagnitude > 0.001f ? transform.forward : Vector3.forward;
        }

        float hunger = Mathf.Lerp(0.85f, 3.6f, Mathf.Clamp01(hungerPressure));
        return toTarget.normalized * hunger * Mathf.Lerp(0.85f, 1.35f, Candidate.Genome.HungerDrive);
    }

    // Gets the comfortable feeder limit used by the sim
    private int GetComfortableFeederLimit()
    {
        float sharing = Candidate != null && Candidate.Genome != null ? Candidate.Genome.FoodSharing : 0.5f;
        return Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(MinComfortableFeeders, MaxComfortableFeeders, sharing)), 1, Mathf.Max(1, MaxComfortableFeeders));
    }

    // Gets the resource crowd penalty used by the sim
    private float GetResourceCrowdPenalty()
    {
        float sharing = Candidate != null && Candidate.Genome != null ? Candidate.Genome.FoodSharing : 0.5f;
        float territorial = Candidate != null && Candidate.Genome != null ? Candidate.Genome.Territoriality : 0.0f;
        float basePenalty = Mathf.Lerp(ResourceCrowdPenaltyLowSharing, ResourceCrowdPenaltyHighSharing, sharing);
        return basePenalty * Mathf.Lerp(0.85f, 1.35f, territorial);
    }

    // Gets the preferred food kind used by the sim
    private string GetPreferredFoodKind()
    {
        if (ShouldContinueEatingFreshKillCarrion())
        {
            return "Carrion";
        }

        if (IsLockedMeatSpecialist() || IsPredatorPlantBlockedRole())
        {
            if (HasUsableCarrionTarget())
            {
                return "Carrion";
            }

            return "Meat";
        }

        if (ShouldSuppressPlantsForHunter() && (nearestPrey != null || focusedPrey != null))
        {
            return HasUsableCarrionTarget() && nearestPrey == null && focusedPrey == null ? "Carrion" : "Meat";
        }

        float plant = Candidate.Genome.PlantDiet;
        float meat = Candidate.Genome.MeatDiet;
        float carrion = Candidate.Genome.CarrionDiet;

        if (meat >= plant && meat >= carrion)
        {
            return "Meat";
        }

        if (carrion >= plant && carrion >= meat)
        {
            return "Carrion";
        }

        return "Plant";
    }

    // Checks if it is locked meat specialist
    private bool IsLockedMeatSpecialist()
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return false;
        }

        return Candidate.Genome.MeatDiet >= 0.70f && Candidate.Genome.PlantDiet <= 0.18f;
    }

    // Checks if it is peaceful grazer role
    private bool IsPeacefulGrazerRole()
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return false;
        }

        EvolutionGenome g = Candidate.Genome;
        float nonPlantBest = Mathf.Max(g.MeatDiet, g.CarrionDiet);
        bool clearlyPlantLed = g.PlantDiet >= 0.42f && g.PlantDiet >= nonPlantBest + 0.06f;
        bool notTrueHunter = g.MeatDiet < GrazerHuntBlockMeatThreshold || g.PlantDiet >= g.MeatDiet;
        return clearlyPlantLed && notTrueHunter;
    }

    // Checks if it is ecological predator role
    private bool IsEcologicalPredatorRole()
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return false;
        }

        EvolutionGenome g = Candidate.Genome;
        float bestNonMeat = Mathf.Max(g.PlantDiet, g.CarrionDiet);
        bool meatDominant = g.MeatDiet >= bestNonMeat + 0.05f || g.MeatDiet >= 0.66f;
        bool hasPredatorBody = g.JawSize >= 1.06f || g.Speed >= 5.05f || (!string.IsNullOrEmpty(g.BodyMorphId) && g.BodyMorphId.Contains("streamlined"));
        bool hasPredatorMind = g.Aggression >= 0.28f || brainOutputHuntBias > 0.28f;
        return g.MeatDiet >= EcologicalPredatorMeatThreshold && meatDominant && hasPredatorMind && hasPredatorBody;
    }

    // Checks if it has usable carrion target available
    private bool HasUsableCarrionTarget()
    {
        return (claimedFreshKillCarrion != null && !claimedFreshKillCarrion.IsConsumed)
            || (nearestCarrion != null && !nearestCarrion.IsConsumed)
            || (retainedCarrion != null && !retainedCarrion.IsConsumed);
    }

    // Checks if it has visible meat target for diet priority available
    private bool HasVisibleMeatTargetForDietPriority()
    {
        return HasUsableCarrionTarget()
            || focusedPrey != null
            || nearestPrey != null
            || retainedPrey != null;
    }

    // Checks if it is predator plant blocked role
    private bool IsPredatorPlantBlockedRole()
    {
        if (!StrictPredatorMeatPriority || Candidate == null || Candidate.Genome == null)
        {
            return false;
        }

        EvolutionGenome g = Candidate.Genome;
        float bestNonMeat = Mathf.Max(g.PlantDiet, g.CarrionDiet);
        bool meatBiased = g.MeatDiet >= PredatorPlantBlockMeatThreshold && g.MeatDiet >= bestNonMeat - 0.08f;
        bool predatorShape = g.JawSize >= 1.04f
            || g.JawLength >= 1.04f
            || g.Speed >= 4.9f
            || (!string.IsNullOrEmpty(g.BodyMorphId) && g.BodyMorphId.Contains("streamlined"))
            || (!string.IsNullOrEmpty(g.JawMorphId) && (g.JawMorphId.Contains("hunter") || g.JawMorphId.Contains("predator")));
        bool predatorMind = g.Aggression >= 0.22f || brainOutputHuntBias > 0.18f || currentBrainMode == FishAutonomousBehaviourMode.Hunting || currentBrainMode == FishAutonomousBehaviourMode.Ambushing;

        return meatBiased && (predatorShape || predatorMind || IsEcologicalPredatorRole() || IsLockedMeatSpecialist());
    }

    // Checks if it can use plant as predator emergency fallback with the current state
    private bool CanUsePlantAsPredatorEmergencyFallback()
    {
        if (!IsPredatorPlantBlockedRole())
        {
            return true;
        }

        if (HasVisibleMeatTargetForDietPriority())
        {
            return false;
        }

        // This is an absolute last-resort fallback, not normal predator grazing.
        // Do not also require low health; that made weak predator niches die out before
        // they could find carrion or an easier prey target.
        return GetEffectiveEnergyRatio() <= PredatorPlantEmergencyEnergyRatio
            && GetStomachFullness01() <= PredatorPlantEmergencyStomachRatio;
    }

    // Clears plant target for predator ready for fresh data
    private void ClearPlantTargetForPredator()
    {
        nearestFood = null;
        retainedFood = null;
    }

    // Checks if it should predator chill after feeding right now
    private bool ShouldPredatorChillAfterFeeding()
    {
        if (!IsEcologicalPredatorRole() && !IsLockedMeatSpecialist())
        {
            return false;
        }

        if (ShouldContinueEatingFreshKillCarrion() || survivalEmergencyTimer > 0f || IsCriticallyStarving())
        {
            return false;
        }

        if (UsePredatorHuntCommitment && predatorExhaustionTimer > 0f && GetEffectiveEnergyRatio() > SurvivalCriticalEnergyRatio)
        {
            return true;
        }

        if (!SatisfiedPredatorsStopHunting)
        {
            return GetEffectiveEnergyRatio() >= HunterChillEnergyRatio && GetStomachFullness01() >= HunterChillStomachRatio;
        }

        float energyRatio = GetEffectiveEnergyRatio();
        float stomachRatio = GetStomachFullness01();
        bool comfortablyFed = energyRatio >= HunterChillEnergyRatio && stomachRatio >= HunterChillStomachRatio;
        bool veryHighReserve = energyRatio >= Mathf.Clamp01(HunterChillEnergyRatio + 0.18f);
        return comfortablyFed || veryHighReserve;
    }

    // Checks if it is committed predator for targeting
    private bool IsCommittedPredatorForTargeting()
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return false;
        }

        if ((UsePredatorHuntCommitment && predatorExhaustionTimer > 0f && !IsCriticallyStarving()) || ShouldPredatorChillAfterFeeding() || IsPeacefulGrazerRole())
        {
            return false;
        }

        return IsEcologicalPredatorRole()
            || currentBrainMode == FishAutonomousBehaviourMode.Hunting
            || currentBrainMode == FishAutonomousBehaviourMode.Ambushing
            || (brainWantsHunt && Candidate.Genome.MeatDiet >= EcologicalPredatorMeatThreshold);
    }

    // Checks if it should suppress plants for hunter right now
    private bool ShouldSuppressPlantsForHunter()
    {
        if (!IsCommittedPredatorForTargeting())
        {
            return false;
        }

        if (ShouldContinueEatingFreshKillCarrion())
        {
            return false;
        }

        if (GetEffectiveEnergyRatio() >= HunterPlantSuppressionStopRatio && GetStomachFullness01() >= 0.55f)
        {
            return false;
        }

        return (nearestPrey != null && CanAttackPrey(nearestPrey)) || (focusedPrey != null && hunterPreyFocusTimer > 0f && CanAttackPrey(focusedPrey));
    }

    // Handles suppress plant targets while hunting
    private void SuppressPlantTargetsWhileHunting()
    {
        if (!ShouldSuppressPlantsForHunter())
        {
            return;
        }

        nearestFood = null;
        retainedFood = null;
    }

    // Handles focus prey
    private void FocusPrey(MarineCreatureAgent prey, float duration)
    {
        if (prey == null || prey == this || !CanAttackPrey(prey))
        {
            return;
        }

        if (focusedPrey != prey)
        {
            predatorChaseTimer = 0f;
            predatorTimeSinceLastBite = Mathf.Min(predatorTimeSinceLastBite, PredatorFailedChaseGiveUpTime);
        }

        focusedPrey = prey;
        nearestPrey = prey;
        retainedPrey = prey;
        retainedTargetTimer = Mathf.Max(retainedTargetTimer, TargetRetainTime);

        float focusDuration = duration;
        if (prey.GetWoundedScent01() > 0.25f || prey.GetHealthRatio() < 0.55f)
        {
            focusDuration *= PredatorWoundedPreyCommitBonus;
        }
        hunterPreyFocusTimer = Mathf.Max(hunterPreyFocusTimer, focusDuration);
    }

    // Clears focused prey ready for fresh data
    private void ClearFocusedPrey(bool startExhaustion)
    {
        focusedPrey = null;
        retainedPrey = null;
        hunterPreyFocusTimer = 0f;
        predatorChaseTimer = 0f;
        predatorTimeSinceLastBite = 999f;
        if (startExhaustion && UsePredatorHuntCommitment)
        {
            predatorExhaustionTimer = Mathf.Max(predatorExhaustionTimer, PredatorExhaustionAfterFailedChase * Mathf.Lerp(0.75f, 1.25f, 1f - Candidate.Genome.Aggression));
        }
    }

    // Checks if it should abandon focused prey right now
    private bool ShouldAbandonFocusedPrey()
    {
        if (!UsePredatorHuntCommitment || focusedPrey == null || Candidate == null || Candidate.Genome == null)
        {
            return false;
        }

        if (ShouldContinueEatingFreshKillCarrion())
        {
            return true;
        }

        float distance = Vector3.Distance(GetMouthWorldPosition(), focusedPrey.GetBiteTargetPosition());
        float closeDistance = Mathf.Max(PredatorCloseChaseDistance, GetScaledMouthRadius() * 3.0f + focusedPrey.GetPersonalRadius());
        bool closeEnoughToKeepTrying = distance <= closeDistance;
        bool preyIsWounded = focusedPrey.GetHealthRatio() < 0.48f || focusedPrey.GetWoundedScent01() > 0.35f;

        float allowedCommit = PredatorFailedChaseGiveUpTime * Mathf.Lerp(0.85f, 1.35f, Candidate.Genome.Aggression + brainOutputHuntBias * 0.25f);
        if (preyIsWounded)
        {
            allowedCommit *= PredatorWoundedPreyCommitBonus;
        }
        if (closeEnoughToKeepTrying)
        {
            allowedCommit *= 1.35f;
        }

        bool tooLongWithoutBite = predatorChaseTimer >= allowedCommit && predatorTimeSinceLastBite >= allowedCommit * 0.75f;
        bool energyRisk = GetEffectiveEnergyRatio() <= SurvivalCriticalEnergyRatio && predatorTimeSinceLastBite >= PredatorFailedChaseGiveUpTime * 0.65f;
        return tooLongWithoutBite || energyRisk;
    }

    // Gets the wounded scent01 used by the sim
    public float GetWoundedScent01()
    {
        if (!UseRealisticInjuryMovement)
        {
            return 0f;
        }

        float healthWound = Mathf.InverseLerp(InjuryMovementStartHealthRatio, 0.20f, GetHealthRatio());
        float freshWound = woundedScentTimer > 0f ? Mathf.Clamp01(woundedScentTimer / Mathf.Max(0.01f, WoundedScentDuration)) : 0f;
        return Mathf.Clamp01(Mathf.Max(healthWound, freshWound * 0.75f));
    }

    // Gets the injury severity01 used by the sim
    private float GetInjurySeverity01()
    {
        if (!UseRealisticInjuryMovement)
        {
            return 0f;
        }

        float healthSeverity = Mathf.InverseLerp(InjuryMovementStartHealthRatio, 0.20f, GetHealthRatio());
        float recentBiteSeverity = survivalRecentDamageTimer > 0f ? 0.12f : 0f;
        return Mathf.Clamp01(Mathf.Max(healthSeverity, recentBiteSeverity));
    }

    // Checks if it should continue eating fresh kill carrion right now
    private bool ShouldContinueEatingFreshKillCarrion()
    {
        if (!PrioritiseFreshKills || freshKillPriorityTimer <= 0f || claimedFreshKillCarrion == null || claimedFreshKillCarrion.IsConsumed)
        {
            return false;
        }

        if (GetRemainingStomachSpace() <= 0.05f)
        {
            return false;
        }

        // Own kills should be treated as the safest, most valuable food source.
        // Do not leave just because energy is high; keep feeding until the stomach is mostly full,
        // the carrion is gone, or the priority timer expires.
        float stomachRatio = GetStomachFullness01();
        if (stomachRatio >= FreshKillStopStomachRatio)
        {
            return false;
        }

        float energyRatio = GetEffectiveEnergyRatio();
        return energyRatio < 0.995f || stomachRatio < FreshKillStopStomachRatio;
    }

    // Registers fresh kill with the manager list
    private void RegisterFreshKill(Vector3 deathPosition)
    {
        if (!PrioritiseFreshKills || Candidate == null || Candidate.Genome == null)
        {
            return;
        }

        // Only meaningful predators should claim a kill. Opportunistic biters can still eat carrion normally.
        float predatorDrive = Candidate.Genome.MeatDiet * 0.65f + Candidate.Genome.CarrionDiet * 0.25f + Candidate.Genome.Aggression * 0.25f;
        if (predatorDrive < 0.42f)
        {
            return;
        }

        freshKillSearchPosition = deathPosition;
        hasFreshKillSearchPosition = true;
        freshKillPriorityTimer = Mathf.Max(freshKillPriorityTimer, FreshKillPriorityTime);
        ResolveFreshKillCarrion();
    }

    // Handles resolve fresh kill carrion
    private void ResolveFreshKillCarrion()
    {
        if (!PrioritiseFreshKills || !hasFreshKillSearchPosition || freshKillPriorityTimer <= 0f)
        {
            return;
        }

        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null)
        {
            return;
        }

        List<CarrionSource> nearby = manager.GetNearbyCarrion(freshKillSearchPosition, Mathf.Max(0.5f, FreshKillClaimRadius));
        CarrionSource best = null;
        float bestDistance = FreshKillClaimRadius * FreshKillClaimRadius;
        for (int i = 0; i < nearby.Count; i++)
        {
            CarrionSource carrion = nearby[i];
            if (carrion == null || carrion.IsConsumed)
            {
                continue;
            }

            float distance = (carrion.transform.position - freshKillSearchPosition).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = carrion;
            }
        }

        if (best == null)
        {
            return;
        }

        claimedFreshKillCarrion = best;
        nearestCarrion = best;
        retainedCarrion = best;
        retainedTargetTimer = Mathf.Max(retainedTargetTimer, TargetRetainTime);
        focusedPrey = null;
        hunterPreyFocusTimer = 0f;
        temporarilyIgnoredCarrion = null;
        if (nearestFood != null)
        {
            temporarilyIgnoredFood = nearestFood;
            ignoredResourceTimer = Mathf.Max(ignoredResourceTimer, Mathf.Min(8f, FreshKillPriorityTime * 0.35f));
            nearestFood = null;
            retainedFood = null;
        }
        RememberFoodArea(best.transform.position, false);
    }

    // Applies fresh kill carrion priority to the current object
    private void ApplyFreshKillCarrionPriority()
    {
        if (!PrioritiseFreshKills)
        {
            return;
        }

        if (claimedFreshKillCarrion == null && hasFreshKillSearchPosition && freshKillPriorityTimer > 0f)
        {
            ResolveFreshKillCarrion();
        }

        if (!ShouldContinueEatingFreshKillCarrion())
        {
            if (freshKillPriorityTimer <= 0f || claimedFreshKillCarrion == null || claimedFreshKillCarrion.IsConsumed || GetRemainingStomachSpace() <= 0.05f)
            {
                ClearFreshKillClaim();
            }
            return;
        }

        nearestCarrion = claimedFreshKillCarrion;
        retainedCarrion = claimedFreshKillCarrion;
        retainedTargetTimer = Mathf.Max(retainedTargetTimer, TargetRetainTime);
        nearestFood = null;
        retainedFood = null;
    }

    // Clears fresh kill claim ready for fresh data
    private void ClearFreshKillClaim()
    {
        claimedFreshKillCarrion = null;
        hasFreshKillSearchPosition = false;
        freshKillPriorityTimer = 0f;
    }

    // Gets the current escape pull used by the sim
    private Vector3 GetCurrentEscapePull(Vector3 boundaryPull)
    {
        lastCurrentStress = 0f;
        lastCurrentFlow = Vector3.zero;

        if (!UseCurrentEscapeSteering)
        {
            return Vector3.zero;
        }

        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null)
        {
            return Vector3.zero;
        }

        Vector3 position = rb != null ? rb.position : transform.position;
        Vector3 flow = manager.GetCurrentVelocityAt(position);
        float stress = manager.GetCurrentStressAt(position);
        lastCurrentStress = stress;
        lastCurrentFlow = flow;

        bool strongFlow = flow.magnitude >= CurrentEscapeFlowThreshold || stress >= CurrentEscapeStressThreshold;
        bool boundaryBlocked = boundaryPull.sqrMagnitude > 0.08f;
        bool probablyStuck = lowProgressTimer >= StuckCheckTime * 0.55f || stuckEscapeTimer > 0f;

        if (!strongFlow && currentEscapeTimer <= 0f)
        {
            return Vector3.zero;
        }

        if (strongFlow && (stress >= CurrentEscapeStressThreshold || boundaryBlocked || probablyStuck))
        {
            Vector3 centre = GetDirectionToSimulationCentre();
            centre.y *= 0.35f;
            if (centre.sqrMagnitude <= 0.001f)
            {
                centre = transform.forward;
            }

            Vector3 side = Vector3.zero;
            Vector3 againstFlow = Vector3.zero;
            if (flow.sqrMagnitude > 0.001f)
            {
                Vector3 flowDir = flow.normalized;
                againstFlow = -flowDir;
                side = Vector3.Cross(Vector3.up, flowDir);
                if (side.sqrMagnitude <= 0.001f)
                {
                    side = GetFallbackSideDirection();
                }
                if (Vector3.Dot(-side, centre) > Vector3.Dot(side, centre))
                {
                    side = -side;
                }
            }
            else
            {
                side = GetFallbackSideDirection();
            }

            Vector3 escape = centre.normalized * CurrentEscapeCentreBias
                + side.normalized * CurrentEscapeSideBias
                + againstFlow * CurrentEscapeAgainstFlowBias;

            if (boundaryBlocked)
            {
                escape += boundaryPull.normalized * CurrentEscapeBoundaryBoost;
            }

            if (probablyStuck)
            {
                escape += GetFallbackSideDirection() * CurrentEscapeStuckBoost;
            }

            if (lastTerrainAvoidance.sqrMagnitude > 0.05f)
            {
                Vector3 terrainSafe = lastTerrainAvoidance;
                terrainSafe.y = Mathf.Max(0f, terrainSafe.y);
                if (terrainSafe.sqrMagnitude > 0.001f)
                {
                    escape += terrainSafe.normalized * FleeWallBackOffWeight;
                }
            }

            if (escape.sqrMagnitude > 0.001f)
            {
                currentEscapeDirection = Vector3.Slerp(
                    currentEscapeDirection.sqrMagnitude > 0.001f ? currentEscapeDirection.normalized : escape.normalized,
                    escape.normalized,
                    0.45f);
                currentEscapeTimer = Mathf.Max(currentEscapeTimer, CurrentEscapeMemoryTime);
                RememberDangerArea(position);
            }
        }

        if (currentEscapeTimer <= 0f || currentEscapeDirection.sqrMagnitude <= 0.001f)
        {
            return Vector3.zero;
        }

        float stressBoost = Mathf.Lerp(0.55f, 1.4f, Mathf.Clamp01(stress));
        float boundaryBoost = boundaryBlocked ? CurrentEscapeBoundaryBoost : 1f;
        float stuckBoost = probablyStuck ? CurrentEscapeStuckBoost : 1f;
        return currentEscapeDirection.normalized * CurrentEscapeWeight * stressBoost * boundaryBoost * stuckBoost;
    }

    // Gets the cached schooling pull used by the sim
    private Vector3 GetCachedSchoolingPull(float hungerPressure)
    {
        if (socialScanTimer <= 0f)
        {
            socialScanTimer = Mathf.Max(0.02f, SocialScanInterval) * Random.Range(0.85f, 1.15f);
            cachedSchoolPull = GetEvolvedSchoolingPull(hungerPressure);
        }

        return cachedSchoolPull;
    }

    // Gets the evolved schooling pull used by the sim
    private Vector3 GetEvolvedSchoolingPull(float hungerPressure)
    {
        lastSchoolCohesion = Vector3.zero;
        lastSchoolAlignment = Vector3.zero;
        lastSchoolSeparation = Vector3.zero;
        lastEmergencyUnstick = Vector3.zero;
        lastFriendlyCount = 0;
        lastDifferentCount = 0;
        lastDenseCrowdCount = 0;
        lastFeedingCrowdCount = 0;

        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null || Candidate == null || Candidate.Genome == null)
        {
            return Vector3.zero;
        }

        Vector3 ownPosition = rb != null ? rb.position : transform.position;
        float schoolSearchRadius = Mathf.Max(SchoolPerceptionRadius, Mathf.Max(DenseCrowdRadius, FeedingCrowdRadius));
        List<MarineCreatureAgent> creatures = manager.GetNearbyCreatures(ownPosition, schoolSearchRadius);
        if (creatures == null || creatures.Count <= 1)
        {
            return Vector3.zero;
        }
        float ownRadius = GetPersonalRadius();
        float schoolDrive = Mathf.Clamp01(Candidate.Genome.GroupingChance * 0.65f + Candidate.Genome.SchoolTightness * 0.45f + Candidate.Genome.FoodSharing * 0.15f);
        float schoolReductionNearFood = HasCloseFoodTarget() ? Mathf.Lerp(1f, FoodPrioritySchoolReduction, hungerPressure) : 1f;
        float cohesionWeight = CohesionWeight * schoolDrive * schoolReductionNearFood;
        float alignmentWeight = AlignmentWeight * schoolDrive * schoolReductionNearFood;
        float separationWeight = SeparationWeight * Mathf.Lerp(1.25f, 0.65f, Candidate.Genome.SchoolTightness);

        Vector3 friendlyCentre = Vector3.zero;
        Vector3 friendlyDirection = Vector3.zero;
        Vector3 separation = Vector3.zero;
        Vector3 differentSpacing = Vector3.zero;
        int friendlyCount = 0;
        int separationCount = 0;
        int differentCount = 0;
        int used = 0;

        for (int i = 0; i < creatures.Count; i++)
        {
            MarineCreatureAgent other = creatures[i];
            if (other == null || other == this || other.Candidate == null || other.Candidate.Genome == null || other.EffectiveStats == null)
            {
                continue;
            }

            Vector3 otherPosition = other.transform.position;
            Vector3 awayVector = ownPosition - otherPosition;
            float distance = awayVector.magnitude;
            if (distance <= 0.0001f)
            {
                awayVector = GetFallbackSideDirection();
                distance = 0.0001f;
            }

            if (distance > SchoolPerceptionRadius)
            {
                continue;
            }

            bool friendly = IsFriendlyByMorph(other);
            bool realThreat = IsActualThreat(other);
            if (realThreat)
            {
                continue;
            }

            if (IsTryingToReachCurrentPrey(other))
            {
                continue;
            }

            float otherRadius = other.GetPersonalRadius();
            float personalSpace = (ownRadius + otherRadius) * PersonalSpaceMultiplier;
            Vector3 away = awayVector / distance;
            Vector3 mostlySidewaysAway = away;
            mostlySidewaysAway.y *= HorizontalSeparationBias;
            if (mostlySidewaysAway.sqrMagnitude <= 0.0001f)
            {
                mostlySidewaysAway = GetHorizontalAwayFrom(otherPosition);
            }
            mostlySidewaysAway.Normalize();

            if (friendly)
            {
                float leaderWeight = Mathf.Lerp(0.65f, 1.45f, other.GetLeadershipWeight());
                friendlyCentre += otherPosition * leaderWeight;
                Vector3 otherVelocity = other.GetCurrentVelocityOrForward();
                friendlyDirection += otherVelocity.normalized * leaderWeight;
                friendlyCount++;

                if (distance < personalSpace)
                {
                    float t = 1f - Mathf.Clamp01(distance / personalSpace);
                    float emergency = distance < personalSpace * EmergencyUnstickRadiusMultiplier ? EmergencyUnstickWeight : 1f;
                    separation += mostlySidewaysAway * t * t * emergency;
                    separationCount++;
                }
            }
            else
            {
                float differentSpace = personalSpace * Mathf.Lerp(1.0f, 1.45f, Candidate.Genome.Territoriality);
                if (distance < differentSpace)
                {
                    float t = 1f - Mathf.Clamp01(distance / differentSpace);
                    differentSpacing += mostlySidewaysAway * t * t;
                    differentCount++;
                }
            }

            used++;
            if (used >= MaxSchoolNeighbours)
            {
                break;
            }
        }

        if (friendlyCount > 0)
        {
            friendlyCentre /= friendlyCount;
            Vector3 toCentre = friendlyCentre - ownPosition;
            if (toCentre.sqrMagnitude > 0.001f)
            {
                lastSchoolCohesion = toCentre.normalized * cohesionWeight;
            }

            friendlyDirection /= friendlyCount;
            if (friendlyDirection.sqrMagnitude > 0.001f)
            {
                lastSchoolAlignment = friendlyDirection.normalized * alignmentWeight;
            }
        }

        if (separationCount > 0)
        {
            lastSchoolSeparation = separation.normalized * separationWeight;
        }

        if (differentCount > 0)
        {
            lastSchoolSeparation += differentSpacing.normalized * DifferentMorphSpacingWeight * Mathf.Lerp(0.55f, 1.25f, Candidate.Genome.Territoriality + Candidate.Genome.Aggression * 0.35f);
        }

        lastFriendlyCount = friendlyCount;
        lastDifferentCount = differentCount;

        lastLeaderPull = GetHungryLeaderPull(creatures, ownPosition, hungerPressure);

        Vector3 social = lastSchoolCohesion + lastSchoolAlignment + lastSchoolSeparation + lastLeaderPull;
        ClampSocialVertical(ref social);
        return social;
    }

    // Gets the cached crowd stabilisation pull used by the sim
    private Vector3 GetCachedCrowdStabilisationPull(float hungerPressure)
    {
        if (crowdScanTimer <= 0f)
        {
            crowdScanTimer = Mathf.Max(0.02f, SocialScanInterval) * Random.Range(0.85f, 1.15f);
            cachedCrowdPull = GetCrowdStabilisationPull(hungerPressure);
        }

        return cachedCrowdPull;
    }

    // Gets the crowd stabilisation pull used by the sim
    private Vector3 GetCrowdStabilisationPull(float hungerPressure)
    {
        lastEmergencyUnstick = Vector3.zero;

        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null || Candidate == null || Candidate.Genome == null)
        {
            return Vector3.zero;
        }

        Vector3 ownPosition = rb != null ? rb.position : transform.position;
        float crowdSearchRadius = Mathf.Max(OverlapSearchRadius, Mathf.Max(DenseCrowdRadius, FeedingCrowdRadius));
        List<MarineCreatureAgent> creatures = manager.GetNearbyCreatures(ownPosition, crowdSearchRadius);
        if (creatures == null || creatures.Count <= 1)
        {
            return Vector3.zero;
        }
        Vector3 horizontalPush = Vector3.zero;
        Vector3? staticTarget = GetPrimaryStaticFoodTargetPosition();
        bool aroundStaticTarget = staticTarget.HasValue && Vector3.Distance(ownPosition, staticTarget.Value) <= FeedingCrowdRadius;
        int denseCount = 0;
        int feedingCount = 0;

        for (int i = 0; i < creatures.Count; i++)
        {
            MarineCreatureAgent other = creatures[i];
            if (other == null || other == this || other.Candidate == null || other.Candidate.Genome == null)
            {
                continue;
            }

            Vector3 otherPosition = other.transform.position;
            Vector3 away = ownPosition - otherPosition;
            float distance = away.magnitude;
            if (distance <= 0.0001f)
            {
                away = GetFallbackSideDirection();
                distance = 0.0001f;
            }

            if (distance <= DenseCrowdRadius)
            {
                Vector3 horizontalAway = away;
                horizontalAway.y *= DenseCrowdVerticalInfluence;
                if (horizontalAway.sqrMagnitude <= 0.0001f)
                {
                    horizontalAway = GetHorizontalAwayFrom(otherPosition);
                }

                float t = 1f - Mathf.Clamp01(distance / DenseCrowdRadius);
                horizontalPush += horizontalAway.normalized * t * t;
                denseCount++;
            }

            if (aroundStaticTarget && staticTarget.HasValue && Vector3.Distance(otherPosition, staticTarget.Value) <= FeedingCrowdRadius)
            {
                feedingCount++;
            }
        }

        lastDenseCrowdCount = denseCount;
        lastFeedingCrowdCount = feedingCount;

        Vector3 pull = Vector3.zero;
        if (denseCount > 0 && horizontalPush.sqrMagnitude > 0.0001f)
        {
            float weight = DenseCrowdHorizontalWeight * Mathf.Lerp(1.15f, 0.45f, Candidate.Genome.SchoolTightness);
            pull += horizontalPush.normalized * weight;
            lastEmergencyUnstick = horizontalPush.normalized * weight;
        }

        if (aroundStaticTarget && staticTarget.HasValue && feedingCount >= FeedingCrowdSoftLimit)
        {
            float angle = ((Candidate != null ? Candidate.Id : GetInstanceID()) * 137.508f) * Mathf.Deg2Rad;
            Vector3 ring = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 targetSlot = staticTarget.Value + ring * Mathf.Lerp(0.85f, 1.65f, Candidate.Genome.FoodSharing);
            Vector3 toSlot = targetSlot - ownPosition;
            toSlot.y *= 0.04f;
            if (toSlot.sqrMagnitude > 0.0001f)
            {
                pull += toSlot.normalized * FeedingCrowdSideStepWeight * Mathf.Clamp01(0.35f + hungerPressure);
            }
        }

        ClampSocialVertical(ref pull);
        return pull;
    }


    // Gets the hungry leader pull used by the sim
    private Vector3 GetHungryLeaderPull(List<MarineCreatureAgent> creatures, Vector3 ownPosition, float hungerPressure)
    {
        currentHungryLeader = null;

        if (IsHungryEnoughToSearch())
        {
            return Vector3.zero;
        }

        if (Candidate == null || Candidate.Genome == null || creatures == null)
        {
            return Vector3.zero;
        }

        float followDrive = Mathf.Clamp01(Candidate.Genome.GroupingChance * 0.35f + Candidate.Genome.FoodSharing * 0.45f + (1f - Candidate.Genome.Selfishness) * 0.35f);
        if (followDrive <= 0.12f)
        {
            return Vector3.zero;
        }

        MarineCreatureAgent bestLeader = null;
        float bestScore = 0f;
        float radiusSqr = HungryLeaderSearchRadius * HungryLeaderSearchRadius;

        for (int i = 0; i < creatures.Count; i++)
        {
            MarineCreatureAgent other = creatures[i];
            if (other == null || other == this || other.Candidate == null || other.Candidate.Genome == null)
            {
                continue;
            }

            if (!IsFriendlyByMorph(other) || !other.IsHungryEnoughToSearch())
            {
                continue;
            }

            Vector3 toOther = other.transform.position - ownPosition;
            float distSqr = toOther.sqrMagnitude;
            if (distSqr > radiusSqr)
            {
                continue;
            }

            float distanceScore = 1f - Mathf.Clamp01(Mathf.Sqrt(distSqr) / Mathf.Max(0.1f, HungryLeaderSearchRadius));
            float score = other.GetHungerPressure() * 0.45f + other.Candidate.Genome.Leadership * 0.35f + other.Candidate.Genome.Bravery * 0.20f;
            score *= Mathf.Lerp(0.35f, 1f, distanceScore);

            if (score > bestScore)
            {
                bestScore = score;
                bestLeader = other;
            }
        }

        if (bestLeader == null)
        {
            return Vector3.zero;
        }

        currentHungryLeader = bestLeader;
        Vector3 pull = bestLeader.transform.position - ownPosition;
        pull.y *= 0.25f;
        if (pull.sqrMagnitude <= 0.001f)
        {
            return Vector3.zero;
        }

        if (Candidate != null)
        {
            Candidate.LeaderFollowEvents++;
        }

        return pull.normalized * HungryLeaderPullWeight * followDrive * bestScore;
    }

    // Handles clamp social vertical
    private void ClampSocialVertical(ref Vector3 social)
    {
        float maxY = Mathf.Max(0f, MaxSocialVerticalComponent);
        social.y = Mathf.Clamp(social.y * 0.15f, -maxY, maxY);
    }

    // Handles stabilise vertical steering
    private void StabiliseVerticalSteering(ref Vector3 combined, Vector3 targetPull, Vector3 dangerPull, Vector3 boundaryPull, float hungerPressure)
    {
        bool targetNeedsVertical = targetPull.sqrMagnitude > 0.01f && Mathf.Abs(targetPull.normalized.y) > 0.22f;
        bool dangerNeedsVertical = dangerPull.sqrMagnitude > 0.01f && Mathf.Abs(dangerPull.normalized.y) > 0.35f;
        bool boundsNeedVertical = boundaryPull.sqrMagnitude > 0.01f && Mathf.Abs(boundaryPull.normalized.y) > 0.35f;

        if (lastDenseCrowdCount > 0 && !targetNeedsVertical && !dangerNeedsVertical && !boundsNeedVertical)
        {
            combined.y *= Mathf.Lerp(0.12f, 0.45f, Candidate != null && Candidate.Genome != null ? Candidate.Genome.DepthFlexibility : 0.5f);
        }

        if (lastFeedingCrowdCount >= FeedingCrowdSoftLimit && !targetNeedsVertical)
        {
            combined.y *= 0.18f;
        }
    }

    // Gets the cached danger avoidance pull used by the sim
    private Vector3 GetCachedDangerAvoidancePull(float hungerPressure)
    {
        if (dangerScanTimer <= 0f)
        {
            dangerScanTimer = Mathf.Max(0.02f, SocialScanInterval) * Random.Range(0.85f, 1.15f);
            cachedDangerPull = GetDangerAvoidancePull(hungerPressure);
        }

        return cachedDangerPull;
    }

    // Gets the danger avoidance pull used by the sim
    private Vector3 GetDangerAvoidancePull(float hungerPressure)
    {
        lastThreatAvoidance = Vector3.zero;
        lastThreatCount = 0;

        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null)
        {
            return Vector3.zero;
        }

        Vector3 ownPosition = rb != null ? rb.position : transform.position;
        float dangerSearchRadius = Mathf.Max(10f, GetThreatRange() * 2.2f);
        List<MarineCreatureAgent> creatures = manager.GetNearbyCreatures(ownPosition, dangerSearchRadius);
        if (creatures == null || creatures.Count <= 1)
        {
            return Vector3.zero;
        }
        float fear = Mathf.Lerp(0.25f, 1.35f, 1f - Candidate.Genome.RiskTolerance);
        Vector3 avoid = Vector3.zero;

        for (int i = 0; i < creatures.Count; i++)
        {
            MarineCreatureAgent other = creatures[i];
            if (other == null || other == this || !IsActualThreat(other))
            {
                continue;
            }

            Vector3 away = ownPosition - other.transform.position;
            float distance = away.magnitude;
            if (distance <= 0.001f)
            {
                away = GetFallbackSideDirection();
                distance = 0.001f;
            }

            float dangerRange = Mathf.Max(4f, GetThreatRange() + other.GetThreatRange() * 0.5f);
            if (distance > dangerRange)
            {
                continue;
            }

            float t = 1f - Mathf.Clamp01(distance / dangerRange);
            Vector3 direction = away.normalized;
            if (Mathf.Abs(direction.y) < 0.3f)
            {
                direction.y *= 0.35f;
            }
            avoid += direction.normalized * t * t * fear;
            lastThreatCount++;
        }

        if (groupPredatorWarningTimer > 0f && groupPredatorWarningDirection.sqrMagnitude > 0.001f)
        {
            // Early group warnings create a flee pull without making every predator in the area count as a direct threat.
            // This stops the whole ecosystem freezing while still letting schools scatter when one fish spots danger.
            avoid += groupPredatorWarningDirection.normalized * Mathf.Lerp(2.0f, 4.8f, fear);
        }

        if (avoid.sqrMagnitude > 0.001f)
        {
            lastThreatAvoidance = avoid.normalized * Mathf.Lerp(2.2f, 5.5f, fear);
        }

        lastThreatAvoidance += GetDangerMemoryPull();
        return lastThreatAvoidance;
    }

    // Gets the depth preference pull used by the sim
    private Vector3 GetDepthPreferencePull(Vector3 targetPull, Vector3 dangerPull, float hungerPressure)
    {
        if (EvolutionEcosystemManager.Instance == null || Candidate == null || Candidate.Genome == null)
        {
            return Vector3.zero;
        }

        Vector3? activeTarget = GetPrimaryTargetPosition();
        if (activeTarget.HasValue && Mathf.Abs(activeTarget.Value.y - transform.position.y) > 0.75f)
        {
            return Vector3.zero;
        }

        Vector3 centre = EvolutionEcosystemManager.Instance.transform.position;
        Vector3 half = EvolutionEcosystemManager.Instance.SimulationAreaSize * 0.5f;
        float low = centre.y - half.y + DepthPadding;
        float high = centre.y + half.y - DepthPadding;
        float preferredY = Mathf.Lerp(low, high, Candidate.Genome.PreferredDepth01);
        float yError = preferredY - transform.position.y;
        float normalised = Mathf.Clamp(yError / Mathf.Max(0.1f, half.y), -1f, 1f);

        bool fleeingOrThreatened = currentBrainMode == FishAutonomousBehaviourMode.Fleeing || survivalEmergencyTimer > 0f || groupPredatorWarningTimer > 0f || lastThreatCount > 0 || HasCloseVisiblePredatorThreat();
        if (fleeingOrThreatened && normalised < 0f && EvolutionEcosystemManager.Instance.TryGetTerrainHeight(transform.position, out float depthGroundY))
        {
            float clearance = transform.position.y - depthGroundY;
            if (clearance < FleeFloorAvoidanceClearance)
            {
                normalised = Mathf.Max(0f, normalised * FleeDownwardClampNearFloor);
            }
        }
        float flexibility = Candidate.Genome.DepthFlexibility;
        float strength = DepthPreferenceStrength * Mathf.Lerp(1.15f, 0.15f, flexibility);

        if (targetPull.sqrMagnitude > 0.01f || dangerPull.sqrMagnitude > 0.01f)
        {
            strength *= Mathf.Lerp(0.25f, 0.65f, flexibility);
        }

        strength *= Mathf.Lerp(0.85f, 0.45f, hungerPressure);
        Vector3 depth = Vector3.up * normalised * strength;
        return depth;
    }

    // Gets the close target pull used by the sim
    private Vector3 GetCloseTargetPull(float hungerPressure)
    {
        Vector3? target = GetPrimaryMovementTargetPosition();
        if (!target.HasValue)
        {
            return Vector3.zero;
        }

        Vector3 mouth = GetMouthWorldPosition();
        Vector3 toTarget = target.Value - mouth;
        float distance = toTarget.magnitude;
        if (distance <= 0.001f || distance > CloseTargetSlowdownDistance)
        {
            return Vector3.zero;
        }

        float closeness = 1f - Mathf.Clamp01(distance / CloseTargetSlowdownDistance);
        return toTarget.normalized * CloseTargetPull * closeness * Mathf.Lerp(0.65f, 1.35f, hungerPressure);
    }

    // Gets the food queue pull used by the sim
    private Vector3 GetFoodQueuePull(float hungerPressure)
    {
        return Vector3.zero;
    }

    // Gets the brain pull used by the sim
    private Vector3 GetBrainPull(float energyRatio)
    {
        if (!EvaluateEvolvedBrain(energyRatio, GetHealthRatio(), GetStomachFullness01(), GetHungerPressure()))
        {
            return Vector3.zero;
        }

        Vector3 brain = new Vector3(
            brainOutputs.Length > 0 ? brainOutputs[0] : 0f,
            brainOutputs.Length > 1 ? brainOutputs[1] : 0f,
            brainOutputs.Length > 2 ? brainOutputs[2] : 0f);

        if (brain.sqrMagnitude <= 0.001f)
        {
            return Vector3.zero;
        }

        brain.y *= Mathf.Clamp(EffectiveStats != null ? EffectiveStats.VerticalControl : 1f, 0.25f, 2.5f);
        return brain.normalized * BrainInfluence * Mathf.Lerp(0.35f, 1f, energyRatio);
    }

    // Handles evaluate evolved brain
    private bool EvaluateEvolvedBrain(float energyRatio, float healthRatio, float stomachRatio, float hungerPressure)
    {
        if (Candidate == null || Candidate.Genome == null || Candidate.Genome.Brain == null)
        {
            return false;
        }

        SimpleNeuralNetwork network = Candidate.Genome.Brain;
        if (brainEvaluationFrame == Time.frameCount && brainOutputs != null && brainOutputs.Length >= network.OutputCount)
        {
            return true;
        }

        if (brainInputs == null || brainInputs.Length != network.InputCount || brainOutputs == null || brainOutputs.Length < network.OutputCount || brainHiddenScratch == null || brainHiddenScratch.Length < network.HiddenCount)
        {
            brainInputs = new float[network.InputCount];
            brainOutputs = new float[network.OutputCount];
            brainHiddenScratch = new float[network.HiddenCount];
        }

        for (int i = 0; i < brainInputs.Length; i++)
        {
            brainInputs[i] = 0f;
        }

        Vector3 foodDirection = lastFoodDirection;
        if (foodDirection.sqrMagnitude <= 0.001f && retainedFood != null && !retainedFood.IsConsumed)
        {
            foodDirection = (retainedFood.transform.position - transform.position).normalized;
        }

        Vector3 carrionDirection = lastCarrionDirection;
        if (carrionDirection.sqrMagnitude <= 0.001f && retainedCarrion != null && !retainedCarrion.IsConsumed)
        {
            carrionDirection = (retainedCarrion.transform.position - transform.position).normalized;
        }

        Vector3 preyDirection = lastPreyDirection;
        if (preyDirection.sqrMagnitude <= 0.001f && retainedPrey != null)
        {
            preyDirection = (retainedPrey.transform.position - transform.position).normalized;
        }

        Vector3 mateDirection = Vector3.zero;
        if (currentMateTarget != null)
        {
            mateDirection = (currentMateTarget.transform.position - transform.position).normalized;
        }

        float homeDistance01 = 0f;
        if (hasHomeArea)
        {
            homeDistance01 = Mathf.Clamp01(Vector3.Distance(transform.position, homeArea) / Mathf.Max(1f, HomeAttractionDistance * 2f));
        }

        float threat01 = Mathf.Clamp01(lastThreatAvoidance.magnitude + (dangerMemoryTimer > 0f ? 0.35f : 0f) + (mobbingPressureTimer > 0f ? 0.25f : 0f));
        float social01 = Mathf.Clamp01(lastFriendlyCount / Mathf.Max(1f, (float)MaxSchoolNeighbours));
        float prey01 = retainedPrey != null || nearestPrey != null ? 1f : 0f;
        float food01 = retainedFood != null || nearestFood != null || hasFoodMemory ? 1f : 0f;
        float mate01 = currentMateTarget != null ? 1f : 0f;
        float mode01 = Mathf.Clamp01((float)((int)currentBrainMode) / Mathf.Max(1f, (float)((int)FishAutonomousBehaviourMode.Recovering)));

        SetBrainInput(0, energyRatio);
        SetBrainInput(1, healthRatio);
        SetBrainInput(2, stomachRatio);
        SetBrainInput(3, hungerPressure);
        SetBrainInput(4, foodDirection.x);
        SetBrainInput(5, foodDirection.y);
        SetBrainInput(6, foodDirection.z);
        SetBrainInput(7, carrionDirection.x);
        SetBrainInput(8, carrionDirection.y);
        SetBrainInput(9, carrionDirection.z);
        SetBrainInput(10, preyDirection.x);
        SetBrainInput(11, preyDirection.y);
        SetBrainInput(12, preyDirection.z);
        SetBrainInput(13, threat01);
        SetBrainInput(14, mateDirection.x);
        SetBrainInput(15, mateDirection.y);
        SetBrainInput(16, mateDirection.z);
        SetBrainInput(17, social01);
        SetBrainInput(18, homeDistance01);
        float memoryDecay = Candidate.Genome != null ? Mathf.Clamp01(Candidate.Genome.BrainMemoryDecay) : 0.82f;
        brainMemoryFood = Mathf.Lerp(food01, brainMemoryFood, memoryDecay);
        brainMemoryPrey = Mathf.Lerp(prey01, brainMemoryPrey, memoryDecay);
        brainMemoryThreat = Mathf.Lerp(threat01, brainMemoryThreat, memoryDecay);
        brainMemoryEnergyStress = Mathf.Lerp(Mathf.Clamp01((1f - energyRatio) * 0.55f + (1f - healthRatio) * 0.45f), brainMemoryEnergyStress, memoryDecay);

        SetBrainInput(19, Mathf.Clamp01(food01 * 0.35f + prey01 * 0.25f + mate01 * 0.2f + mode01 * 0.2f));
        SetBrainInput(20, brainMemoryFood);
        SetBrainInput(21, brainMemoryPrey);
        SetBrainInput(22, brainMemoryThreat);
        SetBrainInput(23, Mathf.Clamp01(brainMemoryEnergyStress + lastCurrentStress * 0.35f));

        if (!network.EvaluateNonAlloc(brainInputs, brainOutputs, brainHiddenScratch))
        {
            brainEvaluationFrame = -1;
            return false;
        }

        brainEvaluationFrame = Time.frameCount;
        brainOutputFoodBias = GetBrainOutput(3);
        brainOutputHuntBias = GetBrainOutput(4);
        brainOutputFleeBias = GetBrainOutput(5);
        brainOutputMateSocialBias = GetBrainOutput(6);
        brainOutputExploreHomeBias = GetBrainOutput(7);
        brainOutputRestBias = GetBrainOutput(8);
        brainOutputSprintBias = GetBrainOutput(9);
        return true;
    }

    // Sets the brain input value used later
    private void SetBrainInput(int index, float value)
    {
        if (brainInputs != null && index >= 0 && index < brainInputs.Length)
        {
            brainInputs[index] = Mathf.Clamp(value, -1f, 1f);
        }
    }

    // Gets the brain output used by the sim
    private float GetBrainOutput(int index)
    {
        if (brainOutputs == null || index < 0 || index >= brainOutputs.Length)
        {
            return 0f;
        }

        return Mathf.Clamp(brainOutputs[index], -1f, 1f);
    }

    private void ApplyEvolvedBrainDecisionBiases(
        ref float foodDesire,
        ref float huntDesire,
        ref float mateDesire,
        ref float schoolDesire,
        ref float exploreDesire,
        ref float homeDesire,
        ref float fleeDesire,
        ref float restDesire)
    {
        if (BrainDecisionInfluence <= 0f || Candidate == null || Candidate.Genome == null || Candidate.Genome.Brain == null)
        {
            return;
        }

        float strength = Mathf.Clamp01(BrainDecisionInfluence);
        foodDesire = Mathf.Clamp01(foodDesire + brainOutputFoodBias * strength);
        huntDesire = Mathf.Clamp01(huntDesire + brainOutputHuntBias * strength * Mathf.Lerp(0.45f, 1.35f, Candidate.Genome.MeatDiet));
        fleeDesire = Mathf.Clamp01(fleeDesire + brainOutputFleeBias * strength * Mathf.Lerp(0.45f, 1.25f, 1f - Candidate.Genome.RiskTolerance));
        mateDesire = Mathf.Clamp01(mateDesire + brainOutputMateSocialBias * strength * 0.85f);
        schoolDesire = Mathf.Clamp01(schoolDesire + brainOutputMateSocialBias * strength * 0.65f * Mathf.Lerp(0.5f, 1.25f, Candidate.Genome.GroupingChance));
        restDesire = Mathf.Clamp01(restDesire + brainOutputRestBias * strength * Mathf.Lerp(0.35f, 1.15f, 1f - GetHealthRatio()));

        if (brainOutputExploreHomeBias >= 0f)
        {
            exploreDesire = Mathf.Clamp01(exploreDesire + brainOutputExploreHomeBias * strength);
            homeDesire = Mathf.Clamp01(homeDesire - brainOutputExploreHomeBias * strength * 0.45f);
        }
        else
        {
            homeDesire = Mathf.Clamp01(homeDesire + -brainOutputExploreHomeBias * strength);
            exploreDesire = Mathf.Clamp01(exploreDesire + brainOutputExploreHomeBias * strength * 0.45f);
        }
    }

    // Gets the wander pull used by the sim
    private Vector3 GetWanderPull(float energyRatio)
    {
        wanderTimer -= Time.fixedDeltaTime;
        if (wanderTimer <= 0f || wanderDirection.sqrMagnitude < 0.01f)
        {
            PickNewWanderDirection();
        }

        return wanderDirection * WanderStrength * Mathf.Lerp(0.25f, 1f, energyRatio) * Mathf.Lerp(0.75f, 1.2f, Candidate.Genome.ActivityCycle);
    }

    // Gets the home area pull used by the sim
    private Vector3 GetHomeAreaPull(float hungerPressure)
    {
        if (!hasHomeArea || Candidate == null || Candidate.Genome == null || hungerPressure > Candidate.Genome.HungerThreshold * 0.75f)
        {
            return Vector3.zero;
        }

        Vector3 toHome = homeArea - transform.position;
        float distance = toHome.magnitude;
        if (distance <= HomeAttractionDistance || distance <= 0.001f)
        {
            return Vector3.zero;
        }

        float confidence = Mathf.Clamp01(homeConfidence * HomeMemoryStrength);
        float exploration = Mathf.Clamp01(Candidate.Genome.ExplorationDrive);
        float pullStrength = HomeAttractionWeight * confidence * Mathf.Lerp(1.25f, 0.35f, exploration);

        toHome.y *= Mathf.Lerp(0.35f, 1f, Candidate.Genome.DepthFlexibility);
        if (toHome.sqrMagnitude <= 0.001f)
        {
            return Vector3.zero;
        }

        return toHome.normalized * pullStrength;
    }

    // Gets the danger memory pull used by the sim
    private Vector3 GetDangerMemoryPull()
    {
        if (dangerMemoryTimer <= 0f)
        {
            return Vector3.zero;
        }

        Vector3 away = transform.position - rememberedDangerArea;
        float distance = away.magnitude;
        if (distance <= 0.001f || distance > GroupDangerRadius * 2.5f)
        {
            return Vector3.zero;
        }

        away.y *= 0.25f;
        if (away.sqrMagnitude <= 0.001f)
        {
            return Vector3.zero;
        }

        float t = 1f - Mathf.Clamp01(distance / Mathf.Max(0.1f, GroupDangerRadius * 2.5f));
        return away.normalized * DangerMemoryAvoidanceWeight * t * t;
    }

    // Gets the emergency unstick pull used by the sim
    private Vector3 GetEmergencyUnstickPull()
    {
        return lastEmergencyUnstick;
    }

    // Handles move fish
    private void MoveFish(float hungerPressure)
    {
        if (rb == null)
        {
            return;
        }

        bool feedingOrHunting = GetPrimaryTargetPosition().HasValue;
        bool holdingBite = feedingHoldTimer > 0f && hasStaticFeedingPoint;
        Vector3 rotationSource = wantedDirection;
        if (holdingBite)
        {
            Vector3 lookAtFood = currentStaticFeedingPoint - GetMouthWorldPosition();
            if (lookAtFood.sqrMagnitude > 0.0001f)
            {
                rotationSource = lookAtFood.normalized;
            }
        }

        Vector3 rotationDirection = ClampPitchForUprightSwimming(rotationSource, feedingOrHunting || holdingBite);
        Quaternion targetRotation = BuildUprightFishRotation(rotationDirection);
        Vector3 localWanted = transform.InverseTransformDirection(rotationDirection);
        float crowdBankScale = lastDenseCrowdCount > 0 ? CrowdedBankReduction : 1f;
        float bank = Mathf.Clamp(-localWanted.x * MaxBankAngle * crowdBankScale, -MaxBankAngle, MaxBankAngle);
        targetRotation *= Quaternion.AngleAxis(bank, Vector3.forward);

        float injurySeverity = GetInjurySeverity01();
        float turnResponsiveness = RotationResponsiveness * (holdingBite ? FeedingTurnResponsiveness : 1f);
        float turnInjuryScale = 1f - injurySeverity * InjuryMaxTurnLoss;
        float turn = Mathf.Max(35f, EffectiveStats.TurnRate) * turnResponsiveness * Mathf.Clamp(turnInjuryScale, 0.35f, 1f);
        Quaternion nextRotation = Quaternion.RotateTowards(rb.rotation, targetRotation, turn * Time.fixedDeltaTime);
        rb.MoveRotation(nextRotation);
        if (SuppressPhysicsSpin && !rb.isKinematic)
        {
            rb.angularVelocity = Vector3.zero;
        }

        float alignment = Mathf.Clamp01((Vector3.Dot(transform.forward, wantedDirection) + 1f) * 0.5f);
        float sharpTurnScale = Mathf.Lerp(Mathf.Clamp01(1f - SharpTurnSpeedLoss), 1f, alignment);
        float targetScale = Mathf.Lerp(NormalCruiseSpeedScale, HungryCruiseSpeedScale, Mathf.Clamp01(hungerPressure));
        if (currentBrainMode == FishAutonomousBehaviourMode.Fleeing && cStartBurstTimer > 0f)
        {
            targetScale *= 1.85f;
        }
        else if (currentBrainMode == FishAutonomousBehaviourMode.Ambushing)
        {
            targetScale *= nearestPrey != null && CanAttackPrey(nearestPrey) ? 1.25f : 0.38f;
        }
        else if (currentBrainMode == FishAutonomousBehaviourMode.Courtship)
        {
            targetScale *= 0.42f;
        }
        else if (currentBrainMode == FishAutonomousBehaviourMode.MobbingPredator)
        {
            targetScale *= 1.15f;
        }
        else if (currentBrainMode == FishAutonomousBehaviourMode.Sleeping)
        {
            targetScale *= 0.10f;
        }

        Vector3? target = GetPrimaryMovementTargetPosition();
        if (!target.HasValue && currentMateTarget != null && ShouldSeekMate())
        {
            target = currentMateTarget.transform.position;
        }
        if (target.HasValue)
        {
            float distance = Vector3.Distance(GetMouthWorldPosition(), target.Value);
            float closeT = Mathf.Clamp01(distance / Mathf.Max(0.01f, CloseTargetSlowdownDistance));
            targetScale *= Mathf.Lerp(MinimumCruiseSpeedScale, 1f, closeT);
        }

        bool directFeedingMotion = IsCloseStaticFeedingTarget() && !holdingBite && !ShouldIgnoreStaticFoodBecauseThreatened();
        float stomachSlow = Mathf.Lerp(1f, Mathf.Clamp01(1f - FullStomachSlowdown), GetStomachFullness01());
        float injurySpeedScale = 1f - injurySeverity * InjuryMaxSpeedLoss;
        targetScale = Mathf.Max(MinimumCruiseSpeedScale, targetScale * sharpTurnScale * stomachSlow * Mathf.Clamp(injurySpeedScale, 0.35f, 1f));
        isSprintingThisTick = ShouldSprintThisTick(target, hungerPressure);
        if (isSprintingThisTick)
        {
            targetScale *= SprintSpeedMultiplier;
        }
        if (holdingBite)
        {
            targetScale = 0f;
        }

        Vector3 steerDirection = ClampPitchForUprightSwimming(wantedDirection, directFeedingMotion || feedingOrHunting);
        if (directFeedingMotion)
        {
            Vector3? rawStaticTarget = GetPrimaryStaticFoodTargetPosition();
            if (rawStaticTarget.HasValue)
            {
                Vector3 directToFood = rawStaticTarget.Value - GetMouthWorldPosition();
                if (directToFood.sqrMagnitude > 0.0001f)
                {
                    steerDirection = ClampPitchForUprightSwimming(directToFood.normalized, true);
                }
            }
        }

        Vector3 movementDirection = directFeedingMotion
            ? steerDirection
            : Vector3.Slerp(transform.forward, steerDirection, Mathf.Lerp(0.35f, 0.85f, Mathf.Clamp01(EffectiveStats.VerticalControl * 0.5f)));

        if (movementDirection.sqrMagnitude <= 0.001f)
        {
            movementDirection = transform.forward;
        }

        Vector3 desiredVelocity = movementDirection.normalized * EffectiveStats.Speed * targetScale;
        EvolutionEcosystemManager currentManager = EvolutionEcosystemManager.Instance;
        if (currentManager != null)
        {
            Vector3 currentFlow = currentManager.GetCurrentVelocityAt(rb.position);
            if (currentFlow.sqrMagnitude > 0.001f)
            {
                float against = Vector3.Dot(desiredVelocity.normalized, -currentFlow.normalized);
                if (against > 0f)
                {
                    float resistanceScale = currentEscapeTimer > 0f ? 0.35f : 1f;
                    desiredVelocity *= Mathf.Lerp(1f, 0.58f, Mathf.Clamp01(against) * resistanceScale);
                }
                float currentPush = currentEscapeTimer > 0f ? Mathf.Clamp01(CurrentEscapeCurrentDriftMultiplier) : 1f;
                if (currentEscapeTimer > 0f && lastTerrainAvoidance.sqrMagnitude > 0.05f)
                {
                    currentPush = Mathf.Min(currentPush, 0.04f);
                }
                desiredVelocity += currentFlow * currentPush;
            }
        }
        if (UseFleeTerrainSafety && (currentBrainMode == FishAutonomousBehaviourMode.Fleeing || survivalEmergencyTimer > 0f || currentEscapeTimer > 0f) && lastTerrainAvoidance.sqrMagnitude > 0.05f)
        {
            float maxUp = Mathf.Max(0.25f, EffectiveStats.Speed * FleeWallVerticalVelocityLimit);
            desiredVelocity.y = Mathf.Clamp(desiredVelocity.y, -EffectiveStats.Speed * 0.65f, maxUp);
            Vector3 horizontalAvoid = new Vector3(lastTerrainAvoidance.x, 0f, lastTerrainAvoidance.z);
            if (horizontalAvoid.sqrMagnitude > 0.001f)
            {
                desiredVelocity += horizontalAvoid.normalized * FleeWallBackOffWeight;
            }
        }

        desiredVelocity = PreventOutwardVelocityAtBounds(desiredVelocity);

        float accel = Mathf.Max(1f, EffectiveStats.Acceleration) * SteeringAcceleration;
        accel *= Mathf.Clamp(1f - injurySeverity * InjuryMaxAccelerationLoss, 0.35f, 1f);
        if (holdingBite)
        {
            currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, FeedingHoldDamping * Time.fixedDeltaTime);
        }
        else
        {
            currentVelocity = Vector3.MoveTowards(currentVelocity, desiredVelocity, accel * Time.fixedDeltaTime);
        }
        if (UseFleeTerrainSafety && (currentBrainMode == FishAutonomousBehaviourMode.Fleeing || survivalEmergencyTimer > 0f || currentEscapeTimer > 0f) && lastTerrainAvoidance.sqrMagnitude > 0.05f)
        {
            float maxUp = Mathf.Max(0.25f, EffectiveStats.Speed * FleeWallVerticalVelocityLimit);
            currentVelocity.y = Mathf.Clamp(currentVelocity.y, -EffectiveStats.Speed * 0.65f, maxUp);
        }
        if (UseFleeTerrainSafety && (currentBrainMode == FishAutonomousBehaviourMode.Fleeing || survivalEmergencyTimer > 0f || currentEscapeTimer > 0f) && EvolutionEcosystemManager.Instance != null && EvolutionEcosystemManager.Instance.TryGetTerrainHeight(rb.position, out float swimGroundY))
        {
            float clearance = rb.position.y - swimGroundY;
            if (clearance < FleeFloorAvoidanceClearance)
            {
                currentVelocity.y = Mathf.Max(currentVelocity.y, Mathf.Max(0f, FleeFloorMaxDownVelocity));
            }
        }
        currentVelocity = PreventOutwardVelocityAtBounds(currentVelocity);
        if (UseKinematicSwimming)
        {
            rb.MovePosition(rb.position + currentVelocity * Time.fixedDeltaTime);
        }
        else
        {
            rb.linearVelocity = currentVelocity;
        }

        ResolveKinematicOverlaps(holdingBite);

        if (EvolutionEcosystemManager.Instance != null)
        {
            Vector3 clamped = EvolutionEcosystemManager.Instance.ClampToSimulationArea(rb.position);
            if ((clamped - rb.position).sqrMagnitude > 0.0001f)
            {
                rb.position = clamped;
                currentVelocity = PreventOutwardVelocityAtBounds(currentVelocity) * BoundaryVelocityDamping;
                if (!UseKinematicSwimming)
                {
                    rb.linearVelocity = currentVelocity;
                }
            }
        }
    }

    // Handles resolve kinematic overlaps
    private void ResolveKinematicOverlaps(bool feedingHold)
    {
        if (EvolutionEcosystemManager.Instance == null || rb == null || KinematicOverlapIterations <= 0)
        {
            return;
        }

        List<MarineCreatureAgent> creatures = EvolutionEcosystemManager.Instance.GetActiveCreatures();
        if (creatures == null || creatures.Count <= 1)
        {
            return;
        }

        for (int iteration = 0; iteration < KinematicOverlapIterations; iteration++)
        {
            Vector3 correction = Vector3.zero;
            int overlaps = 0;
            Vector3 ownPosition = rb.position;
            float ownRadius = GetPersonalRadius() * (feedingHold ? FeedingPersonalSpaceBoost : 1f);
            float searchRadius = Mathf.Max(OverlapSearchRadius, ownRadius * 2.5f);

            for (int i = 0; i < creatures.Count; i++)
            {
                MarineCreatureAgent other = creatures[i];
                if (other == null || other == this)
                {
                    continue;
                }

                Vector3 otherPosition = other.transform.position;
                Vector3 away = ownPosition - otherPosition;
                float distance = away.magnitude;
                if (distance > searchRadius)
                {
                    continue;
                }

                float otherRadius = other.GetPersonalRadius();
                float required = (ownRadius + otherRadius) * PersonalSpaceMultiplier;
                if (IsTryingToReachCurrentPrey(other))
                {
                    required *= HuntingContactSpaceMultiplier;
                }
                if (distance >= required)
                {
                    continue;
                }

                if (distance <= 0.001f)
                {
                    away = GetFallbackSideDirection();
                    distance = 0.001f;
                }

                away.y *= KinematicOverlapVerticalInfluence;
                if (away.sqrMagnitude <= 0.001f)
                {
                    away = GetHorizontalAwayFrom(otherPosition);
                }

                float penetration = required - distance;
                correction += away.normalized * penetration;
                overlaps++;
            }

            if (overlaps <= 0 || correction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float strength = feedingHold ? FeedingOverlapResolveStrength : KinematicOverlapResolveStrength;
            Vector3 step = correction / overlaps * Mathf.Clamp01(strength);
            step.y *= KinematicOverlapVerticalInfluence;
            rb.position += step;
            currentVelocity += step / Mathf.Max(Time.fixedDeltaTime, 0.001f) * 0.15f;
        }
    }

    // Handles clamp pitch for upright swimming
    private Vector3 ClampPitchForUprightSwimming(Vector3 direction, bool activelyTargeting)
    {
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return transform.forward.sqrMagnitude > 0.001f ? transform.forward : Vector3.forward;
        }

        Vector3 dir = direction.normalized;
        Vector3 horizontal = new Vector3(dir.x, 0f, dir.z);
        if (horizontal.sqrMagnitude <= 0.0001f)
        {
            horizontal = new Vector3(transform.forward.x, 0f, transform.forward.z);
            if (horizontal.sqrMagnitude <= 0.0001f)
            {
                horizontal = Vector3.forward;
            }
        }
        horizontal.Normalize();

        float maxPitch = Mathf.Clamp(MaxNormalPitchAngle + (activelyTargeting ? FeedingPitchAllowance : 0f), 5f, 88f);
        float maxY = Mathf.Sin(maxPitch * Mathf.Deg2Rad);
        float clampedY = Mathf.Clamp(dir.y, -maxY, maxY);
        float horizontalMagnitude = Mathf.Sqrt(Mathf.Max(0.0001f, 1f - clampedY * clampedY));
        return (horizontal * horizontalMagnitude + Vector3.up * clampedY).normalized;
    }

    // Builds the upright fish rotation data from the current values
    private Quaternion BuildUprightFishRotation(Vector3 forward)
    {
        if (forward.sqrMagnitude <= 0.0001f)
        {
            forward = transform.forward.sqrMagnitude > 0.001f ? transform.forward : Vector3.forward;
        }

        forward.Normalize();
        Vector3 up = Vector3.up;

        if (Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.92f)
        {
            Vector3 safeRight = Vector3.Cross(Vector3.up, transform.forward);
            if (safeRight.sqrMagnitude <= 0.0001f)
            {
                safeRight = transform.right.sqrMagnitude > 0.0001f ? transform.right : Vector3.right;
            }
            up = Vector3.Cross(forward, safeRight.normalized).normalized;
        }

        return Quaternion.LookRotation(forward, up);
    }

    // Updates stuck detection using the current sim state
    private void UpdateStuckDetection(Vector3 combinedSteering)
    {
        Vector3 position = rb != null ? rb.position : transform.position;
        float moved = Vector3.Distance(position, lastPosition);
        bool shouldBeMoving = combinedSteering.sqrMagnitude > 0.2f;
        float actualSpeed = currentVelocity.magnitude;
        bool barelyMoved = moved < StuckPositionDeltaThreshold * Time.fixedDeltaTime * 4f || actualSpeed < StuckSpeedThreshold;

        TrackStaticResourceProgress();

        if (shouldBeMoving && barelyMoved)
        {
            lowProgressTimer += Time.fixedDeltaTime;
        }
        else
        {
            lowProgressTimer = Mathf.Max(0f, lowProgressTimer - Time.fixedDeltaTime * 0.75f);
        }

        if (lowProgressTimer >= StuckCheckTime)
        {
            lowProgressTimer = 0f;
            stuckEscapeTimer = StuckEscapeDuration;
            stuckEscapeDirection = BuildStuckEscapeDirection();
            currentVelocity = stuckEscapeDirection * Mathf.Max(EffectiveStats.Speed * 0.35f, currentVelocity.magnitude * 0.35f);
            MaybeAbandonStuckStaticResource();
        }

        lastPosition = position;
    }

    // Handles track static resource progress
    private void TrackStaticResourceProgress()
    {
        Vector3? target = GetPrimaryStaticFoodTargetPosition();
        if (!target.HasValue)
        {
            staticTargetNoProgressTimer = 0f;
            lastPrimaryStaticTargetDistance = float.MaxValue;
            return;
        }

        float distance = Vector3.Distance(GetMouthWorldPosition(), target.Value);
        if ((target.Value - lastPrimaryStaticTargetPosition).sqrMagnitude > 0.25f)
        {
            staticTargetNoProgressTimer = 0f;
            lastPrimaryStaticTargetPosition = target.Value;
            lastPrimaryStaticTargetDistance = distance;
            return;
        }

        if (distance < lastPrimaryStaticTargetDistance - StuckResourceDistanceEpsilon)
        {
            staticTargetNoProgressTimer = 0f;
            lastPrimaryStaticTargetDistance = distance;
        }
        else
        {
            staticTargetNoProgressTimer += Time.fixedDeltaTime;
        }
    }

    // Handles maybe abandon stuck static resource
    private void MaybeAbandonStuckStaticResource()
    {
        if (staticTargetNoProgressTimer < StuckResourceProgressTime)
        {
            return;
        }

        if (nearestFood != null && !nearestFood.IsConsumed)
        {
            temporarilyIgnoredFood = nearestFood;
            nearestFood = null;
        }

        if (nearestCarrion != null && !nearestCarrion.IsConsumed)
        {
            temporarilyIgnoredCarrion = nearestCarrion;
            nearestCarrion = null;
        }

        ignoredResourceTimer = ResourceAbandonDuration;
        staticTargetNoProgressTimer = 0f;
        lastPrimaryStaticTargetDistance = float.MaxValue;
    }

    // Builds the stuck escape direction data from the current values
    private Vector3 BuildStuckEscapeDirection()
    {
        Vector3 side = GetFallbackSideDirection();
        Vector3 dir = lastSchoolSeparation + lastBoundaryPush + side * 1.25f;

        Vector3? target = GetPrimaryStaticFoodTargetPosition();
        if (target.HasValue)
        {
            Vector3 toTarget = target.Value - transform.position;
            Vector3 horizontalToTarget = new Vector3(toTarget.x, 0f, toTarget.z);
            if (horizontalToTarget.sqrMagnitude > 0.001f)
            {
                Vector3 perpendicular = Vector3.Cross(horizontalToTarget.normalized, Vector3.up).normalized;
                float sign = ((Candidate != null ? Candidate.Id : GetInstanceID()) % 2 == 0) ? 1f : -1f;
                dir += perpendicular * sign * HardUnstickSideStepWeight;

                if (horizontalToTarget.magnitude < Mathf.Max(1.0f, GetPersonalRadius() + GetScaledMouthRadius()))
                {
                    dir -= horizontalToTarget.normalized * 1.35f;
                }
            }
        }

        if (dir.sqrMagnitude <= 0.001f)
        {
            Vector2 random = Random.insideUnitCircle.normalized;
            if (random.sqrMagnitude <= 0.001f)
            {
                random = Vector2.right;
            }
            dir = new Vector3(random.x, 0f, random.y);
        }

        dir.y *= 0.04f;
        if (dir.sqrMagnitude <= 0.001f)
        {
            dir = Vector3.right;
        }

        return dir.normalized;
    }

    // Handles pick new wander direction
    private void PickNewWanderDirection()
    {
        wanderTimer = Random.Range(WanderRefreshMin, WanderRefreshMax);
        wanderDirection = Random.insideUnitSphere;
        wanderDirection.y *= Mathf.Lerp(0.25f, 1.0f, Candidate != null && Candidate.Genome != null ? Candidate.Genome.DepthFlexibility : 0.5f);
        if (wanderDirection.sqrMagnitude < 0.01f)
        {
            wanderDirection = transform.forward;
        }
        wanderDirection.Normalize();
    }

    // Gets the boundary avoidance direction used by the sim
    private Vector3 GetBoundaryAvoidanceDirection()
    {
        if (EvolutionEcosystemManager.Instance == null)
        {
            return Vector3.zero;
        }

        Vector3 centre = EvolutionEcosystemManager.Instance.transform.position;
        Vector3 half = EvolutionEcosystemManager.Instance.SimulationAreaSize * 0.5f;
        Vector3 position = rb != null ? rb.position : transform.position;
        Vector3 min = centre - half;
        Vector3 max = centre + half;
        Vector3 push = Vector3.zero;

        AddBoundaryPush(position.x - min.x, Vector3.right, ref push);
        AddBoundaryPush(max.x - position.x, Vector3.left, ref push);
        AddBoundaryPush(position.y - min.y, Vector3.up, ref push);
        AddBoundaryPush(max.y - position.y, Vector3.down, ref push);
        AddBoundaryPush(position.z - min.z, Vector3.forward, ref push);
        AddBoundaryPush(max.z - position.z, Vector3.back, ref push);

        if (push.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        return push.normalized * BoundaryAvoidanceStrength;
    }

    // Handles add boundary push
    private void AddBoundaryPush(float distanceToEdge, Vector3 inwardDirection, ref Vector3 push)
    {
        if (BoundaryAvoidanceDistance <= 0f || distanceToEdge > BoundaryAvoidanceDistance)
        {
            return;
        }

        float t = 1f - Mathf.Clamp01(distanceToEdge / BoundaryAvoidanceDistance);
        push += inwardDirection * t * t;
    }

    // Handles prevent outward direction at bounds
    private void PreventOutwardDirectionAtBounds(ref Vector3 direction)
    {
        if (EvolutionEcosystemManager.Instance == null)
        {
            return;
        }

        Vector3 centre = EvolutionEcosystemManager.Instance.transform.position;
        Vector3 half = EvolutionEcosystemManager.Instance.SimulationAreaSize * 0.5f;
        Vector3 position = rb != null ? rb.position : transform.position;
        Vector3 min = centre - half;
        Vector3 max = centre + half;
        float margin = Mathf.Max(0.01f, BoundaryHardStopMargin);

        if (position.x <= min.x + margin && direction.x < 0f) direction.x = 0f;
        if (position.x >= max.x - margin && direction.x > 0f) direction.x = 0f;
        if (position.y <= min.y + margin && direction.y < 0f) direction.y = 0f;
        if (position.y >= max.y - margin && direction.y > 0f) direction.y = 0f;
        if (position.z <= min.z + margin && direction.z < 0f) direction.z = 0f;
        if (position.z >= max.z - margin && direction.z > 0f) direction.z = 0f;
    }

    // Handles prevent outward velocity at bounds
    private Vector3 PreventOutwardVelocityAtBounds(Vector3 velocity)
    {
        PreventOutwardDirectionAtBounds(ref velocity);
        return velocity;
    }

    // Handles direction to
    private Vector3 DirectionTo(Vector3? targetPosition)
    {
        if (!targetPosition.HasValue)
        {
            return Vector3.zero;
        }

        Vector3 direction = targetPosition.Value - GetMouthWorldPosition();
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        return direction.normalized;
    }

    // Gets the direction to simulation centre used by the sim
    private Vector3 GetDirectionToSimulationCentre()
    {
        if (EvolutionEcosystemManager.Instance == null)
        {
            return transform.forward;
        }

        Vector3 direction = EvolutionEcosystemManager.Instance.transform.position - transform.position;
        return direction.sqrMagnitude < 0.05f ? transform.forward : direction.normalized;
    }

    // Gets the horizontal away from used by the sim
    private Vector3 GetHorizontalAwayFrom(Vector3 otherPosition)
    {
        Vector3 away = transform.position - otherPosition;
        away.y = 0f;
        if (away.sqrMagnitude <= 0.0001f)
        {
            away = transform.right;
            away.y = 0f;
        }
        if (away.sqrMagnitude <= 0.0001f)
        {
            away = Vector3.right;
        }
        return away.normalized;
    }

    // Gets the fallback side direction used by the sim
    private Vector3 GetFallbackSideDirection()
    {
        Vector3 side = transform.right;
        side.y = 0f;
        if (side.sqrMagnitude <= 0.0001f)
        {
            Vector2 random = Random.insideUnitCircle.normalized;
            if (random.sqrMagnitude <= 0.0001f)
            {
                random = Vector2.right;
            }
            side = new Vector3(random.x, 0f, random.y);
        }
        return side.normalized;
    }

    // Gets the current velocity or forward used by the sim
    private Vector3 GetCurrentVelocityOrForward()
    {
        Vector3 velocity = UseKinematicSwimming ? currentVelocity : (rb != null ? rb.linearVelocity : currentVelocity);
        if (velocity.sqrMagnitude <= 0.001f)
        {
            velocity = transform.forward;
        }
        return velocity;
    }

    // Checks if it has close food target available
    private bool HasCloseFoodTarget()
    {
        Vector3? target = GetPrimaryTargetPosition();
        if (!target.HasValue)
        {
            return false;
        }

        return Vector3.Distance(GetMouthWorldPosition(), target.Value) <= CloseTargetSlowdownDistance * 1.4f;
    }

    // Checks if it should commit to static feeding target right now
    private bool ShouldCommitToStaticFeedingTarget(float hungerPressure)
    {
        if (hungerPressure < 0.08f)
        {
            return false;
        }

        Vector3? target = GetPrimaryStaticFoodTargetPosition();
        if (!target.HasValue)
        {
            return false;
        }

        float mouthDistance = Vector3.Distance(GetMouthWorldPosition(), target.Value);
        float bodyDistance = Vector3.Distance(transform.position, target.Value);
        float commitDistance = Mathf.Max(FeedingCommitDistance, GetPersonalRadius() + GetScaledMouthRadius() + 1.1f);

        return mouthDistance <= commitDistance || bodyDistance <= commitDistance;
    }

    // Checks if it is close static feeding target
    private bool IsCloseStaticFeedingTarget()
    {
        Vector3? target = GetPrimaryStaticFoodTargetPosition();
        if (!target.HasValue)
        {
            return false;
        }

        float mouthDistance = Vector3.Distance(GetMouthWorldPosition(), target.Value);
        float bodyDistance = Vector3.Distance(transform.position, target.Value);
        float directDistance = Mathf.Max(CloseFeedingDirectMotionDistance, GetPersonalRadius() + GetScaledMouthRadius() + 1.2f);

        return mouthDistance <= directDistance || bodyDistance <= directDistance;
    }

    // Gets the primary movement target position used by the sim
    private Vector3? GetPrimaryMovementTargetPosition()
    {
        if (nearestPrey != null && CanAttackPrey(nearestPrey))
        {
            return nearestPrey.GetBiteTargetPosition();
        }

        Vector3? staticTarget = GetPrimaryStaticFoodTargetPosition();
        if (staticTarget.HasValue && !ShouldIgnoreStaticFoodBecauseThreatened())
        {
            return GetMovementTargetForStaticResource(staticTarget.Value);
        }

        if (currentMateTarget != null && ShouldSeekMate())
        {
            return currentMateTarget.transform.position;
        }

        return null;
    }

    // Gets the movement target for static resource used by the sim
    private Vector3 GetMovementTargetForStaticResource(Vector3 resourcePosition)
    {
        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null)
        {
            return resourcePosition;
        }

        int crowd = manager.CountCreaturesNearPoint(resourcePosition, ResourceCrowdRadius, this);
        int comfort = GetComfortableFeederLimit();
        float distanceToResource = Vector3.Distance(transform.position, resourcePosition);

        // When close enough to actually feed, commit to the resource centre. Otherwise, crowded
        // resources get individual side slots so a school does not compress into one vertical pile.
        float contactDistance = Mathf.Max(StaticFoodContactConsumeDistance, GetPersonalRadius() + GetScaledMouthRadius() + 0.35f);
        if (crowd < Mathf.Max(2, comfort) || distanceToResource <= contactDistance)
        {
            return resourcePosition;
        }

        float radius = Mathf.Max(0.35f, ResourceApproachSlotRadius + GetPersonalRadius() * 0.45f);
        float id = Candidate != null ? Candidate.Id : GetInstanceID();
        float angle = id * 137.508f * Mathf.Deg2Rad;
        Vector3 ring = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

        // Do not force the slot to the target's exact depth. The fish can approach laterally first,
        // then make a short vertical correction when it has space.
        Vector3 slot = resourcePosition + ring * radius;
        slot.y = Mathf.Lerp(transform.position.y, resourcePosition.y, 0.35f);
        return slot;
    }

    // Handles wants static resource now
    private bool WantsStaticResourceNow()
    {
        if (feedingHoldTimer > 0f)
        {
            return true;
        }

        if (Candidate == null || Candidate.Genome == null)
        {
            return true;
        }

        if (ShouldIgnoreStaticFoodBecauseThreatened())
        {
            return false;
        }

        if ((IsLockedMeatSpecialist() || IsPredatorPlantBlockedRole()) && !HasUsableCarrionTarget() && !ShouldContinueEatingFreshKillCarrion())
        {
            // Meat predators should search/rest/hunt instead of grazing.
            return CanUsePlantAsPredatorEmergencyFallback();
        }

        if (ShouldSuppressPlantsForHunter() && !HasUsableCarrionTarget() && !ShouldContinueEatingFreshKillCarrion())
        {
            return false;
        }

        if (GetHealthRatio() <= DesperateHealthRatio)
        {
            return true;
        }

        if (ShouldContinueEatingFreshKillCarrion())
        {
            return true;
        }

        if (IsSatisfiedEnoughToLeaveResource())
        {
            return false;
        }

        if (IsHungryEnoughToSearch())
        {
            return true;
        }

        float opportunism = Candidate.Genome.Selfishness * 0.45f + Candidate.Genome.HungerDrive * 0.25f + Candidate.Genome.ExplorationDrive * 0.15f;
        return GetStomachFullness01() < 0.18f && GetEffectiveEnergyRatio() < Mathf.Lerp(0.45f, 0.62f, opportunism);
    }

    // Checks if it is satisfied enough to leave resource
    private bool IsSatisfiedEnoughToLeaveResource()
    {
        float energyRatio = Mathf.Clamp01(CurrentEnergy / Mathf.Max(0.01f, EffectiveStats != null ? EffectiveStats.EnergyCapacity : Candidate.Genome.EnergyCapacity));
        float stomachRatio = GetStomachFullness01();
        float hungerDrive = Candidate != null && Candidate.Genome != null ? Candidate.Genome.HungerDrive : 0.5f;
        float selfish = Candidate != null && Candidate.Genome != null ? Candidate.Genome.Selfishness : 0.2f;
        float energyTarget = Mathf.Lerp(LeaveResourceEnergyRatio * 0.86f, LeaveResourceEnergyRatio * 1.08f, hungerDrive);
        float stomachTarget = Mathf.Lerp(LeaveResourceStomachRatio * 0.82f, LeaveResourceStomachRatio * 1.12f, selfish);

        return energyRatio >= energyTarget || stomachRatio >= stomachTarget;
    }

    // Gets the primary static food target position used by the sim
    private Vector3? GetPrimaryStaticFoodTargetPosition()
    {
        if (!WantsStaticResourceNow())
        {
            return null;
        }

        if (ShouldContinueEatingFreshKillCarrion() && claimedFreshKillCarrion != null && !claimedFreshKillCarrion.IsConsumed)
        {
            return claimedFreshKillCarrion.transform.position;
        }

        if (IsPredatorPlantBlockedRole() && !CanUsePlantAsPredatorEmergencyFallback())
        {
            ClearPlantTargetForPredator();

            if (nearestCarrion != null && !nearestCarrion.IsConsumed)
            {
                return nearestCarrion.transform.position;
            }

            if (retainedCarrion != null && !retainedCarrion.IsConsumed)
            {
                return retainedCarrion.transform.position;
            }

            return null;
        }

        string preferred = GetPreferredFoodKind();

        if (preferred == "Carrion" && nearestCarrion != null && !nearestCarrion.IsConsumed)
        {
            return nearestCarrion.transform.position;
        }

        if (preferred == "Plant" && nearestFood != null && !nearestFood.IsConsumed)
        {
            return nearestFood.transform.position;
        }

        if (!IsLockedMeatSpecialist() && nearestFood != null && !nearestFood.IsConsumed)
        {
            return nearestFood.transform.position;
        }

        if (nearestCarrion != null && !nearestCarrion.IsConsumed)
        {
            return nearestCarrion.transform.position;
        }

        if (IsHungryEnoughToSearch() && hasFoodMemory && !rememberedFoodWasBad)
        {
            return rememberedFoodArea;
        }

        return null;
    }

    // Gets the primary target position used by the sim
    private Vector3? GetPrimaryTargetPosition()
    {
        if (focusedPrey != null && hunterPreyFocusTimer > 0f && CanAttackPrey(focusedPrey))
        {
            return focusedPrey.GetBiteTargetPosition();
        }

        if (nearestPrey != null && CanAttackPrey(nearestPrey))
        {
            return nearestPrey.GetBiteTargetPosition();
        }

        if (ShouldContinueEatingFreshKillCarrion() && claimedFreshKillCarrion != null && !claimedFreshKillCarrion.IsConsumed)
        {
            return claimedFreshKillCarrion.transform.position;
        }

        if (IsPredatorPlantBlockedRole() && !CanUsePlantAsPredatorEmergencyFallback())
        {
            ClearPlantTargetForPredator();

            if (nearestCarrion != null && !nearestCarrion.IsConsumed)
            {
                return nearestCarrion.transform.position;
            }

            if (retainedCarrion != null && !retainedCarrion.IsConsumed)
            {
                return retainedCarrion.transform.position;
            }

            return null;
        }

        string preferred = GetPreferredFoodKind();
        if (preferred == "Carrion" && nearestCarrion != null && !nearestCarrion.IsConsumed)
        {
            return nearestCarrion.transform.position;
        }

        if (preferred == "Plant" && nearestFood != null && !nearestFood.IsConsumed)
        {
            return nearestFood.transform.position;
        }

        if (!IsLockedMeatSpecialist() && nearestFood != null && !nearestFood.IsConsumed)
        {
            return nearestFood.transform.position;
        }

        if (nearestCarrion != null && !nearestCarrion.IsConsumed)
        {
            return nearestCarrion.transform.position;
        }

        return null;
    }

    // Checks if it is friendly by morph
    private bool IsFriendlyByMorph(MarineCreatureAgent other)
    {
        if (other == null || other == this || other.Candidate == null || other.Candidate.Genome == null || Candidate == null || Candidate.Genome == null)
        {
            return false;
        }

        // IMPORTANT:
        // Do not call CanAttackPrey() from here.
        // CanAttackPrey() checks prey.GetGroupDangerSupport(), and group support checks IsFriendlyByMorph().
        // Calling attack checks from friendship checks creates this recursion:
        // CanAttackPrey -> GetGroupDangerSupport -> IsFriendlyByMorph -> CanAttackPrey -> ...
        // This helper must stay as a cheap morphology/social compatibility check only.
        float similarity = Candidate.Genome.GetMorphSimilarity(other.Candidate.Genome);
        if (ProtectSimilarPredatorsFromSpawnKilling && similarity >= SimilarPredatorPeaceSimilarity && IsEcologicalPredatorRole() && other.IsEcologicalPredatorRole())
        {
            return true;
        }

        if (similarity >= MorphSimilarityForSchool)
        {
            return true;
        }

        string ownMorph = CreatureDebugTypeUtility.GetMorphologyName(Candidate.Genome);
        string otherMorph = CreatureDebugTypeUtility.GetMorphologyName(other.Candidate.Genome);
        return ownMorph == otherMorph;
    }

    // Builds the predator signature data from the current values
    private string BuildPredatorSignature(MarineCreatureAgent predator)
    {
        if (predator == null || predator.Candidate == null || predator.Candidate.Genome == null)
        {
            return "";
        }

        EvolutionGenome g = predator.Candidate.Genome;
        return g.BodyMorphId + "|" + g.JawMorphId + "|" + g.TailMorphId;
    }

    // Checks if it is remembered predator type
    private bool IsRememberedPredatorType(MarineCreatureAgent other)
    {
        if (rememberedPredatorTypeTimer <= 0f || string.IsNullOrEmpty(rememberedPredatorSignature) || other == null)
        {
            return false;
        }

        string signature = BuildPredatorSignature(other);
        if (signature == rememberedPredatorSignature)
        {
            return true;
        }

        return other.Candidate != null && other.Candidate.Genome != null && other.GetPredatorDrive01() > 0.55f && Candidate != null && Candidate.Genome != null && Candidate.Genome.GetMorphSimilarity(other.Candidate.Genome) < MorphSimilarityForSchool;
    }

    // Checks if it is actual threat
    private bool IsActualThreat(MarineCreatureAgent other)
    {
        if (other == null || other == this)
        {
            return false;
        }

        return IsPredatorThreateningThis(other, ClosePredatorThreatRange, ClosePredatorThreatHalfAngle, true, false);
    }

    // Checks if it has close visible predator threat available
    private bool HasCloseVisiblePredatorThreat()
    {
        if (survivalThreatTarget != null && IsPredatorThreateningThis(survivalThreatTarget, ClosePredatorThreatRange * 1.25f, ClosePredatorThreatHalfAngle, true, false))
        {
            return true;
        }

        if (nearestCreature != null && IsPredatorThreateningThis(nearestCreature, ClosePredatorThreatRange, ClosePredatorThreatHalfAngle, true, false))
        {
            return true;
        }

        if (groupWarningPredator != null && groupPredatorWarningTimer > 0f && IsPredatorThreateningThis(groupWarningPredator, ClosePredatorThreatRange, ClosePredatorThreatHalfAngle, true, false))
        {
            return true;
        }

        return false;
    }

    // Checks if it is predator threatening this
    private bool IsPredatorThreateningThis(MarineCreatureAgent predator, float range, float halfAngle, bool requireCloseOrApproach, bool allowEarlyWarning)
    {
        if (predator == null || predator == this || predator.CurrentHealth <= 0f)
        {
            return false;
        }

        Vector3 toPredator = predator.transform.position - transform.position;
        float distanceSqr = toPredator.sqrMagnitude;
        float safeRange = Mathf.Max(0.1f, range);
        if (distanceSqr > safeRange * safeRange)
        {
            return false;
        }

        float distance = Mathf.Sqrt(distanceSqr);
        bool recentlyHitByThisPredator = survivalRecentDamageTimer > 0f && survivalThreatTarget == predator;
        if (recentlyHitByThisPredator)
        {
            return true;
        }

        bool predatorCanHurtThis = predator.GetPredatorDrive01() > 0.56f || predator.IsEcologicalPredatorRole() || predator.IsLockedMeatSpecialist();
        if (!predatorCanHurtThis && !IsRememberedPredatorType(predator))
        {
            return false;
        }

        bool veryClose = distance <= ClosePredatorThreatRange * 0.45f;
        bool seen = veryClose || IsInForwardSideVisionCone(predator.transform.position, halfAngle);
        if (!seen)
        {
            return false;
        }

        bool predatorIsTargetingThis = predator.nearestPrey == this
            || predator.retainedPrey == this
            || predator.focusedPrey == this
            || predator.survivalThreatTarget == this;

        Vector3 predatorToMe = transform.position - predator.transform.position;
        predatorToMe.y *= 0.35f;
        bool predatorFacingThis = predatorToMe.sqrMagnitude > 0.001f && Vector3.Dot(predator.transform.forward.normalized, predatorToMe.normalized) >= PredatorApproachDotForThreat;
        bool activeHunter = predator.currentBrainMode == FishAutonomousBehaviourMode.Hunting || predator.currentBrainMode == FishAutonomousBehaviourMode.Ambushing || predator.brainWantsHunt;

        // Early group warnings should not mean "a predator exists somewhere in view".
        // They only fire when the predator is close, actively targeting, or visibly
        // approaching as a hunter. Otherwise the whole ecosystem freezes/panics.
        if (allowEarlyWarning && !requireCloseOrApproach)
        {
            return veryClose || predatorIsTargetingThis || (activeHunter && predatorFacingThis);
        }

        return veryClose || predatorIsTargetingThis || (activeHunter && predatorFacingThis);
    }

    // Checks if it is in forward side vision cone
    private bool IsInForwardSideVisionCone(Vector3 worldPosition, float halfAngle)
    {
        Vector3 toTarget = worldPosition - transform.position;
        toTarget.y *= 0.45f;
        if (toTarget.sqrMagnitude <= 0.001f)
        {
            return true;
        }

        Vector3 forward = transform.forward;
        forward.y *= 0.45f;
        if (forward.sqrMagnitude <= 0.001f)
        {
            forward = transform.forward;
        }

        float dot = Vector3.Dot(forward.normalized, toTarget.normalized);
        float threshold = Mathf.Cos(Mathf.Clamp(halfAngle, 1f, 179f) * Mathf.Deg2Rad);
        return dot >= threshold;
    }

    // Checks if it should react to group predator warnings right now
    private bool ShouldReactToGroupPredatorWarnings()
    {
        if (Candidate == null || Candidate.Genome == null || CurrentHealth <= 0f)
        {
            return false;
        }

        // Group warnings are for prey/schooling fish. Predators should not start fleeing
        // just because the school is warning each other about predators nearby.
        // Direct attacks, low-health survival logic and close threats still work elsewhere.
        if (IsLockedMeatSpecialist() || IsEcologicalPredatorRole())
        {
            return false;
        }

        // Strong meat-biased fish can still receive direct threat checks, but they should
        // not be dragged into school-wide panic behaviour.
        if (Candidate.Genome.MeatDiet >= 0.58f && Candidate.Genome.MeatDiet >= Candidate.Genome.PlantDiet + 0.10f)
        {
            return false;
        }

        return true;
    }

    // Checks if it should send group predator warnings right now
    private bool ShouldSendGroupPredatorWarnings()
    {
        if (!ShouldReactToGroupPredatorWarnings())
        {
            return false;
        }

        // Solitary/brave fish can still flee alone, but do not alarm a whole group unless
        // they have at least some social tendency.
        return Candidate.Genome.GroupingChance >= 0.18f || Candidate.Genome.FoodSharing >= 0.25f || Candidate.Genome.SchoolTightness >= 0.25f;
    }

    // Handles process early group predator warning
    private void ProcessEarlyGroupPredatorWarning(EvolutionEcosystemManager manager, float senseRange)
    {
        if (!UseCloseLineOfSightThreats || manager == null || groupPredatorWarningCooldownTimer > 0f || !ShouldSendGroupPredatorWarnings())
        {
            return;
        }

        float range = Mathf.Min(Mathf.Max(ClosePredatorThreatRange, EarlyGroupPredatorWarningRange), Mathf.Max(ClosePredatorThreatRange, senseRange));
        List<MarineCreatureAgent> nearby = manager.GetNearbyCreatures(transform.position, range);
        if (nearby == null || nearby.Count <= 1)
        {
            return;
        }

        MarineCreatureAgent seenPredator = null;
        for (int i = 0; i < nearby.Count; i++)
        {
            MarineCreatureAgent other = nearby[i];
            if (other == null || other == this)
            {
                continue;
            }

            if (IsPredatorThreateningThis(other, range, EarlyGroupPredatorWarningHalfAngle, false, true))
            {
                seenPredator = other;
                break;
            }
        }

        if (seenPredator == null)
        {
            return;
        }

        ReceiveGroupPredatorWarning(seenPredator);
        groupPredatorWarningCooldownTimer = GroupPredatorWarningCooldown;

        List<MarineCreatureAgent> allies = manager.GetNearbyCreatures(transform.position, GroupPredatorWarningRadius);
        for (int i = 0; i < allies.Count; i++)
        {
            MarineCreatureAgent ally = allies[i];
            if (ally == null || ally == this || ally.CurrentHealth <= 0f || !ally.ShouldReactToGroupPredatorWarnings())
            {
                continue;
            }

            if (!IsFriendlyByMorph(ally))
            {
                continue;
            }

            ally.ReceiveGroupPredatorWarning(seenPredator);
        }
    }

    // Handles receive group predator warning
    private void ReceiveGroupPredatorWarning(MarineCreatureAgent predator)
    {
        if (predator == null || predator == this || !ShouldReactToGroupPredatorWarnings())
        {
            return;
        }

        Vector3 away = transform.position - predator.transform.position;
        away.y *= 0.18f;
        if (away.sqrMagnitude <= 0.001f)
        {
            away = GetFallbackSideDirection();
        }

        Vector3 warningDirection = away.normalized;
        if (AllowEscapeTowardPredatorWhenTerrainBlocked && lastTerrainAvoidance.sqrMagnitude > 0.05f)
        {
            Vector3 safePath = lastTerrainAvoidance;
            safePath.y = Mathf.Max(0f, safePath.y);
            if (safePath.sqrMagnitude > 0.001f)
            {
                warningDirection = (warningDirection * TerrainBlockedThreatEscapeDamping + safePath.normalized * TerrainBlockedSafePathWeight).normalized;
            }
        }

        groupPredatorWarningDirection = warningDirection;
        groupPredatorWarningTimer = Mathf.Max(groupPredatorWarningTimer, GroupPredatorWarningDuration);
        groupWarningPredator = predator;
        if (Vector3.SqrMagnitude(predator.transform.position - transform.position) <= ClosePredatorThreatRange * ClosePredatorThreatRange)
        {
            threatFoodSuppressionTimer = Mathf.Max(threatFoodSuppressionTimer, Mathf.Min(ThreatFoodSuppressionMemoryTime, GroupPredatorWarningDuration));
        }
        rememberedDangerArea = predator.transform.position;
    }

    // Gets the leadership weight used by the sim
    private float GetLeadershipWeight()
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return 0.25f;
        }

        float energyRatio = Mathf.Clamp01(CurrentEnergy / Mathf.Max(0.01f, EffectiveStats != null ? EffectiveStats.EnergyCapacity : Candidate.Genome.EnergyCapacity));
        return Mathf.Clamp01(Candidate.Genome.Leadership * 0.55f + energyRatio * 0.25f + Candidate.Genome.RiskTolerance * 0.2f);
    }

    // Handles clamp runtime collider settings
    private void ClampRuntimeColliderSettings()
    {
        RootColliderIsTrigger = true;
        MaxRuntimeRootColliderRadius = Mathf.Clamp(MaxRuntimeRootColliderRadius, 0.18f, 0.45f);
        RootColliderRadius = Mathf.Clamp(RootColliderRadius, 0.18f, MaxRuntimeRootColliderRadius);
        HuntingContactSpaceMultiplier = Mathf.Clamp(HuntingContactSpaceMultiplier, 0.05f, 1f);
    }

    // Gets the safe root collider radius used by the sim
    private float GetSafeRootColliderRadius()
    {
        float maxRadius = Mathf.Clamp(MaxRuntimeRootColliderRadius, 0.18f, 0.45f);
        return Mathf.Clamp(RootColliderRadius, 0.18f, maxRadius);
    }

    // Gets the personal radius used by the sim
    private float GetPersonalRadius()
    {
        // Logic personal space is world-space. The root transform scale already represents body size,
        // so use one clean size multiplication here instead of the double scale that broke hunting.
        float size = EffectiveStats != null ? EffectiveStats.BodySize : Candidate != null && Candidate.Genome != null ? Candidate.Genome.BodySize : 1f;
        return Mathf.Max(0.16f, GetSafeRootColliderRadius() * Mathf.Max(0.35f, size));
    }

    // Checks if it is trying to reach current prey
    private bool IsTryingToReachCurrentPrey(MarineCreatureAgent other)
    {
        if (other == null || other != nearestPrey || Candidate == null || Candidate.Genome == null)
        {
            return false;
        }

        if (currentBrainMode != FishAutonomousBehaviourMode.Hunting && currentBrainMode != FishAutonomousBehaviourMode.Ambushing && !brainWantsHunt)
        {
            return false;
        }

        return Candidate.Genome.MeatDiet >= 0.35f && Candidate.Genome.Aggression >= 0.18f;
    }

    // Gets the group danger support used by the sim
    private float GetGroupDangerSupport()
    {
        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null)
        {
            return 0f;
        }

        float support = 0f;
        List<MarineCreatureAgent> creatures = manager.GetNearbyCreatures(transform.position, GroupDangerRadius);
        for (int i = 0; i < creatures.Count; i++)
        {
            MarineCreatureAgent other = creatures[i];
            if (other == null || other == this || !IsFriendlyByMorph(other))
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, other.transform.position);
            if (distance > GroupDangerRadius)
            {
                continue;
            }

            float t = 1f - Mathf.Clamp01(distance / GroupDangerRadius);
            support += t;
        }

        return support * GroupDangerWeight;
    }


    // Gets the max health used by the sim
    private float GetMaxHealth()
    {
        float body = EffectiveStats != null ? EffectiveStats.BodySize : Candidate != null && Candidate.Genome != null ? Candidate.Genome.BodySize : 1f;
        float armour = EffectiveStats != null ? EffectiveStats.Defence : Candidate != null && Candidate.Genome != null ? Candidate.Genome.Armour : 0f;
        return BaseHealth * Mathf.Lerp(0.75f, 1.65f, Mathf.InverseLerp(0.4f, 2.8f, body)) + armour * 8f;
    }

    // Gets the stomach capacity used by the sim
    private float GetStomachCapacity()
    {
        if (Candidate == null || Candidate.Genome == null || EffectiveStats == null)
        {
            return BaseStomachCapacity;
        }

        return Mathf.Max(4f, BaseStomachCapacity * Candidate.Genome.StomachSize * Mathf.Lerp(0.65f, 1.8f, Mathf.InverseLerp(0.4f, 2.8f, EffectiveStats.BodySize)));
    }

    // Gets the stomach total used by the sim
    private float GetStomachTotal()
    {
        return Mathf.Max(0f, StomachPlant + StomachMeat + StomachCarrion);
    }

    // Gets the stomach fullness01 used by the sim
    public float GetStomachFullness01()
    {
        return Mathf.Clamp01(GetStomachTotal() / Mathf.Max(0.01f, GetStomachCapacity()));
    }

    // Gets the health ratio used by the sim
    public float GetHealthRatio()
    {
        return Mathf.Clamp01(CurrentHealth / Mathf.Max(0.01f, GetMaxHealth()));
    }

    // Gets the effective energy ratio used by the sim
    public float GetEffectiveEnergyRatio()
    {
        float energyRatio = Mathf.Clamp01(CurrentEnergy / Mathf.Max(0.01f, EffectiveStats != null ? EffectiveStats.EnergyCapacity : Candidate.Genome.EnergyCapacity));
        float stomachRatio = GetStomachFullness01();
        return Mathf.Clamp01(energyRatio * 0.55f + stomachRatio * 0.45f);
    }

    // Gets the hunger pressure used by the sim
    public float GetHungerPressure()
    {
        return 1f - GetEffectiveEnergyRatio();
    }

    // Checks if it is hungry enough to search
    public bool IsHungryEnoughToSearch()
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return true;
        }

        if (GetHealthRatio() <= DesperateHealthRatio)
        {
            return true;
        }

        float threshold = Mathf.Clamp(Candidate.Genome.HungerThreshold, 0.08f, 0.95f);
        return GetHungerPressure() >= threshold;
    }

    // Updates resource satisfaction state using the current sim state
    private void UpdateResourceSatisfactionState()
    {
        if (Candidate == null || Candidate.Genome == null || EffectiveStats == null)
        {
            ShouldLeaveCurrentResource = false;
            return;
        }

        ShouldLeaveCurrentResource = IsSatisfiedEnoughToLeaveResource();
    }

    // Turns stomach contents into energy based on diet and digestion stats
    private void DigestStomach()
    {
        if (Candidate == null || Candidate.Genome == null || EffectiveStats == null)
        {
            return;
        }

        float metabolism = Mathf.Max(0.1f, Candidate.Genome.Metabolism);
        float digestBudget = BaseDigestionPerSecond * metabolism * Time.fixedDeltaTime;
        float total = GetStomachTotal();
        if (total <= 0.001f || digestBudget <= 0f)
        {
            return;
        }

        float amount = Mathf.Min(total, digestBudget);
        float plantShare = StomachPlant / total;
        float meatShare = StomachMeat / total;
        float carrionShare = StomachCarrion / total;

        float plantDigested = Mathf.Min(StomachPlant, amount * plantShare);
        float meatDigested = Mathf.Min(StomachMeat, amount * meatShare);
        float carrionDigested = Mathf.Min(StomachCarrion, amount * carrionShare);

        StomachPlant -= plantDigested;
        StomachMeat -= meatDigested;
        StomachCarrion -= carrionDigested;

        float gained = plantDigested * Mathf.Lerp(0.95f, 1.45f, Candidate.Genome.PlantDiet) * PlantEnergyGainMultiplier;
        gained += meatDigested * Mathf.Lerp(1.05f, 1.65f, Candidate.Genome.MeatDiet) * MeatEnergyGainMultiplier;
        gained += carrionDigested * Mathf.Lerp(0.85f, 1.45f, Candidate.Genome.CarrionDiet) * CarrionEnergyGainMultiplier;

        CurrentEnergy = Mathf.Min(CurrentEnergy + gained, EffectiveStats.EnergyCapacity);
        Candidate.EnergyGained += gained;
    }

    // Gets the bite mass used by the sim
    private float GetBiteMass(bool meatBite)
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return BaseBiteMass;
        }

        float jaw = Candidate.Genome.JawSize * 0.55f + Candidate.Genome.JawWidth * 0.25f + Candidate.Genome.JawLength * 0.20f;
        float bite = BaseBiteMass * Mathf.Clamp(jaw, 0.35f, 2.6f);
        if (meatBite)
        {
            bite *= Mathf.Lerp(0.65f, 1.25f, Candidate.Genome.MeatDiet + Candidate.Genome.CarrionDiet * 0.35f);
        }
        return Mathf.Max(0.25f, bite);
    }

    // Gets the remaining stomach space used by the sim
    private float GetRemainingStomachSpace()
    {
        return Mathf.Max(0f, GetStomachCapacity() - GetStomachTotal());
    }

    // Handles add to stomach
    private void AddToStomach(float plant, float meat, float carrion)
    {
        float space = GetRemainingStomachSpace();
        float total = Mathf.Max(0f, plant + meat + carrion);
        if (space <= 0f || total <= 0f)
        {
            return;
        }

        float scale = Mathf.Min(1f, space / total);
        StomachPlant += plant * scale;
        StomachMeat += meat * scale;
        StomachCarrion += carrion * scale;
    }

    // Handles remember food area
    private void RememberFoodArea(Vector3 foodPosition, bool badMemory)
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return;
        }

        float memory = Mathf.Clamp01(Candidate.Genome.FoodMemoryStrength);
        if (memory <= 0.05f)
        {
            return;
        }

        Vector2 noise = Random.insideUnitCircle * FoodMemoryAreaRadius * Mathf.Lerp(1.2f, 0.25f, memory);
        rememberedFoodArea = new Vector3(foodPosition.x + noise.x, foodPosition.y, foodPosition.z + noise.y);
        foodMemoryTimer = (badMemory ? BadFoodMemoryDuration : FoodMemoryDuration) * Mathf.Lerp(0.45f, 1.45f, memory);
        rememberedFoodWasBad = badMemory;
        hasFoodMemory = true;
    }

    // Counts down good and bad food memories
    private void UpdateFoodMemoryTimers()
    {
        if (!hasFoodMemory)
        {
            return;
        }

        foodMemoryTimer -= Time.fixedDeltaTime;

        if (!rememberedFoodWasBad && Vector3.Distance(transform.position, rememberedFoodArea) <= FoodMemoryAreaRadius * 0.45f)
        {
            bool foundSomething = (nearestFood != null && !nearestFood.IsConsumed) || (nearestCarrion != null && !nearestCarrion.IsConsumed);
            if (!foundSomething)
            {
                rememberedFoodWasBad = true;
                foodMemoryTimer = Mathf.Min(foodMemoryTimer, BadFoodMemoryDuration);
            }
        }

        if (foodMemoryTimer <= 0f)
        {
            hasFoodMemory = false;
            rememberedFoodWasBad = false;
        }
    }

    // Spends energy based on movement, body traits and local environment pressure
    private void DrainEnergy()
    {
        float environmentDrain = 1f;
        float localHealthPressure = 0f;
        if (EvolutionEcosystemManager.Instance != null)
        {
            environmentDrain = EvolutionEcosystemManager.Instance.GetLocalEnergyDrainMultiplier(transform.position);
            localHealthPressure = EvolutionEcosystemManager.Instance.GetLocalHealthPressure(transform.position);
        }

        float movementCost = currentVelocity.magnitude / Mathf.Max(0.1f, EffectiveStats.Speed);
        float metabolism = Candidate != null && Candidate.Genome != null ? Mathf.Max(0.1f, Candidate.Genome.Metabolism) : 1f;
        float drain = BaseEnergyDrainPerSecond * EffectiveStats.EnergyDrainMultiplier * environmentDrain;
        drain *= Mathf.Lerp(0.68f, 1.15f, metabolism);
        drain += movementCost * 0.10f;
        if (isSprintingThisTick)
        {
            float injurySprintPenalty = 1f + GetInjurySeverity01() * Mathf.Max(0f, InjurySprintEnergyExtraMultiplier);
            drain *= Mathf.Max(1f, SprintEnergyCostMultiplier) * injurySprintPenalty;
        }

        float stomachReserve = GetStomachFullness01();
        if (stomachReserve > 0.01f)
        {
            drain *= Mathf.Lerp(1f, 0.58f, stomachReserve);
            CurrentEnergy = Mathf.Min(EffectiveStats.EnergyCapacity, CurrentEnergy + stomachReserve * EnergyRecoveredPerStoredStomachMass * Time.fixedDeltaTime);
        }

        CurrentEnergy = Mathf.Max(0f, CurrentEnergy - drain * Time.fixedDeltaTime);
        if (localHealthPressure > 0f)
        {
            CurrentHealth -= localHealthPressure * Time.fixedDeltaTime;
        }

        float energyRatio = Mathf.Clamp01(CurrentEnergy / Mathf.Max(0.01f, EffectiveStats.EnergyCapacity));
        float stomachRatio = GetStomachFullness01();
        if (energyRatio <= 0.01f && stomachRatio <= 0.01f)
        {
            float damage = StarvationHealthDamagePerSecond * Mathf.Lerp(0.75f, 1.45f, metabolism) * Time.fixedDeltaTime;
            CurrentHealth -= damage;
            if (Candidate != null)
            {
                Candidate.StarvationDamageTaken += damage;
            }
        }
        else
        {
            float healMultiplier = GetRestingHealMultiplier(energyRatio, stomachRatio);
            if (healMultiplier > 0f)
            {
                CurrentHealth = Mathf.Min(GetMaxHealth(), CurrentHealth + HealthRecoveryPerSecond * healMultiplier * Time.fixedDeltaTime);
            }
        }
    }

    // Attempts to eat plant food if the fish is close enough and allowed to feed
    private void TryEatFood()
    {
        if (IsPredatorPlantBlockedRole() && !CanUsePlantAsPredatorEmergencyFallback())
        {
            ClearPlantTargetForPredator();
            return;
        }

        if (feedingHoldTimer > 0f || ShouldIgnoreStaticFoodBecauseThreatened() || !WantsStaticResourceNow() || nearestFood == null || nearestFood.IsConsumed || GetRemainingStomachSpace() <= 0.05f)
        {
            return;
        }

        if (!CanConsumeStaticTarget(nearestFood.transform.position))
        {
            return;
        }

        float biteMass = Mathf.Min(GetBiteMass(false) * 0.72f, GetRemainingStomachSpace());
        float eatenMass = nearestFood.ConsumeBiteBy(biteMass, Candidate != null ? Candidate.Id : 0);
        if (eatenMass <= 0f)
        {
            RememberFoodArea(nearestFood.transform.position, true);
            nearestFood = null;
            return;
        }

        AddToStomach(eatenMass, 0f, 0f);
        Candidate.PlantEnergyConsumed += eatenMass;
        Candidate.FoodEaten++;
        Candidate.Genome.ReinforceDietUsage(Candidate.PlantEnergyConsumed, Candidate.MeatEnergyConsumed, Candidate.CarrionEnergyConsumed, DietLearningRate);
        OnSuccessfulStaticBite(nearestFood.transform.position, true);
        if (nearestFood == null || nearestFood.IsConsumed)
        {
            nearestFood = null;
        }
    }

    // Attempts to eat carrion or claimed kills when the fish reaches them
    private void TryEatCarrion()
    {
        bool eatingOwnKill = ShouldContinueEatingFreshKillCarrion() && claimedFreshKillCarrion != null && nearestCarrion == claimedFreshKillCarrion;

        if (feedingHoldTimer > 0f || (!eatingOwnKill && ShouldIgnoreStaticFoodBecauseThreatened()) || (!eatingOwnKill && !WantsStaticResourceNow()) || nearestCarrion == null || nearestCarrion.IsConsumed || GetRemainingStomachSpace() <= 0.05f)
        {
            return;
        }

        if (!CanConsumeStaticTarget(nearestCarrion.transform.position))
        {
            return;
        }

        float biteMultiplier = eatingOwnKill ? 1.45f : 0.82f;
        float biteMass = Mathf.Min(GetBiteMass(true) * biteMultiplier, GetRemainingStomachSpace());
        float eatenMass = nearestCarrion.ConsumeBiteBy(biteMass, Candidate != null ? Candidate.Id : 0);
        if (eatenMass <= 0f)
        {
            RememberFoodArea(nearestCarrion.transform.position, true);
            nearestCarrion = null;
            return;
        }

        AddToStomach(0f, 0f, eatenMass);
        if (claimedFreshKillCarrion != null && nearestCarrion == claimedFreshKillCarrion && ShouldContinueEatingFreshKillCarrion())
        {
            freshKillPriorityTimer = Mathf.Max(freshKillPriorityTimer, FreshKillPriorityTime * 0.45f);
        }
        Candidate.CarrionEnergyConsumed += eatenMass;
        Candidate.CarrionEaten++;
        Candidate.Genome.ReinforceDietUsage(Candidate.PlantEnergyConsumed, Candidate.MeatEnergyConsumed, Candidate.CarrionEnergyConsumed, DietLearningRate);
        OnSuccessfulStaticBite(nearestCarrion.transform.position, false);
        if (nearestCarrion == null || nearestCarrion.IsConsumed)
        {
            nearestCarrion = null;
        }
    }

    // Handles on successful static bite
    private void OnSuccessfulStaticBite(Vector3 resourcePosition, bool plantFood)
    {
        feedingHoldTimer = FeedingHoldAfterBite;
        currentStaticFeedingPoint = resourcePosition;
        hasStaticFeedingPoint = true;
        currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, FeedingHoldDamping * Time.fixedDeltaTime);
        RememberFoodArea(resourcePosition, false);

        if (!IsSatisfiedEnoughToLeaveResource())
        {
            return;
        }

        if (plantFood && nearestFood != null)
        {
            temporarilyIgnoredFood = nearestFood;
            nearestFood = null;
            retainedFood = null;
        }
        else if (!plantFood && nearestCarrion != null)
        {
            temporarilyIgnoredCarrion = nearestCarrion;
            nearestCarrion = null;
            retainedCarrion = null;
        }

        ignoredResourceTimer = Mathf.Max(ignoredResourceTimer, LeaveResourceIgnoreTime);
        retainedTargetTimer = 0f;
        staticTargetNoProgressTimer = 0f;
        PickNewWanderDirection();
    }


    // Checks if it should sleep to heal right now
    private bool ShouldSleepToHeal()
    {
        if (!HealOnlyWhileRestingOrSleeping || CurrentHealth >= GetMaxHealth() * SleepHealthRatioThreshold)
        {
            return false;
        }

        if (recentDamageHealLockTimer > 0f || brainFleeDesire > 0.18f || lastThreatCount > 0 || groupPredatorWarningTimer > 0f || HasCloseVisiblePredatorThreat())
        {
            return false;
        }

        float energyRatio = GetEffectiveEnergyRatio();
        float stomachRatio = GetStomachFullness01();
        bool hasReserve = energyRatio >= SleepMinimumEnergyRatio || stomachRatio >= SleepMinimumStomachRatio;
        bool notUrgentlyHungry = brainFoodDesire < 0.50f && !HasCloseFoodTarget();
        return hasReserve && notUrgentlyHungry;
    }

    // Gets the resting heal multiplier used by the sim
    private float GetRestingHealMultiplier(float energyRatio, float stomachRatio)
    {
        if (CurrentHealth >= GetMaxHealth())
        {
            return 0f;
        }

        if (recentDamageHealLockTimer > 0f)
        {
            return 0f;
        }

        bool hasReserve = energyRatio > 0.45f || stomachRatio > 0.25f;
        if (!hasReserve)
        {
            return 0f;
        }

        if (!HealOnlyWhileRestingOrSleeping)
        {
            return 1f;
        }

        if (currentBrainMode == FishAutonomousBehaviourMode.Sleeping)
        {
            return SleepingHealMultiplier;
        }

        if (currentBrainMode == FishAutonomousBehaviourMode.Resting)
        {
            return RestingHealMultiplier;
        }

        return 0f;
    }

    // Checks if it should sprint this tick right now
    private bool ShouldSprintThisTick(Vector3? currentTarget, float hungerPressure)
    {
        if (!UseSprintBursts || sprintCooldownTimer > 0f || rb == null || EffectiveStats == null)
        {
            return false;
        }

        float energyRatio = GetEffectiveEnergyRatio();
        float stomachRatio = GetStomachFullness01();
        if (energyRatio < SprintMinimumEnergyRatio && stomachRatio < SprintMinimumStomachRatio)
        {
            sprintCooldownTimer = SprintCooldownAfterExhaustion;
            return false;
        }

        if (survivalEmergencyTimer > 0f && SurvivalCanOverrideBrainConservation)
        {
            return true;
        }

        // The evolved brain can learn to conserve energy by suppressing sprinting, or spend energy
        // aggressively when a valuable chase/escape target exists.
        bool highValueSprintContext = currentBrainMode == FishAutonomousBehaviourMode.Hunting
            || currentBrainMode == FishAutonomousBehaviourMode.Ambushing
            || currentBrainMode == FishAutonomousBehaviourMode.Fleeing
            || currentEscapeTimer > 0f;
        if (brainOutputSprintBias < -0.45f && currentBrainMode != FishAutonomousBehaviourMode.Fleeing)
        {
            return false;
        }
        if (brainOutputSprintBias > 0.55f && highValueSprintContext && currentTarget.HasValue)
        {
            return true;
        }

        if (currentBrainMode == FishAutonomousBehaviourMode.Fleeing)
        {
            return cStartBurstTimer > 0f || GetHealthRatio() <= SprintFleeHealthRatio || lastThreatCount > 0;
        }

        if (UseCurrentEscapeSteering && currentEscapeTimer > 0f && lastCurrentStress >= CurrentEscapeSprintStress)
        {
            return true;
        }

        if ((currentBrainMode == FishAutonomousBehaviourMode.Hunting || currentBrainMode == FishAutonomousBehaviourMode.Ambushing || brainWantsHunt)
            && nearestPrey != null && CanAttackPrey(nearestPrey))
        {
            float distance = Vector3.Distance(GetMouthWorldPosition(), nearestPrey.GetBiteTargetPosition());
            float minimumUsefulDistance = Mathf.Max(GetScaledMouthRadius() * 1.4f, GetPersonalRadius() * 0.75f);
            return distance > minimumUsefulDistance && distance <= SprintChaseDistance * Mathf.Lerp(0.75f, 1.45f, Candidate.Genome.Aggression);
        }

        return false;
    }

    // Gets the best immediate bite target used by the sim
    private MarineCreatureAgent GetBestImmediateBiteTarget()
    {
        MarineCreatureAgent best = null;
        float bestScore = float.MaxValue;

        ConsiderImmediateBiteCandidate(nearestPrey, ref best, ref bestScore);
        MarineCreatureAgent overlap = UsePredatorOverlapBiteCheck ? FindBiteCandidateByOverlap() : null;
        ConsiderImmediateBiteCandidate(overlap, ref best, ref bestScore);

        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager != null)
        {
            float scanRadius = Mathf.Max(GetScaledMouthRadius() * PredatorBiteScanRadiusMultiplier, GetPersonalRadius() + 1.35f);
            List<MarineCreatureAgent> nearby = manager.GetNearbyCreatures(GetMouthWorldPosition(), scanRadius + 1.5f);
            for (int i = 0; i < nearby.Count; i++)
            {
                ConsiderImmediateBiteCandidate(nearby[i], ref best, ref bestScore);
            }
        }

        if (best != null)
        {
            nearestPrey = best;
        }

        return best;
    }

    // Handles consider immediate bite candidate
    private void ConsiderImmediateBiteCandidate(MarineCreatureAgent candidate, ref MarineCreatureAgent best, ref float bestScore)
    {
        if (candidate == null || candidate == this || !CanAttackPrey(candidate))
        {
            return;
        }

        Vector3 target = candidate.GetBiteTargetPosition();
        bool biteable = IsPositionInsideMouthArea(target) || IsCloseEnoughForPredatorContactBite(candidate);
        if (!biteable)
        {
            return;
        }

        float score = (target - GetMouthWorldPosition()).sqrMagnitude;
        if (score < bestScore)
        {
            bestScore = score;
            best = candidate;
        }
    }

    // Finds bite candidate by overlap by checking the current options
    private MarineCreatureAgent FindBiteCandidateByOverlap()
    {
        Vector3 mouth = GetMouthWorldPosition();
        float radius = Mathf.Max(GetScaledMouthRadius() * PredatorBiteScanRadiusMultiplier, GetPersonalRadius() + PredatorContactBiteMouthPadding);
        int hitCount = Physics.OverlapSphereNonAlloc(mouth, radius, predatorBiteOverlapBuffer, ~0, QueryTriggerInteraction.Collide);
        MarineCreatureAgent best = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = predatorBiteOverlapBuffer[i];
            if (hit == null)
            {
                continue;
            }

            MarineCreatureAgent candidate = null;
            CreatureHurtbox hurtbox = hit.GetComponent<CreatureHurtbox>();
            if (hurtbox != null && hurtbox.Owner != null)
            {
                candidate = hurtbox.Owner;
            }
            else
            {
                candidate = hit.GetComponentInParent<MarineCreatureAgent>();
            }

            if (candidate == null || candidate == this || !CanAttackPrey(candidate))
            {
                continue;
            }

            float score = (candidate.GetBiteTargetPosition() - mouth).sqrMagnitude;
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    // Attempts a predator bite using distance, mouth checks and hurtboxes
    private void TryBitePrey()
    {
        if (biteTimer > 0f)
        {
            return;
        }

        MarineCreatureAgent biteTarget = GetBestImmediateBiteTarget();
        if (biteTarget == null)
        {
            return;
        }

        Vector3 preyTarget = biteTarget.GetBiteTargetPosition();
        bool validMouthBite = IsPositionInsideMouthArea(preyTarget);

        // Hunting should not depend on Unity collision contact. If the hunter has physically reached the prey,
        // allow a close-body bite even when the mouth-angle check is slightly off. This stops oversized visual
        // bounds or avoidance spacing from making predators orbit forever without landing hits.
        if (!validMouthBite && IsCloseEnoughForPredatorContactBite(biteTarget))
        {
            validMouthBite = true;
        }

        if (!validMouthBite)
        {
            if (Vector3.Distance(GetMouthWorldPosition(), preyTarget) <= GetScaledMouthRadius() * 1.45f)
            {
                CurrentEnergy -= MissedAttackEnergyCost;
                biteTimer = BiteCooldown * 0.5f;
            }
            return;
        }

        Vector3 preyDeathPosition = biteTarget.transform.position;
        float damage = GetBiteDamageAgainst(biteTarget);
        bool committedHunter = currentBrainMode == FishAutonomousBehaviourMode.Hunting
            || currentBrainMode == FishAutonomousBehaviourMode.Ambushing
            || brainWantsHunt
            || GetEffectiveEnergyRatio() <= HungryPredatorCommitEnergyRatio;

        if (committedHunter)
        {
            damage *= Mathf.Max(1f, PredatorCommittedBiteDamageMultiplier);
        }

        bool killed = biteTarget.ReceiveBite(this, damage, out float energyGained);
        predatorTimeSinceLastBite = 0f;
        predatorChaseTimer = 0f;
        RememberFoodArea(preyDeathPosition, false);
        if (!killed)
        {
            FocusPrey(biteTarget, HunterBiteFocusTime);
        }

        // Important: predators must gain usable meat from successful bites, not only from final kills.
        // Otherwise hunters can land hits but still starve/converge out before getting a kill.
        if (energyGained <= 0f)
        {
            float biteMeat = PredatorBiteBaseMeatReward + damage * Mathf.Max(0f, PredatorBiteMeatFromDamageMultiplier);
            biteMeat *= Mathf.Lerp(0.75f, 1.35f, Candidate.Genome.MeatDiet);
            energyGained = biteMeat;
        }

        float meatStored = Mathf.Min(energyGained, GetRemainingStomachSpace());
        AddToStomach(0f, meatStored, 0f);
        if (!killed && UsePredatorHuntCommitment && (GetEffectiveEnergyRatio() >= PredatorSatisfiedAfterBiteEnergyRatio || GetStomachFullness01() >= PredatorSatisfiedAfterBiteStomachRatio))
        {
            ClearFocusedPrey(false);
            predatorExhaustionTimer = Mathf.Max(predatorExhaustionTimer, PredatorExhaustionAfterEnoughBites);
        }
        Candidate.MeatEnergyConsumed += meatStored;
        Candidate.Genome.ReinforceDietUsage(Candidate.PlantEnergyConsumed, Candidate.MeatEnergyConsumed, Candidate.CarrionEnergyConsumed, DietLearningRate);
        Candidate.PreyBites++;
        Candidate.BiteDamageDealt += damage;
        if (killed)
        {
            Candidate.PreyKills++;
            RegisterFreshKill(preyDeathPosition);
        }
        biteTimer = BiteCooldown;
    }

    // Checks if it is close enough for predator contact bite
    private bool IsCloseEnoughForPredatorContactBite(MarineCreatureAgent prey)
    {
        if (prey == null)
        {
            return false;
        }

        Vector3 mouth = GetMouthWorldPosition();
        Vector3 preyTarget = prey.GetBiteTargetPosition();
        float mouthDistance = Vector3.Distance(mouth, preyTarget);
        float bodyDistance = Vector3.Distance(transform.position, prey.transform.position);

        float ownRadius = GetPersonalRadius();
        float preyRadius = prey.GetPersonalRadius();
        float mouthAllowance = Mathf.Max(GetScaledMouthRadius() * PredatorBiteScanRadiusMultiplier, ownRadius + PredatorContactBiteMouthPadding);
        float bodyAllowance = Mathf.Max(ownRadius + preyRadius + PredatorContactBiteBodyPadding, GetScaledMouthRadius() * 2.95f);

        return mouthDistance <= mouthAllowance || bodyDistance <= bodyAllowance;
    }

    // Checks if it can consume static target with the current state
    private bool CanConsumeStaticTarget(Vector3 targetPosition)
    {
        if (IsPositionInsideMouthArea(targetPosition))
        {
            return true;
        }

        float bodyDistance = Vector3.Distance(transform.position, targetPosition);
        float bodyContact = Mathf.Max(StaticFoodContactConsumeDistance, StaticBodyContactConsumeDistance, GetPersonalRadius() + GetScaledMouthRadius() * 0.85f);
        if (bodyDistance <= bodyContact)
        {
            return true;
        }

        float mouthDistance = Vector3.Distance(GetMouthWorldPosition(), targetPosition);
        float mouthContact = Mathf.Max(GetScaledMouthRadius() * 1.85f, StaticMouthContactConsumeDistance, StaticFoodContactConsumeDistance * 0.9f);
        return mouthDistance <= mouthContact;
    }

    // Checks if it is position inside mouth area
    private bool IsPositionInsideMouthArea(Vector3 targetPosition)
    {
        Vector3 mouthPosition = GetMouthWorldPosition();
        Vector3 toTarget = targetPosition - mouthPosition;
        float radius = GetScaledMouthRadius();
        float distance = toTarget.magnitude;

        if (distance <= radius)
        {
            if (distance <= 0.0001f)
            {
                return true;
            }

            float dot = Vector3.Dot(transform.forward, toTarget.normalized);
            float requiredDot = Mathf.Cos((MouthAngle * 0.5f) * Mathf.Deg2Rad);
            return dot >= requiredDot;
        }

        float forgivingDistance = radius + CloseContactEatForgiveness * Mathf.Max(0.25f, EffectiveStats.BodySize);
        return Vector3.Distance(transform.position, targetPosition) <= forgivingDistance;
    }

    // Gets the mouth world position used by the sim
    public Vector3 GetMouthWorldPosition()
    {
        float bodyScale = EffectiveStats != null ? EffectiveStats.BodySize : transform.localScale.x;
        float offsetMultiplier = EffectiveStats != null ? EffectiveStats.MouthForwardOffsetMultiplier : 1f;
        return transform.position + transform.forward * MouthForwardOffset * bodyScale * offsetMultiplier;
    }

    // Gets the scaled mouth radius used by the sim
    public float GetScaledMouthRadius()
    {
        float bodyScale = EffectiveStats != null ? EffectiveStats.BodySize : transform.localScale.x;
        float mouthMultiplier = EffectiveStats != null ? EffectiveStats.MouthRadiusMultiplier : 1f;
        return Mathf.Max(0.05f, MouthRadius * bodyScale * mouthMultiplier);
    }

    // Gets the bite target position used by the sim
    public Vector3 GetBiteTargetPosition()
    {
        float bodyScale = EffectiveStats != null ? EffectiveStats.BodySize : transform.localScale.x;
        return transform.position + transform.up * bodyScale * 0.12f;
    }

    // Gets the threat range used by the sim
    public float GetThreatRange()
    {
        if (Candidate == null || Candidate.Genome == null || EffectiveStats == null)
        {
            return 0f;
        }
        return EffectiveStats.ThreatRange * EffectiveStats.VisionRange;
    }

    // Gets the bite damage used by the sim
    public float GetBiteDamage()
    {
        if (Candidate == null || Candidate.Genome == null || EffectiveStats == null)
        {
            return BaseBiteDamage;
        }

        float damage = BaseBiteDamage + EffectiveStats.BiteDamage;
        damage += Candidate.Genome.Aggression * 4f;
        damage += Mathf.Max(0f, Candidate.Genome.JawSize - 1f) * 3.5f;
        damage *= Mathf.Lerp(0.55f, 1.35f, Candidate.Genome.MeatDiet);
        damage *= Mathf.Lerp(0.9f, 1.22f, Candidate.Genome.Aggression);
        return Mathf.Max(2f, damage);
    }

    // Gets the bite damage against used by the sim
    private float GetBiteDamageAgainst(MarineCreatureAgent prey)
    {
        float damage = GetBiteDamage();
        if (prey == null || prey.Candidate == null || prey.Candidate.Genome == null || Candidate == null || Candidate.Genome == null)
        {
            return damage;
        }

        float ownSize = Mathf.Max(0.1f, EffectiveStats != null ? EffectiveStats.BodySize : Candidate.Genome.BodySize);
        float preySize = Mathf.Max(0.1f, prey.EffectiveStats != null ? prey.EffectiveStats.BodySize : prey.Candidate.Genome.BodySize);
        float sizeRatio = Mathf.Clamp(ownSize / preySize, 0.35f, 3.5f);
        float sizeMultiplier = Mathf.Lerp(0.50f, 1.55f, Mathf.InverseLerp(0.45f, 2.6f, sizeRatio));
        damage *= Mathf.Lerp(1f, sizeMultiplier, Mathf.Clamp01(PredatorSizeDamageScale));
        damage *= Mathf.Max(1f, SharkLikeStrikeDamageMultiplier);
        damage *= Mathf.Lerp(1.28f, 1.0f, prey.GetHealthRatio());
        damage *= Mathf.Lerp(1f, 1.18f, prey.GetWoundedScent01());
        if (prey.lastFriendlyCount >= prey.GroupCounterMinimumAllies && prey.GetHealthRatio() > 0.55f)
        {
            damage *= Mathf.Clamp(HealthyGroupedPreyDamageDamping, 0.45f, 0.95f);
        }
        return Mathf.Max(1.2f, damage);
    }

    // Checks if it should treat as risky mate instead of prey right now
    private bool ShouldTreatAsRiskyMateInsteadOfPrey(MarineCreatureAgent other, float morphSimilarity)
    {
        if (!AllowPredatorMateRiskWindow || other == null || Candidate == null || Candidate.Genome == null || other.Candidate == null || other.Candidate.Genome == null)
        {
            return false;
        }

        bool oppositeSex = (Candidate.Genome.SexGene >= 0.5f) != (other.Candidate.Genome.SexGene >= 0.5f);
        if (!oppositeSex || morphSimilarity < PredatorMateSimilarity)
        {
            return false;
        }

        bool bothPredators = IsEcologicalPredatorRole() && other.GetPredatorDrive01() >= 0.45f;
        if (!bothPredators)
        {
            return false;
        }

        bool bothMature = IsMatureForMating() && other.IsMatureForMating();
        if (!bothMature)
        {
            return false;
        }

        bool enoughReserve = (GetEffectiveEnergyRatio() >= PredatorMateRiskEnergyRatio || GetStomachFullness01() >= PredatorMateRiskStomachRatio)
            && (other.GetEffectiveEnergyRatio() >= PredatorMateRiskEnergyRatio * 0.85f || other.GetStomachFullness01() >= PredatorMateRiskStomachRatio * 0.85f);
        return enoughReserve;
    }

    // Checks if it should avoid similar predator combat right now
    private bool ShouldAvoidSimilarPredatorCombat(MarineCreatureAgent other, float morphSimilarity)
    {
        if (!ProtectSimilarPredatorsFromSpawnKilling || other == null || other == this)
        {
            return false;
        }

        if (morphSimilarity < SimilarPredatorPeaceSimilarity)
        {
            return false;
        }

        bool bothPredatorLike = IsEcologicalPredatorRole() && other.IsEcologicalPredatorRole();
        if (!bothPredatorLike)
        {
            return false;
        }

        // Only break same-niche predator peace if this fish is genuinely about to starve.
        return !(GetEffectiveEnergyRatio() <= SimilarPredatorAttackEnergyRatio && GetStomachFullness01() <= SimilarPredatorAttackStomachRatio);
    }

    // Checks if this fish is allowed to attack the target based on diet, hunger and danger
    public bool CanAttackPrey(MarineCreatureAgent prey)
    {
        if (prey == null || prey == this || Candidate == null || Candidate.Genome == null || prey.Candidate == null || prey.Candidate.Genome == null || EffectiveStats == null)
        {
            return false;
        }

        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null || !manager.EnablePredation)
        {
            return false;
        }

        float energyRatio = Mathf.Clamp01(CurrentEnergy / Mathf.Max(0.01f, EffectiveStats.EnergyCapacity));
        if (IsPeacefulGrazerRole() || ShouldPredatorChillAfterFeeding())
        {
            return false;
        }

        bool committedPredator = energyRatio <= HungryPredatorCommitEnergyRatio
            || Candidate.Genome.MeatDiet >= CommittedPredatorMorphAttackThreshold
            || Candidate.Genome.Aggression >= 0.46f
            || currentBrainMode == FishAutonomousBehaviourMode.Hunting
            || currentBrainMode == FishAutonomousBehaviourMode.Ambushing;

        float morphSimilarity = Candidate.Genome.GetMorphSimilarity(prey.Candidate.Genome);
        if (ShouldTreatAsRiskyMateInsteadOfPrey(prey, morphSimilarity))
        {
            return false;
        }
        if (ShouldAvoidSimilarPredatorCombat(prey, morphSimilarity))
        {
            return false;
        }

        if (morphSimilarity >= MorphSimilarityForSchool)
        {
            bool hungryEnoughToBreakSchool = energyRatio <= HungryPredatorCommitEnergyRatio;
            if (!hungryEnoughToBreakSchool && !committedPredator && (Candidate.Genome.Aggression < SameMorphAttackAggressionRequired || Candidate.Genome.MeatDiet < SameMorphAttackMeatRequired))
            {
                return false;
            }
        }

        bool normalPredator = IsEcologicalPredatorRole()
            && Candidate.Genome.MeatDiet >= manager.MinimumMeatDietToHunt
            && Candidate.Genome.Aggression >= manager.MinimumAggressionToHunt;
        bool starvingPredator = energyRatio <= StarvingAttackEnergyRatio
            && Candidate.Genome.MeatDiet >= StarvingAttackMeatRequired
            && Candidate.Genome.Aggression >= StarvingAttackAggressionRequired
            && !IsPeacefulGrazerRole();
        if (!normalPredator && !starvingPredator)
        {
            return false;
        }

        float preySize = Mathf.Max(0.1f, prey.EffectiveStats != null ? prey.EffectiveStats.BodySize : prey.Candidate.Genome.BodySize);
        float ownSize = Mathf.Max(0.1f, EffectiveStats.BodySize);
        float allowedRatio = manager.MaxPreySizeRatio + Candidate.Genome.RiskTolerance * 0.45f + Candidate.Genome.Aggression * 0.25f;
        if (preySize / ownSize > allowedRatio)
        {
            return false;
        }

        float attackerConfidence = EffectiveStats.DangerFactor + Candidate.Genome.Aggression + Candidate.Genome.RiskTolerance + Candidate.Genome.MeatDiet;
        if (mobbingPressureTimer > 0f)
        {
            attackerConfidence -= 0.45f;
        }
        if (currentBrainMode == FishAutonomousBehaviourMode.Ambushing)
        {
            attackerConfidence += Candidate.Genome.Stealth * 0.25f;
        }
        float preyDanger = prey.EffectiveStats != null ? prey.EffectiveStats.DangerFactor : prey.Candidate.Genome.DangerFactor;
        preyDanger += prey.Candidate.Genome.SpikeSize * 0.18f + prey.Candidate.Genome.Armour * 0.12f;
        preyDanger += Mathf.Max(0f, preySize / ownSize - 1f) * 0.75f;
        // Emergency optimisation: avoid a fresh scan when running 80+ fish, but still let grouped prey matter.
        if (!EmergencyPerformanceMode)
        {
            preyDanger += prey.GetGroupDangerSupport();
        }
        else if (prey.lastFriendlyCount >= prey.GroupCounterMinimumAllies)
        {
            float cachedGroup = prey.lastFriendlyCount * CachedGroupDefenceThreatWeight;
            cachedGroup *= Mathf.Lerp(0.75f, 1.35f, prey.Candidate.Genome.GroupingChance + prey.Candidate.Genome.Bravery);
            preyDanger += cachedGroup;
        }
        if (prey.lastFriendlyCount >= prey.GroupCounterMinimumAllies)
        {
            preyDanger += Mathf.Min(1.25f, prey.lastFriendlyCount * 0.22f) * Mathf.Lerp(0.85f, 1.35f, prey.Candidate.Genome.GroupingChance + prey.Candidate.Genome.Bravery);
        }

        float preyEnergyRatio = Mathf.Clamp01(prey.CurrentEnergy / Mathf.Max(0.01f, prey.EffectiveStats != null ? prey.EffectiveStats.EnergyCapacity : prey.Candidate.Genome.EnergyCapacity));
        float scareStrength = preyDanger * manager.PredatorFearOfDangerFactor * Mathf.Lerp(0.65f, 1.15f, preyEnergyRatio);

        if (prey.Candidate.Genome.PlantDiet >= prey.Candidate.Genome.MeatDiet && scareStrength > attackerConfidence + 0.25f)
        {
            // Defensive prey should discourage weak predators, but not completely delete the hunter niche.
            // Committed or starving predators can still try; the bite damage/armour system handles the cost.
            if (!committedPredator)
            {
                return false;
            }
        }

        float preyHealthRatio = prey.GetHealthRatio();
        bool preyPlantLed = prey.Candidate.Genome.PlantDiet >= prey.Candidate.Genome.MeatDiet && prey.Candidate.Genome.PlantDiet >= prey.Candidate.Genome.CarrionDiet;
        bool preyHealthyGroupedGrazer = preyPlantLed && preyHealthRatio > 0.62f && prey.GetWoundedScent01() < 0.12f && prey.lastFriendlyCount >= Mathf.Max(2, prey.GroupCounterMinimumAllies);
        bool predatorIsStarving = energyRatio <= StarvingAttackEnergyRatio || GetStomachFullness01() <= 0.12f;
        if (preyHealthyGroupedGrazer && !predatorIsStarving)
        {
            return false;
        }

        return true;
    }

    // Tries to group defensive counter attack and returns whether it worked
    private void TryGroupDefensiveCounterAttack(MarineCreatureAgent attacker, float receivedDamage)
    {
        if (!EnableGroupDefenceCounterAttacks || attacker == null || attacker == this || Candidate == null || Candidate.Genome == null)
        {
            return;
        }

        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null)
        {
            return;
        }

        int allyCount = 0;
        float allyAggression = 0f;
        List<MarineCreatureAgent> nearby = manager.GetNearbyCreatures(transform.position, GroupCounterSupportRadius);
        for (int i = 0; i < nearby.Count; i++)
        {
            MarineCreatureAgent ally = nearby[i];
            if (ally == null || ally == this || ally.Candidate == null || ally.Candidate.Genome == null)
            {
                continue;
            }

            if (!IsFriendlyByMorph(ally))
            {
                continue;
            }

            allyCount++;
            allyAggression += ally.Candidate.Genome.Aggression + ally.Candidate.Genome.Bravery + ally.Candidate.Genome.GroupingChance;
            ally.ReceiveMobbingPressure(attacker, attacker.transform.position);
        }

        float ownDefenceDrive = Candidate.Genome.Aggression * 0.25f + Candidate.Genome.Bravery * 0.30f + Candidate.Genome.DangerFactor * 0.25f + Candidate.Genome.SpikeSize * 0.35f + Candidate.Genome.GroupingChance * 0.20f;
        float groupDrive = allyCount > 0 ? Mathf.Clamp01(allyAggression / Mathf.Max(1f, allyCount * 2.2f)) : 0f;
        bool hasGroup = allyCount >= GroupCounterMinimumAllies;
        bool spikyEnough = ownDefenceDrive >= 0.65f;
        if (!hasGroup && !spikyEnough)
        {
            return;
        }

        float counterDamage = receivedDamage * GroupCounterDamageMultiplier * Mathf.Lerp(0.35f, 1.25f, Mathf.Clamp01(ownDefenceDrive + groupDrive));
        counterDamage += Mathf.Max(0, allyCount - 1) * 0.35f;
        if (attacker != null && attacker.GetEffectiveEnergyRatio() > attacker.HungryPredatorCommitEnergyRatio && attacker.GetStomachFullness01() > attacker.HunterChillStomachRatio * 0.8f)
        {
            counterDamage *= 0.45f;
        }
        counterDamage = Mathf.Clamp(counterDamage, 0.05f, GroupCounterMaxDamage);
        attacker.ReceiveCounterDamage(this, counterDamage);

        if (Candidate != null)
        {
            Candidate.GroupDefenceDamageDealt += counterDamage;
            Candidate.GroupDefenceEvents++;
        }
    }

    // Handles receive counter damage
    private void ReceiveCounterDamage(MarineCreatureAgent defender, float damage)
    {
        if (damage <= 0f || CurrentHealth <= 0f)
        {
            return;
        }

        CurrentHealth -= damage;
        CurrentEnergy = Mathf.Max(0f, CurrentEnergy - damage * 0.35f);
        recentDamageHealLockTimer = Mathf.Max(recentDamageHealLockTimer, HealDelayAfterDamage);
        mobbingPressureTimer = Mathf.Max(mobbingPressureTimer, GroupCounterMobbingPressureTime);
        if (defender != null)
        {
            mobbingPressureDirection = (transform.position - defender.transform.position).normalized;
            RememberDangerArea(defender.transform.position);
        }

        if (CurrentHealth <= 0f)
        {
            Die(false);
        }
    }

    // Handles receive bite
    public bool ReceiveBite(MarineCreatureAgent attacker, float incomingDamage, out float energyGainedByAttacker)
    {
        energyGainedByAttacker = 0f;
        if (CurrentHealth <= 0f)
        {
            return true;
        }

        float defence = EffectiveStats != null ? EffectiveStats.Defence : Candidate.Genome.Armour;
        float reduction = Mathf.Clamp01(defence * ArmourDamageReductionPerPoint);
        float damage = Mathf.Max(0.25f, incomingDamage * (1f - reduction));
        CurrentHealth -= damage;
        woundedScentTimer = Mathf.Max(woundedScentTimer, WoundedScentDuration);
        recentDamageHealLockTimer = Mathf.Max(recentDamageHealLockTimer, HealDelayAfterDamage);
        survivalRecentDamageTimer = Mathf.Max(survivalRecentDamageTimer, SurvivalRecentDamagePanicTime);
        if (attacker != null)
        {
            survivalThreatTarget = attacker;
            Vector3 away = transform.position - attacker.transform.position;
            if (away.sqrMagnitude > 0.001f)
            {
                survivalEscapeDirection = away.normalized;
            }
        }
        if (currentBrainMode == FishAutonomousBehaviourMode.Sleeping)
        {
            SetBrainMode(FishAutonomousBehaviourMode.Fleeing);
        }
        CurrentEnergy = Mathf.Max(0f, CurrentEnergy - damage * 0.2f);
        if (attacker != null)
        {
            RememberDangerArea(attacker.transform.position);
            rememberedPredatorSignature = BuildPredatorSignature(attacker);
            rememberedPredatorTypeTimer = Mathf.Max(rememberedPredatorTypeTimer, PredatorTypeAvoidanceTime);
            TryGroupDefensiveCounterAttack(attacker, damage);
        }

        if (CurrentHealth <= 0f)
        {
            float size = EffectiveStats != null ? EffectiveStats.BodySize : Candidate.Genome.BodySize;
            float gainMultiplier = EvolutionEcosystemManager.Instance != null ? EvolutionEcosystemManager.Instance.BiteEnergyGainMultiplier : 0.32f;
            energyGainedByAttacker = Mathf.Max(5f, size * 22f) * gainMultiplier;
            Die(false);
            return true;
        }

        return false;
    }

    // Learns safer home areas and remembers dangerous places
    private void UpdateHabitatMemory()
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return;
        }

        if (!hasHomeArea)
        {
            hasHomeArea = true;
            homeArea = transform.position;
            homeConfidence = 0.45f;
        }

        bool threatened = lastThreatCount > 0 || GetHealthRatio() < 0.35f;
        bool fed = GetEffectiveEnergyRatio() > 0.55f || GetStomachFullness01() > 0.25f;

        if (threatened)
        {
            RememberDangerArea(transform.position);
            homeConfidence = Mathf.Max(0f, homeConfidence - UnsafeHomeDecayRate * Time.fixedDeltaTime);

            if (Vector3.Distance(transform.position, homeArea) < HomeAttractionDistance * 1.25f)
            {
                Vector3 away = transform.position - rememberedDangerArea;
                away.y = 0f;
                if (away.sqrMagnitude <= 0.001f)
                {
                    away = GetFallbackSideDirection();
                }
                homeArea += away.normalized * SafeHomeLearningRate * 5f;
            }
            return;
        }

        if (fed && !IsHungryEnoughToSearch())
        {
            float learning = SafeHomeLearningRate * Mathf.Lerp(0.45f, 1.35f, Candidate.Genome.FoodMemoryStrength);
            homeArea = Vector3.Lerp(homeArea, transform.position, learning * Time.fixedDeltaTime);
            homeConfidence = Mathf.Clamp01(homeConfidence + learning * Time.fixedDeltaTime);
        }

        UpdateCandidateSpawnMemory();
    }

    // Updates candidate spawn memory using the current sim state
    private void UpdateCandidateSpawnMemory()
    {
        if (Candidate == null || Candidate.Genome == null || !hasHomeArea)
        {
            return;
        }

        float safeScore = Mathf.Clamp01(homeConfidence * 0.55f + GetEffectiveEnergyRatio() * 0.20f + GetStomachFullness01() * 0.15f + (lastThreatCount == 0 ? 0.10f : 0f));
        if (!Candidate.HasPreferredSpawnArea || safeScore >= Candidate.PreferredSpawnConfidence)
        {
            Candidate.HasPreferredSpawnArea = true;
            Candidate.PreferredSpawnArea = homeArea;
            Candidate.PreferredSpawnNiche = EvolutionNicheUtility.BuildCoreNicheKey(Candidate);
            Candidate.PreferredSpawnConfidence = Mathf.Lerp(Candidate.PreferredSpawnConfidence, safeScore, 0.12f);
        }
    }

    // Handles remember danger area
    private void RememberDangerArea(Vector3 dangerPosition)
    {
        rememberedDangerArea = dangerPosition;
        dangerMemoryTimer = DangerMemoryDuration * Mathf.Lerp(0.5f, 1.4f, Candidate != null && Candidate.Genome != null ? Candidate.Genome.FoodMemoryStrength : 0.5f);
    }


    // Checks if it should seek mate right now
    private bool ShouldSeekMate()
    {
        if (Candidate == null || Candidate.Genome == null || EffectiveStats == null)
        {
            return false;
        }

        if (!IsMatureForMating() || !HasMatingEnergy())
        {
            return false;
        }

        if (HasImmediateSurvivalThreat() || currentEscapeTimer > 0f || GetHealthRatio() <= InjuredSurvivalHealthRatio)
        {
            return false;
        }

        float stomachReady = GetStomachFullness01();
        float energyReady = GetEffectiveEnergyRatio();

        if (IsHungryEnoughToSearch() && energyReady < MateSeekingEnergyRatio + 0.08f && stomachReady < MateSeekingStomachRatio + 0.06f)
        {
            return false;
        }

        // Either stored energy or a partly full stomach is enough. This is important
        // because digestion speed is an evolved trait, so slow digesters should still mate
        // while carrying food reserves.
        if (energyReady < MateSeekingEnergyRatio && stomachReady < MateSeekingStomachRatio)
        {
            return false;
        }

        return true;
    }

    // Updates mate target using the current sim state
    private void UpdateMateTarget()
    {
        if (!ShouldSeekMate() || EvolutionEcosystemManager.Instance == null)
        {
            currentMateTarget = null;
            mateTargetTimer = 0f;
            return;
        }

        if (currentMateTarget != null && currentMateTarget.Candidate != null && currentMateTarget.Candidate.Genome != null)
        {
            bool stillValid = currentMateTarget.IsMatureForMating()
                && currentMateTarget.HasMatingEnergy()
                && Candidate.Genome.GetMorphSimilarity(currentMateTarget.Candidate.Genome) >= RequiredMateMorphSimilarity
                && Vector3.Distance(transform.position, currentMateTarget.transform.position) <= MateSearchRadius * 1.25f
                && ((Candidate.Genome.SexGene >= 0.5f) != (currentMateTarget.Candidate.Genome.SexGene >= 0.5f));

            if (stillValid && mateTargetTimer > 0f)
            {
                return;
            }
        }

        currentMateTarget = EvolutionEcosystemManager.Instance.GetBestMateFor(this, MateSearchRadius, RequiredMateMorphSimilarity);
        mateTargetTimer = Mathf.Max(0.25f, MateTargetRefreshTime);
    }

    // Gets the mate seeking pull used by the sim
    private Vector3 GetMateSeekingPull(float hungerPressure)
    {
        if (!ShouldSeekMate())
        {
            return Vector3.zero;
        }

        if (currentMateTarget == null)
        {
            UpdateMateTarget();
        }

        if (currentMateTarget == null)
        {
            return Vector3.zero;
        }

        Vector3 toMate = currentMateTarget.transform.position - transform.position;
        if (toMate.sqrMagnitude <= 0.001f)
        {
            return Vector3.zero;
        }

        float distance = toMate.magnitude;
        float closeSlow = Mathf.Clamp01(distance / Mathf.Max(0.1f, MatePairDistance * 1.8f));
        float drive = Mathf.Lerp(0.35f, 1f, closeSlow);
        drive *= Mathf.Lerp(0.6f, 1.35f, Candidate.Genome.MateDrive);
        drive *= Mathf.Lerp(1f, 0.25f, Mathf.Clamp01(hungerPressure));

        return toMate.normalized * MateSeekingWeight * drive;
    }

    // Gets the cached terrain avoidance pull used by the sim
    private Vector3 GetCachedTerrainAvoidancePull()
    {
        bool urgentTerrainCheck = currentBrainMode == FishAutonomousBehaviourMode.Fleeing
            || survivalEmergencyTimer > 0f
            || currentEscapeTimer > 0f
            || currentBrainMode == FishAutonomousBehaviourMode.Recovering;

        if (urgentTerrainCheck)
        {
            terrainScanTimer = Mathf.Max(0.03f, TerrainScanInterval * 0.22f);
            cachedTerrainPull = GetTerrainAvoidancePull();
            return cachedTerrainPull;
        }

        if (terrainScanTimer <= 0f)
        {
            terrainScanTimer = Mathf.Max(0.02f, TerrainScanInterval) * Random.Range(0.85f, 1.15f);
            cachedTerrainPull = GetTerrainAvoidancePull();
        }

        return cachedTerrainPull;
    }

    // Gets the terrain avoidance pull used by the sim
    private Vector3 GetTerrainAvoidancePull()
    {
        lastTerrainAvoidance = Vector3.zero;

        if (!UseTerrainAvoidance || EvolutionEcosystemManager.Instance == null)
        {
            return Vector3.zero;
        }

        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        Vector3 position = rb != null ? rb.position : transform.position;
        Vector3 pull = Vector3.zero;

        if (manager.TryGetTerrainHeight(position, out float terrainY))
        {
            float clearance = position.y - terrainY;
            float wantedClearance = Mathf.Max(0.1f, TerrainFloorClearance + GetPersonalRadius() * 0.45f);
            if (clearance < wantedClearance)
            {
                float t = 1f - Mathf.Clamp01(clearance / wantedClearance);
                pull += Vector3.up * TerrainFloorLiftWeight * t;
            }
        }

        Vector3 baseForward = wantedDirection.sqrMagnitude > 0.01f ? wantedDirection.normalized : transform.forward;
        Vector3 origin = position + Vector3.up * TerrainProbeForwardHeight;
        float radius = Mathf.Max(0.15f, GetPersonalRadius() * 0.45f);
        LayerMask mask = manager.TerrainRaycastMask;
        bool urgentTerrainCheck = currentBrainMode == FishAutonomousBehaviourMode.Fleeing
            || survivalEmergencyTimer > 0f
            || currentEscapeTimer > 0f
            || currentBrainMode == FishAutonomousBehaviourMode.Recovering;
        float lookAhead = urgentTerrainCheck ? Mathf.Max(MinimumFleeTerrainLookAhead, TerrainLookAhead * Mathf.Max(1f, FleeTerrainLookAheadMultiplier)) : TerrainLookAhead;
        float sideLookAhead = urgentTerrainCheck ? Mathf.Max(MinimumFleeTerrainLookAhead * 0.75f, TerrainSideLookAhead * Mathf.Max(1f, FleeTerrainLookAheadMultiplier)) : TerrainSideLookAhead;

        AddTerrainRayAvoidance(origin, baseForward, lookAhead, radius, mask, ref pull);

        Vector3 flatForward = new Vector3(baseForward.x, 0f, baseForward.z);
        if (flatForward.sqrMagnitude <= 0.001f)
        {
            flatForward = new Vector3(transform.forward.x, 0f, transform.forward.z);
        }
        if (flatForward.sqrMagnitude > 0.001f)
        {
            flatForward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, flatForward).normalized;
            AddTerrainRayAvoidance(origin, (flatForward + right * 0.55f).normalized, sideLookAhead, radius, mask, ref pull);
            AddTerrainRayAvoidance(origin, (flatForward - right * 0.55f).normalized, sideLookAhead, radius, mask, ref pull);
        }

        if (pull.sqrMagnitude > 0.001f)
        {
            pull = pull.normalized * Mathf.Min(TerrainAvoidanceWeight, pull.magnitude);
        }

        lastTerrainAvoidance = pull;
        return pull;
    }

    // Handles add terrain ray avoidance
    private void AddTerrainRayAvoidance(Vector3 origin, Vector3 direction, float distance, float radius, LayerMask mask, ref Vector3 pull)
    {
        if (direction.sqrMagnitude <= 0.001f || distance <= 0f)
        {
            return;
        }

        if (Physics.SphereCast(origin, radius, direction.normalized, out RaycastHit hit, distance, mask, QueryTriggerInteraction.Ignore))
        {
            float closeness = 1f - Mathf.Clamp01(hit.distance / Mathf.Max(0.01f, distance));
            Vector3 normal = hit.normal;
            Vector3 slide = Vector3.ProjectOnPlane(direction, normal);
            if (slide.sqrMagnitude <= 0.001f)
            {
                slide = GetFallbackSideDirection();
            }

            if (normal.y < 0.25f)
            {
                slide.y = 0f;
                if (slide.sqrMagnitude <= 0.001f)
                {
                    slide = GetFallbackSideDirection();
                }
            }

            Vector3 away = (normal * TerrainAvoidanceWeight + slide.normalized * TerrainWallSlideWeight) * closeness;
            if (normal.y < 0.25f)
            {
                away.y = 0f;
            }

            pull += away;
        }
    }

    // Checks mating needs and creates eggs when a safe mate is reached
    private void TryMateAndLayEggs()
    {
        if (eggLayTimer > 0f || EvolutionEcosystemManager.Instance == null || Candidate == null || Candidate.Genome == null)
        {
            return;
        }

        if (!IsMatureForMating() || !HasMatingEnergy())
        {
            return;
        }

        bool isFemale = Candidate.Genome.SexGene >= 0.5f;
        if (!isFemale)
        {
            eggLayTimer = Mathf.Max(2f, EggLayCooldown * 0.25f);
            return;
        }

        MarineCreatureAgent mate = currentMateTarget;
        if (mate == null || !mate.HasMatingEnergy())
        {
            mate = EvolutionEcosystemManager.Instance.GetBestMateFor(this, MateSearchRadius, RequiredMateMorphSimilarity);
        }

        if (mate == null || !mate.HasMatingEnergy())
        {
            eggLayTimer = Mathf.Max(3f, EggLayCooldown * 0.18f);
            return;
        }

        float mateDistance = Vector3.Distance(transform.position, mate.transform.position);
        if (mateDistance > MatePairDistance * 1.35f)
        {
            eggLayTimer = Mathf.Min(eggLayTimer, 1.0f);
            return;
        }

        if (EnableAdvancedFishBehaviours && CourtshipDuration > 0.05f)
        {
            if (courtshipTimer <= 0f && currentBrainMode != FishAutonomousBehaviourMode.Courtship)
            {
                courtshipTimer = CourtshipDuration;
                SetBrainMode(FishAutonomousBehaviourMode.Courtship);
                eggLayTimer = 0.35f;
                return;
            }

            if (courtshipTimer > 0.05f)
            {
                eggLayTimer = 0.25f;
                return;
            }
        }

        float body = EffectiveStats != null ? EffectiveStats.BodySize : Candidate.Genome.BodySize;
        float energyRatio = GetEffectiveEnergyRatio();
        int eggs = Mathf.RoundToInt(Mathf.Lerp(MinimumEggsPerClutch, MaximumEggsPerClutch, Mathf.Clamp01((body - 0.4f) / 2.4f)) * Mathf.Lerp(0.65f, 1.25f, energyRatio));
        eggs = Mathf.Clamp(eggs, Mathf.Max(1, MinimumEggsPerClutch), Mathf.Max(MinimumEggsPerClutch, MaximumEggsPerClutch));

        List<EvolutionCandidate> children = new List<EvolutionCandidate>();
        float mutationMultiplier = EvolutionEcosystemManager.Instance.GetEnvironmentMutationMultiplierAt(transform.position);
        mutationMultiplier *= Mathf.Lerp(0.95f, 1.2f, EvolutionEcosystemManager.Instance.GetCurrentStressAt(transform.position));

        for (int i = 0; i < eggs; i++)
        {
            EvolutionGenome childGenome = EvolutionBreedingUtility.CreateChildGenome(Candidate.Genome, mate.Candidate.Genome, mutationMultiplier);
            EvolutionCandidate child = new EvolutionCandidate(childGenome);
            child.ParentId = Candidate.Id;
            children.Add(child);
        }

        Vector3 eggPosition = EvolutionEcosystemManager.Instance.FindSafeEggPositionNear(transform.position, Candidate.Genome, NestSearchRadius, NestSearchSamples);
        float eggHealth = Mathf.Max(8f, EggHealthPerBodySize * body) * Mathf.Lerp(0.75f, 1.35f, Candidate.Genome.EggProtection);
        float eggMass = eggs * EggMassPerEgg * Mathf.Lerp(0.75f, 1.35f, body);
        guardedEggCluster = EvolutionEcosystemManager.Instance.SpawnEggCluster(this, mate, eggPosition, children, EggHatchTime, eggHealth, eggMass);
        currentEggThreat = null;

        CurrentEnergy = Mathf.Max(0f, CurrentEnergy - EggLayEnergyCost * Mathf.Lerp(0.75f, 1.45f, eggs / Mathf.Max(1f, (float)MaximumEggsPerClutch)));
        mate.CurrentEnergy = Mathf.Max(0f, mate.CurrentEnergy - EggLayEnergyCost * 0.35f);
        eggLayTimer = EggLayCooldown * Mathf.Lerp(0.75f, 1.35f, 1f - Candidate.Genome.NestingDrive);
        reproductionTimer = ReproductionCooldown;
        Candidate.ReproductionCount += eggs;
        Candidate.EggsLaid += eggs;
    }

    // Checks if it is mature for mating
    public bool IsMatureForMating()
    {
        return !IsJuvenile && AgeSeconds >= MaturityAgeSeconds;
    }

    // Checks if it has mating energy available
    public bool HasMatingEnergy()
    {
        if (Candidate == null || Candidate.Genome == null || EffectiveStats == null)
        {
            return false;
        }

        float requiredEnergy = MateEnergyRatioRequired;
        float requiredHealthRatio = 0.42f;
        if (IsEcologicalPredatorRole())
        {
            requiredEnergy = Mathf.Max(requiredEnergy, 0.68f);
            requiredHealthRatio = 0.68f;
        }

        return GetEffectiveEnergyRatio() >= requiredEnergy && CurrentHealth >= GetMaxHealth() * requiredHealthRatio;
    }

    // Sets the juvenile on hatch value used later
    public void SetJuvenileOnHatch()
    {
        IsJuvenile = true;
        AgeSeconds = 0f;
        ApplyJuvenileScale();
    }

    // Updates juvenile growth using the current sim state
    private void UpdateJuvenileGrowth()
    {
        if (!IsJuvenile)
        {
            return;
        }

        ApplyJuvenileScale();
        if (AgeSeconds >= JuvenileGrowTime)
        {
            IsJuvenile = false;
            transform.localScale = Vector3.one;
        }
    }

    // Applies juvenile scale to the current object
    private void ApplyJuvenileScale()
    {
        float t = Mathf.Clamp01(AgeSeconds / Mathf.Max(1f, JuvenileGrowTime));
        transform.localScale = Vector3.one * Mathf.Lerp(JuvenileStartScale, 1f, t);
    }

    // Handles add meat to stomach from egg
    public void AddMeatToStomachFromEgg(float mass)
    {
        if (mass <= 0f)
        {
            return;
        }

        AddToStomach(0f, mass, 0f);
        if (Candidate != null)
        {
            Candidate.MeatEnergyConsumed += mass;
            Candidate.FoodMassConsumed += mass;
        }
    }

    // Updates survival, fitness and behaviour counters for end-of-generation selection
    private void UpdateMetrics()
    {
        if (Candidate == null)
        {
            return;
        }

        Candidate.SurvivalTime = aliveTimer;
        Candidate.FinalEnergy = CurrentEnergy;
        Candidate.FinalHealth = CurrentHealth;
        Candidate.FinalStomachFullness = GetStomachFullness01();
        float currentSpeed = currentVelocity.magnitude;
        Candidate.DistanceTravelled += currentSpeed * Time.fixedDeltaTime;
        Candidate.AverageSpeedUsed = Mathf.Lerp(Candidate.AverageSpeedUsed, currentSpeed, 0.02f);

        AddBrainModeMetric(Time.fixedDeltaTime);
        AddBrainOutputEvidenceMetric();
        if (survivalEmergencyTimer > 0f)
        {
            Candidate.SurvivalEmergencyTime += Time.fixedDeltaTime;
        }

        if (Candidate.Genome != null)
        {
            Candidate.Genome.DecayUnusedBehaviourTraits(BehaviourDecayRate * Time.fixedDeltaTime);
        }
    }

    // Handles add brain output evidence metric
    private void AddBrainOutputEvidenceMetric()
    {
        if (Candidate == null)
        {
            return;
        }

        int oldCount = Mathf.Max(0, Candidate.BrainOutputSamples);
        int newCount = oldCount + 1;
        Candidate.AverageBrainFoodBias = RunningAverage(Candidate.AverageBrainFoodBias, brainOutputFoodBias, oldCount, newCount);
        Candidate.AverageBrainHuntBias = RunningAverage(Candidate.AverageBrainHuntBias, brainOutputHuntBias, oldCount, newCount);
        Candidate.AverageBrainFleeBias = RunningAverage(Candidate.AverageBrainFleeBias, brainOutputFleeBias, oldCount, newCount);
        Candidate.AverageBrainMateSocialBias = RunningAverage(Candidate.AverageBrainMateSocialBias, brainOutputMateSocialBias, oldCount, newCount);
        Candidate.AverageBrainExploreHomeBias = RunningAverage(Candidate.AverageBrainExploreHomeBias, brainOutputExploreHomeBias, oldCount, newCount);
        Candidate.AverageBrainRestBias = RunningAverage(Candidate.AverageBrainRestBias, brainOutputRestBias, oldCount, newCount);
        Candidate.AverageBrainSprintBias = RunningAverage(Candidate.AverageBrainSprintBias, brainOutputSprintBias, oldCount, newCount);
        Candidate.BrainOutputSamples = newCount;
    }

    // Handles running average
    private float RunningAverage(float currentAverage, float newValue, int oldCount, int newCount)
    {
        if (newCount <= 1)
        {
            return newValue;
        }

        return (currentAverage * oldCount + newValue) / newCount;
    }

    // Handles add brain mode metric
    private void AddBrainModeMetric(float deltaTime)
    {
        if (Candidate == null)
        {
            return;
        }

        switch (currentBrainMode)
        {
            case FishAutonomousBehaviourMode.Resting: Candidate.RestingTime += deltaTime; break;
            case FishAutonomousBehaviourMode.Sleeping: Candidate.RestingTime += deltaTime; break;
            case FishAutonomousBehaviourMode.Exploring: Candidate.ExploringTime += deltaTime; break;
            case FishAutonomousBehaviourMode.Schooling: Candidate.SchoolingTime += deltaTime; break;
            case FishAutonomousBehaviourMode.FollowingLeader: Candidate.SchoolingTime += deltaTime; break;
            case FishAutonomousBehaviourMode.Foraging: Candidate.ForagingTime += deltaTime; break;
            case FishAutonomousBehaviourMode.Feeding: Candidate.FeedingTime += deltaTime; break;
            case FishAutonomousBehaviourMode.SeekingMate: Candidate.MateSeekingTime += deltaTime; break;
            case FishAutonomousBehaviourMode.Courtship: Candidate.MateSeekingTime += deltaTime; break;
            case FishAutonomousBehaviourMode.Nesting: Candidate.MateSeekingTime += deltaTime; break;
            case FishAutonomousBehaviourMode.GuardingEggs: Candidate.MateSeekingTime += deltaTime; break;
            case FishAutonomousBehaviourMode.Hunting: Candidate.HuntingTime += deltaTime; break;
            case FishAutonomousBehaviourMode.Ambushing: Candidate.HuntingTime += deltaTime; break;
            case FishAutonomousBehaviourMode.MobbingPredator: Candidate.FleeingTime += deltaTime; break;
            case FishAutonomousBehaviourMode.Fleeing: Candidate.FleeingTime += deltaTime; break;
            case FishAutonomousBehaviourMode.Recovering: Candidate.RecoveryTime += deltaTime; break;
        }
    }

    // Updates debug movement state using the current sim state
    private void UpdateDebugMovementState(float hungerPressure, Vector3 targetPull, Vector3 dangerPull, Vector3 schoolPull, Vector3 boundaryPull, Vector3 emergencyPull)
    {
        if (stuckEscapeTimer > 0f || emergencyPull.sqrMagnitude > 0.01f)
        {
            debugMoveState = "Recovering";
        }
        else
        {
            debugMoveState = currentBrainMode.ToString();
        }

        if (Mathf.Abs(wantedDirection.y) > 0.35f)
        {
            if (targetPull.sqrMagnitude > 0.05f && Mathf.Abs(targetPull.y) > 0.15f) debugVerticalReason = "Target depth";
            else if (dangerPull.sqrMagnitude > 0.05f && Mathf.Abs(dangerPull.y) > 0.15f) debugVerticalReason = "Threat depth";
            else if (boundaryPull.sqrMagnitude > 0.05f && Mathf.Abs(boundaryPull.y) > 0.15f) debugVerticalReason = "Bounds";
            else if (lastTerrainAvoidance.sqrMagnitude > 0.05f && Mathf.Abs(lastTerrainAvoidance.y) > 0.15f) debugVerticalReason = "Terrain";
            else debugVerticalReason = "Preferred depth";
        }
        else
        {
            debugVerticalReason = "Cruise";
        }
    }

    // Draws optional debug rays for movement and target checks
    private void DrawRuntimeDebugRays()
    {
        EcosystemDebugSettings settings = EcosystemDebugSettings.Instance;
        bool drawRays = LocalDebugRays || (settings != null && settings.DrawCreatureMovementRays);
        if (!drawRays || rb == null)
        {
            return;
        }

        float duration = settings != null ? settings.FoodRayDuration : 0f;
        float wantedLength = settings != null ? settings.WantedDirectionRayLength : 4f;
        float velocityScale = settings != null ? settings.VelocityRayScale : 0.4f;
        Vector3 mouth = GetMouthWorldPosition();

        if (settings == null || settings.DrawWantedDirectionRays) Debug.DrawRay(transform.position, wantedDirection * wantedLength, Color.blue, duration);
        if (settings == null || settings.DrawVelocityRays) Debug.DrawRay(transform.position, currentVelocity * velocityScale, Color.white, duration);
        if (nearestFood != null && (settings == null || settings.DrawFoodTargetRays)) Debug.DrawLine(mouth, nearestFood.transform.position, Color.green, duration);
        if (nearestCarrion != null && (settings == null || settings.DrawCarrionTargetRays)) Debug.DrawLine(mouth, nearestCarrion.transform.position, new Color(0.55f, 0.3f, 0.1f), duration);
        if (nearestPrey != null && (settings == null || settings.DrawPreyTargetRays)) Debug.DrawLine(mouth, nearestPrey.GetBiteTargetPosition(), Color.red, duration);
        if (nearestCreature != null && (settings == null || settings.DrawSocialTargetRays)) Debug.DrawLine(transform.position, nearestCreature.transform.position, IsActualThreat(nearestCreature) ? Color.red : new Color(0.4f, 0.7f, 1f), duration);
        if (settings == null || settings.DrawBoidRays)
        {
            if (lastSchoolCohesion.sqrMagnitude > 0.001f) Debug.DrawRay(transform.position, lastSchoolCohesion, new Color(0.1f, 0.9f, 1f), duration);
            if (lastSchoolAlignment.sqrMagnitude > 0.001f) Debug.DrawRay(transform.position, lastSchoolAlignment, new Color(0.55f, 0.75f, 1f), duration);
            if (lastSchoolSeparation.sqrMagnitude > 0.001f) Debug.DrawRay(transform.position, lastSchoolSeparation, Color.cyan, duration);
            if (lastThreatAvoidance.sqrMagnitude > 0.001f) Debug.DrawRay(transform.position, lastThreatAvoidance, new Color(1f, 0.35f, 0.1f), duration);
            if (lastEmergencyUnstick.sqrMagnitude > 0.001f) Debug.DrawRay(transform.position, lastEmergencyUnstick, Color.magenta, duration);
        }
        if ((settings == null || settings.DrawBoundaryPush) && lastBoundaryPush.sqrMagnitude > 0.001f) Debug.DrawRay(transform.position, lastBoundaryPush, Color.yellow, duration);
        if (lastTerrainAvoidance.sqrMagnitude > 0.001f) Debug.DrawRay(transform.position, lastTerrainAvoidance, new Color(1f, 0.45f, 0.05f), duration);
        if (currentMateTarget != null && ShouldSeekMate()) Debug.DrawLine(transform.position, currentMateTarget.transform.position, new Color(1f, 0.3f, 1f), duration);
    }

    // Builds a short debug string for the current state
    public string GetDebugSummary()
    {
        if (Candidate == null || Candidate.Genome == null || EffectiveStats == null)
        {
            return "Uninitialised creature";
        }

        return DebugName +
               " | Energy " + CurrentEnergy.ToString("F0") + "/" + EffectiveStats.EnergyCapacity.ToString("F0") +
               " | Health " + CurrentHealth.ToString("F0") + "/" + GetMaxHealth().ToString("F0") +
               " | Age " + AgeSeconds.ToString("F0") + "s" +
               " | Sex " + (Candidate.Genome.SexGene >= 0.5f ? "F" : "M") +
               " | Stage " + (IsJuvenile ? "Juvenile" : IsMatureForMating() ? "Adult" : "Young") +
               " | Stomach " + GetStomachFullness01().ToString("P0") +
               " | Speed " + EffectiveStats.Speed.ToString("F1") +
               " | Vision " + EffectiveStats.VisionRange.ToString("F1") +
               " | Mouth " + GetScaledMouthRadius().ToString("F2") +
               " | Bite " + GetBiteDamage().ToString("F1") +
               " | Def " + EffectiveStats.Defence.ToString("F1") +
               " | Danger " + EffectiveStats.DangerFactor.ToString("F1") +
               " | State " + debugMoveState +
               " | Leader " + (currentHungryLeader != null ? currentHungryLeader.DebugName : "None") +
               " | Memory " + (hasFoodMemory ? (rememberedFoodWasBad ? "Bad" : "Food") : "None") +
               " | School F/D/T " + lastFriendlyCount + "/" + lastDifferentCount + "/" + lastThreatCount +
               " | Crowd " + lastDenseCrowdCount + "/Food" + lastFeedingCrowdCount +
               " | Vertical " + debugVerticalReason +
               " | " + EffectiveStats.MorphSummary +
               " | Diet P/M/C " + Candidate.Genome.PlantDiet.ToString("F2") + "/" + Candidate.Genome.MeatDiet.ToString("F2") + "/" + Candidate.Genome.CarrionDiet.ToString("F2");
    }

    // Records final metrics, spawns carrion when needed and removes the fish
    public void Die(bool causedByExtinctionEvent)
    {
        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.SpawnCarrionFromDeath(this, causedByExtinctionEvent);
            EvolutionEcosystemManager.Instance.UnregisterCreature(this);
        }

        Destroy(gameObject);
    }

    // Gets the habitat debug summary used by the sim
    public string GetHabitatDebugSummary()
    {
        if (!hasHomeArea)
        {
            return "No home area";
        }

        float homeDistance = Vector3.Distance(transform.position, homeArea);
        string danger = dangerMemoryTimer > 0f ? " danger memory" : " safe";
        return "Home " + homeDistance.ToString("F1") + "m | Confidence " + homeConfidence.ToString("F2") + danger;
    }

    // Gets the debug move state used by the sim
    public string GetDebugMoveState()
    {
        return debugMoveState;
    }

    // Gets the brain debug summary used by the sim
    public string GetBrainDebugSummary()
    {
        string complexity = Candidate != null && Candidate.Genome != null && Candidate.Genome.Brain != null
            ? " | NN " + Candidate.Genome.Brain.HiddenCount + "h/" + Candidate.Genome.Brain.GetConnectionCount() + "c"
            : "";

        string extra = claimedFreshKillCarrion != null ? " | claimed kill" : (currentEscapeTimer > 0f ? " | escaping current" : "");
        return currentBrainMode + " | " + brainReason + extra + " | F/M/S/E/H/T "
            + brainFoodDesire.ToString("F2") + "/"
            + brainMateDesire.ToString("F2") + "/"
            + brainSchoolDesire.ToString("F2") + "/"
            + brainExploreDesire.ToString("F2") + "/"
            + brainHomeDesire.ToString("F2") + "/"
            + brainFleeDesire.ToString("F2")
            + " | NN bias F/H/Fl/R/S " + brainOutputFoodBias.ToString("F1") + "/" + brainOutputHuntBias.ToString("F1") + "/" + brainOutputFleeBias.ToString("F1") + "/" + brainOutputRestBias.ToString("F1") + "/" + brainOutputSprintBias.ToString("F1")
            + complexity;
    }

    // Gets the brain mode used by the sim
    public FishAutonomousBehaviourMode GetBrainMode()
    {
        return currentBrainMode;
    }

    // Gets the debug vertical reason used by the sim
    public string GetDebugVerticalReason()
    {
        return debugVerticalReason;
    }

    // Gets the friendly schoolmate count used by the sim
    public int GetFriendlySchoolmateCount()
    {
        return lastFriendlyCount;
    }

    // Gets the threat count used by the sim
    public int GetThreatCount()
    {
        return lastThreatCount;
    }

    // Gets the preferred depth01 used by the sim
    public float GetPreferredDepth01()
    {
        return Candidate != null && Candidate.Genome != null ? Candidate.Genome.PreferredDepth01 : 0.5f;
    }

    // Draws scene gizmos so setup can be checked visually
    private void OnDrawGizmos()
    {
        DrawGizmosInternal(false);
    }

    // Draws selected-only gizmos so setup can be checked without clutter
    private void OnDrawGizmosSelected()
    {
        DrawGizmosInternal(true);
    }

    // Draws gizmos internal for debugging
    private void DrawGizmosInternal(bool selectedStyle)
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return;
        }

        EcosystemDebugSettings settings = EcosystemDebugSettings.Instance;

        // Selected fish used to draw the full vision sphere by default. That made the Scene view look like
        // the root collider was enormous, even though it was only a debug gizmo. Vision is now only drawn
        // when the global debug setting asks for it.
        bool drawVision = settings != null && settings.DrawVisionRange;
        bool drawMouth = selectedStyle || (settings != null && settings.DrawMouthRange);
        bool drawBite = selectedStyle || (settings != null && settings.DrawBiteRange);

        if (drawVision)
        {
            Gizmos.color = new Color(0f, 0.8f, 1f, selectedStyle ? 0.8f : 0.25f);
            Gizmos.DrawWireSphere(transform.position, EffectiveStats != null ? EffectiveStats.VisionRange : Candidate.Genome.VisionRange);
        }

        if (selectedStyle)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.75f);
            Gizmos.DrawWireSphere(transform.position, GetPersonalRadius());
        }

        if (drawMouth)
        {
            Gizmos.color = new Color(1f, 0.2f, 1f, selectedStyle ? 0.9f : 0.45f);
            Gizmos.DrawWireSphere(GetMouthWorldPosition(), GetScaledMouthRadius());
            Gizmos.DrawLine(transform.position, GetMouthWorldPosition());
        }

        if (drawBite)
        {
            Gizmos.color = new Color(1f, 0f, 0f, selectedStyle ? 0.9f : 0.35f);
            Gizmos.DrawWireSphere(GetMouthWorldPosition(), GetScaledMouthRadius() * 1.05f);
        }
    }

    // Draws the old debug UI when it is enabled
    private void OnGUI()
    {
        EcosystemDebugSettings settings = EcosystemDebugSettings.Instance;
        if (!(LocalDebugLabels || (settings != null && settings.ShowCreatureLabels)))
        {
            return;
        }

        if (Candidate == null || Candidate.Genome == null || Camera.main == null)
        {
            return;
        }

        if (labelFrame != Time.frameCount)
        {
            labelFrame = Time.frameCount;
            labelsDrawnThisFrame = 0;
        }

        int maxLabels = settings != null ? settings.MaxLabelCount : 80;
        if (labelsDrawnThisFrame >= maxLabels)
        {
            return;
        }

        float maxDistance = settings != null ? settings.LabelMaxDistance : 80f;
        float distance = Vector3.Distance(Camera.main.transform.position, transform.position);
        if (distance > maxDistance)
        {
            return;
        }

        float bodySize = EffectiveStats != null ? EffectiveStats.BodySize : Candidate.Genome.BodySize;
        Vector3 worldPoint = transform.position + Vector3.up * bodySize * 1.4f;
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(worldPoint);
        if (screenPoint.z <= 0f)
        {
            return;
        }

        Vector2 offset = settings != null ? settings.LabelOffset : new Vector2(0f, -14f);
        Rect rect = new Rect(screenPoint.x - 125f + offset.x, Screen.height - screenPoint.y + offset.y, 250f, 96f);
        Color oldColour = GUI.color;
        GUI.color = CreatureDebugTypeUtility.GetTypeColour(DebugBehaviourType);

        string label = DebugName + "\nE " + CurrentEnergy.ToString("F0") + " H " + CurrentHealth.ToString("F0") + " S " + GetStomachFullness01().ToString("P0") + " | " + CreatureDebugTypeUtility.GetMorphologyName(Candidate.Genome);
        if (settings != null && settings.ShowDietInLabels)
        {
            label += "\nP" + Candidate.Genome.PlantDiet.ToString("F1") + " M" + Candidate.Genome.MeatDiet.ToString("F1") + " C" + Candidate.Genome.CarrionDiet.ToString("F1");
        }

        if (settings == null || settings.ShowMovementStateInLabels)
        {
            label += "\n" + debugMoveState + " F" + lastFriendlyCount + " T" + lastThreatCount;
        }

        if (settings != null && settings.ShowVerticalReasonInLabels)
        {
            label += " | Y: " + debugVerticalReason;
        }

        GUI.Label(rect, label);
        GUI.color = oldColour;
        labelsDrawnThisFrame++;
    }
}
