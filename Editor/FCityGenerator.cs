using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using FCG;

public class FCityGenerator : EditorWindow
{
    private enum SatelliteLayoutMode
    {
        Preset,
        Random,
        Custom
    }

    [Serializable]
    private class SatellitePoint
    {
        public bool enabled = true;
        public float x;
        public float z;

        public SatellitePoint(float x, float z)
        {
            this.x = x;
            this.z = z;
        }
    }

    [Serializable]
    private class SatellitePointState
    {
        public bool enabled = true;
        public float x;
        public float z;
    }

    [Serializable]
    private class GeneratorEditorState
    {
        public bool withDowntownArea = true;
        public float downTownSize = 100f;
        public bool withSatteliteCity = false;
        public bool borderFlat = false;
        public int satteliteCityCount = 1;
        public bool connectSatteliteCities = true;
        public bool connectSatteliteCitiesTogether = true;
        public bool autoGenerateBuildingsAfterStreets = false;
        public bool autoAddTrafficAfterStreets = false;
        public int satelliteLayoutMode = 0;
        public int satelliteConnectionMode = 1;
        public int satelliteMaxNeighborLinks = 1;
        public bool satelliteCloseLoop = false;
        public float satelliteConnectionStep = 0f;
        public bool createCityAnchors = true;
        public bool createConnectionDebugLines = false;
        public float connectionDebugLineHeight = 3f;
        public bool useSatteliteSeed = false;
        public int satteliteSeed = 12345;
        public float randomSatelliteMinX = -1000f;
        public float randomSatelliteMaxX = 1000f;
        public float randomSatelliteMinZ = -2200f;
        public float randomSatelliteMaxZ = -1200f;
        public Vector2 satteliteGlobalOffset = Vector2.zero;
        public int quickGenerateCitySize = 3;
        public int trafficLightHand = 0;
        public bool japanTrafficLight = false;
        public bool generateLightmapUVs = false;
        public string activeProfilePath = "";
        public List<SatellitePointState> customSatellitePoints = new List<SatellitePointState>();
    }

    private CityGenerator cityGenerator;
    private const string EditorStatePrefsKey = "FCG.EditorState.v3";
    private string generationStatus = "Ready.";
    private CityGenerationProfile activeProfile;

    private bool generateLightmapUVs = false;
    private bool withDowntownArea = true;
    private float downTownSize = 100;

    private bool withSatteliteCity = false;
    private bool borderFlat = false;
    private int satteliteCityCount = 1;
    private bool connectSatteliteCities = true;
    private bool connectSatteliteCitiesTogether = true;
    private bool autoGenerateBuildingsAfterStreets = false;
    private bool autoAddTrafficAfterStreets = false;
    private SatelliteLayoutMode satelliteLayoutMode = SatelliteLayoutMode.Preset;
    private CityGenerator.SatelliteConnectionMode satelliteConnectionMode = CityGenerator.SatelliteConnectionMode.Chain;
    private int satelliteMaxNeighborLinks = 1;
    private bool satelliteCloseLoop = false;
    private float satelliteConnectionStep = 0f;
    private bool createCityAnchors = true;
    private bool createConnectionDebugLines = false;
    private float connectionDebugLineHeight = 3f;
    private bool useSatteliteSeed = false;
    private int satteliteSeed = 12345;
    private float randomSatelliteMinX = -1000f;
    private float randomSatelliteMaxX = 1000f;
    private float randomSatelliteMinZ = -2200f;
    private float randomSatelliteMaxZ = -1200f;
    private Vector2 satteliteGlobalOffset = Vector2.zero;
    private Vector2 satellitePointsScroll;
    private List<SatellitePoint> customSatellitePoints = new List<SatellitePoint>();
    private int quickGenerateCitySize = 3;


    private int trafficLightHand = 0;
    private string[] selStrings = { "Right Hand", "Left Hand" };
    private bool japanTrafficLight = false;


    [MenuItem("Window/Fantastic City Generator")]
    static void Init()
    {

        FCityGenerator window = (FCityGenerator)EditorWindow.GetWindow(typeof(FCityGenerator));

        window.Show();

    }

    int enableUpdate = 0;

#if UNITY_EDITOR
    void OnEnable()
    {
        EnsureCustomSatellitePoints();
        LoadEditorStateFromPrefs();
    }

    void OnDisable()
    {
        SaveEditorStateToPrefs();
    }

    void Update()
    {

        if (enableUpdate == 0) return;

        enableUpdate++;

        if (enableUpdate <= 5)
            HideLadders();

        if (enableUpdate >= 5)
            enableUpdate = 0;

    }
#endif

    private void EnsureCustomSatellitePoints()
    {
        if (customSatellitePoints == null)
            customSatellitePoints = new List<SatellitePoint>();

        if (customSatellitePoints.Count == 0)
        {
            customSatellitePoints.Add(new SatellitePoint(0, -1516));
            customSatellitePoints.Add(new SatellitePoint(-300, -1516));
            customSatellitePoints.Add(new SatellitePoint(200, -1516));
        }
    }

    private Vector2[] GetPresetSatelliteOffsets()
    {
        return new Vector2[]
        {
            new Vector2(0, -1516),
            new Vector2(-300, -1516),
            new Vector2(200, -1516),
            new Vector2(-100, -1516),
            new Vector2(700, -1316),
            new Vector2(500, -1316),
            new Vector2(-700, -1316),
            new Vector2(-500, -1316),
            new Vector2(1000, -1750),
            new Vector2(-1000, -1750),
            new Vector2(350, -2200),
            new Vector2(-350, -2200)
        };
    }

    private List<Vector2> BuildEnabledCustomSatelliteOffsets()
    {
        EnsureCustomSatellitePoints();

        List<Vector2> points = new List<Vector2>();
        for (int i = 0; i < customSatellitePoints.Count; i++)
        {
            if (customSatellitePoints[i].enabled)
                points.Add(new Vector2(customSatellitePoints[i].x, customSatellitePoints[i].z));
        }

        return points;
    }

    private GeneratorEditorState CaptureEditorState()
    {
        EnsureCustomSatellitePoints();

        GeneratorEditorState state = new GeneratorEditorState();
        state.withDowntownArea = withDowntownArea;
        state.downTownSize = downTownSize;
        state.withSatteliteCity = withSatteliteCity;
        state.borderFlat = borderFlat;
        state.satteliteCityCount = satteliteCityCount;
        state.connectSatteliteCities = connectSatteliteCities;
        state.connectSatteliteCitiesTogether = connectSatteliteCitiesTogether;
        state.autoGenerateBuildingsAfterStreets = autoGenerateBuildingsAfterStreets;
        state.autoAddTrafficAfterStreets = autoAddTrafficAfterStreets;
        state.satelliteLayoutMode = (int)satelliteLayoutMode;
        state.satelliteConnectionMode = (int)satelliteConnectionMode;
        state.satelliteMaxNeighborLinks = satelliteMaxNeighborLinks;
        state.satelliteCloseLoop = satelliteCloseLoop;
        state.satelliteConnectionStep = satelliteConnectionStep;
        state.createCityAnchors = createCityAnchors;
        state.createConnectionDebugLines = createConnectionDebugLines;
        state.connectionDebugLineHeight = connectionDebugLineHeight;
        state.useSatteliteSeed = useSatteliteSeed;
        state.satteliteSeed = satteliteSeed;
        state.randomSatelliteMinX = randomSatelliteMinX;
        state.randomSatelliteMaxX = randomSatelliteMaxX;
        state.randomSatelliteMinZ = randomSatelliteMinZ;
        state.randomSatelliteMaxZ = randomSatelliteMaxZ;
        state.satteliteGlobalOffset = satteliteGlobalOffset;
        state.quickGenerateCitySize = quickGenerateCitySize;
        state.trafficLightHand = trafficLightHand;
        state.japanTrafficLight = japanTrafficLight;
        state.generateLightmapUVs = generateLightmapUVs;
        state.activeProfilePath = activeProfile ? AssetDatabase.GetAssetPath(activeProfile) : string.Empty;
        state.customSatellitePoints = new List<SatellitePointState>(customSatellitePoints.Count);

        for (int i = 0; i < customSatellitePoints.Count; i++)
        {
            SatellitePoint p = customSatellitePoints[i];
            state.customSatellitePoints.Add(new SatellitePointState { enabled = p.enabled, x = p.x, z = p.z });
        }

        return state;
    }

