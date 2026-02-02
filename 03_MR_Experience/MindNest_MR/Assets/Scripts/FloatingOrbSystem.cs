/*
 * FloatingOrbSystem.cs
 * ====================
 * 
 * Floating Light Orb System for Behavioral Activation Tasks
 * 
 * Features:
 * - 5-8 floating orbs orbiting around tree
 * - Click detection for task interaction
 * - Particle explosion effect on orb click
 * - Task display and completion tracking
 * 
 * Author: MindNest Team
 * Date: 2026-01-28
 */

using UnityEngine;
using System.Collections.Generic;

namespace MindNest.MR
{
    /// <summary>
    /// Data structure for a single floating orb
    /// </summary>
    public class OrbData
    {
        public GameObject orbObject;
        public float orbitRadius;
        public float orbitSpeed;
        public float orbitAngle;
        public float height;
        public float bobSpeed;
        public float bobMagnitude;
        public int taskIndex;
    }
    
    /// <summary>
    /// Manages floating light orbs around the tree
    /// </summary>
    public class FloatingOrbSystem : MonoBehaviour
    {
        [Header("Orb Settings")]
        [Tooltip("Number of orbs to spawn")]
        public int orbCount = 6;
        
        [Tooltip("Orb size (diameter in meters)")]
        public float orbSize = 0.3f;
        
        [Tooltip("Orbit radius range (min, max)")]
        public Vector2 orbitRadiusRange = new Vector2(2f, 4f);
        
        [Tooltip("Orbit speed range (degrees per second)")]
        public Vector2 orbitSpeedRange = new Vector2(10f, 30f);
        
        [Tooltip("Height range from tree base (meters)")]
        public Vector2 heightRange = new Vector2(2f, 5f);
        
        [Tooltip("Vertical bobbing magnitude (meters)")]
        public float bobMagnitude = 0.2f;
        
        [Tooltip("Vertical bobbing speed")]
        public float bobSpeed = 1.5f;
        
        [Tooltip("Orb emissive color")]
        public Color orbColor = new Color(1f, 0.9f, 0.5f, 1f); // Golden glow
        
        [Tooltip("Emission intensity")]
        public float emissionIntensity = 2.0f;
        
        [Header("Task Settings")]
        [Tooltip("Task list (from tree_final.html)")]
        public string[] tasks = new string[]
        {
            "Call a friend you haven't talked to in a while",
            "Go for a 15-minute walk outside",
            "Organize your workspace or a small area at home",
            "Write down three things you're grateful for",
            "Cook a simple meal from scratch",
            "Water your plants or spend time with a pet",
            "Read 10 pages of a book",
            "Do 10 minutes of stretching or light exercise",
            "Listen to your favorite music and dance",
            "Help someone with a small task",
            "Take a photo of something beautiful you see today",
            "Try a new hobby or skill for 20 minutes",
            "Clean one drawer or shelf",
            "Spend 5 minutes meditating or deep breathing",
            "Send a kind message to someone",
            "Draw, paint, or create something",
            "Watch the sunset or sunrise",
            "Declutter your phone (delete unused apps)",
            "Plan tomorrow's schedule",
            "Do something that makes you laugh"
        };
        
        [Tooltip("Nutrients rewarded per completed task")]
        public int nutrientsPerTask = 50;
        
        // ============================================================================
        // Internal State
        // ============================================================================
        
        private List<OrbData> orbs = new List<OrbData>();
        private List<int> availableTasks = new List<int>();
        private Transform treeTransform;
        
        // ============================================================================
        // Unity Lifecycle
        // ============================================================================
        
        void Awake()
        {
            // Initialize available tasks
            for (int i = 0; i < tasks.Length; i++)
            {
                availableTasks.Add(i);
            }
        }
        
        void Start()
        {
            treeTransform = transform;
        }
        
        void Update()
        {
            UpdateOrbPositions();
        }
        
        // ============================================================================
        // Orb Management
        // ============================================================================
        
        public void SpawnOrbs()
        {
            Debug.Log($"✨ Spawning {orbCount} floating orbs");
            
            // Clear existing orbs
            ClearOrbs();
            
            // Spawn new orbs
            for (int i = 0; i < orbCount; i++)
            {
                CreateOrb(i);
            }
        }
        
