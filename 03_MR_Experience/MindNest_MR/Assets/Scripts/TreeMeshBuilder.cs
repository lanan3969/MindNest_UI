/*
 * TreeMeshBuilder.cs
 * ==================
 * 
 * Procedural 3D Tree Generator
 * 
 * Features:
 * - Procedural trunk generation using cylinder meshes
 * - Dynamic branch system (5-8 main branches)
 * - Particle-based leaves (10,000-20,000 particles)
 * - Season-aware leaf colors
 * - Growth-based scaling
 * 
 * Author: MindNest Team
 * Date: 2026-01-28
 */

using UnityEngine;
using System.Collections.Generic;

namespace MindNest.MR
{
    /// <summary>
    /// Builds a 3D procedural tree with mesh geometry and particle leaves
    /// </summary>
    public class TreeMeshBuilder : MonoBehaviour
    {
        [Header("Growth Configuration")]
        [Tooltip("Current nutrient level")]
        public int currentNutrients = 0;
        
        [Tooltip("Growth milestones")]
        public int[] growthMilestones = { 0, 300, 700, 1200 }; // Sapling, Young, Mature, Ancient
        
        [Header("Trunk Settings")]
        [Tooltip("Base trunk height (meters)")]
        public float baseTrunkHeight = 2.5f;
        
        [Tooltip("Maximum trunk height (meters)")]
        public float maxTrunkHeight = 5.0f;
        
        [Tooltip("Base trunk radius (meters)")]
        public float baseTrunkRadius = 0.15f;
        
        [Tooltip("Maximum trunk radius (meters)")]
        public float maxTrunkRadius = 0.35f;
        
        [Tooltip("Trunk segments (for mesh detail)")]
        public int trunkSegments = 16;
        
        [Tooltip("Trunk color")]
        public Color trunkColor = new Color(0.38f, 0.25f, 0.13f); // Brown bark
        
        [Header("Branch Settings")]
        [Tooltip("Number of main branches per growth stage")]
        public int[] branchesPerStage = { 0, 3, 6, 8, 10 }; // Stages 0-4
        
        [Tooltip("Branch length multiplier")]
        public float branchLengthMultiplier = 0.6f;
        
        [Tooltip("Branch radius multiplier")]
        public float branchRadiusMultiplier = 0.5f;
        
        [Tooltip("Branch segments (for mesh detail)")]
        public int branchSegments = 8;
        
        [Tooltip("Branch angle variation")]
        public float branchAngleVariation = 30f;
        
        [Header("Leaf Particle Settings")]
        [Tooltip("Number of leaf particles")]
        public int leafParticleCount = 15000;
        
        [Tooltip("Leaf particle size range")]
        public Vector2 leafSizeRange = new Vector2(0.08f, 0.15f);
        
        [Tooltip("Leaf emission radius")]
        public float leafEmissionRadius = 2.5f;
        
        [Tooltip("Leaf color (spring/summer)")]
        public Color leafColorSpring = new Color(0.4f, 0.9f, 0.3f, 0.9f); // Bright green
        
        [Tooltip("Leaf color (autumn)")]
        public Color leafColorAutumn = new Color(1f, 0.6f, 0.2f, 0.9f); // Orange
        
        [Tooltip("Leaf color (winter)")]
        public Color leafColorWinter = new Color(0.9f, 0.95f, 1f, 0.7f); // Icy blue-white
        
        // ============================================================================
        // Internal State
        // ============================================================================
        
        private GameObject trunkObj;
        private List<GameObject> branchObjs = new List<GameObject>();
        private GameObject leafParticleObj;
        private ParticleSystem leafParticleSystem;
        
        private int currentGrowthStage = 0;
        private string currentSeason = "spring";
        
        // ============================================================================
        // Unity Lifecycle
        // ============================================================================
        
        void Awake()
        {
            InitializeTree();
            UpdateTreeGrowth();
        }
        
        // ============================================================================
        // Initialization
        // ============================================================================
        
        void InitializeTree()
        {
            Debug.Log("🌳 TreeMeshBuilder: Initializing procedural 3D tree");
            
            // Create trunk
            CreateTrunk();
            
            // Create leaf particle system
            CreateLeafParticles();
            
            Debug.Log("✅ TreeMeshBuilder: Initialization complete");
        }
        
        void CreateTrunk()
        {
            trunkObj = new GameObject("Trunk");
            trunkObj.transform.SetParent(transform, false);
            trunkObj.transform.localPosition = Vector3.zero;
            
            MeshFilter meshFilter = trunkObj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = trunkObj.AddComponent<MeshRenderer>();
            
            // Create trunk mesh
            meshFilter.mesh = GenerateCylinderMesh(baseTrunkRadius, baseTrunkHeight, trunkSegments);
            
            // Create material
            Material trunkMat = new Material(Shader.Find("Standard"));
            trunkMat.color = trunkColor;
            trunkMat.SetFloat("_Glossiness", 0.1f); // Low gloss for bark
            meshRenderer.material = trunkMat;
            
            Debug.Log($"✅ Created trunk: height={baseTrunkHeight}m, radius={baseTrunkRadius}m");
        }
        
