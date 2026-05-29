using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;

namespace FCG
{
    public class CityGenerator : MonoBehaviour
    {
        public enum SatelliteConnectionMode
        {
            MainOnly = 0,
            Chain = 1,
            Nearest = 2,
            FullMesh = 3
        }

        private struct SatelliteCityLayout
        {
            public float x;
            public float z;

            public SatelliteCityLayout(float x, float z)
            {
                this.x = x;
                this.z = z;
            }
        }

        [System.Serializable]
        public class GenerationStats
        {
            public int citySize;
            public bool withSatelliteCity;
            public int generatedSatelliteCities;
            public int generatedConnectionSegments;
            public int generatedCityObjectCount;
            public float generationDurationMs;
        }

        [System.Serializable]
        public class CityNetworkNode
        {
            public int id;
            public bool mainCity;
            public Vector3 position;
            public List<int> linkedNodeIds = new List<int>();
        }

        [System.Serializable]
        public class CityNetworkLink
        {
            public int fromNodeId;
            public int toNodeId;
            public int segments;
            public float distance;
            public bool toMainCity;
        }

        [System.Serializable]
        public class GenerationNetwork
        {
            public List<CityNetworkNode> nodes = new List<CityNetworkNode>();
            public List<CityNetworkLink> links = new List<CityNetworkLink>();
        }

        private int nB = 0;
        private Vector3 center;
        private int residential = 0;
        private bool _residential = false;

        GameObject cityMaker;

        [HideInInspector]
        public GameObject[] miniBorder;

        [HideInInspector]
        public GameObject[] smallBorder;

        [HideInInspector]
        public GameObject[] mediumBorder;

        [HideInInspector]
        public GameObject[] largeBorder;
        
        [HideInInspector]
        public GameObject[] miniBorderFlat;

        [HideInInspector]
        public GameObject[] smallBorderFlat;

        [HideInInspector]
        public GameObject[] mediumBorderFlat;

        [HideInInspector]
        public GameObject[] largeBorderFlat;
         
        [HideInInspector]
        public GameObject[] miniBorderWithExitOfCity;

        [HideInInspector]
        public GameObject[] smallBorderWithExitOfCity;

        [HideInInspector]
        public GameObject[] mediumBorderWithExitOfCity;

        [HideInInspector]
        public GameObject[] largeBorderWithExitOfCity;

        [HideInInspector]
        public GameObject[] largeBlocks;

        private bool[] _largeBlocks;



        [HideInInspector]
        public GameObject[] bigLargeBlocks;


        [HideInInspector]
        public GameObject[] forward50;
        [HideInInspector]
        public GameObject[] forward100;
        [HideInInspector]
        public GameObject[] forward300;
        [HideInInspector]
        public GameObject[] forward400;
        [HideInInspector]
        public GameObject[] forwardLeft400;
        [HideInInspector]
        public GameObject[] forwardRight400;
        [HideInInspector]
        public GameObject[] left200;
        [HideInInspector]
        public GameObject[] left300;
        [HideInInspector]
        public GameObject[] right200;
        [HideInInspector]
        public GameObject[] right300;



        private bool[] _bigLargeBlocks;


        [HideInInspector]
        public GameObject[] BB;  // Buildings in suburban areas (not in the corner)
        [HideInInspector]
        public GameObject[] BC;  // Down Town Buildings(Not in the corner)
        [HideInInspector]
        public GameObject[] BR;  // Residential buildings in suburban areas (not in the corner)
        [HideInInspector]
        public GameObject[] DC;  // Corner buildings that occupy both sides of the block
        [HideInInspector]
        public GameObject[] EB;  // Corner buildings in suburban areas
        [HideInInspector]
        public GameObject[] EC;  // Down Town Corner Buildings 
        [HideInInspector]
        public GameObject[] MB;  //  Buildings that occupy both sides of the block 
        [HideInInspector]
        public GameObject[] BK;  //  Buildings that occupy an entire block
        [HideInInspector]
        public GameObject[] SB;  //  Large buildings that occupy larger blocks 
        [HideInInspector]
        public GameObject[] BBS;  //  Buildings on slopes (neighborhood)
        [HideInInspector]
        public GameObject[] BCS;  //  Down Town Buildings on slopes

        private int[] _BB;
        private int[] _BC;
        private int[] _BR;
        //private int[] _DC;
        private int[] _EB;
        private int[] _EC;

        private int[] _EBS;
        private int[] _ECS;

        private int[] _MB;
        private int[] _BK;
        private int[] _SB;
        private int[] _BBS;
        private int[] _BCS;

        private GameObject[] tempArray;
        private int numB;


        float distCenter = 300;
        bool withDowntownArea = true;
        float downTownSize = 100;
        private int generationConnectionSegmentsCount = 0;
        public GenerationStats LastGenerationStats { get; private set; } = new GenerationStats();
        public GenerationNetwork LastGenerationNetwork { get; private set; } = new GenerationNetwork();
        public event Action<GenerationStats, GenerationNetwork> OnGenerationCompleted;

        public void ClearCity()
        {
            if (!cityMaker)
                cityMaker = GameObject.Find("City-Maker");

            if (cityMaker)
                DestroyImmediate(cityMaker);

            LastGenerationNetwork = new GenerationNetwork();

        }