    private void ApplyEditorState(GeneratorEditorState state)
    {
        if (state == null)
            return;

        withDowntownArea = state.withDowntownArea;
        downTownSize = Mathf.Clamp(state.downTownSize, 50f, 200f);
        withSatteliteCity = state.withSatteliteCity;
        borderFlat = state.borderFlat;
        satteliteCityCount = Mathf.Clamp(state.satteliteCityCount, 1, 64);
        connectSatteliteCities = state.connectSatteliteCities;
        connectSatteliteCitiesTogether = state.connectSatteliteCitiesTogether;
        autoGenerateBuildingsAfterStreets = state.autoGenerateBuildingsAfterStreets;
        autoAddTrafficAfterStreets = state.autoAddTrafficAfterStreets;
        satelliteLayoutMode = (SatelliteLayoutMode)Mathf.Clamp(state.satelliteLayoutMode, 0, 2);
        satelliteConnectionMode = (CityGenerator.SatelliteConnectionMode)Mathf.Clamp(state.satelliteConnectionMode, 0, 3);
        satelliteMaxNeighborLinks = Mathf.Clamp(state.satelliteMaxNeighborLinks, 1, 6);
        satelliteCloseLoop = state.satelliteCloseLoop;
        satelliteConnectionStep = Mathf.Max(0f, state.satelliteConnectionStep);
        createCityAnchors = state.createCityAnchors;
        createConnectionDebugLines = state.createConnectionDebugLines;
        connectionDebugLineHeight = Mathf.Clamp(state.connectionDebugLineHeight, 0f, 20f);
        useSatteliteSeed = state.useSatteliteSeed;
        satteliteSeed = state.satteliteSeed;
        randomSatelliteMinX = state.randomSatelliteMinX;
        randomSatelliteMaxX = state.randomSatelliteMaxX;
        randomSatelliteMinZ = state.randomSatelliteMinZ;
        randomSatelliteMaxZ = state.randomSatelliteMaxZ;
        satteliteGlobalOffset = state.satteliteGlobalOffset;
        quickGenerateCitySize = Mathf.Clamp(state.quickGenerateCitySize, 1, 4);
        trafficLightHand = Mathf.Clamp(state.trafficLightHand, 0, 1);
        japanTrafficLight = state.japanTrafficLight;
        generateLightmapUVs = state.generateLightmapUVs;
        if (!string.IsNullOrEmpty(state.activeProfilePath))
            activeProfile = AssetDatabase.LoadAssetAtPath<CityGenerationProfile>(state.activeProfilePath);

        customSatellitePoints = new List<SatellitePoint>();
        if (state.customSatellitePoints != null)
        {
            for (int i = 0; i < state.customSatellitePoints.Count; i++)
            {
                SatellitePointState p = state.customSatellitePoints[i];
                customSatellitePoints.Add(new SatellitePoint(p.x, p.z) { enabled = p.enabled });
            }
        }

        EnsureCustomSatellitePoints();
    }

    private void SaveEditorStateToPrefs()
    {
        GeneratorEditorState state = CaptureEditorState();
        string json = JsonUtility.ToJson(state);
        EditorPrefs.SetString(EditorStatePrefsKey, json);
    }

    private void LoadEditorStateFromPrefs()
    {
        if (!EditorPrefs.HasKey(EditorStatePrefsKey))
            return;

        string json = EditorPrefs.GetString(EditorStatePrefsKey, string.Empty);
        if (string.IsNullOrEmpty(json))
            return;

        try
        {
            GeneratorEditorState state = JsonUtility.FromJson<GeneratorEditorState>(json);
            ApplyEditorState(state);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Failed to load editor state: " + ex.Message);
        }
    }

    private void ExportCurrentSettingsToFile()
    {
        string path = EditorUtility.SaveFilePanel("Export FCG Settings", Application.dataPath, "fcg-settings.json", "json");
        if (string.IsNullOrEmpty(path))
            return;

        string json = JsonUtility.ToJson(CaptureEditorState(), true);
        File.WriteAllText(path, json);
        generationStatus = "Settings exported: " + path;
    }

    private void ImportSettingsFromFile()
    {
        string path = EditorUtility.OpenFilePanel("Import FCG Settings", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            string json = File.ReadAllText(path);
            GeneratorEditorState state = JsonUtility.FromJson<GeneratorEditorState>(json);
            ApplyEditorState(state);
            SaveEditorStateToPrefs();
            generationStatus = "Settings imported: " + path;
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to import settings: " + ex.Message);
            generationStatus = "Import failed.";
        }
    }

    private void ResetEditorSettings()
    {
        ApplyEditorState(new GeneratorEditorState());
        SaveEditorStateToPrefs();
        generationStatus = "Settings reset to defaults.";
    }

    private void RandomizeSatelliteSettings()
    {
        withSatteliteCity = true;
        satteliteCityCount = UnityEngine.Random.Range(1, 13);
        satelliteLayoutMode = (SatelliteLayoutMode)UnityEngine.Random.Range(0, 3);
        satelliteConnectionMode = (CityGenerator.SatelliteConnectionMode)UnityEngine.Random.Range(0, 4);
        satelliteMaxNeighborLinks = UnityEngine.Random.Range(1, 4);
        satelliteCloseLoop = UnityEngine.Random.Range(0, 100) < 50;
        useSatteliteSeed = UnityEngine.Random.Range(0, 100) < 60;
        satteliteSeed = UnityEngine.Random.Range(1, int.MaxValue / 2);
        randomSatelliteMinX = UnityEngine.Random.Range(-1400f, -300f);
        randomSatelliteMaxX = UnityEngine.Random.Range(300f, 1400f);
        randomSatelliteMinZ = UnityEngine.Random.Range(-2600f, -1400f);
        randomSatelliteMaxZ = UnityEngine.Random.Range(-1400f, -900f);
        satteliteGlobalOffset = new Vector2(UnityEngine.Random.Range(-300f, 300f), UnityEngine.Random.Range(-300f, 300f));
        generationStatus = "Satellite settings randomized.";
    }