        void CreateLeafParticles()
        {
            leafParticleObj = new GameObject("LeafParticles");
            leafParticleObj.transform.SetParent(transform, false);
            leafParticleObj.transform.localPosition = new Vector3(0, baseTrunkHeight * 0.7f, 0);
            
            leafParticleSystem = leafParticleObj.AddComponent<ParticleSystem>();
            
            var main = leafParticleSystem.main;
            main.startLifetime = Mathf.Infinity; // Persistent leaves
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(leafSizeRange.x, leafSizeRange.y);
            main.startColor = leafColorSpring;
            main.maxParticles = leafParticleCount;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = false;
            main.playOnAwake = false;
            
            var emission = leafParticleSystem.emission;
            emission.enabled = false; // Manual emission
            
            var shape = leafParticleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = leafEmissionRadius;
            shape.radiusThickness = 0.8f; // Hollow sphere (crown shape)
            
            var renderer = leafParticleSystem.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            renderer.material.SetColor("_Color", leafColorSpring);
            renderer.material.SetFloat("_Mode", 3); // Transparent
            renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            renderer.material.SetInt("_ZWrite", 0);
            renderer.material.EnableKeyword("_ALPHABLEND_ON");
            renderer.material.renderQueue = 3000;
            
            Debug.Log($"✅ Created leaf particle system with max {leafParticleCount} particles");
        }
        
        // ============================================================================
        // Mesh Generation
        // ============================================================================
        
        Mesh GenerateCylinderMesh(float radius, float height, int segments)
        {
            Mesh mesh = new Mesh();
            mesh.name = "ProceduralCylinder";
            
            int vertexCount = (segments + 1) * 2 + 2; // Body + top/bottom caps
            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uv = new Vector2[vertexCount];
            int[] triangles = new int[segments * 12]; // 2 triangles per segment * 6 faces
            
            // Body vertices
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                
                vertices[i] = new Vector3(x, 0, z); // Bottom
                vertices[i + segments + 1] = new Vector3(x, height, z); // Top
                
                uv[i] = new Vector2((float)i / segments, 0);
                uv[i + segments + 1] = new Vector2((float)i / segments, 1);
            }
            
            // Center vertices for caps
            int bottomCenterIndex = (segments + 1) * 2;
            int topCenterIndex = bottomCenterIndex + 1;
            vertices[bottomCenterIndex] = new Vector3(0, 0, 0);
            vertices[topCenterIndex] = new Vector3(0, height, 0);
            uv[bottomCenterIndex] = new Vector2(0.5f, 0.5f);
            uv[topCenterIndex] = new Vector2(0.5f, 0.5f);
            
            // Triangles
            int triIndex = 0;
            
            // Body
            for (int i = 0; i < segments; i++)
            {
                int bottomLeft = i;
                int bottomRight = i + 1;
                int topLeft = i + segments + 1;
                int topRight = i + segments + 2;
                
                // First triangle
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = bottomRight;
                
                // Second triangle
                triangles[triIndex++] = bottomRight;
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = topRight;
            }
            
            // Bottom cap
            for (int i = 0; i < segments; i++)
            {
                triangles[triIndex++] = bottomCenterIndex;
                triangles[triIndex++] = i + 1;
                triangles[triIndex++] = i;
            }
            
            // Top cap
            for (int i = 0; i < segments; i++)
            {
                triangles[triIndex++] = topCenterIndex;
                triangles[triIndex++] = i + segments + 1;
                triangles[triIndex++] = i + segments + 2;
            }
            
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }
        
        // ============================================================================
        // Growth Management
        // ============================================================================
        
        public void SetNutrientLevel(int nutrients)
        {
            currentNutrients = nutrients;
            UpdateTreeGrowth();
        }
        
        void UpdateTreeGrowth()
        {
            // Calculate growth stage (0-4)
            int newStage = 0;
            for (int i = 0; i < growthMilestones.Length; i++)
            {
                if (currentNutrients >= growthMilestones[i])
                {
                    newStage = i;
                }
            }
            
            // Update trunk
            UpdateTrunk();
            
            // Update branches if stage changed
            if (newStage != currentGrowthStage)
            {
                currentGrowthStage = newStage;
                UpdateBranches();
            }
            
            // Update leaves
            UpdateLeaves();
            
            Debug.Log($"🌳 Tree growth updated: nutrients={currentNutrients}, stage={currentGrowthStage}");
        }
        