        public bool CanGenerate(int size, bool withSatteliteCity, out string reason)
        {
            if (largeBlocks == null || largeBlocks.Length == 0)
            {
                reason = "largeBlocks array is empty.";
                return false;
            }

            if (size == 1)
            {
                if (withSatteliteCity && !HasPrefabs(miniBorderWithExitOfCity))
                {
                    reason = "miniBorderWithExitOfCity is required for satellite generation in size 1.";
                    return false;
                }

                if (!withSatteliteCity && !HasPrefabs(miniBorder) && !HasPrefabs(miniBorderFlat))
                {
                    reason = "miniBorder/miniBorderFlat arrays are empty.";
                    return false;
                }
            }
            else if (size == 2)
            {
                if (withSatteliteCity && !HasPrefabs(smallBorderWithExitOfCity))
                {
                    reason = "smallBorderWithExitOfCity is required for satellite generation in size 2.";
                    return false;
                }

                if (!withSatteliteCity && !HasPrefabs(smallBorder) && !HasPrefabs(smallBorderFlat))
                {
                    reason = "smallBorder/smallBorderFlat arrays are empty.";
                    return false;
                }
            }
            else if (size == 3)
            {
                if (withSatteliteCity && !HasPrefabs(mediumBorderWithExitOfCity))
                {
                    reason = "mediumBorderWithExitOfCity is required for satellite generation in size 3.";
                    return false;
                }

                if (!withSatteliteCity && !HasPrefabs(mediumBorder) && !HasPrefabs(mediumBorderFlat))
                {
                    reason = "mediumBorder/mediumBorderFlat arrays are empty.";
                    return false;
                }
            }
            else if (size == 4)
            {
                if (withSatteliteCity && !HasPrefabs(largeBorderWithExitOfCity))
                {
                    reason = "largeBorderWithExitOfCity is required for satellite generation in size 4.";
                    return false;
                }

                if (!withSatteliteCity && !HasPrefabs(largeBorder) && !HasPrefabs(largeBorderFlat))
                {
                    reason = "largeBorder/largeBorderFlat arrays are empty.";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        public void GenerateCity(CityGenerationRequest request)
        {
            if (request == null)
            {
                Debug.LogWarning("CityGenerator: request is null, using defaults.");
                request = new CityGenerationRequest();
            }

            request.Normalize();

            GenerateCity(
                request.citySize,
                request.withSatelliteCity,
                request.borderFlat,
                request.satelliteCityCount,
                request.connectSatellitesToMain,
                request.connectSatellitesTogether,
                request.randomSatelliteLayout,
                request.useSatelliteSeed,
                request.satelliteSeed,
                request.randomSatelliteMin.x,
                request.randomSatelliteMax.x,
                request.randomSatelliteMin.y,
                request.randomSatelliteMax.y,
                request.customSatelliteOffsets,
                request.useCustomSatelliteOffsets,
                request.satelliteGlobalOffset,
                request.satelliteConnectionMode,
                request.satelliteMaxNeighborLinks,
                request.satelliteCloseLoop,
                request.useCitySeed,
                request.citySeed,
                request.autoSatelliteCount,
                request.randomSatelliteSizes,
                request.satelliteCityMinSize,
                request.satelliteCityMaxSize,
                request.satelliteBuildingDensity,
                request.autoGenerateBuildings,
                request.withDownTownArea,
                request.downTownSize,
                request.connectionStepOverride,
                request.createCityAnchors,
                request.createConnectionDebugLines,
                request.connectionDebugLineHeight,
                request.generateHighways,
                request.highwayWidth,
                request.highwayThickness);
        }

        public void GenerateCity(
            int size,
            bool withSatteliteCity = false,
            bool borderFlat = false,
            int satteliteCityCount = 1,
            bool connectSatteliteCities = true,
            bool connectSatteliteCitiesTogether = true,
            bool randomSatteliteLayout = false,
            bool useSatteliteSeed = false,
            int satteliteSeed = 0,
            float randomMinX = -1000f,
            float randomMaxX = 1000f,
            float randomMinZ = -2200f,
            float randomMaxZ = -1200f,
            List<Vector2> customSatteliteOffsets = null,
            bool useCustomSatteliteOffsets = false,
            Vector2 satteliteGlobalOffset = default(Vector2),
            SatelliteConnectionMode satelliteConnectionMode = SatelliteConnectionMode.Chain,
            int satelliteMaxNeighborLinks = 1,
            bool satelliteCloseLoop = false,
            bool useCitySeed = false,
            int citySeed = 0,
            bool autoSatelliteCount = false,
            bool randomSatelliteSizes = false,
            int satelliteCityMinSize = 1,
            int satelliteCityMaxSize = 1,
            float satelliteBuildingDensity = 1f,
            bool autoGenerateBuildings = false,
            bool withDownTownArea = false,
            float downTownSize = 100f,
            float connectionStepOverride = 0f,
            bool createCityAnchors = true,
            bool createConnectionDebugLines = false,
            float connectionDebugLineHeight = 3f,
            bool generateHighways = true,
            float highwayWidth = 18f,
            float highwayThickness = 0.2f)
        {
            float startTime = Time.realtimeSinceStartup;
            // If requested, seed the Unity RNG so generation is deterministic
            if (useCitySeed)
            {
                Random.InitState(citySeed);
            }

            satelliteBuildingDensityField = Mathf.Clamp01(satelliteBuildingDensity);
            generationConnectionSegmentsCount = 0;
            int generatedSatellites = 0;
            List<Vector3> satelliteCenters = new List<Vector3>();
            List<CityNetworkLink> generatedLinks = new List<CityNetworkLink>();

            if (!CanGenerate(size, withSatteliteCity, out string generationReason))
            {
                Debug.LogError("CityGenerator: " + generationReason + " Load assets before generating streets.");
                return;
            }

            bool satCity = false;

            if (size == 1)
            {
                // Very Small City
                satCity = GenerateStreetsVerySmall(borderFlat, withSatteliteCity);
            }
            else if (size == 2)
            {
                // Small City
                satCity = GenerateStreetsSmall(borderFlat, withSatteliteCity );
            }
            else if (size == 3)
            {
                // Medium City
                satCity = GenerateStreets(borderFlat, withSatteliteCity);
            }
            else if (size == 4)
            {
                // Large City
                satCity = GenerateStreetsBig(borderFlat, withSatteliteCity);
            }


            if (satCity)
            {
                Transform exitPositipon = CityExitPosition();

                if (exitPositipon != null)
                {
                    int count = Mathf.Clamp(satteliteCityCount, 1, 64);
                    if (autoSatelliteCount)
                    {
                        int defaultCount = 1;
                        switch (size)
                        {
                            case 1: defaultCount = 1; break;
                            case 2: defaultCount = 2; break;
                            case 3: defaultCount = 4; break;
                            case 4: defaultCount = 8; break;
                        }
                        count = Mathf.Clamp(defaultCount, 1, 64);
                    }
                    List<SatelliteCityLayout> layouts;

                    if (useCustomSatteliteOffsets && customSatteliteOffsets != null && customSatteliteOffsets.Count > 0)
                        layouts = BuildCustomSatelliteLayouts(customSatteliteOffsets);
                    else if (randomSatteliteLayout)
                        layouts = BuildRandomSatelliteLayouts(count, randomMinX, randomMaxX, randomMinZ, randomMaxZ, useSatteliteSeed, satteliteSeed);
                    else
                        layouts = BuildSatelliteLayouts(count);

                    for (int i = 0; i < layouts.Count; i++)
                    {
                        SatelliteCityLayout layout = layouts[i];
                        layout.x += satteliteGlobalOffset.x;
                        layout.z += satteliteGlobalOffset.y;
                        layouts[i] = layout;
                    }

                    for (int i = 0; i < layouts.Count; i++)
                    {
                        SatelliteCityLayout layout = layouts[i];

                        int satSize = 1;
                        if (randomSatelliteSizes)
                        {
                            int minS = Mathf.Clamp(satelliteCityMinSize, 1, 4);
                            int maxS = Mathf.Clamp(satelliteCityMaxSize, 1, 4);
                            if (maxS < minS) { int t = minS; minS = maxS; maxS = t; }
                            satSize = Random.Range(minS, maxS + 1);
                        }

                        // Generate satellite city with selected size
                        switch (satSize)
                        {
                            case 1:
                                GenerateStreetsVerySmall(false, false, true, layout.x, layout.z, exitPositipon);
                                break;
                            case 2:
                                GenerateStreetsSmall(false, false, true, layout.x, layout.z, exitPositipon);
                                break;
                            case 3:
                                GenerateStreets(false, false, true, layout.x, layout.z, exitPositipon);
                                break;
                            case 4:
                                GenerateStreetsBig(false, false, true, layout.x, layout.z, exitPositipon);
                                break;
                            default:
                                GenerateStreetsVerySmall(false, false, true, layout.x, layout.z, exitPositipon);
                                break;
                        }

                        generatedSatellites++;

                        Vector3 satPosition = GetSatellitePositionFromExit(exitPositipon, layout.x, layout.z);
                        satelliteCenters.Add(satPosition);

                        if (connectSatteliteCities)
                        {
                            int segments = ConnectCities(exitPositipon.position, satPosition, connectionStepOverride, generateHighways, highwayWidth, highwayThickness);
                            generationConnectionSegmentsCount += segments;
                            if (segments > 0)
                                generatedLinks.Add(CreateNetworkLink(0, i + 1, exitPositipon.position, satPosition, segments, true));
                        }
                    }

                    if (connectSatteliteCitiesTogether && satelliteCenters.Count > 1)
                        generationConnectionSegmentsCount += ConnectSatelliteNetwork(
                            satelliteCenters,
                            satelliteConnectionMode,
                            satelliteMaxNeighborLinks,
                            satelliteCloseLoop,
                            connectionStepOverride,
                            generatedLinks,
                            1,
                            generateHighways,
                            highwayWidth,
                            highwayThickness);
                }
                else
                {
                    Debug.Log("ExitCity gameobject not found");

                }

            }



            DayNight dayNight = FindObjectOfType<DayNight>();
            if (dayNight)
                dayNight.ChangeMaterial();

            if (!cityMaker)
                cityMaker = GameObject.Find("City-Maker");

            Vector3 mainCityPosition = ResolveMainCityPosition();
            LastGenerationNetwork = BuildGenerationNetwork(mainCityPosition, satelliteCenters, generatedLinks);

            if (autoGenerateBuildings)
                GenerateAllBuildings(withDownTownArea, downTownSize);

            if (createCityAnchors)
                BuildGeneratedNetworkAnchors(LastGenerationNetwork, createConnectionDebugLines, connectionDebugLineHeight);
            else
                ClearGeneratedNetworkAnchors();

            LastGenerationStats = new GenerationStats
            {
                citySize = size,
                withSatelliteCity = withSatteliteCity,
                generatedSatelliteCities = generatedSatellites,
                generatedConnectionSegments = generationConnectionSegmentsCount,
                generatedCityObjectCount = cityMaker ? cityMaker.transform.childCount : 0,
                generationDurationMs = (Time.realtimeSinceStartup - startTime) * 1000f
            };

            if (OnGenerationCompleted != null)
                OnGenerationCompleted(LastGenerationStats, CloneGenerationNetwork(LastGenerationNetwork));

        }

        public string GetLastGenerationSummary()
        {
            if (LastGenerationStats == null)
                return "No generation data available.";

            return string.Format(
                "Size: {0} | Satellite: {1} | Satellites: {2} | Connections: {3} | Objects: {4} | Time: {5:0.0} ms",
                LastGenerationStats.citySize,
                LastGenerationStats.withSatelliteCity ? "On" : "Off",
                LastGenerationStats.generatedSatelliteCities,
                LastGenerationStats.generatedConnectionSegments,
                LastGenerationStats.generatedCityObjectCount,
                LastGenerationStats.generationDurationMs);
        }

        public string GetLastGenerationNetworkSummary()
        {
            if (LastGenerationNetwork == null || LastGenerationNetwork.nodes == null || LastGenerationNetwork.nodes.Count == 0)
                return "Network: no nodes";

            return string.Format(
                "Network: nodes={0}, links={1}, satellites={2}",
                LastGenerationNetwork.nodes.Count,
                LastGenerationNetwork.links != null ? LastGenerationNetwork.links.Count : 0,
                Mathf.Max(0, LastGenerationNetwork.nodes.Count - 1));
        }

        public GenerationNetwork GetLastGenerationNetworkClone()
        {
            return CloneGenerationNetwork(LastGenerationNetwork);
        }

        private Vector3 ResolveMainCityPosition()
        {
            Transform cityExit = CityExitPosition();
            if (cityExit != null)
                return cityExit.position;

            if (cityMaker)
                return cityMaker.transform.position;

            return Vector3.zero;
        }

        private static void AddLinkedNodeIfMissing(CityNetworkNode node, int linkedNodeId)
        {
            if (node == null)
                return;
            if (node.linkedNodeIds == null)
                node.linkedNodeIds = new List<int>();
            if (!node.linkedNodeIds.Contains(linkedNodeId))
                node.linkedNodeIds.Add(linkedNodeId);
        }

        private GenerationNetwork BuildGenerationNetwork(Vector3 mainCityPosition, List<Vector3> satelliteCenters, List<CityNetworkLink> generatedLinks)
        {
            GenerationNetwork network = new GenerationNetwork();

            network.nodes.Add(new CityNetworkNode
            {
                id = 0,
                mainCity = true,
                position = mainCityPosition,
                linkedNodeIds = new List<int>()
            });

            if (satelliteCenters != null)
            {
                for (int i = 0; i < satelliteCenters.Count; i++)
                {
                    network.nodes.Add(new CityNetworkNode
                    {
                        id = i + 1,
                        mainCity = false,
                        position = satelliteCenters[i],
                        linkedNodeIds = new List<int>()
                    });
                }
            }

            if (generatedLinks != null)
            {
                for (int i = 0; i < generatedLinks.Count; i++)
                {
                    CityNetworkLink link = generatedLinks[i];
                    if (link == null)
                        continue;

                    network.links.Add(new CityNetworkLink
                    {
                        fromNodeId = link.fromNodeId,
                        toNodeId = link.toNodeId,
                        segments = link.segments,
                        distance = link.distance,
                        toMainCity = link.toMainCity
                    });
                }
            }

            Dictionary<int, CityNetworkNode> nodeMap = new Dictionary<int, CityNetworkNode>();
            for (int i = 0; i < network.nodes.Count; i++)
                nodeMap[network.nodes[i].id] = network.nodes[i];

            for (int i = 0; i < network.links.Count; i++)
            {
                CityNetworkLink link = network.links[i];
                if (nodeMap.TryGetValue(link.fromNodeId, out CityNetworkNode fromNode))
                    AddLinkedNodeIfMissing(fromNode, link.toNodeId);
                if (nodeMap.TryGetValue(link.toNodeId, out CityNetworkNode toNode))
                    AddLinkedNodeIfMissing(toNode, link.fromNodeId);
            }

            return network;
        }

        private GenerationNetwork CloneGenerationNetwork(GenerationNetwork source)
        {
            GenerationNetwork clone = new GenerationNetwork();
            if (source == null)
                return clone;

            if (source.nodes != null)
            {
                for (int i = 0; i < source.nodes.Count; i++)
                {
                    CityNetworkNode node = source.nodes[i];
                    if (node == null)
                        continue;

                    CityNetworkNode copy = new CityNetworkNode
                    {
                        id = node.id,
                        mainCity = node.mainCity,
                        position = node.position,
                        linkedNodeIds = (node.linkedNodeIds != null) ? new List<int>(node.linkedNodeIds) : new List<int>()
                    };
                    clone.nodes.Add(copy);
                }
            }

            if (source.links != null)
            {
                for (int i = 0; i < source.links.Count; i++)
                {
                    CityNetworkLink link = source.links[i];
                    if (link == null)
                        continue;

                    clone.links.Add(new CityNetworkLink
                    {
                        fromNodeId = link.fromNodeId,
                        toNodeId = link.toNodeId,
                        segments = link.segments,
                        distance = link.distance,
                        toMainCity = link.toMainCity
                    });
                }
            }

            return clone;
        }

        private void BuildGeneratedNetworkAnchors(GenerationNetwork network, bool createDebugLines, float lineHeight)
        {
            if (!cityMaker)
                cityMaker = GameObject.Find("City-Maker");
            if (!cityMaker)
                return;

            ClearGeneratedNetworkAnchors();

            if (network == null || network.nodes == null || network.nodes.Count == 0)
                return;

            GameObject root = new GameObject("Generated-City-Network");
            root.transform.SetParent(cityMaker.transform);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            Dictionary<int, Transform> nodeTransforms = new Dictionary<int, Transform>();
            for (int i = 0; i < network.nodes.Count; i++)
            {
                CityNetworkNode node = network.nodes[i];
                if (node == null)
                    continue;

                string name = node.mainCity ? "Main-City-Anchor" : "Satellite-City-" + node.id.ToString("00");
                GameObject nodeObject = new GameObject(name);
                nodeObject.transform.SetParent(root.transform);
                nodeObject.transform.position = node.position;

                GeneratedCityAnchor anchor = nodeObject.AddComponent<GeneratedCityAnchor>();
                anchor.nodeId = node.id;
                anchor.mainCity = node.mainCity;
                anchor.linkedNodeIds = (node.linkedNodeIds != null) ? new List<int>(node.linkedNodeIds) : new List<int>();

                nodeTransforms[node.id] = nodeObject.transform;
            }

            if (!createDebugLines || network.links == null || network.links.Count == 0)
                return;

            Material lineMaterial = GetDebugLineMaterial();
            Color mainLinkColor = new Color(0.2f, 0.9f, 1f, 1f);
            Color cityLinkColor = new Color(1f, 0.72f, 0.15f, 1f);

            for (int i = 0; i < network.links.Count; i++)
            {
                CityNetworkLink link = network.links[i];
                if (link == null)
                    continue;
                if (!nodeTransforms.TryGetValue(link.fromNodeId, out Transform fromNode))
                    continue;
                if (!nodeTransforms.TryGetValue(link.toNodeId, out Transform toNode))
                    continue;

                GameObject lineObject = new GameObject("Link-" + link.fromNodeId + "-" + link.toNodeId);
                lineObject.transform.SetParent(root.transform);

                LineRenderer line = lineObject.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.numCapVertices = 2;
                line.widthMultiplier = 8f;
                if (lineMaterial != null)
                    line.sharedMaterial = lineMaterial;
                line.startColor = link.toMainCity ? mainLinkColor : cityLinkColor;
                line.endColor = line.startColor;
                line.SetPosition(0, fromNode.position + Vector3.up * lineHeight);
                line.SetPosition(1, toNode.position + Vector3.up * lineHeight);
            }
        }

        private Material debugLineMaterial;

        private Material GetDebugLineMaterial()
        {
            if (debugLineMaterial)
                return debugLineMaterial;

            Shader shader = Shader.Find("Sprites/Default");
            if (!shader)
                shader = Shader.Find("Unlit/Color");
            if (!shader)
                return null;

            debugLineMaterial = new Material(shader);
            debugLineMaterial.name = "FCG-CityNetwork-LineMaterial";
            return debugLineMaterial;
        }

        private void ClearGeneratedNetworkAnchors()
        {
            if (!cityMaker)
                cityMaker = GameObject.Find("City-Maker");
            if (!cityMaker)
                return;

            Transform root = cityMaker.transform.Find("Generated-City-Network");
            if (root)
                DestroyImmediate(root.gameObject);
        }

        private Transform CityExitPosition()
        {

            if (GameObject.Find("ExitCity"))
                return GameObject.Find("ExitCity").transform;
            else
                return null;

        }

        private Vector3 GetSatellitePositionFromExit(Transform exitPosition, float satteliteCityPositionX, float satteliteCityPositionZ)
        {
            return exitPosition.position + exitPosition.right * satteliteCityPositionX + exitPosition.forward * satteliteCityPositionZ;
        }

        private bool HasPrefabs(GameObject[] prefabs)
        {
            return prefabs != null && prefabs.Length > 0;
        }

        private bool ValidateBuildingPrefabPools(out string message)
        {
            if (!HasPrefabs(BB)) { message = "BB pool is empty."; return false; }
            if (!HasPrefabs(BC)) { message = "BC pool is empty."; return false; }
            if (!HasPrefabs(BR)) { message = "BR pool is empty."; return false; }
            if (!HasPrefabs(EB)) { message = "EB pool is empty."; return false; }
            if (!HasPrefabs(EC)) { message = "EC pool is empty."; return false; }
            if (!HasPrefabs(MB)) { message = "MB pool is empty."; return false; }
            if (!HasPrefabs(BK)) { message = "BK pool is empty."; return false; }
            if (!HasPrefabs(SB)) { message = "SB pool is empty."; return false; }
            if (!HasPrefabs(BBS)) { message = "BBS pool is empty."; return false; }
            if (!HasPrefabs(BCS)) { message = "BCS pool is empty."; return false; }

            message = string.Empty;
            return true;
        }

        private GameObject InstantiateRandomPrefab(GameObject[] prefabs, Vector3 position, Quaternion rotation, Transform parent, string prefabGroupName)
        {
            if (!HasPrefabs(prefabs))
            {
                Debug.LogWarning("CityGenerator: no prefabs available in group '" + prefabGroupName + "'.");
                return null;
            }

            return (GameObject)Instantiate(prefabs[Random.Range(0, prefabs.Length)], position, rotation, parent);
        }

        private float satelliteBuildingDensityField = 1f;

        private GameObject InstantiateIfAllowed(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (prefab == null) return null;
            if (satelliteBuildingDensityField >= 1f) return (GameObject)Instantiate(prefab, position, rotation, parent);

            // Check distance to nearest satellite node
            float nearest = float.MaxValue;
            if (LastGenerationNetwork != null && LastGenerationNetwork.nodes != null)
            {
                for (int i = 0; i < LastGenerationNetwork.nodes.Count; i++)
                {
                    var node = LastGenerationNetwork.nodes[i];
                    if (node == null) continue;
                    if (node.mainCity) continue;
                    float d = Vector3.Distance(node.position, position);
                    if (d < nearest) nearest = d;
                }
            }

            // If no satellites or far away, instantiate
            if (nearest == float.MaxValue || nearest > 800f)
                return (GameObject)Instantiate(prefab, position, rotation, parent);

            // Inside satellite area: use density probability
            if (Random.value <= satelliteBuildingDensityField)
                return (GameObject)Instantiate(prefab, position, rotation, parent);

            return null;
        }

        private List<SatelliteCityLayout> BuildSatelliteLayouts(int count)
        {
            List<SatelliteCityLayout> templates = new List<SatelliteCityLayout>
            {
                new SatelliteCityLayout(0, -1516),
                new SatelliteCityLayout(-300, -1516),
                new SatelliteCityLayout(200, -1516),
                new SatelliteCityLayout(-100, -1516),
                new SatelliteCityLayout(700, -1316),
                new SatelliteCityLayout(500, -1316),
                new SatelliteCityLayout(-700, -1316),
                new SatelliteCityLayout(-500, -1316),
                new SatelliteCityLayout(1000, -1750),
                new SatelliteCityLayout(-1000, -1750),
                new SatelliteCityLayout(350, -2200),
                new SatelliteCityLayout(-350, -2200)
            };

            List<SatelliteCityLayout> selected = new List<SatelliteCityLayout>(count);
            for (int i = 0; i < count; i++)
                selected.Add(templates[i % templates.Count]);

            return selected;
        }

        private List<SatelliteCityLayout> BuildCustomSatelliteLayouts(List<Vector2> customOffsets)
        {
            List<SatelliteCityLayout> selected = new List<SatelliteCityLayout>(customOffsets.Count);
            for (int i = 0; i < customOffsets.Count; i++)
                selected.Add(new SatelliteCityLayout(customOffsets[i].x, customOffsets[i].y));

            return selected;
        }

        private List<SatelliteCityLayout> BuildRandomSatelliteLayouts(
            int count,
            float randomMinX,
            float randomMaxX,
            float randomMinZ,
            float randomMaxZ,
            bool useSatteliteSeed,
            int satteliteSeed)
        {
            if (randomMinX > randomMaxX)
            {
                float t = randomMinX;
                randomMinX = randomMaxX;
                randomMaxX = t;
            }

            if (randomMinZ > randomMaxZ)
            {
                float t = randomMinZ;
                randomMinZ = randomMaxZ;
                randomMaxZ = t;
            }

            System.Random rng = useSatteliteSeed ? new System.Random(satteliteSeed) : new System.Random();
            List<SatelliteCityLayout> selected = new List<SatelliteCityLayout>(count);

            for (int i = 0; i < count; i++)
            {
                float x = randomMinX + (float)rng.NextDouble() * (randomMaxX - randomMinX);
                float z = randomMinZ + (float)rng.NextDouble() * (randomMaxZ - randomMinZ);
                selected.Add(new SatelliteCityLayout(x, z));
            }

            return selected;
        }

        private GameObject GetConnectionPrefab()
        {
            if (forward400 != null && forward400.Length > 0)
                return forward400[Random.Range(0, forward400.Length)];
            if (forward300 != null && forward300.Length > 0)
                return forward300[Random.Range(0, forward300.Length)];
            if (forward100 != null && forward100.Length > 0)
                return forward100[Random.Range(0, forward100.Length)];
            if (forward50 != null && forward50.Length > 0)
                return forward50[Random.Range(0, forward50.Length)];

            return null;
        }

        private float GetConnectionStep()
        {
            if (forward400 != null && forward400.Length > 0) return 400f;
            if (forward300 != null && forward300.Length > 0) return 300f;
            if (forward100 != null && forward100.Length > 0) return 100f;
            if (forward50 != null && forward50.Length > 0) return 50f;

            return 300f;
        }

        private int ConnectCities(Vector3 startPosition, Vector3 endPosition, float connectionStepOverride = 0f, bool generateHighways = true, float highwayWidth = 18f, float highwayThickness = 0.2f)
        {
            if (generateHighways)
            {
                return CreateHighwayConnection(startPosition, endPosition, connectionStepOverride, highwayWidth, highwayThickness);
            }

            GameObject roadPrefab = GetConnectionPrefab();
            if (roadPrefab == null || cityMaker == null)
                return 0;

            Vector3 delta = endPosition - startPosition;
            delta.y = 0;

            float distance = delta.magnitude;
            if (distance < 1f)
                return 0;

            Vector3 direction = delta.normalized;
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

            float step = (connectionStepOverride > 1f) ? connectionStepOverride : GetConnectionStep();
            int segments = Mathf.Max(1, Mathf.CeilToInt(distance / step));

            for (int i = 0; i < segments; i++)
            {
                float t = (i + 0.5f) / segments;
                Vector3 pos = Vector3.Lerp(startPosition, endPosition, t);
                Instantiate(roadPrefab, pos, rotation, cityMaker.transform);
            }

            return segments;
        }

        private int ConnectAndTrack(
            Vector3 fromPosition,
            Vector3 toPosition,
            float connectionStepOverride,
            int fromNodeId,
            int toNodeId,
            List<CityNetworkLink> generatedLinks,
            bool toMainCity,
            bool generateHighways = true,
            float highwayWidth = 18f,
            float highwayThickness = 0.2f)
        {
            int segments = ConnectCities(fromPosition, toPosition, connectionStepOverride, generateHighways, highwayWidth, highwayThickness);
            if (segments > 0 && generatedLinks != null)
                generatedLinks.Add(CreateNetworkLink(fromNodeId, toNodeId, fromPosition, toPosition, segments, toMainCity));

            return segments;
        }

        private CityNetworkLink CreateNetworkLink(
            int fromNodeId,
            int toNodeId,
            Vector3 fromPosition,
            Vector3 toPosition,
            int segments,
            bool toMainCity)
        {
            Vector3 a = fromPosition;
            Vector3 b = toPosition;
            a.y = 0f;
            b.y = 0f;

            return new CityNetworkLink
            {
                fromNodeId = fromNodeId,
                toNodeId = toNodeId,
                segments = segments,
                distance = Vector3.Distance(a, b),
                toMainCity = toMainCity
            };
        }

        private int CreateHighwayConnection(Vector3 startPosition, Vector3 endPosition, float connectionStepOverride, float highwayWidth, float highwayThickness)
        {
            if (cityMaker == null)
                return 0;

            Vector3 delta = endPosition - startPosition;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance < 1f)
                return 0;

            float step = (connectionStepOverride > 1f) ? connectionStepOverride : Mathf.Max(250f, highwayWidth * 2f);
            int segments = Mathf.Max(1, Mathf.CeilToInt(distance / step));

            for (int i = 0; i < segments; i++)
            {
                Vector3 a = Vector3.Lerp(startPosition, endPosition, i / (float)segments);
                Vector3 b = Vector3.Lerp(startPosition, endPosition, (i + 1f) / (float)segments);
                CreateHighwaySegment(a, b, cityMaker.transform, highwayWidth, highwayThickness);
            }

            return segments;
        }

        private void CreateHighwaySegment(Vector3 startPosition, Vector3 endPosition, Transform parent, float highwayWidth, float highwayThickness)
        {
            Vector3 delta = endPosition - startPosition;
            delta.y = 0f;
            if (delta.magnitude < 0.001f)
                return;

            Vector3 center = (startPosition + endPosition) * 0.5f;
            Quaternion rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);

            float halfLength = delta.magnitude * 0.5f;
            float halfWidth = highwayWidth * 0.5f;
            float shoulderWidth = Mathf.Max(1.5f, highwayWidth * 0.12f);
            float laneMarkerWidth = Mathf.Max(0.18f, highwayWidth * 0.04f);

            CreateHighwayMesh("Highway-Asphalt", center, rotation, parent, halfLength, halfWidth, 0.02f, new Color(0.19f, 0.20f, 0.23f, 1f));
            CreateHighwayMesh("Highway-Shoulder", center, rotation, parent, halfLength + 0.02f, halfWidth + shoulderWidth, 0.005f, new Color(0.25f, 0.26f, 0.29f, 1f));

            CreateLaneMarker(center, rotation, parent, halfLength - 0.5f, laneMarkerWidth, 0.03f, new Color(1f, 1f, 1f, 1f));
            CreateLaneMarker(center, rotation, parent, halfLength - 1.4f, laneMarkerWidth, 0.03f, new Color(1f, 1f, 1f, 1f));

            GameObject divider = new GameObject("Highway-Divider");
            divider.transform.SetParent(parent, false);
            divider.transform.position = center;
            divider.transform.rotation = rotation;

            MeshFilter dividerFilter = divider.AddComponent<MeshFilter>();
            MeshRenderer dividerRenderer = divider.AddComponent<MeshRenderer>();
            Mesh dividerMesh = new Mesh();

            float dividerHalfWidth = Mathf.Max(0.08f, highwayWidth * 0.02f);
            Vector3[] dVertices = new Vector3[4]
            {
                new Vector3(-dividerHalfWidth, 0.035f, -halfLength + 0.2f),
                new Vector3( dividerHalfWidth, 0.035f, -halfLength + 0.2f),
                new Vector3(-dividerHalfWidth, 0.035f,  halfLength - 0.2f),
                new Vector3( dividerHalfWidth, 0.035f,  halfLength - 0.2f)
            };
            int[] dTriangles = new int[] { 0, 2, 1, 1, 2, 3 };
            Vector2[] dUv = new Vector2[] { new Vector2(0,0), new Vector2(1,0), new Vector2(0,1), new Vector2(1,1) };
            dividerMesh.vertices = dVertices;
            dividerMesh.triangles = dTriangles;
            dividerMesh.uv = dUv;
            dividerMesh.RecalculateNormals();
            dividerMesh.RecalculateBounds();
            dividerFilter.sharedMesh = dividerMesh;

            Material dividerMaterial = new Material(Shader.Find("Sprites/Default"));
            dividerMaterial.color = new Color(0.92f, 0.92f, 0.92f, 1f);
            dividerRenderer.sharedMaterial = dividerMaterial;
        }

        private void CreateHighwayMesh(string name, Vector3 center, Quaternion rotation, Transform parent, float halfLength, float halfWidth, float height, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.rotation = rotation;

            MeshFilter filter = go.AddComponent<MeshFilter>();
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(-halfWidth, height, -halfLength),
                new Vector3( halfWidth, height, -halfLength),
                new Vector3(-halfWidth, height,  halfLength),
                new Vector3( halfWidth, height,  halfLength)
            };
            int[] triangles = new int[] { 0, 2, 1, 1, 2, 3 };
            Vector2[] uv = new Vector2[] { new Vector2(0,0), new Vector2(1,0), new Vector2(0,1), new Vector2(1,1) };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            filter.sharedMesh = mesh;

            Material material = new Material(Shader.Find("Standard"));
            material.color = color;
            renderer.sharedMaterial = material;
        }