        private void CreateOrb(int index)
        {
            // Create orb data
            OrbData orbData = new OrbData
            {
                orbitRadius = Random.Range(orbitRadiusRange.x, orbitRadiusRange.y),
                orbitSpeed = Random.Range(orbitSpeedRange.x, orbitSpeedRange.y),
                orbitAngle = (float)index / orbCount * 360f + Random.Range(-30f, 30f),
                height = Random.Range(heightRange.x, heightRange.y),
                bobSpeed = bobSpeed + Random.Range(-0.3f, 0.3f),
                bobMagnitude = bobMagnitude * Random.Range(0.7f, 1.3f),
                taskIndex = GetRandomTaskIndex()
            };
            
            // Create orb GameObject
            GameObject orbObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orbObj.name = $"LightOrb_{index}";
            orbObj.transform.SetParent(treeTransform, false);
            orbObj.transform.localScale = Vector3.one * orbSize;
            
            // Remove collider (we'll add a larger one for easier clicking)
            Destroy(orbObj.GetComponent<Collider>());
            
            // Add larger sphere collider for easier interaction
            SphereCollider collider = orbObj.AddComponent<SphereCollider>();
            collider.radius = 1.5f; // 1.5x larger hit area
            
            // Create emissive material
            MeshRenderer renderer = orbObj.GetComponent<MeshRenderer>();
            Material mat = new Material(Shader.Find("Standard"));
            mat.SetColor("_Color", orbColor);
            mat.SetColor("_EmissionColor", orbColor * emissionIntensity);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            renderer.material = mat;
            
            orbData.orbObject = orbObj;
            orbs.Add(orbData);
            
            Debug.Log($"✨ Created orb {index}: radius={orbData.orbitRadius:F2}m, height={orbData.height:F2}m, task={tasks[orbData.taskIndex]}");
        }
        
        public void ClearOrbs()
        {
            foreach (OrbData orbData in orbs)
            {
                if (orbData.orbObject != null)
                {
                    Destroy(orbData.orbObject);
                }
            }
            orbs.Clear();
        }
        
        private void UpdateOrbPositions()
        {
            float deltaTime = Time.deltaTime;
            
            foreach (OrbData orbData in orbs)
            {
                if (orbData.orbObject == null) continue;
                
                // Update orbit angle
                orbData.orbitAngle += orbData.orbitSpeed * deltaTime;
                if (orbData.orbitAngle >= 360f)
                {
                    orbData.orbitAngle -= 360f;
                }
                
                // Calculate position
                float angleRad = orbData.orbitAngle * Mathf.Deg2Rad;
                float x = Mathf.Cos(angleRad) * orbData.orbitRadius;
                float z = Mathf.Sin(angleRad) * orbData.orbitRadius;
                
                // Add vertical bobbing
                float bob = Mathf.Sin(Time.time * orbData.bobSpeed) * orbData.bobMagnitude;
                float y = orbData.height + bob;
                
                // Set position relative to tree
                orbData.orbObject.transform.localPosition = new Vector3(x, y, z);
            }
        }
        
        // ============================================================================
        // Task Management
        // ============================================================================
        
        private int GetRandomTaskIndex()
        {
            if (availableTasks.Count == 0)
            {
                // Reset available tasks if all have been used
                for (int i = 0; i < tasks.Length; i++)
                {
                    availableTasks.Add(i);
                }
            }
            
            int randomIndex = Random.Range(0, availableTasks.Count);
            int taskIndex = availableTasks[randomIndex];
            availableTasks.RemoveAt(randomIndex);
            
            return taskIndex;
        }
        
        public string GetOrbTask(GameObject orbObj)
        {
            foreach (OrbData orbData in orbs)
            {
                if (orbData.orbObject == orbObj)
                {
                    return tasks[orbData.taskIndex];
                }
            }
            return null;
        }
        
        public void RemoveOrb(GameObject orbObj)
        {
            for (int i = 0; i < orbs.Count; i++)
            {
                if (orbs[i].orbObject == orbObj)
                {
                    // Create explosion effect
                    CreateExplosionEffect(orbObj.transform.position);
                    
                    // Destroy orb
                    Destroy(orbObj);
                    orbs.RemoveAt(i);
                    
                    Debug.Log($"💥 Orb removed: {orbs.Count} remaining");
                    return;
                }
            }
        }
        
        private void CreateExplosionEffect(Vector3 position)
        {
            // Create temporary particle system for explosion
            GameObject explosionObj = new GameObject("OrbExplosion");
            explosionObj.transform.position = position;
            
            ParticleSystem ps = explosionObj.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.startLifetime = 1.0f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startColor = orbColor;
            main.maxParticles = 50;
            main.loop = false;
            main.duration = 0.5f;
            
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 50)
            });
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;
            
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            renderer.material.SetColor("_Color", orbColor);
            renderer.material.SetFloat("_Mode", 3);
            renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            renderer.material.SetInt("_ZWrite", 0);
            renderer.material.EnableKeyword("_ALPHABLEND_ON");
            renderer.material.renderQueue = 3000;
            
            ps.Play();
            
            // Destroy explosion object after particles finish
            Destroy(explosionObj, main.duration + main.startLifetime.constantMax);
        }
        
        public int GetRemainingOrbCount()
        {
            return orbs.Count;
        }
        
        public int GetNutrientsPerTask()
        {
            return nutrientsPerTask;
        }
    }
}


