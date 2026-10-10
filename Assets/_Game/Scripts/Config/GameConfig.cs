using UnityEngine;

namespace BaitSwitch
{
    /// <summary>
    /// Every tunable number in the game (docs/16-balance.md). Never hard-code these elsewhere.
    /// Create one asset: Assets > Create > Bait & Switch > Game Config, keep it in Resources/GameConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "Bait & Switch/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        static GameConfig _i;
        public static GameConfig I => _i != null ? _i : (_i = Resources.Load<GameConfig>("GameConfig"));

        [Header("World")]
        public Vector2 seaSize = new Vector2(40f, 22.5f);
        public float surfaceY = 0f;
        public float seabedY = -20f;
        public float fishBaseSize = 0.9f;
        public float fishHitRadius = 0.45f;

        [Header("Match")]
        public int seats = 6;
        public int startingAnglers = 1;
        public int roundsPerMatch = 3;
        public float introSeconds = 5f;
        public float roundSeconds = 120f;
        public float ledgerSeconds = 10f;
        public float duskStart = 50f;
        public float nightStart = 80f;

        [Header("Fish movement")]
        public float fishAccel = 25f;
        public float fishMaxSpeed = 4f;
        public float waterDrag = 2f;
        public float fishTurnRate = 540f; // deg/s
        public float dashSpeed = 11f;
        public float dashDuration = 0.25f;
        public float dashCooldown = 2f;

        [Header("Ink")]
        public float inkRadius = 2.5f;
        public float inkDuration = 3f;
        public float inkCooldown = 12f;

        [Header("Nibble and growth")]
        public float nibbleRange = 0.7f;
        public float nibbleTime = 0.6f;
        public float growthDay = 1f;
        public float growthDusk = 1.5f;
        public float growthNight = 2f;
        public float sizePerGrowth = 0.15f;
        public float speedPerGrowth = -0.03f;

        [Header("Leviathan")]
        public float leviathanThreshold = 6f;
        public float leviathanScale = 2.5f;
        public float leviathanBiteTime = 2f;
        public float leviathanGrowthAfterDrag = 3f;
        public float leviathanStamina = 300f;
        public float leviathanSingleHookBreak = 1f;

        [Header("Angler and hook")]
        public float dinghySpeed = 3f;
        public float maxLineLength = 20f;
        public float hookSpring = 30f;
        public float hookDrag = 4f;
        public float hookGravity = 6f;
        public float strikeJerk = 1.2f;
        public float strikeJerkTime = 0.15f;
        public float catchRadius = 0.6f;
        public float strikeCooldown = 1f;
        public float missScatterRadius = 3f;
        public float missScatterTime = 2f;
        public float reelUpSpeed = 8f;
        public float rebaitTime = 0.5f;
        public float schoolLandTime = 1f;
        public float snagSpeed = 3f; // spiky hook snags above this hook speed

        [Header("Sonar and marks")]
        public float sonarRadius = 12f;
        public float sonarOutline = 0.5f;
        public float sonarCooldown = 15f;
        public float markDuration = 3f;
        public float markCooldown = 5f;

        [Header("Tug-of-war (per second unless noted)")]
        public float tensionReel = 35f;
        public float tensionFishPull = 45f; // x pull 0..1
        public float tensionEaseOff = -50f;
        public float tensionFishRests = -20f;
        public float tensionDash = 25f; // instant
        public float snapThreshold = 100f;
        public float reelInSpeed = 3.5f; // x (1 - 0.6 * pull)
        public float reelPullDamping = 0.6f;
        public float linePayOut = 1.5f; // x pull
        public float staminaDrain = 25f; // x pull
        public float staminaRegen = 15f;
        public float exhaustedTime = 1.5f;
        public float fatigueStart = 6f;
        public float fatigueRate = 8f; // threshold lost per s
        public float tearLoose = 12f;
        public float landDistance = 0.5f;

        [Header("School")]
        public int schoolCount = 150;
        public float schoolRespawn = 5f;
        public float schoolCruise = 2.5f;
        public float separationRadius = 0.8f;
        public float neighbourRadius = 2.5f;
        public float avoidHookRadius = 1.2f;
        public float avoidHookMinSpeed = 1f;
        public float curiosityPerSecond = 0.02f;
        public float curiosityRange = 5f;
        public Vector2 schoolNibble = new Vector2(0.3f, 0.55f);
        public float schoolEatChance = 0.1f;
        public float scatterTime = 2f;

        [Header("Bots")]
        public Vector2 fishBotGreed = new Vector2(0.2f, 0.6f);
        public Vector2 anglerBotReaction = new Vector2(0.25f, 0.45f);
        public float anglerBotMiss = 0.25f;
        public float anglerBotReelBelow = 65f;
        public float anglerBotReleaseAbove = 80f;

        [Header("Network")]
        public int simHz = 30;
        public int snapshotHz = 15;
        public int inputHz = 30;
        public int tugHz = 10;
        public float interpDelay = 0.1f;

        [Header("Server lifecycle")]
        public float idleStopSeconds = 180f;
        public float noNewMatchAfterMinutes = 52f;

        [Header("Scoring")]
        public int ptsEatBait = 2;
        public int ptsEscape = 3;
        public int ptsSurvive = 5;
        public int ptsLastFish = 5;
        public int ptsBoatDrag = 10;
        public int ptsLandSchool = 1;
        public int ptsLandPlayer = 10;
        public int ptsMarkAssist = 2;
        public int ptsLandLeviathan = 15;
        public int ptsCleanSweep = 5;
        public int ptsAppetite = 5;

        public float GrowthForPhase(Phase p) => p == Phase.Night ? growthNight : p == Phase.Dusk ? growthDusk : growthDay;
        public Phase PhaseAt(float t) => t >= nightStart ? Phase.Night : t >= duskStart ? Phase.Dusk : Phase.Day;
    }

    public enum Phase : byte { Day, Dusk, Night }
    public enum Role : byte { Fish, Angler, Spectator }
}