        private void CreateLaneMarker(Vector3 center, Quaternion rotation, Transform parent, float halfLength, float markWidth, float height, Color color)
        {
            GameObject go = new GameObject("Highway-Lane-Mark");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.rotation = rotation;

            MeshFilter filter = go.AddComponent<MeshFilter>();
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(-markWidth * 0.5f, height, -halfLength),
                new Vector3( markWidth * 0.5f, height, -halfLength),
                new Vector3(-markWidth * 0.5f, height,  halfLength),
                new Vector3( markWidth * 0.5f, height,  halfLength)
            };
            int[] triangles = new int[] { 0, 2, 1, 1, 2, 3 };
            Vector2[] uv = new Vector2[] { new Vector2(0,0), new Vector2(1,0), new Vector2(0,1), new Vector2(1,1) };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            filter.sharedMesh = mesh;

            Material material = new Material(Shader.Find("Sprites/Default"));
            material.color = color;
            renderer.sharedMaterial = material;
        }

        private string BuildEdgeKey(int a, int b)
        {
            if (a < b) return a + "_" + b;
            return b + "_" + a;
        }

        private int ConnectSatelliteNetwork(
            List<Vector3> satelliteCenters,
            SatelliteConnectionMode mode,
            int maxNeighborLinks,
            bool closeLoop,
            float connectionStepOverride,
            List<CityNetworkLink> generatedLinks = null,
            int nodeIndexOffset = 0,
            bool generateHighways = true,
            float highwayWidth = 18f,
            float highwayThickness = 0.2f)
        {
            if (satelliteCenters == null || satelliteCenters.Count == 0)
                return 0;

            int createdSegments = 0;

            if (mode == SatelliteConnectionMode.MainOnly)
                return 0;

            if (mode == SatelliteConnectionMode.Chain)
            {
                for (int i = 1; i < satelliteCenters.Count; i++)
                {
                    createdSegments += ConnectAndTrack(
                        satelliteCenters[i - 1],
                        satelliteCenters[i],
                        connectionStepOverride,
                        i - 1 + nodeIndexOffset,
                        i + nodeIndexOffset,
                        generatedLinks,
                        false,
                        generateHighways,
                        highwayWidth,
                        highwayThickness);
                }

                if (closeLoop && satelliteCenters.Count > 2)
                    createdSegments += ConnectAndTrack(
                        satelliteCenters[satelliteCenters.Count - 1],
                        satelliteCenters[0],
                        connectionStepOverride,
                        satelliteCenters.Count - 1 + nodeIndexOffset,
                        nodeIndexOffset,
                        generatedLinks,
                        false,
                        generateHighways,
                        highwayWidth,
                        highwayThickness);

                return createdSegments;
            }

            if (mode == SatelliteConnectionMode.FullMesh)
            {
                for (int i = 0; i < satelliteCenters.Count; i++)
                {
                    for (int j = i + 1; j < satelliteCenters.Count; j++)
                    {
                        createdSegments += ConnectAndTrack(
                            satelliteCenters[i],
                            satelliteCenters[j],
                            connectionStepOverride,
                            i + nodeIndexOffset,
                            j + nodeIndexOffset,
                            generatedLinks,
                            false,
                            generateHighways,
                            highwayWidth,
                            highwayThickness);
                    }
                }
                return createdSegments;
            }

            // Nearest mode
            int n = satelliteCenters.Count;
            int links = Mathf.Clamp(maxNeighborLinks, 1, Mathf.Max(1, n - 1));
            HashSet<string> connected = new HashSet<string>();

            for (int i = 0; i < n; i++)
            {
                List<int> neighbors = new List<int>(n - 1);
                for (int j = 0; j < n; j++)
                {
                    if (i == j) continue;
                    neighbors.Add(j);
                }

                neighbors.Sort((a, b) =>
                {
                    float da = Vector3.SqrMagnitude(satelliteCenters[i] - satelliteCenters[a]);
                    float db = Vector3.SqrMagnitude(satelliteCenters[i] - satelliteCenters[b]);
                    return da.CompareTo(db);
                });

                int toConnect = Mathf.Min(links, neighbors.Count);
                for (int k = 0; k < toConnect; k++)
                {
                    int j = neighbors[k];
                    string key = BuildEdgeKey(i, j);
                    if (connected.Contains(key)) continue;

                    connected.Add(key);
                    createdSegments += ConnectAndTrack(
                        satelliteCenters[i],
                        satelliteCenters[j],
                        connectionStepOverride,
                        i + nodeIndexOffset,
                        j + nodeIndexOffset,
                        generatedLinks,
                        false,
                        generateHighways,
                        highwayWidth,
                        highwayThickness);
                }
            }

            if (closeLoop && satelliteCenters.Count > 2)
                createdSegments += ConnectAndTrack(
                    satelliteCenters[satelliteCenters.Count - 1],
                    satelliteCenters[0],
                    connectionStepOverride,
                    satelliteCenters.Count - 1 + nodeIndexOffset,
                    nodeIndexOffset,
                    generatedLinks,
                    false,
                    generateHighways,
                    highwayWidth,
                    highwayThickness);

            return createdSegments;
        }

