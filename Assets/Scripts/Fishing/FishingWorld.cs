using System;
using UnityEngine;

namespace CanaryFishing.Fishing
{
    public sealed class FishingWorldClock : MonoBehaviour
    {
        [SerializeField, Range(0f, 24f)] private float timeOfDay = 8f;
        [SerializeField, Min(0f)] private float gameHoursPerSecond = 0.08f;
        public float TimeOfDay => timeOfDay;
        public bool IsNight => timeOfDay < 6f || timeOfDay >= 20f;
        private void Update() => timeOfDay = (timeOfDay + gameHoursPerSecond * Time.deltaTime) % 24f;
    }

    [Serializable]
    public sealed class FishSpawnRule
    {
        public FishData fish;
        [Range(0f, 1f)] public float weight = 1f;
        public float minDepth;
        public float maxDepth = 20f;
        public bool nightOnly;
    }

    public sealed class DynamicFishSpawner : MonoBehaviour
    {
        [SerializeField] private FishAI fishPrefab;
        [SerializeField] private FishingRodController rod;
        [SerializeField] private Transform lure;
        [SerializeField] private FishingWorldClock clock;
        [SerializeField] private FishSpawnRule[] rules;
        [SerializeField] private float waterSurfaceY;
        [SerializeField, Min(1f)] private float spawnInterval = 8f;
        [SerializeField, Min(1)] private int maxFish = 5;
        [SerializeField] private Vector2 spawnDepth = new Vector2(1f, 8f);
        private float timer;

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f || rules == null || rules.Length == 0 || FindObjectsByType<FishAI>(FindObjectsSortMode.None).Length >= maxFish) return;
            timer = spawnInterval;
            FishSpawnRule selected = ChooseRule();
            if (selected == null || selected.fish == null || fishPrefab == null) return;
            float depth = UnityEngine.Random.Range(spawnDepth.x, spawnDepth.y);
            FishAI fish = Instantiate(fishPrefab, transform.position + new Vector3(UnityEngine.Random.Range(-8f, 8f), -depth, UnityEngine.Random.Range(2f, 12f)), Quaternion.identity);
            Rigidbody body = fish.GetComponent<Rigidbody>();
            if (body == null) body = fish.gameObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            fish.Initialize(selected.fish, rod, lure, body, waterSurfaceY);
        }

        private FishSpawnRule ChooseRule()
        {
            float total = 0f;
            foreach (FishSpawnRule rule in rules) if (rule != null && (!rule.nightOnly || clock == null || clock.IsNight)) total += Mathf.Max(0f, rule.weight);
            float pick = UnityEngine.Random.value * total;
            foreach (FishSpawnRule rule in rules)
            {
                if (rule == null || (rule.nightOnly && clock != null && !clock.IsNight)) continue;
                pick -= Mathf.Max(0f, rule.weight); if (pick <= 0f) return rule;
            }
            return null;
        }
    }
}