        void UpdateTrunk()
        {
            if (trunkObj == null) return;
            
            // Calculate trunk dimensions based on nutrients
            float growthProgress = Mathf.Min((float)currentNutrients / growthMilestones[growthMilestones.Length - 1], 1f);
            float currentHeight = Mathf.Lerp(baseTrunkHeight, maxTrunkHeight, growthProgress);
            float currentRadius = Mathf.Lerp(baseTrunkRadius, maxTrunkRadius, growthProgress);
            
            // Regenerate trunk mesh
            MeshFilter meshFilter = trunkObj.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.mesh = GenerateCylinderMesh(currentRadius, currentHeight, trunkSegments);
            }
        }
        
        void UpdateBranches()
        {
            // Clear old branches
            foreach (GameObject branchObj in branchObjs)
            {
                if (branchObj != null)
                {
                    Destroy(branchObj);
                }
            }
            branchObjs.Clear();
            
            // Get number of branches for current stage
            if (currentGrowthStage >= branchesPerStage.Length) return;
            int branchCount = branchesPerStage[currentGrowthStage];
            
            // Calculate trunk dimensions for branch attachment
            float growthProgress = Mathf.Min((float)currentNutrients / growthMilestones[growthMilestones.Length - 1], 1f);
            float trunkHeight = Mathf.Lerp(baseTrunkHeight, maxTrunkHeight, growthProgress);
            float trunkRadius = Mathf.Lerp(baseTrunkRadius, maxTrunkRadius, growthProgress);
            
            // Create branches
            for (int i = 0; i < branchCount; i++)
            {
                CreateBranch(i, branchCount, trunkHeight, trunkRadius);
            }
            
            Debug.Log($"🌿 Created {branchCount} branches for stage {currentGrowthStage}");
        }
        
        void CreateBranch(int index, int totalBranches, float trunkHeight, float trunkRadius)
        {
            GameObject branchObj = new GameObject($"Branch_{index}");
            branchObj.transform.SetParent(transform, false);
            
            // Calculate branch position (spiral around trunk)
            float heightRatio = 0.5f + (float)index / totalBranches * 0.4f; // 50%-90% up trunk
            float yPos = trunkHeight * heightRatio;
            float angle = (float)index / totalBranches * 360f + Random.Range(-branchAngleVariation, branchAngleVariation);
            
            branchObj.transform.localPosition = new Vector3(0, yPos, 0);
            branchObj.transform.localRotation = Quaternion.Euler(45f, angle, 0); // 45° upward
            
            // Create branch mesh
            MeshFilter meshFilter = branchObj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = branchObj.AddComponent<MeshRenderer>();
            
            float branchLength = trunkHeight * branchLengthMultiplier;
            float branchRadius = trunkRadius * branchRadiusMultiplier;
            
            meshFilter.mesh = GenerateCylinderMesh(branchRadius, branchLength, branchSegments);
            
            // Create material
            Material branchMat = new Material(Shader.Find("Standard"));
            branchMat.color = trunkColor * 0.9f; // Slightly darker than trunk
            branchMat.SetFloat("_Glossiness", 0.1f);
            meshRenderer.material = branchMat;
            
            branchObjs.Add(branchObj);
        }
        
        void UpdateLeaves()
        {
            if (leafParticleSystem == null) return;
            
            // Calculate number of leaves based on nutrients
            int leafCount = Mathf.Min((int)((float)currentNutrients / growthMilestones[growthMilestones.Length - 1] * leafParticleCount), leafParticleCount);
            
            if (leafCount <= 0)
            {
                leafParticleSystem.Clear();
                return;
            }
            
            // Update particle count
            var main = leafParticleSystem.main;
            main.maxParticles = leafCount;
            
            // Emit particles if not already at capacity
            if (leafParticleSystem.particleCount < leafCount)
            {
                leafParticleSystem.Emit(leafCount - leafParticleSystem.particleCount);
            }
            
            // Update leaf crown position (follow trunk height)
            float growthProgress = Mathf.Min((float)currentNutrients / growthMilestones[growthMilestones.Length - 1], 1f);
            float trunkHeight = Mathf.Lerp(baseTrunkHeight, maxTrunkHeight, growthProgress);
            leafParticleObj.transform.localPosition = new Vector3(0, trunkHeight * 0.8f, 0);
            
            // Update emission radius
            var shape = leafParticleSystem.shape;
            shape.radius = Mathf.Lerp(1.0f, leafEmissionRadius, growthProgress);
        }
        
        // ============================================================================
        // Season Management
        // ============================================================================
        
        public void SetSeason(string season)
        {
            currentSeason = season.ToLower();
            
            Color targetColor = leafColorSpring;
            switch (currentSeason)
            {
                case "spring":
                case "summer":
                    targetColor = leafColorSpring;
                    break;
                case "autumn":
                case "fall":
                    targetColor = leafColorAutumn;
                    break;
                case "winter":
                    targetColor = leafColorWinter;
                    break;
            }
            
            // Update particle color
            if (leafParticleSystem != null)
            {
                var main = leafParticleSystem.main;
                main.startColor = targetColor;
                
                var renderer = leafParticleSystem.GetComponent<ParticleSystemRenderer>();
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.SetColor("_Color", targetColor);
                }
            }
            
            Debug.Log($"🍂 Season changed to: {season} (color: {targetColor})");
        }
        
        // ============================================================================
        // Public Utilities
        // ============================================================================
        
        public void Show(bool show)
        {
            gameObject.SetActive(show);
        }
    }
}