        private bool GenerateStreetsVerySmall(bool borderFlat = false, bool withSatteliteCity = false, bool satteliteCity = false, float satteliteCityPositionX = 0, float satteliteCityPositionZ = 0, Transform mainCityExitPosition = null)
        {

            if (satteliteCity && !cityMaker)
                satteliteCity = false;

            if (!satteliteCity)
            {
                ClearCity();
                cityMaker = new GameObject("City-Maker");
            }

            GameObject block;

            if (!satteliteCity)
                distCenter = 150;

            int nb = 0;

            int le = largeBlocks.Length;
            nb = Random.Range(0, le);

            Transform baseExit = (mainCityExitPosition != null) ? mainCityExitPosition : CityExitPosition();
            Vector3 satellitePosition = (baseExit != null) ? GetSatellitePositionFromExit(baseExit, satteliteCityPositionX, satteliteCityPositionZ) : new Vector3(satteliteCityPositionX, 0, satteliteCityPositionZ);

            if (satteliteCity && smallBorderWithExitOfCity.Length > 0)
                block = (GameObject)Instantiate(largeBlocks[nb], satellitePosition, Quaternion.Euler(0, 0, 0), cityMaker.transform);
            else
                block = (GameObject)Instantiate(largeBlocks[nb], new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0), cityMaker.transform);