    private void CreateRuntimeProfileAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Runtime Profile",
            "CityGenerationProfile",
            "asset",
            "Choose where to save the runtime generation profile.");

        if (string.IsNullOrEmpty(path))
            return;

        CityGenerationProfile profile = ScriptableObject.CreateInstance<CityGenerationProfile>();
        profile.citySize = quickGenerateCitySize;
        profile.clearBeforeGenerate = true;
        profile.autoGenerateBuildings = autoGenerateBuildingsAfterStreets;
        profile.autoAddTraffic = autoAddTrafficAfterStreets;

        profile.withSatelliteCity = withSatteliteCity;
        profile.borderFlat = borderFlat;
        profile.satelliteCityCount = satteliteCityCount;
        profile.connectSatellitesToMain = connectSatteliteCities;
        profile.connectSatellitesTogether = connectSatteliteCitiesTogether;
        profile.satelliteConnectionMode = satelliteConnectionMode;
        profile.satelliteMaxNeighborLinks = satelliteMaxNeighborLinks;
        profile.satelliteCloseLoop = satelliteCloseLoop;
        profile.satelliteConnectionStep = satelliteConnectionStep;
        profile.createCityAnchors = createCityAnchors;
        profile.createConnectionDebugLines = createConnectionDebugLines;
        profile.connectionDebugLineHeight = connectionDebugLineHeight;
        profile.randomSatelliteLayout = satelliteLayoutMode == SatelliteLayoutMode.Random;
        profile.useSatelliteSeed = useSatteliteSeed;
        profile.satelliteSeed = satteliteSeed;
        profile.randomSatelliteMin = new Vector2(randomSatelliteMinX, randomSatelliteMinZ);
        profile.randomSatelliteMax = new Vector2(randomSatelliteMaxX, randomSatelliteMaxZ);
        profile.satelliteGlobalOffset = satteliteGlobalOffset;
        profile.customSatelliteOffsets = BuildEnabledCustomSatelliteOffsets();

        profile.withDownTownArea = withDowntownArea;
        profile.downTownSize = downTownSize;
        profile.rightHand = trafficLightHand == 0;
        profile.japanTrafficLight = japanTrafficLight;

        AssetDatabase.CreateAsset(profile, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = profile;
        generationStatus = "Runtime profile created: " + path;
    }

    private void ApplyActiveProfileToEditor()
    {
        if (!activeProfile)
        {
            generationStatus = "No active profile selected.";
            return;
        }

        quickGenerateCitySize = Mathf.Clamp(activeProfile.citySize, 1, 4);
        autoGenerateBuildingsAfterStreets = activeProfile.autoGenerateBuildings;
        autoAddTrafficAfterStreets = activeProfile.autoAddTraffic;

        withSatteliteCity = activeProfile.withSatelliteCity;
        borderFlat = activeProfile.borderFlat;
        satteliteCityCount = Mathf.Clamp(activeProfile.satelliteCityCount, 1, 64);
        connectSatteliteCities = activeProfile.connectSatellitesToMain;
        connectSatteliteCitiesTogether = activeProfile.connectSatellitesTogether;
        satelliteConnectionMode = activeProfile.satelliteConnectionMode;
        satelliteMaxNeighborLinks = Mathf.Clamp(activeProfile.satelliteMaxNeighborLinks, 1, 6);
        satelliteCloseLoop = activeProfile.satelliteCloseLoop;
        satelliteConnectionStep = Mathf.Max(0f, activeProfile.satelliteConnectionStep);
        createCityAnchors = activeProfile.createCityAnchors;
        createConnectionDebugLines = activeProfile.createConnectionDebugLines;
        connectionDebugLineHeight = Mathf.Clamp(activeProfile.connectionDebugLineHeight, 0f, 20f);
        useSatteliteSeed = activeProfile.useSatelliteSeed;
        satteliteSeed = activeProfile.satelliteSeed;
        randomSatelliteMinX = activeProfile.randomSatelliteMin.x;
        randomSatelliteMinZ = activeProfile.randomSatelliteMin.y;
        randomSatelliteMaxX = activeProfile.randomSatelliteMax.x;
        randomSatelliteMaxZ = activeProfile.randomSatelliteMax.y;
        satteliteGlobalOffset = activeProfile.satelliteGlobalOffset;

        satelliteLayoutMode = activeProfile.randomSatelliteLayout
            ? SatelliteLayoutMode.Random
            : ((activeProfile.customSatelliteOffsets != null && activeProfile.customSatelliteOffsets.Count > 0)
                ? SatelliteLayoutMode.Custom
                : SatelliteLayoutMode.Preset);

        customSatellitePoints = new List<SatellitePoint>();
        if (activeProfile.customSatelliteOffsets != null)
        {
            for (int i = 0; i < activeProfile.customSatelliteOffsets.Count; i++)
            {
                Vector2 p = activeProfile.customSatelliteOffsets[i];
                customSatellitePoints.Add(new SatellitePoint(p.x, p.y));
            }
        }
        EnsureCustomSatellitePoints();

        withDowntownArea = activeProfile.withDownTownArea;
        downTownSize = Mathf.Clamp(activeProfile.downTownSize, 50f, 200f);
        trafficLightHand = activeProfile.rightHand ? 0 : 1;
        japanTrafficLight = activeProfile.japanTrafficLight;

        SaveEditorStateToPrefs();
        generationStatus = "Profile applied: " + activeProfile.name;
    }

    private void SaveEditorSettingsToActiveProfile()
    {
        if (!activeProfile)
        {
            generationStatus = "No active profile selected.";
            return;
        }

        activeProfile.citySize = quickGenerateCitySize;
        activeProfile.autoGenerateBuildings = autoGenerateBuildingsAfterStreets;
        activeProfile.autoAddTraffic = autoAddTrafficAfterStreets;

        activeProfile.withSatelliteCity = withSatteliteCity;
        activeProfile.borderFlat = borderFlat;
        activeProfile.satelliteCityCount = satteliteCityCount;
        activeProfile.connectSatellitesToMain = connectSatteliteCities;
        activeProfile.connectSatellitesTogether = connectSatteliteCitiesTogether;
        activeProfile.satelliteConnectionMode = satelliteConnectionMode;
        activeProfile.satelliteMaxNeighborLinks = satelliteMaxNeighborLinks;
        activeProfile.satelliteCloseLoop = satelliteCloseLoop;
        activeProfile.satelliteConnectionStep = satelliteConnectionStep;
        activeProfile.createCityAnchors = createCityAnchors;
        activeProfile.createConnectionDebugLines = createConnectionDebugLines;
        activeProfile.connectionDebugLineHeight = connectionDebugLineHeight;
        activeProfile.randomSatelliteLayout = satelliteLayoutMode == SatelliteLayoutMode.Random;
        activeProfile.useSatelliteSeed = useSatteliteSeed;
        activeProfile.satelliteSeed = satteliteSeed;
        activeProfile.randomSatelliteMin = new Vector2(randomSatelliteMinX, randomSatelliteMinZ);
        activeProfile.randomSatelliteMax = new Vector2(randomSatelliteMaxX, randomSatelliteMaxZ);
        activeProfile.satelliteGlobalOffset = satteliteGlobalOffset;
        activeProfile.customSatelliteOffsets = BuildEnabledCustomSatelliteOffsets();

        activeProfile.withDownTownArea = withDowntownArea;
        activeProfile.downTownSize = downTownSize;
        activeProfile.rightHand = trafficLightHand == 0;
        activeProfile.japanTrafficLight = japanTrafficLight;

        EditorUtility.SetDirty(activeProfile);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        generationStatus = "Profile saved: " + activeProfile.name;
    }

    private string BuildProjectHealthReport()
    {
        List<string> report = new List<string>();
        report.Add("FCG Health Report");

        if (!cityGenerator)
        {
            cityGenerator = (CityGenerator)AssetDatabase.LoadAssetAtPath("Assets/Fantastic City Generator/Generate.prefab", (typeof(CityGenerator)));
        }

        if (!cityGenerator)
        {
            report.Add("- ERROR: Generate.prefab missing.");
            return string.Join("\n", report.ToArray());
        }

        LoadAssets();
        report.Add("- OK: Generate.prefab found.");

        if (cityGenerator.largeBlocks == null || cityGenerator.largeBlocks.Length == 0)
            report.Add("- ERROR: largeBlocks empty.");
        else
            report.Add("- OK: largeBlocks count = " + cityGenerator.largeBlocks.Length);

        if (cityGenerator.bigLargeBlocks == null || cityGenerator.bigLargeBlocks.Length == 0)
            report.Add("- WARN: bigLargeBlocks empty (fallbacks will be used).");
        else
            report.Add("- OK: bigLargeBlocks count = " + cityGenerator.bigLargeBlocks.Length);

        report.Add((cityGenerator.BB != null && cityGenerator.BB.Length > 0) ? "- OK: BB building pool loaded." : "- WARN: BB building pool empty.");
        report.Add((cityGenerator.BC != null && cityGenerator.BC.Length > 0) ? "- OK: BC building pool loaded." : "- WARN: BC building pool empty.");
        report.Add((cityGenerator.BR != null && cityGenerator.BR.Length > 0) ? "- OK: BR building pool loaded." : "- WARN: BR building pool empty.");
        report.Add((cityGenerator.EB != null && cityGenerator.EB.Length > 0) ? "- OK: EB building pool loaded." : "- WARN: EB building pool empty.");
        report.Add((cityGenerator.EC != null && cityGenerator.EC.Length > 0) ? "- OK: EC building pool loaded." : "- WARN: EC building pool empty.");

        string canGenerateReason;
        if (cityGenerator.CanGenerate(quickGenerateCitySize, withSatteliteCity, out canGenerateReason))
            report.Add("- OK: Current generation setup is valid.");
        else
            report.Add("- ERROR: " + canGenerateReason);

        TrafficSystem traffic = FindObjectOfType<TrafficSystem>();
        report.Add(traffic ? "- OK: TrafficSystem exists in scene." : "- INFO: TrafficSystem not in scene (can be auto-added).");

        DayNight dayNight = FindObjectOfType<DayNight>();
        report.Add(dayNight ? "- OK: DayNight exists in scene." : "- WARN: DayNight not found.");

        return string.Join("\n", report.ToArray());
    }

    private void RunProjectHealthCheck()
    {
        string report = BuildProjectHealthReport();
        Debug.Log(report);
        generationStatus = "Health check completed. Report logged to Console.";
    }

    private string ValidateCurrentConfiguration()
    {
        if (!withSatteliteCity)
            return string.Empty;

        if (satelliteLayoutMode == SatelliteLayoutMode.Custom)
        {
            int enabledPoints = BuildEnabledCustomSatelliteOffsets().Count;
            if (enabledPoints == 0)
                return "Custom satellite mode has no enabled points.";
        }

        if (satelliteLayoutMode == SatelliteLayoutMode.Random)
        {
            if (randomSatelliteMinX > randomSatelliteMaxX || randomSatelliteMinZ > randomSatelliteMaxZ)
                return "Random range min/max values are inverted. They will be auto-corrected.";
        }

        if (connectSatteliteCitiesTogether && satelliteConnectionMode == CityGenerator.SatelliteConnectionMode.MainOnly)
            return "Connect Satellites Together is enabled but Network Mode is MainOnly (no satellite-to-satellite links).";

        if (createConnectionDebugLines && !createCityAnchors)
            return "Network debug lines require 'Create City Anchors' to be enabled.";

        return string.Empty;
    }

    public void LoadAssets(bool force = false)
    {
        cityGenerator = null;

        if (!cityGenerator)
            cityGenerator = (CityGenerator)AssetDatabase.LoadAssetAtPath("Assets/Fantastic City Generator/Generate.prefab", (typeof(CityGenerator)));

        if (!cityGenerator)
        {
            Debug.LogError("Generate.prefab was not found/Loaded in 'Assets/Fantastic City Generator'");
            return;
        }

        string[] s;

        //BB - Street buildings in suburban areas (not in the corner)
        s = System.IO.Directory.GetFiles("Assets/Fantastic City Generator/Buildings/Prefabs/BB", "*.prefab");
        if (force || cityGenerator.BB == null || cityGenerator.BB.Length != s.Length)
            cityGenerator.BB = LoadAssets_sub(s);

        //BC - Down Town Buildings(Not in the corner)
        s = System.IO.Directory.GetFiles("Assets/Fantastic City Generator/Buildings/Prefabs/BC", "*.prefab");
        if (force || cityGenerator.BC == null || cityGenerator.BC.Length != s.Length)
            cityGenerator.BC = LoadAssets_sub(s);

        //BK - Buildings that occupy an entire block
        s = System.IO.Directory.GetFiles("Assets/Fantastic City Generator/Buildings/Prefabs/BK", "*.prefab");
        if (force || cityGenerator.BK == null || cityGenerator.BK.Length != s.Length)
            cityGenerator.BK = LoadAssets_sub(s);

        //BR - Residential buildings in suburban areas (not in the corner)
        s = System.IO.Directory.GetFiles("Assets/Fantastic City Generator/Buildings/Prefabs/BR", "*.prefab");
        if (force || cityGenerator.BR == null || cityGenerator.BR.Length != s.Length)
            cityGenerator.BR = LoadAssets_sub(s);

        //DC - Corner buildings that occupy both sides of the block
        s = System.IO.Directory.GetFiles("Assets/Fantastic City Generator/Buildings/Prefabs/DC", "*.prefab");
        if (force || cityGenerator.DC == null || cityGenerator.DC.Length != s.Length)
            cityGenerator.DC = LoadAssets_sub(s);

        //EB - Corner buildings in suburban areas
        s = System.IO.Directory.GetFiles("Assets/Fantastic City Generator/Buildings/Prefabs/EB", "*.prefab");
        if (force || cityGenerator.EB == null || cityGenerator.EB.Length != s.Length)
            cityGenerator.EB = LoadAssets_sub(s);

        //EC - Down Town Corner Buildings 
        s = System.IO.Directory.GetFiles("Assets/Fantastic City Generator/Buildings/Prefabs/EC", "*.prefab");
        if (force || cityGenerator.EC == null || cityGenerator.EC.Length != s.Length)
            cityGenerator.EC = LoadAssets_sub(s);

        //MB - Buildings that occupy both sides of the block
        s = System.IO.Directory.GetFiles("Assets/Fantastic City Generator/Buildings/Prefabs/MB", "*.prefab");
        if (force || cityGenerator.MB == null || cityGenerator.MB.Length != s.Length)
            cityGenerator.MB = LoadAssets_sub(s);

        //SB - Large buildings that occupy larger blocks
        s = System.IO.Directory.GetFiles("Assets/Fantastic City Generator/Buildings/Prefabs/SB", "*.prefab");
        if (force || cityGenerator.SB == null || cityGenerator.SB.Length != s.Length)
            cityGenerator.SB = LoadAssets_sub(s);

        //BBS - Buildings on slopes (neighborhood)
        s = System.IO.Directory.GetFiles("Assets/Fantastic City Generator/Buildings/Prefabs/BBS", "*.prefab");
        if (force || cityGenerator.BBS == null || cityGenerator.BBS.Length != s.Length)
            cityGenerator.BBS = LoadAssets_sub(s);

        //BCS - Down Town Buildings on slopes
        s = System.IO.Directory.GetFiles("Assets/Fantastic City Generator/Buildings/Prefabs/BCS", "*.prefab");
        if (force || cityGenerator.BCS == null || cityGenerator.BCS.Length != s.Length)
            cityGenerator.BCS = LoadAssets_sub(s);

    }



    private GameObject[] LoadAssets_sub(string[] s)
    {

        int i = s.Length;
        GameObject[] g = new GameObject[i];

        for (int h = 0; h < i; h++)
            g[h] = AssetDatabase.LoadAssetAtPath(s[h], typeof(GameObject)) as GameObject;

        if (g == null)
            Debug.LogError("Error in LoadAssets");

        return g;

    }

    private void GenerateCity(int size, bool borderFlat = false)
    {

        LoadAssets();

        EnsureCustomSatellitePoints();

        bool useRandomLayout = withSatteliteCity && satelliteLayoutMode == SatelliteLayoutMode.Random;
        bool useCustomLayout = withSatteliteCity && satelliteLayoutMode == SatelliteLayoutMode.Custom;

        List<Vector2> customOffsets = useCustomLayout ? BuildEnabledCustomSatelliteOffsets() : null;
        int effectiveSatelliteCount = satteliteCityCount;
        if (useCustomLayout)
            effectiveSatelliteCount = Mathf.Max(1, (customOffsets != null) ? customOffsets.Count : 0);

        if (!cityGenerator.CanGenerate(size, withSatteliteCity, out string canGenerateReason))
        {
            generationStatus = "Cannot generate: " + canGenerateReason;
            return;
        }

        cityGenerator.GenerateCity(
            size,
            withSatteliteCity,
            borderFlat,
            effectiveSatelliteCount,
            connectSatteliteCities,
            connectSatteliteCitiesTogether,
            useRandomLayout,
            useSatteliteSeed,
            satteliteSeed,
            randomSatelliteMinX,
            randomSatelliteMaxX,
            randomSatelliteMinZ,
            randomSatelliteMaxZ,
            customOffsets,
            useCustomLayout,
            satteliteGlobalOffset,
            satelliteConnectionMode,
            satelliteMaxNeighborLinks,
            satelliteCloseLoop,
            satelliteConnectionStep,
            createCityAnchors,
            createConnectionDebugLines,
            connectionDebugLineHeight);

        generationStatus = cityGenerator.GetLastGenerationSummary();

        if (autoGenerateBuildingsAfterStreets)
        {
            cityGenerator.GenerateAllBuildings(withDowntownArea, downTownSize);
            enableUpdate = 1;
            generationStatus += " | Buildings: generated";
        }

        if (trafficSystem)
        {
            InverseCarDirection((trafficLightHand == 1 && japanTrafficLight) ? 2 : trafficLightHand);

            trafficSystem.UpdateAllWayPoints();

        }


        DestroyImmediate(GameObject.Find("CarContainer"));

        if (autoAddTrafficAfterStreets)
        {
            AddVehicles((trafficLightHand == 1 && japanTrafficLight) ? 2 : trafficLightHand);
            generationStatus += " | Traffic: refreshed";
        }

        SaveEditorStateToPrefs();


    }

    private void GenerateFullPipeline(int size, bool clearBefore = true)
    {
        if (clearBefore && cityGenerator)
            cityGenerator.ClearCity();

        GenerateCity(size, borderFlat);

        if (!autoGenerateBuildingsAfterStreets && GameObject.Find("Marcador"))
        {
            LoadAssets(true);
            cityGenerator.GenerateAllBuildings(withDowntownArea, downTownSize);
            enableUpdate = 1;
        }

        if (!autoAddTrafficAfterStreets)
            AddVehicles((trafficLightHand == 1 && japanTrafficLight) ? 2 : trafficLightHand);

        SaveEditorStateToPrefs();
    }



    public void HideLadders()
    {

        RaycastHit hit;

        GameObject[] tempArray = GameObject.FindObjectsOfType(typeof(GameObject)).Select(g => g as GameObject).Where(g => g.name == "RayCast-HideLadder").ToArray();
        foreach (GameObject ray in tempArray)
        {

            if (Physics.Raycast(ray.transform.position, ray.transform.forward, out hit, 1.5f))
                ray.transform.GetChild(0).gameObject.SetActive(false);
            else
                ray.transform.GetChild(0).gameObject.SetActive(true);

        }


    }


    void OnGUI()
    {
        EditorGUI.BeginChangeCheck();

        GUILayout.Space(10);

        GUILayout.Label("Fantastic City Generator", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (!cityGenerator)
            cityGenerator = (CityGenerator)AssetDatabase.LoadAssetAtPath("Assets/Fantastic City Generator/Generate.prefab", (typeof(CityGenerator)));

        if (!cityGenerator)
            Debug.LogError("Generate.prefab was not found in 'Assets/Fantastic City Generator'");

        EditorGUILayout.EndHorizontal();

        GUILayout.BeginHorizontal("box");
        GUILayout.Label("Active Profile", GUILayout.Width(88));
        activeProfile = (CityGenerationProfile)EditorGUILayout.ObjectField(activeProfile, typeof(CityGenerationProfile), false);
        if (GUILayout.Button("Apply Profile", GUILayout.Width(110)))
            ApplyActiveProfileToEditor();
        if (GUILayout.Button("Save To Profile", GUILayout.Width(110)))
            SaveEditorSettingsToActiveProfile();
        if (GUILayout.Button("Health Check", GUILayout.Width(100)))
            RunProjectHealthCheck();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal("box");
        if (GUILayout.Button("Save Session", GUILayout.Width(110)))
        {
            SaveEditorStateToPrefs();
            generationStatus = "Session saved.";
        }
        if (GUILayout.Button("Export Preset", GUILayout.Width(120)))
            ExportCurrentSettingsToFile();
        if (GUILayout.Button("Import Preset", GUILayout.Width(120)))
            ImportSettingsFromFile();
        if (GUILayout.Button("Create Runtime Profile", GUILayout.Width(160)))
            CreateRuntimeProfileAsset();
        if (GUILayout.Button("Reset Defaults", GUILayout.Width(120)))
            ResetEditorSettings();
        if (GUILayout.Button("Randomize", GUILayout.Width(100)))
            RandomizeSatelliteSettings();
        GUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(generationStatus, MessageType.None);
        if (cityGenerator)
        {
            EditorGUILayout.HelpBox(cityGenerator.GetLastGenerationSummary(), MessageType.Info);
            EditorGUILayout.HelpBox(cityGenerator.GetLastGenerationNetworkSummary(), MessageType.None);
        }
        string validationWarning = ValidateCurrentConfiguration();
        if (!string.IsNullOrEmpty(validationWarning))
            EditorGUILayout.HelpBox(validationWarning, MessageType.Warning);

        GUILayout.Space(5);

        GUILayout.BeginVertical("box");

        GUILayout.Space(5);
        GUILayout.Label(new GUIContent("Generate Streets", "Make City"));

        GUILayout.Space(5);

        GUILayout.BeginHorizontal("box");

        if (GUILayout.Button("Small"))
            GenerateCity(1, borderFlat);


        if (GUILayout.Button("Medium"))
            GenerateCity(2, borderFlat);

        if (GUILayout.Button("Large"))
            GenerateCity(3, borderFlat);

        if (GUILayout.Button("Very Large"))
            GenerateCity(4, borderFlat);


        GUILayout.Space(5);


        GUILayout.EndHorizontal();

        GUILayout.Space(6);
        GUILayout.BeginHorizontal("box");
        GUILayout.Label("Quick Generate", GUILayout.Width(100));
        quickGenerateCitySize = EditorGUILayout.IntSlider(quickGenerateCitySize, 1, 4);
        if (GUILayout.Button("Generate Setup", GUILayout.Width(140)))
            GenerateCity(quickGenerateCitySize, borderFlat);
        if (GUILayout.Button("Generate Full Pipeline", GUILayout.Width(170)))
            GenerateFullPipeline(quickGenerateCitySize, true);
        if (GUILayout.Button("Quick Random", GUILayout.Width(120)))
        {
            RandomizeSatelliteSettings();
            GenerateCity(quickGenerateCitySize, borderFlat);
        }
        GUILayout.EndHorizontal();

        withSatteliteCity = GUILayout.Toggle(withSatteliteCity, "With Sattelite City", GUILayout.Width(240));

        if (withSatteliteCity)
        {
            GUILayout.Space(10);
            satelliteLayoutMode = (SatelliteLayoutMode)EditorGUILayout.EnumPopup("Satellite Layout", satelliteLayoutMode);

            if (satelliteLayoutMode != SatelliteLayoutMode.Custom)
            {
                GUILayout.Label(new GUIContent("Sattelite Cities Count:", "Number of sattelite cities to generate"));
                satteliteCityCount = EditorGUILayout.IntSlider(satteliteCityCount, 1, 64);
            }

            connectSatteliteCities = GUILayout.Toggle(connectSatteliteCities, "Connect to Main City", GUILayout.Width(240));
            connectSatteliteCitiesTogether = GUILayout.Toggle(connectSatteliteCitiesTogether, "Connect Satellites Together", GUILayout.Width(240));

            GUILayout.Space(6);
            GUILayout.Label(new GUIContent("Connection Mode", "How satellite cities connect to each other"));
            satelliteConnectionMode = (CityGenerator.SatelliteConnectionMode)EditorGUILayout.EnumPopup("Network Mode", satelliteConnectionMode);
            if (satelliteConnectionMode == CityGenerator.SatelliteConnectionMode.Nearest)
                satelliteMaxNeighborLinks = EditorGUILayout.IntSlider("Nearest Links", satelliteMaxNeighborLinks, 1, 6);
            satelliteCloseLoop = GUILayout.Toggle(satelliteCloseLoop, "Close Satellite Loop", GUILayout.Width(240));
            satelliteConnectionStep = EditorGUILayout.FloatField("Road Segment Length (0=Auto)", satelliteConnectionStep);
            if (satelliteConnectionStep < 0f) satelliteConnectionStep = 0f;
            createCityAnchors = GUILayout.Toggle(createCityAnchors, "Create City Anchors", GUILayout.Width(240));
            createConnectionDebugLines = GUILayout.Toggle(createConnectionDebugLines, "Draw Network Debug Lines", GUILayout.Width(240));
            if (createConnectionDebugLines)
                connectionDebugLineHeight = EditorGUILayout.Slider("Debug Line Height", connectionDebugLineHeight, 0f, 20f);

            GUILayout.Space(6);
            GUILayout.Label(new GUIContent("Satellite Global Offset", "Shift all satellite cities relative to ExitCity"));
            satteliteGlobalOffset = EditorGUILayout.Vector2Field("Global Offset (X/Z)", satteliteGlobalOffset);

            if (satelliteLayoutMode == SatelliteLayoutMode.Random)
            {
                GUILayout.Space(6);
                GUILayout.Label(new GUIContent("Random Range", "Random position range relative to ExitCity"));
                randomSatelliteMinX = EditorGUILayout.FloatField("Min X", randomSatelliteMinX);
                randomSatelliteMaxX = EditorGUILayout.FloatField("Max X", randomSatelliteMaxX);
                randomSatelliteMinZ = EditorGUILayout.FloatField("Min Z", randomSatelliteMinZ);
                randomSatelliteMaxZ = EditorGUILayout.FloatField("Max Z", randomSatelliteMaxZ);
                useSatteliteSeed = GUILayout.Toggle(useSatteliteSeed, "Use Fixed Seed", GUILayout.Width(240));
                if (useSatteliteSeed)
                    satteliteSeed = EditorGUILayout.IntField("Seed", satteliteSeed);
            }
            else if (satelliteLayoutMode == SatelliteLayoutMode.Custom)
            {
                EnsureCustomSatellitePoints();
                GUILayout.Space(6);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Point", GUILayout.Width(120)))
                    customSatellitePoints.Add(new SatellitePoint(0, -1516));
                if (GUILayout.Button("Load Preset Points", GUILayout.Width(160)))
                {
                    customSatellitePoints.Clear();
                    Vector2[] presets = GetPresetSatelliteOffsets();
                    for (int i = 0; i < presets.Length; i++)
                        customSatellitePoints.Add(new SatellitePoint(presets[i].x, presets[i].y));
                }
                if (GUILayout.Button("Clear Points", GUILayout.Width(120)))
                    customSatellitePoints.Clear();
                GUILayout.EndHorizontal();

                satellitePointsScroll = GUILayout.BeginScrollView(satellitePointsScroll, GUILayout.Height(170));
                int removeIndex = -1;
                for (int i = 0; i < customSatellitePoints.Count; i++)
                {
                    SatellitePoint p = customSatellitePoints[i];
                    GUILayout.BeginHorizontal("box");
                    p.enabled = GUILayout.Toggle(p.enabled, "", GUILayout.Width(20));
                    GUILayout.Label("P" + (i + 1), GUILayout.Width(28));
                    p.x = EditorGUILayout.FloatField("X", p.x);
                    p.z = EditorGUILayout.FloatField("Z", p.z);
                    if (GUILayout.Button("X", GUILayout.Width(24)))
                        removeIndex = i;
                    GUILayout.EndHorizontal();
                }
                if (removeIndex >= 0 && removeIndex < customSatellitePoints.Count)
                    customSatellitePoints.RemoveAt(removeIndex);
                GUILayout.EndScrollView();

                int enabledCount = BuildEnabledCustomSatelliteOffsets().Count;
                EditorGUILayout.HelpBox("Enabled custom points: " + enabledCount, MessageType.Info);
            }
        }
        else
        {
            GUILayout.Space(10);
            borderFlat = GUILayout.Toggle(borderFlat, "Border Flat", GUILayout.Width(240));
        }

        GUILayout.Space(10);
        autoGenerateBuildingsAfterStreets = GUILayout.Toggle(autoGenerateBuildingsAfterStreets, "Auto Generate Buildings", GUILayout.Width(240));
        autoAddTrafficAfterStreets = GUILayout.Toggle(autoAddTrafficAfterStreets, "Auto Add Traffic", GUILayout.Width(240));

        GUILayout.Space(10);


        if (GUILayout.Button("Clear Streets "))
        {
            cityGenerator.ClearCity();
            generationStatus = "Streets cleared.";
            SaveEditorStateToPrefs();
        }

        GUILayout.Space(10);

        GUILayout.EndVertical();

        GUILayout.Space(10);



        GUILayout.BeginVertical("box");

        GUILayout.Space(5);

        GUILayout.Label(new GUIContent("Buildings", "Make or Clear Buildings"));

        GUILayout.Space(5);

        GUILayout.BeginHorizontal("box");


        GUILayout.Space(5);

        if (GUILayout.Button("Generate Buildings"))
        {
            if (!GameObject.Find("Marcador")) return;

            LoadAssets(true);

            cityGenerator.GenerateAllBuildings(withDowntownArea, downTownSize);
            enableUpdate = 1;
            generationStatus = "Buildings generated.";
            SaveEditorStateToPrefs();



        }


        if (GUILayout.Button("Clear Buildings"))
        {
            if (!GameObject.Find("Marcador")) return;
            cityGenerator.DestroyBuildings();
            generationStatus = "Buildings cleared.";
            //DestroyImmediate(GameObject.Find("CarContainer"));
        }






        GUILayout.EndHorizontal();

        withDowntownArea = GUILayout.Toggle(withDowntownArea, "With Downtown Area?", GUILayout.Width(240));

        if (withDowntownArea)
        {
            GUILayout.Space(10);
            GUILayout.Label(new GUIContent("DownTown Size:", "DownTown Size"));
            downTownSize = EditorGUILayout.Slider(downTownSize, 50, 200);
            GUILayout.Space(10);
        }

        GUILayout.EndVertical();




        GUILayout.Space(10);



        GUILayout.BeginVertical("box");

        GUILayout.Space(5);

        GUILayout.Label(new GUIContent("Traffic System", "Make or Clear Traffic System"));

        GUILayout.Space(5);


        GUILayout.BeginHorizontal("box");

        GUILayout.Space(5);

        if (GUILayout.Button("Add Traffic System"))
        {
            AddVehicles(trafficLightHand);
            generationStatus = "Traffic updated.";
            SaveEditorStateToPrefs();
        }


        if (GUILayout.Button("Remove Traffic System"))
        {
            TrafficSystem traffic = GameObject.FindObjectOfType<TrafficSystem>();
            if (traffic) DestroyImmediate(traffic.gameObject);
            GameObject carContainer = GameObject.Find("CarContainer");
            if (carContainer) DestroyImmediate(carContainer);
            generationStatus = "Traffic removed.";
        }

        GUILayout.Space(5);

        GUILayout.EndHorizontal();

        GUILayout.Space(5);


        GUILayout.Space(5);

        GUILayout.BeginVertical("box");
        GUILayout.Label(new GUIContent("Traffic Hand", "Hand Right/Left"));
        int rh = trafficLightHand;
        trafficLightHand = GUILayout.SelectionGrid(trafficLightHand, selStrings, 2);
        GUILayout.EndVertical();

        bool japanTL = japanTrafficLight;

        if (trafficLightHand != 0)
        {
            japanTrafficLight = GUILayout.Toggle(japanTrafficLight, "Japan Traffic Light (blue)", GUILayout.Width(240));
        }


        if (rh != trafficLightHand || japanTL != japanTrafficLight)
        {
            rh = trafficLightHand;
            japanTL = japanTrafficLight;

            if (GameObject.Find("CarContainer"))
                AddVehicles((trafficLightHand == 1 && japanTrafficLight) ? 2 : trafficLightHand);
            else
                InverseCarDirection((trafficLightHand == 1 && japanTrafficLight) ? 2 : trafficLightHand);

        }


        GUILayout.EndVertical();


        GUILayout.Space(10);

        GUILayout.BeginVertical("box");


        if (GUILayout.Button("Combine Meshes"))
        {


            if (!GameObject.Find("Marcador")) return;


            //It is necessary to remove LODs from buildings before combining meshes
            if (!EditorUtility.DisplayDialog("Mesh combine",
                "Mesh combine the buildings will remove the LODs.\n\nDo you still want to continue? ", "Yes", "No"))
                return;

            float vertexCount = 0;
            float tt;
            GameObject module;
            GameObject[] my_Modules;

            my_Modules = GameObject.FindObjectsOfType(typeof(GameObject)).Select(g => g as GameObject).Where(g => g.name == "Marcador").ToArray();

            tt = my_Modules.Length;

            vertexCount = 0;

            for (int i = 0; i < tt; i++)
            {

                vertexCount = 0;

                module = my_Modules[i];

                GameObject newBlock = new GameObject("_block");
                newBlock.transform.position = module.transform.position;
                newBlock.transform.rotation = module.transform.rotation;
                newBlock.transform.parent = module.transform.parent;

                foreach (Transform child in module.transform)
                {  // E1, E2, 100

                    Component[] temp = child.GetComponentsInChildren(typeof(MeshFilter));

                    //Remove LODs from Buildings before Combine Meshes
                    foreach (MeshFilter currentChild in temp)
                        if (currentChild.gameObject.name.Contains("_LOD"))
                            DestroyImmediate(currentChild.gameObject);

                    temp = child.GetComponentsInChildren(typeof(MeshFilter));

                    foreach (MeshFilter currentChild in temp)
                    {

                        vertexCount += currentChild.sharedMesh.vertexCount;
                        if (vertexCount > 50000)
                        {
                            vertexCount = 0;
                            newBlock = new GameObject("_block");
                            newBlock.transform.position = module.transform.position;
                            newBlock.transform.rotation = module.transform.rotation;
                            newBlock.transform.parent = module.transform.parent;
                        }

                        if (currentChild.gameObject.name.Contains("(Clone)"))
                        {
                            currentChild.gameObject.transform.parent = newBlock.transform;
                        }


                    }


                }

                if (my_Modules[i])
                    DestroyImmediate(my_Modules[i].gameObject);

            }



            GameObject[] myModules = GameObject.FindObjectsOfType(typeof(GameObject)).Select(g => g as GameObject).Where(g => g.name == "_block").ToArray();


            tt = myModules.Length;



            for (int i = 0; i < tt; i++)
            {

                float f = i / tt;

                EditorUtility.DisplayProgressBar("Combining meshes", "Please wait", f);

                module = myModules[i];

                GameObject newObjects = new GameObject("Combined meshes");
                newObjects.transform.parent = module.transform.parent;
                newObjects.transform.localPosition = Vector3.zero;
                newObjects.transform.localRotation = Quaternion.identity;

                CombineMeshes(module.gameObject, newObjects);

            }

            EditorUtility.ClearProgressBar();


        }

        generateLightmapUVs = GUILayout.Toggle(generateLightmapUVs, "Generate Lightmap UVs", GUILayout.Width(240));

        GUILayout.EndVertical();

        if (EditorGUI.EndChangeCheck())
            SaveEditorStateToPrefs();

    }


    private TrafficSystem trafficSystem;

    private void AddVehicles(int right_Hand = 0)
    {

        trafficSystem = FindObjectOfType<TrafficSystem>();

        if (!trafficSystem)
        {
            Instantiate((GameObject)AssetDatabase.LoadAssetAtPath("Assets/Fantastic City Generator/Traffic System/Traffic System.prefab", (typeof(GameObject))));
            trafficSystem = FindObjectOfType<TrafficSystem>();

        }

        if (!trafficSystem)
        {
            Debug.LogError("Add the Traffic System.prefab to Hierarchy");
            return;
        }
        else trafficSystem.name = "Traffic System";

        if (trafficSystem)
        {
            GameObject carContainer = GameObject.Find("CarContainer");
            if (carContainer) DestroyImmediate(carContainer);
            trafficSystem.LoadCars(right_Hand);
            generationStatus = "Traffic loaded.";
        }
    }

    private void InverseCarDirection(int trafficHand)
    {

        if (FindObjectOfType<TrafficSystem>())
            trafficSystem = FindObjectOfType<TrafficSystem>();

        if (!trafficSystem)
        {
            //Instantiate((GameObject)AssetDatabase.LoadAssetAtPath("Assets/Fantastic City Generator/Traffic System/Traffic System.prefab", (typeof(GameObject))));
            trafficSystem = AssetDatabase.LoadAssetAtPath("Assets/Fantastic City Generator/Traffic System/Traffic System.prefab", (typeof(TrafficSystem))) as TrafficSystem;
        }

        if (!trafficSystem)
        {
            Debug.LogError("Not Found System.prefab");
            return;
        }

        trafficSystem.DeffineDirection(trafficHand);

        if (GameObject.Find("CarContainer"))
            AddVehicles((trafficLightHand == 1 && japanTrafficLight) ? 2 : trafficLightHand);

    }

    private List<GameObject> newObjects = new List<GameObject>();


    public void CombineMeshes(GameObject objs, GameObject _Objects)
    {



        // Preserve Cloths
        Component[] temp = objs.GetComponentsInChildren(typeof(Cloth));
        foreach (Cloth currentChild in temp)
        {
            currentChild.gameObject.transform.parent = _Objects.transform;
            //currentChild.gameObject.isStatic = false;
        }


        //Preserve BoxCollider components
        temp = objs.GetComponentsInChildren(typeof(BoxCollider));
        foreach (BoxCollider currentChild in temp)
        {

            GameObject bc = new GameObject("BoxCollider");
            bc.transform.position = currentChild.transform.position;
            bc.transform.rotation = currentChild.transform.rotation;
            bc.transform.localScale = currentChild.transform.localScale;
            bc.transform.parent = _Objects.transform;

            UnityEditorInternal.ComponentUtility.CopyComponent(currentChild);
            UnityEditorInternal.ComponentUtility.PasteComponentAsNew(bc);

        }

        //Preserve MeshCollider components
        temp = objs.GetComponentsInChildren(typeof(MeshCollider));
        foreach (MeshCollider currentChild in temp)
        {

            GameObject bc = new GameObject("MeshCollider");
            bc.transform.position = currentChild.transform.position;
            bc.transform.rotation = currentChild.transform.rotation;
            bc.transform.localScale = currentChild.transform.parent.localScale;

            bc.transform.parent = _Objects.transform;

            UnityEditorInternal.ComponentUtility.CopyComponent(currentChild);
            UnityEditorInternal.ComponentUtility.PasteComponentAsNew(bc);

        }



        newObjects.Clear();

        Combine2(objs, _Objects);

    }




    private void Combine2(GameObject _objs, GameObject _Objects)
    {



        GameObject oldGameObjects = _objs;

        Component[] filters = GetMeshFilters(_objs);

        Matrix4x4 myTransform = _objs.transform.worldToLocalMatrix;
        Hashtable materialToMesh = new Hashtable();

        for (int i = 0; i < filters.Length; i++)
        {


            MeshFilter filter = (MeshFilter)filters[i];
            Renderer curRenderer = filters[i].GetComponent<Renderer>();
            Mesh_CombineUtility.MeshInstance instance = new Mesh_CombineUtility.MeshInstance();
            instance.mesh = filter.sharedMesh;
            if (curRenderer != null && curRenderer.enabled && instance.mesh != null)
            {
                instance.transform = myTransform * filter.transform.localToWorldMatrix;

                Material[] materials = curRenderer.sharedMaterials;
                for (int m = 0; m < materials.Length; m++)
                {


                    instance.subMeshIndex = System.Math.Min(m, instance.mesh.subMeshCount - 1);

                    try
                    {
                        ArrayList objects = (ArrayList)materialToMesh[materials[m]];

                        if (objects != null)
                            objects.Add(instance);
                        else
                        {
                            objects = new ArrayList();
                            objects.Add(instance);
                            materialToMesh.Add(materials[m], objects);
                        }


                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.Message + "   Verify materials in " + curRenderer.name);

                    }



                }
            }
        }



        foreach (DictionaryEntry mtm in materialToMesh)
        {
            ArrayList elements = (ArrayList)mtm.Value;

            Mesh_CombineUtility.MeshInstance[] instances = (Mesh_CombineUtility.MeshInstance[])elements.ToArray(typeof(Mesh_CombineUtility.MeshInstance));


            Material mat = (Material)mtm.Key;

            GameObject go = new GameObject(mat.name);

            go.transform.localScale = Vector3.one;
            go.transform.localPosition = Vector3.zero;
            go.transform.position = Vector3.zero;

            go.AddComponent(typeof(MeshFilter));
            go.AddComponent<MeshRenderer>();
            go.GetComponent<Renderer>().material = (Material)mtm.Key;


            MeshFilter filter = (MeshFilter)go.GetComponent(typeof(MeshFilter));
            filter.sharedMesh = Mesh_CombineUtility.Combine(instances, false);

            newObjects.Add(go);

        }

        if (newObjects.Count < 1)
        {
            return;
        }


        DestroyImmediate(oldGameObjects);


        if (newObjects.Count > 0)
        {
            for (int x = 0; x < newObjects.Count; x++)
            {


                newObjects[x].transform.parent = _Objects.transform;
                newObjects[x].transform.localPosition = Vector3.zero;
                newObjects[x].transform.localRotation = Quaternion.identity;

                // Generate Lightmap UVs ?
                if (generateLightmapUVs)
                {
                    Unwrapping.GenerateSecondaryUVSet(newObjects[x].GetComponent<MeshFilter>().sharedMesh);
                }



            }
        }





    }

    private Component[] GetMeshFilters(GameObject objs)
    {
        List<Component> filters = new List<Component>();
        Component[] temp = null;

        temp = objs.GetComponentsInChildren(typeof(MeshFilter));
        for (int y = 0; y < temp.Length; y++)
            filters.Add(temp[y]);

        return filters.ToArray();

    }



    public static List<T> LoadAllPrefabsOfType<T>(string path) where T : MonoBehaviour
    {
        if (path != "")
        {
            if (path.EndsWith("/"))
            {
                path = path.TrimEnd('/');
            }
        }

        DirectoryInfo dirInfo = new DirectoryInfo(path);
        FileInfo[] fileInf = dirInfo.GetFiles("*.prefab");

        //loop through directory loading the game object and checking if it has the component you want
        List<T> prefabComponents = new List<T>();
        foreach (FileInfo fileInfo in fileInf)
        {
            string fullPath = fileInfo.FullName.Replace(@"\", "/");
            string assetPath = "Assets" + fullPath.Replace(Application.dataPath, "");
            GameObject prefab = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;

            if (prefab != null)
            {
                T hasT = prefab.GetComponent<T>();
                if (hasT != null)
                {
                    prefabComponents.Add(hasT);
                }
            }
        }
        return prefabComponents;
    }







}