            if ((withSatteliteCity || satteliteCity) && miniBorderWithExitOfCity.Length > 0)
            {
                if (satteliteCity)
                    block = InstantiateRandomPrefab(miniBorderWithExitOfCity, satellitePosition, Quaternion.Euler(0, 180, 0), cityMaker.transform, "miniBorderWithExitOfCity");
                else
                    block = InstantiateRandomPrefab(miniBorderWithExitOfCity, new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0), cityMaker.transform, "miniBorderWithExitOfCity");
            }
            else
            {
                if(borderFlat)
                    block = InstantiateRandomPrefab(miniBorderFlat, new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0), cityMaker.transform, "miniBorderFlat");
                else
                    block = InstantiateRandomPrefab(miniBorder, new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0), cityMaker.transform, "miniBorder");
            }

            if (!block) return false;
            block.transform.SetParent(cityMaker.transform);

            return (withSatteliteCity && miniBorderWithExitOfCity.Length > 0);

        }

        private bool GenerateStreetsSmall(bool borderFlat = false, bool withSatteliteCity = false, bool satteliteCity = false, float satteliteCityPositionX = 0, float satteliteCityPositionZ = 0, Transform mainCityExitPosition = null)
        {

            if (satteliteCity && !cityMaker)
                satteliteCity = false;

            if (!satteliteCity)
            {
                ClearCity();

                cityMaker = new GameObject("City-Maker");

            }

            if (!satteliteCity)
                distCenter = 200;

            int nb = 0;

            int le = largeBlocks.Length;
            _largeBlocks = new bool[largeBlocks.Length];

            //Position and Rotation
            Vector3[] ps = new Vector3[3];

            int[] rt = new int[3];

            Transform baseExit = (mainCityExitPosition != null) ? mainCityExitPosition : CityExitPosition();
            Vector3 satellitePosition = (baseExit != null) ? GetSatellitePositionFromExit(baseExit, satteliteCityPositionX, satteliteCityPositionZ) : new Vector3(satteliteCityPositionX, 0, satteliteCityPositionZ);

            float s = Random.Range(0, 6f);

            if (s < 3)
            {
                ps[1] = new Vector3(0, 0, 0); rt[1] = 0;
                ps[2] = new Vector3(0, 0, 300); rt[2] = 0;
            }
            else
            {
                ps[1] = new Vector3(-150, 0, 150); rt[1] = 90;
                ps[2] = new Vector3(150, 0, 150); rt[2] = 90;
            }


            for (int qt = 1; qt < 3; qt++)
            {

                for (int lp = 0; lp < 100; lp++)
                {
                    nb = Random.Range(0, le);
                    if (!_largeBlocks[nb]) break;
                }
                _largeBlocks[nb] = true;

                if (satteliteCity && smallBorderWithExitOfCity.Length > 0)
                    Instantiate(largeBlocks[nb], ps[qt] + ((baseExit != null) ? baseExit.position : Vector3.zero) + new Vector3(-0, 0, -1516) - new Vector3(0, 0, 300), Quaternion.Euler(0, rt[qt] + 180, 0), cityMaker.transform);
                else
                    Instantiate(largeBlocks[nb], ps[qt], Quaternion.Euler(0, rt[qt], 0), cityMaker.transform);

            }


            GameObject block;

            if ((withSatteliteCity || satteliteCity) && smallBorderWithExitOfCity.Length > 0)
            {
                if (satteliteCity)
                    block = InstantiateRandomPrefab(smallBorderWithExitOfCity, satellitePosition, Quaternion.Euler(0, 180, 0), cityMaker.transform, "smallBorderWithExitOfCity");
                else
                    block = InstantiateRandomPrefab(smallBorderWithExitOfCity, new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0), cityMaker.transform, "smallBorderWithExitOfCity");

            }
            else
            {
                if (borderFlat)
                    block = InstantiateRandomPrefab(smallBorderFlat, new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0), cityMaker.transform, "smallBorderFlat");
                else
                    block = InstantiateRandomPrefab(smallBorder, new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0), cityMaker.transform, "smallBorder");
            }

            if (!block) return false;
            block.transform.SetParent(cityMaker.transform);

            return (withSatteliteCity && smallBorderWithExitOfCity.Length > 0);

        }



        private bool GenerateStreets(bool borderFlat = false, bool withSatteliteCity = false, bool satteliteCity = false, float satteliteCityPositionX = 0, float satteliteCityPositionZ = 0, Transform mainCityExitPosition = null)
        {

            if (satteliteCity && !cityMaker)
                satteliteCity = false;

            if (!satteliteCity)
            {

                ClearCity();

                cityMaker = new GameObject("City-Maker");
            }

            if (!satteliteCity)
                distCenter = 300;

            int nb = 0;

            int le = largeBlocks.Length;
            _largeBlocks = new bool[largeBlocks.Length];

            //Position and Rotation
            Vector3[] ps = new Vector3[5];

            int[] rt = new int[5];

            Transform baseExit = (mainCityExitPosition != null) ? mainCityExitPosition : CityExitPosition();
            Vector3 satellitePosition = (baseExit != null) ? GetSatellitePositionFromExit(baseExit, satteliteCityPositionX, satteliteCityPositionZ) : new Vector3(satteliteCityPositionX, 0, satteliteCityPositionZ);

            float s = Random.Range(0, 6f);

            if (s < 2)
            {

                ps[1] = new Vector3(0, 0, 0); rt[1] = 0;
                ps[2] = new Vector3(0, 0, 300); rt[2] = 0;
                ps[3] = new Vector3(450, 0, 150); rt[3] = 90;
                ps[4] = new Vector3(-450, 0, 150); rt[4] = 90;

            }
            else if (s < 3)
            {

                ps[1] = new Vector3(-450, 0, 150); rt[1] = 90;
                ps[2] = new Vector3(-150, 0, 150); rt[2] = 90;
                ps[3] = new Vector3(150, 0, 150); rt[3] = 90;
                ps[4] = new Vector3(450, 0, 150); rt[4] = 90;

            }
            else if (s < 4)
            {

                ps[1] = new Vector3(-450, 0, 150); rt[1] = 90;
                ps[2] = new Vector3(-150, 0, 150); rt[2] = 90;
                ps[3] = new Vector3(300, 0, 0); rt[3] = 0;
                ps[4] = new Vector3(300, 0, 300); rt[4] = 0;

            }
            else
            {

                ps[1] = new Vector3(450, 0, 150); rt[1] = 90;
                ps[2] = new Vector3(150, 0, 150); rt[2] = 90;
                ps[3] = new Vector3(-300, 0, 0); rt[3] = 0;
                ps[4] = new Vector3(-300, 0, 300); rt[4] = 0;

            }


            for (int qt = 1; qt < 5; qt++)
            {

                for (int lp = 0; lp < 100; lp++)
                {
                    nb = Random.Range(0, le);
                    if (!_largeBlocks[nb]) break;
                }
                _largeBlocks[nb] = true;

                Instantiate(largeBlocks[nb], ps[qt], Quaternion.Euler(0, rt[qt], 0), cityMaker.transform);

            }


            GameObject block;

            if ((withSatteliteCity || satteliteCity) && mediumBorderWithExitOfCity.Length > 0)
                block = InstantiateRandomPrefab(mediumBorderWithExitOfCity, satteliteCity ? satellitePosition : new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0), cityMaker.transform, "mediumBorderWithExitOfCity");
            else
            {
                if(borderFlat)
                    block = InstantiateRandomPrefab(mediumBorderFlat, new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0), cityMaker.transform, "mediumBorderFlat");
                else
                    block = InstantiateRandomPrefab(mediumBorder, new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0), cityMaker.transform, "mediumBorder");
            }
                

            if (!block) return false;
            block.transform.SetParent(cityMaker.transform);

            return (withSatteliteCity && mediumBorderWithExitOfCity.Length > 0);

        }


        private bool GenerateStreetsBig(bool borderFlat = false, bool withSatteliteCity = false, bool satteliteCity = false, float satteliteCityPositionX = 0, float satteliteCityPositionZ = 0, Transform mainCityExitPosition = null)
        {

            if (satteliteCity && !cityMaker)
                satteliteCity = false;

            if (!satteliteCity)
            {

                ClearCity();

                cityMaker = new GameObject("City-Maker");
            }

            distCenter = 350;

            int nb = 0;

            int le = largeBlocks.Length;
            int lebig = (bigLargeBlocks != null) ? bigLargeBlocks.Length : 0;

            _largeBlocks = new bool[largeBlocks.Length];
            _bigLargeBlocks = new bool[(bigLargeBlocks != null) ? bigLargeBlocks.Length : 0];

            //Position 
            Vector3[] ps = new Vector3[7];

            //Rotation
            int[] rt = new int[7];

            //Type
            int[] tb = new int[7];   // 1->Large - 2->BigLarge

            int qt;

            Transform baseExit = (mainCityExitPosition != null) ? mainCityExitPosition : CityExitPosition();
            Vector3 satellitePosition = (baseExit != null) ? GetSatellitePositionFromExit(baseExit, satteliteCityPositionX, satteliteCityPositionZ) : new Vector3(satteliteCityPositionX, 0, satteliteCityPositionZ);

            float s = Random.Range(0, 7f);


            if (s < 3)
            {
                qt = 6;

                ps[1] = new Vector3(0, 0, 0); rt[1] = 0; tb[1] = 1;
                ps[2] = new Vector3(0, 0, 300); rt[2] = 0; tb[2] = 1;
                ps[3] = new Vector3(450, 0, 150); rt[3] = 90; tb[3] = 1;
                ps[4] = new Vector3(-450, 0, 150); rt[4] = 90; tb[4] = 1;
                ps[5] = new Vector3(-300, 0, 600); rt[5] = 0; tb[5] = 1;
                ps[6] = new Vector3(300, 0, 600); rt[6] = 0; tb[6] = 1;


            }
            else if (s < 3.5f)
            {
                qt = 6;
                ps[1] = new Vector3(-450, 0, 150); rt[1] = 90; tb[1] = 1;
                ps[2] = new Vector3(-150, 0, 150); rt[2] = 90; tb[2] = 1;
                ps[3] = new Vector3(150, 0, 150); rt[3] = 90; tb[3] = 1;
                ps[4] = new Vector3(450, 0, 150); rt[4] = 90; tb[4] = 1;
                ps[5] = new Vector3(-300, 0, 600); rt[5] = 0; tb[5] = 1;
                ps[6] = new Vector3(300, 0, 600); rt[6] = 0; tb[6] = 1;

            }
            else if (s < 4)
            {
                qt = 6;
                ps[1] = new Vector3(-300, 0, 300); rt[1] = 0; tb[1] = 1;
                ps[2] = new Vector3(-300, 0, 0); rt[2] = 0; tb[2] = 1;
                ps[3] = new Vector3(150, 0, 150); rt[3] = 90; tb[3] = 1;
                ps[4] = new Vector3(450, 0, 150); rt[4] = 90; tb[4] = 1;
                ps[5] = new Vector3(-300, 0, 600); rt[5] = 0; tb[5] = 1;
                ps[6] = new Vector3(300, 0, 600); rt[6] = 0; tb[6] = 1;


            }
            else if (s < 5)
            {
                qt = 5;
                ps[1] = new Vector3(-300, 0, 0); rt[1] = 0; tb[1] = 1;
                ps[2] = new Vector3(300, 0, 0); rt[2] = 0; tb[2] = 1;
                ps[3] = new Vector3(-300, 0, 600); rt[3] = 0; tb[3] = 1;
                ps[4] = new Vector3(300, 0, 600); rt[4] = 0; tb[4] = 1;
                ps[5] = new Vector3(0, 0, 300); rt[5] = 0; tb[5] = 2;



            }
            else
            {
                qt = 6;
                ps[1] = new Vector3(-450, 0, 150); rt[1] = 90; tb[1] = 1;
                ps[2] = new Vector3(300, 0, 0); rt[2] = 0; tb[2] = 1;
                ps[3] = new Vector3(-150, 0, 150); rt[3] = 90; tb[3] = 1;
                ps[4] = new Vector3(450, 0, 450); rt[4] = 90; tb[4] = 1;
                ps[5] = new Vector3(-300, 0, 600); rt[5] = 0; tb[5] = 1;
                ps[6] = new Vector3(150, 0, 450); rt[6] = 90; tb[6] = 1;

            }


            for (int count = 1; count <= qt; count++)
            {

                if (tb[count] == 1)
                {
                    for (int lp = 0; lp < 100; lp++)
                    {
                        nb = Random.Range(0, le);
                        if (!_largeBlocks[nb]) break;
                    }
                    _largeBlocks[nb] = true;

                    Instantiate(largeBlocks[nb], ps[count], Quaternion.Euler(0, rt[count], 0), cityMaker.transform);
                }
                else if (tb[count] == 2)
                {
                    if (lebig == 0)
                    {
                        // Fallback to a regular large block if big blocks are not configured.
                        for (int lp = 0; lp < 100; lp++)
                        {
                            nb = Random.Range(0, le);
                            if (!_largeBlocks[nb]) break;
                        }
                        _largeBlocks[nb] = true;
                        Instantiate(largeBlocks[nb], ps[count], Quaternion.Euler(0, rt[count], 0), cityMaker.transform);
                        continue;
                    }

                    for (int lp = 0; lp < 100; lp++)
                    {
                        nb = Random.Range(0, lebig);
                        if (!_bigLargeBlocks[nb]) break;
                    }
                    _bigLargeBlocks[nb] = true;

                    Instantiate(bigLargeBlocks[nb], ps[count], Quaternion.Euler(0, rt[count], 0), cityMaker.transform);
                }

            }


            GameObject block;

            if ((withSatteliteCity || satteliteCity) && largeBorderWithExitOfCity.Length > 0)
                block = InstantiateRandomPrefab(largeBorderWithExitOfCity, satteliteCity ? satellitePosition : new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0), cityMaker.transform, "largeBorderWithExitOfCity");
            else
            {
                if(borderFlat)
                    block = InstantiateRandomPrefab(largeBorderFlat, new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0), cityMaker.transform, "largeBorderFlat");
                else
                    block = InstantiateRandomPrefab(largeBorder, new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0), cityMaker.transform, "largeBorder");

            }
                

            if (!block) return false;
            block.transform.SetParent(cityMaker.transform);

            return (withSatteliteCity && largeBorderWithExitOfCity.Length > 0);

        }

        private GameObject pB;

        public void GenerateAllBuildings(bool _withDowntownArea, float _downTownSize)
        {
            if (!ValidateBuildingPrefabPools(out string prefabError))
            {
                Debug.LogError("CityGenerator: cannot generate buildings. " + prefabError + " Load building prefabs first.");
                return;
            }

            downTownSize = _downTownSize;

            withDowntownArea = _withDowntownArea;

            if (withDowntownArea)
            {
                GameObject[] tArray = GameObject.FindObjectsOfType(typeof(GameObject)).Select(g => g as GameObject).Where(g => g.name == ("Marcador")).ToArray();

                if (tArray.Length == 1)
                    center = tArray[0].transform.position;
                else
                    center = tArray[Random.Range(1, tArray.Length - 1)].transform.position;

                if (GameObject.Find("DownTownPosition") && Random.Range(1, 10) < 5)
                    center = GameObject.Find("DownTownPosition").transform.position;


            }


            _BB = new int[BB.Length];
            _BC = new int[BC.Length];
            _BR = new int[BR.Length];
            //_DC = new int[DC.Length];
            _EB = new int[EB.Length];
            _EC = new int[EC.Length];
            _MB = new int[MB.Length];
            _BK = new int[BK.Length];
            _SB = new int[SB.Length];

            _EBS = new int[EB.Length];
            _ECS = new int[EC.Length];


            _BBS = new int[BBS.Length];
            _BCS = new int[BCS.Length];

            residential = 0;

            DestroyBuildings();

            GameObject pB = new GameObject();

            nB = 0;

            CreateBuildingsInSuperBlocks();
            CreateBuildingsInBlocks();
            CreateBuildingsInLines();
            CreateBuildingsInDouble();



            Debug.ClearDeveloperConsole();
            Debug.Log(nB + " buildings were created");


            DestroyImmediate(pB);

            DayNight dayNight = FindObjectOfType<DayNight>();
            if(dayNight)
            {
                dayNight.ChangeMaterial();
            }



        }



        public void CreateBuildingsInLines()
        {


            tempArray = GameObject.FindObjectsOfType(typeof(GameObject)).Select(g => g as GameObject).Where(g => g.name == ("Marcador")).ToArray();

            foreach (GameObject lines in tempArray)
            {

                _residential = (residential < 15 && Vector3.Distance(center, lines.transform.position) > 400 && Random.Range(0, 100) < 30);

                foreach (Transform child in lines.transform)
                {

                    if (child.name == "E")
                        CreateBuildingsInCorners(child.gameObject);
                    else if (child.name == "EL")
                    {
                        int ct = 0;
                        do
                        {
                            ct++;
                            if (CreateBuildingsInCorners(child.gameObject, true))
                                break;

                        } while (ct < 300);

                    }
                    else if (child.name.Substring(0, 1) == "S")
                        CreateBuildingsInLine(child.gameObject, 90f, true);
                    else
                        CreateBuildingsInLine(child.gameObject, 90f);

                }

                _residential = false;


            }

        }

        public bool CreateBuildingsInCorners(GameObject child, bool notAnyone = false)
        {

            GameObject pBuilding;

            pB = null;
            int numB = 0;
            int t = 0;
            float pWidth = 0;
            float wComprimento;

            float pScale;
            float remainingMeters;
            GameObject newMarcador;

            float distancia = Vector3.Distance(center, child.transform.position);

            int lp;
            lp = 0;
            int lt = 0;

            float _distCenter = distCenter * (Mathf.Clamp(downTownSize, 50, 200) / 100);

            while (t < 100)
            {

                t++;

                if (distancia < _distCenter && withDowntownArea)
                {

                    do
                    {
                        lp++;
                        lt = 0;
                        do
                        {
                            lt++;
                            numB = Random.Range(0, EC.Length);
                        } while (notAnyone && _ECS[numB] > 0 && lt < 2000);

                        if (_EC[numB] == 0) break;
                        if (lp > 100 && _EC[numB] <= 1) break;
                        if (lp > 150 && _EC[numB] <= 2) break;
                        if (lp > 200 && _EC[numB] <= 3) break;
                        if (lp > 250) break;

                    } while (lp < 300);

                    pWidth = GetWith(EC[numB]);

                    if (pWidth <= 0)
                    {
                        Debug.LogWarning("Error: EC: " + numB);
                        _EC[numB] = 100;
                        return false;
                    }
                    else if (pWidth <= 36.3f)
                    {
                        _EC[numB] += 1;
                        pB = EC[numB];
                        break;
                    }

                }
                else
                {

                    do
                    {
                        lp++;
                        do
                        {
                            lt++;
                            numB = Random.Range(0, EB.Length);
                        } while (notAnyone && _EBS[numB] >= 100 && lt < 2000);

                        if (_EB[numB] == 0) break;
                        if (lp > 100 && _EB[numB] <= 1) break;
                        if (lp > 150 && _EB[numB] <= 2) break;
                        if (lp > 200 && _EB[numB] <= 3) break;
                        if (lp > 250) break;

                    } while (lp < 300);


                    pWidth = GetWith(EB[numB]);

                    if (pWidth <= 0)
                    {
                        Debug.LogWarning("Error: EB: " + numB);
                        _EB[numB] = 100;
                        return false;
                    }
                    else if (pWidth <= 36.3f)
                    {
                        _EB[numB] += 1;
                        pB = EB[numB];
                        break;
                    }

                }



            }



            pBuilding = InstantiateIfAllowed(pB, new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0), null);

            if (pBuilding == null)
            {
                return false;
            }

            if (notAnyone && !TestBaseBuildindCornerOnTheSlope(pBuilding.transform))
            {

                if (distancia < _distCenter && withDowntownArea)
                {
                    _ECS[numB] = 100;
                    _EC[numB] -= 1;
                }
                else
                {
                    _EBS[numB] = 100;
                    _EB[numB] -= 1;
                }


                DestroyImmediate(pBuilding);

                return false;
            }

            pBuilding.name = pBuilding.name;
            pBuilding.transform.SetParent(child.transform);
            pBuilding.transform.localPosition = new Vector3(-(pWidth * 0.5f), 0, 0);
            pBuilding.transform.localRotation = Quaternion.Euler(0, 0, 0);

            nB++;

            // Check space behind the corner building -------------------------------------------------------------------------------------------------------------------
            wComprimento = GetHeight(pB);
            if (wComprimento < 29.9f)
            {

                newMarcador = new GameObject("Marcador");

                newMarcador.transform.SetParent(child.transform);
                newMarcador.transform.localPosition = new Vector3(0, 0, -36);
                newMarcador.transform.localRotation = Quaternion.Euler(0, 0, 0);
                newMarcador.name = (36 - wComprimento).ToString();
                CreateBuildingsInLine(newMarcador, 90);

            }
            else
            {
                remainingMeters = 36 - wComprimento;
                pScale = 1 + (remainingMeters / wComprimento);
                pBuilding.transform.localScale = new Vector3(1, 1, pScale);

            }


            // Check space on the corner building -------------------------------------------------------------------------------------------------------------------


            if (pWidth < 29.9f)
            {

                newMarcador = new GameObject("Marcador");



                newMarcador.transform.SetParent(child.transform);
                newMarcador.transform.localPosition = new Vector3(-pWidth, 0, 0);
                newMarcador.transform.localRotation = Quaternion.Euler(0, 270, 0);
                newMarcador.name = (36 - pWidth).ToString();
                CreateBuildingsInLine(newMarcador, 90);

            }
            else
            {

                remainingMeters = 36 - pWidth;
                pScale = 1 + (remainingMeters / pWidth);
                pBuilding.transform.localScale = new Vector3(pScale, 1, 1);

            }

            return true;

        }

        bool TestBaseBuildindCornerOnTheSlope(Transform buildingCornerOnTheSlope)
        {
            return (buildingCornerOnTheSlope.Find("Base-Corner-0-Collider") || buildingCornerOnTheSlope.Find("Base-Corner-03-Collider") || buildingCornerOnTheSlope.Find("Base-Corner-06-Collider"));
        }

        int RandRotation()
        {
            int r = 0;
            int i = Random.Range(0, 4);
            if (i == 3) r = 180;
            else if (i == 2) r = 90;
            else if (i == 1) r = 270;
            else r = 0;

            return r;


        }


        public void CreateBuildingsInBlocks()
        {

            int numB = 0;

            tempArray = GameObject.FindObjectsOfType(typeof(GameObject)).Select(g => g as GameObject).Where(g => g.name == ("Blocks")).ToArray();

            foreach (GameObject bks in tempArray)
            {

                foreach (Transform bk in bks.transform)
                {

                    if (Random.Range(0, 20) > 5)
                    {

                        int lp = 0;
                        do
                        {
                            lp++;
                            numB = Random.Range(0, BK.Length);
                            if (_BK[numB] == 0) break;
                            if (lp > 125 && _BK[numB] <= 1) break;
                            if (lp > 150 && _BK[numB] <= 2) break;
                            if (lp > 200 && _BK[numB] <= 3) break;
                            if (lp > 250) break;
                        } while (lp < 300);

                        _BK[numB] += 1;

                        GameObject gbk = InstantiateIfAllowed(BK[numB], bk.position, bk.rotation, bk);
                        if (gbk != null) nB++;

                    }
                    else
                    {

                        for (int i = 1; i <= 4; i++)
                        {
                            GameObject nc = new GameObject("E");
                            nc.transform.SetParent(bk);
                            if (i == 1)
                            {
                                nc.transform.localPosition = new Vector3(-36, 0, -36);
                                nc.transform.localRotation = Quaternion.Euler(0, 180, 0);
                            }
                            if (i == 2)
                            {
                                nc.transform.localPosition = new Vector3(-36, 0, 36);
                                nc.transform.localRotation = Quaternion.Euler(0, 270, 0);
                            }
                            if (i == 3)
                            {
                                nc.transform.localPosition = new Vector3(36, 0, 36);
                                nc.transform.localRotation = Quaternion.Euler(0, 0, 0);
                            }
                            if (i == 4)
                            {
                                nc.transform.localPosition = new Vector3(36, 0, -36);
                                nc.transform.localRotation = Quaternion.Euler(0, 90, 0);
                            }
                            CreateBuildingsInCorners(nc);

                        }
                    }


                }

            }

        }

        public void CreateBuildingsInSuperBlocks()
        {

            int numB = 0;

            tempArray = GameObject.FindObjectsOfType(typeof(GameObject)).Select(g => g as GameObject).Where(g => g.name == ("SuperBlocks")).ToArray();

            foreach (GameObject bks in tempArray)
            {

                foreach (Transform bk in bks.transform)
                {


                    int lp = 0;
                    do
                    {
                        lp++;
                        numB = Random.Range(0, SB.Length);
                        if (_SB[numB] == 0) break;
                        if (lp > 125 && _SB[numB] <= 1) break;
                        if (lp > 150 && _SB[numB] <= 2) break;
                        if (lp > 200 && _SB[numB] <= 3) break;
                        if (lp > 250) break;
                    } while (lp < 300);

                    _SB[numB] += 1;

                    GameObject gsb = InstantiateIfAllowed(SB[numB], bk.position, bk.rotation, bk);
                    if (gsb != null) nB++;



                }

            }

        }

        private void CreateBuildingsInLine(GameObject line, float angulo, bool slope = false)
        {


            int index = -1;
            GameObject[] pBuilding;
            pBuilding = new GameObject[50];

            float limit;
            string _name = line.name;

            _name = (slope) ? line.name.Substring(1) : line.name;

            if (_name.Contains("."))
                limit = float.Parse(_name.Split('.')[0]) + float.Parse(_name.Split('.')[1]) / float.Parse("1" + "0000000".Substring(0, _name.Split('.')[1].Length));
            else
                limit = float.Parse(_name);

            float init = 0;
            float pWidth = 0;

            int tt = 0;
            int t;

            int lp;


            float distancia = Vector3.Distance(center, line.transform.position);

            float _distCenter = distCenter * (Mathf.Clamp(downTownSize, 50, 200) / 100);

            while (tt < 100)
            {

                tt++;
                t = 0;


                lp = 0;
                while (t < 200 && init <= limit - 4)
                {

                    t++;

                    if (slope)
                    {
                        if (distancia < _distCenter && withDowntownArea)
                        {
                            do
                            {
                                lp++;
                                numB = Random.Range(0, BCS.Length);
                                if (_BCS[numB] == 0) break;
                                if (lp > 125 && _BCS[numB] <= 1) break;
                                if (lp > 150 && _BCS[numB] <= 2) break;
                                if (lp > 200 && _BCS[numB] <= 3) break;
                                if (lp > 250) break;

                            } while (lp < 300);

                            pWidth = GetWith(BCS[numB]);

                            if (pWidth > 0)
                                if ((init + pWidth) <= (limit + 4))
                                {
                                    pB = BCS[numB];
                                    _BCS[numB] += 1;
                                    break;
                                }
                        }
                        else
                        {

                            do
                            {
                                lp++;
                                numB = Random.Range(0, BBS.Length);
                                if (_BBS[numB] == 0) break;
                                if (lp > 125 && _BBS[numB] <= 1) break;
                                if (lp > 150 && _BBS[numB] <= 2) break;
                                if (lp > 200 && _BBS[numB] <= 3) break;
                                if (lp > 250) break;

                            } while (lp < 300);

                            pWidth = GetWith(BBS[numB]);

                            if (pWidth > 0)
                                if ((init + pWidth) <= (limit + 4))
                                {
                                    pB = BBS[numB];
                                    _BBS[numB] += 1;
                                    break;
                                }

                        }

                    }
                    else if (distancia < _distCenter && withDowntownArea)
                    {

                        do
                        {
                            lp++;
                            numB = Random.Range(0, BC.Length);
                            if (_BC[numB] == 0) break;
                            if (lp > 125 && _BC[numB] <= 1) break;
                            if (lp > 150 && _BC[numB] <= 2) break;
                            if (lp > 200 && _BC[numB] <= 3) break;
                            if (lp > 250) break;

                        } while (lp < 300);

                        pWidth = GetWith(BC[numB]);

                        if (pWidth > 0)
                            if ((init + pWidth) <= (limit + 4))
                            {
                                pB = BC[numB];
                                _BC[numB] += 1;
                                break;
                            }

                    }
                    else if (_residential)
                    {

                        do
                        {
                            lp++;
                            numB = Random.Range(0, BR.Length);
                            if (_BR[numB] == 0) break;
                            if (lp > 100 && _BR[numB] <= 1) break;
                            if (lp > 150 && _BR[numB] <= 2) break;
                            if (lp > 200 && _BR[numB] <= 3) break;
                            if (lp > 250) break;
                        } while (lp < 300);

                        pWidth = GetWith(BR[numB]);

                        if (pWidth <= 0) { Debug.LogWarning("Error: BR: " + numB); _BR[numB] += 1; }
                        else
                        if ((init + pWidth) <= (limit + 4))
                        {
                            pB = BR[numB];
                            _BR[numB] += 1;
                            residential += 1;
                            break;
                        }
                    }
                    else
                    {

                        do
                        {
                            lp++;
                            numB = Random.Range(0, BB.Length);
                            if (_BB[numB] == 0) break;
                            if (lp > 100 && _BB[numB] <= 1) break;
                            if (lp > 150 && _BB[numB] <= 2) break;
                            if (lp > 200 && _BB[numB] <= 3) break;
                            if (lp > 250) break;
                        } while (lp < 300);

                        pWidth = GetWith(BB[numB]);

                        if (pWidth <= 0) { Debug.LogWarning("Error: BB: " + numB); _BB[numB] += 1; }
                        if ((init + pWidth) <= (limit + 4))
                        {
                            pB = BB[numB];
                            _BB[numB] += 1;
                            break;
                        }

                    }


                }


                if (t >= 200 || init > limit - 4)
                {
                    // Didn't find one that fits in the remaining space

                    AdjustsWidth(pBuilding, index + 1, limit - init, 0, slope);
                    break;

                }
                else
                {

                    GameObject tempB = InstantiateIfAllowed(pB, new Vector3(0, 0, init + (pWidth * 0.5f)), Quaternion.Euler(0, angulo, 0), line.transform);
                    if (tempB != null)
                    {
                        index++;
                        nB++;

                        pBuilding[index] = tempB;
                        pBuilding[index].transform.SetParent(line.transform);

                        pBuilding[index].transform.localPosition = new Vector3(0, 0, init + (pWidth * 0.5f));
                        pBuilding[index].transform.localRotation = Quaternion.Euler(0, angulo, 0);
                    }

                    init += pWidth;

                    if (init > limit - 6)
                    {
                        AdjustsWidth(pBuilding, index + 1, limit - init, 0, slope);
                        break;
                    }

                }



            }



        }


        private float GetY(Transform pos, float width)
        {

            RaycastHit hit;

            Vector3 pp = pos.transform.position + pos.transform.forward * 2 + pos.transform.up * 20;

            float l = 20;
            float r = 20;

            if (Physics.Raycast(pp + pos.transform.right * width, Vector3.down, out hit, 40))
                r = hit.distance;

            if (Physics.Raycast(pp - (pos.transform.right * width), Vector3.down, out hit, 40))
                l = hit.distance;

            return (pos.transform.localPosition.y + 20) - ((r < l) ? r : l);

        }

        private void CreateBuildingsInDoubleLine(GameObject line)
        {

            int index = -1;
            GameObject[] pBuilding;
            pBuilding = new GameObject[20];

            float limit;
            string _name = line.name;

            if (_name.Contains("."))
                limit = float.Parse(_name.Split('.')[0]) + float.Parse(_name.Split('.')[1]) / float.Parse("1" + "0000000".Substring(0, _name.Split('.')[1].Length));
            else
                limit = float.Parse(_name);


            float init = 0;
            float pWidth = 0;

            int tt = 0;
            int t;
            int lp;

            while (tt < 100)
            {

                tt++;
                t = 0;

                lp = 0;

                while (t < 200 && init <= limit - 4)
                {

                    t++;

                    do
                    {
                        lp++;
                        numB = Random.Range(0, MB.Length);
                        if (_MB[numB] == 0) break;
                        if (lp > 100 && _MB[numB] <= 1) break;
                        if (lp > 150 && _MB[numB] <= 2) break;
                        if (lp > 200) break;
                    } while (lp < 300);

                    pWidth = GetWith(MB[numB]);


                    if (pWidth <= 0) { Debug.LogWarning("Error: MB: " + numB); _MB[numB] += 1; }
                    else
                    if ((init + pWidth) <= (limit + 4))
                    {
                        _MB[numB] += 1;
                        break;
                    }

                }

                if (t >= 200 || init > limit - 4)
                {
                    AdjustsWidth(pBuilding, index + 1, (limit - init), 0);
                    break;

                }
                else
                {

                    index++;

                    GameObject tempMB = InstantiateIfAllowed(MB[numB], new Vector3(0, 0, 0), Quaternion.Euler(0, 90, 0), line.transform);
                    if (tempMB != null)
                    {
                        pBuilding[index] = tempMB;
                        nB++;

                        pBuilding[index].name = "building";
                        pBuilding[index].transform.SetParent(line.transform);
                        pBuilding[index].transform.localPosition = new Vector3(0, 0, (init + (pWidth * 0.5f)));
                        pBuilding[index].transform.localRotation = Quaternion.Euler(0, 90, 0);
                    }

                    init += pWidth;

                    if (init > limit - 6)
                    {
                        AdjustsWidth(pBuilding, index + 1, (limit - init), 0);
                    }

                }


            }

        }

        private void CreateBuildingsInDouble()
        {
            float limit;

            tempArray = GameObject.FindObjectsOfType(typeof(GameObject)).Select(g => g as GameObject).Where(g => g.name == ("Double")).ToArray();

            GameObject DB;
            GameObject mc2;
            GameObject mc;


            foreach (GameObject dbCross in tempArray)
            {

                foreach (Transform line in dbCross.transform)
                {

                    if (line.name.Contains("."))
                        limit = float.Parse(line.name.Split('.')[0]) + float.Parse(line.name.Split('.')[1]) / float.Parse("1" + "0000000".Substring(0, line.name.Split('.')[1].Length));
                    else
                        limit = float.Parse(line.name);



                    if (Random.Range(0, 10) < 5)
                    {
                        //Bloks

                        float wl;
                        float wl2;

                        do
                        {
                            numB = Random.Range(0, DC.Length);
                            wl = GetHeight(DC[numB]);
                        } while (wl > limit / 2);

                        GameObject e = InstantiateIfAllowed(DC[numB], line.transform.position, line.transform.rotation, line.transform);
                        if (e != null) nB++;

                        do
                        {
                            numB = Random.Range(0, DC.Length);
                            wl2 = GetHeight(DC[numB]);
                        } while (wl2 > limit - (wl + 26));

                        GameObject e2 = InstantiateIfAllowed(DC[numB], line.transform.position, line.rotation, line.transform);
                        if (e2 != null)
                        {
                            e2.transform.SetParent(line.transform);
                            e2.transform.localPosition = new Vector3(0, 0, -limit);
                            e2.transform.localRotation = Quaternion.Euler(0, 180, 0);
                        }

                        DB = new GameObject("" + ((limit - wl - wl2)));
                        DB.transform.SetParent(line.transform);
                        DB.transform.localPosition = new Vector3(0, 0, -(limit - wl2));
                        DB.transform.localRotation = Quaternion.Euler(0, 0, 0);

                        DB.name = "" + ((limit - wl - wl2));

                        CreateBuildingsInDoubleLine(DB);

                    }
                    else
                    {
                        //Lines and Corners

                        mc = new GameObject("Marcador");
                        mc.transform.SetParent(line);
                        mc.transform.localPosition = new Vector3(0, 0, 0);
                        mc.transform.localRotation = Quaternion.Euler(0, 0, 0);


                        for (int i = 1; i <= 4; i++)
                        {
                            mc2 = new GameObject("E");
                            mc2.transform.SetParent(mc.transform);

                            if (i == 1)
                            {
                                mc2.transform.localPosition = new Vector3(36, 0, -limit);
                                mc2.transform.localRotation = Quaternion.Euler(0, 90, 0);
                            }
                            if (i == 2)
                            {
                                mc2.transform.localPosition = new Vector3(36, 0, 0);
                                mc2.transform.localRotation = Quaternion.Euler(0, 0, 0);
                            }
                            if (i == 3)
                            {
                                mc2.transform.localPosition = new Vector3(-36, 0, 0);
                                mc2.transform.localRotation = Quaternion.Euler(0, 270, 0);
                            }
                            if (i == 4)
                            {
                                mc2.transform.localPosition = new Vector3(-36, 0, -limit);
                                mc2.transform.localRotation = Quaternion.Euler(0, 180, 0);
                            }

                            CreateBuildingsInCorners(mc2);

                        }

                        mc2 = new GameObject("" + (limit - 72));
                        mc2.transform.SetParent(mc.transform);
                        mc2.transform.localPosition = new Vector3(-36, 0.001f, -36);
                        mc2.transform.localRotation = Quaternion.Euler(0, 180, 0);
                        CreateBuildingsInLine(mc2, 90f);

                        mc2 = new GameObject("" + (limit - 72));
                        mc2.transform.SetParent(mc.transform);
                        mc2.transform.localPosition = new Vector3(36, 0.001f, -(limit - 36));
                        mc2.transform.localRotation = Quaternion.Euler(0, 0, 0);
                        CreateBuildingsInLine(mc2, 90f);

                    }




                }



            }


        }


        private void AdjustsWidth(GameObject[] tBuildings, int quantity, float remainingMeters, float init, bool slope = false)
        {

            if (remainingMeters == 0)
                return;

            float ajuste = remainingMeters / quantity;

            float zInit = init;
            float pWidth;
            float pScale;
            float gw;


            for (int i = 0; i < quantity; i++)
            {

                gw = GetWith(tBuildings[i]);

                if (gw > 0)
                {
                    pScale = 1 + (ajuste / gw);
                    pWidth = gw + ajuste;

                    tBuildings[i].transform.localPosition = new Vector3(tBuildings[i].transform.localPosition.x, tBuildings[i].transform.localPosition.y, zInit + (pWidth * 0.5f));
                    tBuildings[i].transform.localScale = new Vector3(pScale, 1, 1);
                    zInit += pWidth;


                    if (slope)
                    {
                        float p;

                        p = GetY(tBuildings[i].transform, (gw * pScale) * 0.5f);
                        tBuildings[i].transform.position += new Vector3(0, p, 0);


                    }


                }

            }

        }


        private float GetWith(GameObject building)
        {

            if (!building)
                return 0;


            if (building.transform.GetComponent<MeshFilter>() != null)
            {

                if (building.transform.GetComponent<MeshFilter>().sharedMesh == null)
                {
                    Debug.LogError("Error:  " + building.name + " does not have a mesh renderer at the root. The prefab must be the floor/base mesh. I nside it you place the building. More info: https://youtu.be/kVrWir_WjNY");
                    //return 0;
                }


                return building.transform.GetComponent<MeshFilter>().sharedMesh.bounds.size.x;

            }
            else
            {
                Debug.LogError("Error:  " + building.name + " does not have a mesh renderer at the root. The prefab must be the floor/base mesh. I nside it you place the building. More info: https://youtu.be/kVrWir_WjNY");
                return 0;
            }
        }

        private float GetHeight(GameObject building)
        {

            if (building.GetComponent<MeshFilter>() != null)
                return building.GetComponent<MeshFilter>().sharedMesh.bounds.size.z;
            else
            {
                Debug.LogError("Error:  " + building.name + " does not have a mesh renderer at the root. The prefab must be the floor/base mesh. I nside it you place the building. More info: https://youtu.be/kVrWir_WjNY");
                return 0;
            }

        }


        public void DestroyBuildings()
        {

            DestryObjetcs("Marcador");
            DestryObjetcs("Blocks");
            DestryObjetcs("SuperBlocks");
            DestryObjetcs("Double");

        }


        private void DestryObjetcs(string tag)
        {
            tempArray = GameObject.FindObjectsOfType(typeof(GameObject)).Select(g => g as GameObject).Where(g => g.name == (tag)).ToArray();

            foreach (GameObject objt in tempArray)
                foreach (Transform child in objt.transform)
                    for (int k = child.childCount - 1; k >= 0; k--)
                        DestroyImmediate(child.GetChild(k).gameObject);


        }



    }
}
