using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using FCG;

public class RuntimeGenerationControlUI : MonoBehaviour
{
    [Serializable]
    private class RuntimePanelPreset
    {
        public int citySize = 3;
        public bool clearBeforeGenerate = true;
        public bool autoGenerateBuildings = true;
        public bool autoAddTraffic = true;
        public bool useRequestObject = true;
        public bool requestFollowsInspector = true;

        public bool withSatelliteCity = false;
        public bool borderFlat = false;
        public int satelliteCityCount = 1;
        public bool connectSatellitesToMain = true;
        public bool connectSatellitesTogether = true;
        public int satelliteConnectionMode = 1;
        public int satelliteMaxNeighborLinks = 1;
        public bool satelliteCloseLoop = false;
        public float satelliteConnectionStep = 0f;
        public bool randomSatelliteLayout = false;
        public bool useCustomSatelliteOffsets = false;
        public bool useSatelliteSeed = false;
        public int satelliteSeed = 12345;
        public Vector2 randomSatelliteMin = new Vector2(-1000f, -2200f);
        public Vector2 randomSatelliteMax = new Vector2(1000f, -1200f);
        public Vector2 satelliteGlobalOffset = Vector2.zero;
        public List<Vector2> customSatelliteOffsets = new List<Vector2>();
        public float satelliteBuildingDensity = 1f;

        public bool createCityAnchors = true;
        public bool createConnectionDebugLines = false;
        public float connectionDebugLineHeight = 3f;

        public bool withDownTownArea = true;
        public float downTownSize = 100f;

        public bool rightHand = true;
        public bool japanTrafficLight = false;
    }

    [Header("References")]
    public RunTimeSample runtimeSample;

    [Header("Window")]
    public bool showOnStart = true;
    public KeyCode togglePanelKey = KeyCode.F8;
    public Rect windowRect = new Rect(20f, 20f, 460f, 760f);
    public int maxCustomPoints = 64;

    private bool visible;
    private bool isNight;
    private Vector2 scrollPosition;
    private string status = "Ready.";
    private string presetPath;

    private void Awake()
    {
        visible = showOnStart;
        presetPath = Path.Combine(Application.persistentDataPath, "fcg-runtime-preset.json");

        if (!runtimeSample)
            runtimeSample = FindObjectOfType<RunTimeSample>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(togglePanelKey))
            visible = !visible;
    }

    private bool EnsureRuntimeSample()
    {
        if (!runtimeSample)
            runtimeSample = FindObjectOfType<RunTimeSample>();

        return runtimeSample != null;
    }

    private void OnGUI()
    {
        if (!visible)
            return;

        windowRect = GUILayout.Window(31991, windowRect, DrawWindow, "Fantastic City Generator - Runtime Panel");
    }

    private void DrawWindow(int windowId)
    {
        if (!EnsureRuntimeSample())
        {
            GUILayout.Label("RunTimeSample was not found in scene.");
            if (GUILayout.Button("Retry Search"))
                runtimeSample = FindObjectOfType<RunTimeSample>();
            GUI.DragWindow();
            return;
        }

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label(runtimeSample.GetLastGenerationSummary());
        GUILayout.Label(runtimeSample.GetLastGenerationNetworkSummary());
        GUILayout.Label("Status: " + status);

        GUILayout.Space(6f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate Streets"))
        {
            runtimeSample.GenerateCityAtRuntime();
            status = "Streets generated.";
        }
        if (GUILayout.Button("Full Pipeline"))
        {
            runtimeSample.GenerateFullPipeline();
            status = "Full pipeline generated.";
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Buildings"))
        {
            runtimeSample.GenerateBuildings();
            status = "Buildings generated.";
        }
        if (GUILayout.Button("Traffic"))
        {
            runtimeSample.AddTrafficSystem();
            status = "Traffic refreshed.";
        }
        if (GUILayout.Button("Night/Day"))
        {
            isNight = !isNight;
            runtimeSample.SetNight(isNight);
            status = isNight ? "Night mode enabled." : "Day mode enabled.";
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(8f);
        GUILayout.Label("Pipeline", GUI.skin.box);
        runtimeSample.citySize = Mathf.RoundToInt(GUILayout.HorizontalSlider(runtimeSample.citySize, 1f, 4f));
        GUILayout.Label("City Size: " + runtimeSample.citySize);

        runtimeSample.clearBeforeGenerate = GUILayout.Toggle(runtimeSample.clearBeforeGenerate, "Clear Before Generate");
        runtimeSample.autoGenerateBuildings = GUILayout.Toggle(runtimeSample.autoGenerateBuildings, "Auto Generate Buildings");
        runtimeSample.autoAddTraffic = GUILayout.Toggle(runtimeSample.autoAddTraffic, "Auto Add Traffic");
        runtimeSample.useRequestObject = GUILayout.Toggle(runtimeSample.useRequestObject, "Use Request Object");
        runtimeSample.requestFollowsInspector = GUILayout.Toggle(runtimeSample.requestFollowsInspector, "Request Follows Inspector");

        GUILayout.Space(8f);
        GUILayout.Label("Streets & Satellite Cities", GUI.skin.box);
        runtimeSample.withSatelliteCity = GUILayout.Toggle(runtimeSample.withSatelliteCity, "With Satellite City");

        if (!runtimeSample.withSatelliteCity)
        {
            runtimeSample.borderFlat = GUILayout.Toggle(runtimeSample.borderFlat, "Border Flat");
        }
        else
        {
            runtimeSample.satelliteCityCount = Mathf.RoundToInt(GUILayout.HorizontalSlider(runtimeSample.satelliteCityCount, 1f, 64f));
            GUILayout.Label("Satellite Count: " + runtimeSample.satelliteCityCount);
            runtimeSample.connectSatellitesToMain = GUILayout.Toggle(runtimeSample.connectSatellitesToMain, "Connect To Main");
            runtimeSample.connectSatellitesTogether = GUILayout.Toggle(runtimeSample.connectSatellitesTogether, "Connect Satellites Together");

            int mode = (int)runtimeSample.satelliteConnectionMode;
            string[] modeNames = Enum.GetNames(typeof(CityGenerator.SatelliteConnectionMode));
            mode = GUILayout.SelectionGrid(mode, modeNames, 2);
            runtimeSample.satelliteConnectionMode = (CityGenerator.SatelliteConnectionMode)Mathf.Clamp(mode, 0, modeNames.Length - 1);

            if (runtimeSample.satelliteConnectionMode == CityGenerator.SatelliteConnectionMode.Nearest)
            {
                runtimeSample.satelliteMaxNeighborLinks = Mathf.RoundToInt(GUILayout.HorizontalSlider(runtimeSample.satelliteMaxNeighborLinks, 1f, 6f));
                GUILayout.Label("Nearest Links: " + runtimeSample.satelliteMaxNeighborLinks);
            }

            runtimeSample.satelliteCloseLoop = GUILayout.Toggle(runtimeSample.satelliteCloseLoop, "Close Loop");
            runtimeSample.satelliteConnectionStep = GUILayout.HorizontalSlider(runtimeSample.satelliteConnectionStep, 0f, 500f);
            GUILayout.Label("Road Segment Length: " + runtimeSample.satelliteConnectionStep.ToString("0.0") + " (0 = Auto)");

            runtimeSample.randomSatelliteSizes = GUILayout.Toggle(runtimeSample.randomSatelliteSizes, "Random Satellite Sizes");
            if (runtimeSample.randomSatelliteSizes)
            {
                runtimeSample.satelliteCityMinSize = Mathf.RoundToInt(GUILayout.HorizontalSlider(runtimeSample.satelliteCityMinSize, 1f, 4f));
                runtimeSample.satelliteCityMaxSize = Mathf.RoundToInt(GUILayout.HorizontalSlider(runtimeSample.satelliteCityMaxSize, 1f, 4f));
                GUILayout.Label("Satellite Size Range: " + runtimeSample.satelliteCityMinSize + " - " + runtimeSample.satelliteCityMaxSize);
            }

            int layoutMode = runtimeSample.randomSatelliteLayout ? 1 : (runtimeSample.useCustomSatelliteOffsets ? 2 : 0);
            layoutMode = GUILayout.Toolbar(layoutMode, new string[] { "Preset", "Random", "Custom" });
            runtimeSample.randomSatelliteLayout = layoutMode == 1;
            runtimeSample.useCustomSatelliteOffsets = layoutMode == 2;

            runtimeSample.satelliteGlobalOffset.x = GUILayout.HorizontalSlider(runtimeSample.satelliteGlobalOffset.x, -3000f, 3000f);
            runtimeSample.satelliteGlobalOffset.y = GUILayout.HorizontalSlider(runtimeSample.satelliteGlobalOffset.y, -3000f, 3000f);
            GUILayout.Label("Global Offset X/Z: " + runtimeSample.satelliteGlobalOffset.x.ToString("0") + " / " + runtimeSample.satelliteGlobalOffset.y.ToString("0"));

            if (runtimeSample.randomSatelliteLayout)
            {
                runtimeSample.useSatelliteSeed = GUILayout.Toggle(runtimeSample.useSatelliteSeed, "Use Fixed Seed");
                if (runtimeSample.useSatelliteSeed)
                {
                    runtimeSample.satelliteSeed = Mathf.RoundToInt(GUILayout.HorizontalSlider(runtimeSample.satelliteSeed, 1f, 999999f));
                    GUILayout.Label("Seed: " + runtimeSample.satelliteSeed);
                }

                runtimeSample.randomSatelliteMin.x = GUILayout.HorizontalSlider(runtimeSample.randomSatelliteMin.x, -4000f, 4000f);
                runtimeSample.randomSatelliteMax.x = GUILayout.HorizontalSlider(runtimeSample.randomSatelliteMax.x, -4000f, 4000f);
                runtimeSample.randomSatelliteMin.y = GUILayout.HorizontalSlider(runtimeSample.randomSatelliteMin.y, -4000f, 4000f);
                runtimeSample.randomSatelliteMax.y = GUILayout.HorizontalSlider(runtimeSample.randomSatelliteMax.y, -4000f, 4000f);

                GUILayout.Label("Random X min/max: " + runtimeSample.randomSatelliteMin.x.ToString("0") + " / " + runtimeSample.randomSatelliteMax.x.ToString("0"));
                GUILayout.Label("Random Z min/max: " + runtimeSample.randomSatelliteMin.y.ToString("0") + " / " + runtimeSample.randomSatelliteMax.y.ToString("0"));
            }
            // City-level seed and auto-satellite count
            runtimeSample.useCitySeed = GUILayout.Toggle(runtimeSample.useCitySeed, "Use City Seed");
            if (runtimeSample.useCitySeed)
            {
                runtimeSample.citySeed = Mathf.RoundToInt(GUILayout.HorizontalSlider(runtimeSample.citySeed, 1f, 999999f));
                GUILayout.Label("City Seed: " + runtimeSample.citySeed);
            }

            runtimeSample.autoSatelliteCount = GUILayout.Toggle(runtimeSample.autoSatelliteCount, "Auto Satellite Count (based on city size)");
            else if (runtimeSample.useCustomSatelliteOffsets)
            {
                if (runtimeSample.customSatelliteOffsets == null)
                    runtimeSample.customSatelliteOffsets = new List<Vector2>();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Point") && runtimeSample.customSatelliteOffsets.Count < maxCustomPoints)
                    runtimeSample.customSatelliteOffsets.Add(new Vector2(0f, -1516f));
                if (GUILayout.Button("Clear Points"))
                    runtimeSample.customSatelliteOffsets.Clear();
                GUILayout.EndHorizontal();

                int removeIndex = -1;
                for (int i = 0; i < runtimeSample.customSatelliteOffsets.Count; i++)
                {
                    Vector2 point = runtimeSample.customSatelliteOffsets[i];
                    GUILayout.BeginHorizontal(GUI.skin.box);
                    GUILayout.Label("P" + (i + 1), GUILayout.Width(30f));
                    point.x = GUILayout.HorizontalSlider(point.x, -4000f, 4000f);
                    point.y = GUILayout.HorizontalSlider(point.y, -4000f, 4000f);
                    if (GUILayout.Button("X", GUILayout.Width(24f)))
                        removeIndex = i;
                    GUILayout.EndHorizontal();
                    GUILayout.Label("X/Z: " + point.x.ToString("0") + " / " + point.y.ToString("0"));
                    runtimeSample.customSatelliteOffsets[i] = point;
                }

                if (removeIndex >= 0 && removeIndex < runtimeSample.customSatelliteOffsets.Count)
                    runtimeSample.customSatelliteOffsets.RemoveAt(removeIndex);
            }

            runtimeSample.createCityAnchors = GUILayout.Toggle(runtimeSample.createCityAnchors, "Create City Anchors");
            runtimeSample.createConnectionDebugLines = GUILayout.Toggle(runtimeSample.createConnectionDebugLines, "Draw Debug Links");
            if (runtimeSample.createConnectionDebugLines)
            {
                runtimeSample.connectionDebugLineHeight = GUILayout.HorizontalSlider(runtimeSample.connectionDebugLineHeight, 0f, 20f);
                GUILayout.Label("Debug Link Height: " + runtimeSample.connectionDebugLineHeight.ToString("0.0"));
            }

            runtimeSample.satelliteBuildingDensity = GUILayout.HorizontalSlider(runtimeSample.satelliteBuildingDensity, 0f, 1f);
            GUILayout.Label("Satellite Building Density: " + runtimeSample.satelliteBuildingDensity.ToString("0.00"));
        }

        GUILayout.Space(8f);
        GUILayout.Label("Buildings", GUI.skin.box);
        bool downtown = runtimeSample.IsDownTownAreaEnabled();
        bool newDowntown = GUILayout.Toggle(downtown, "With Downtown Area");
        if (newDowntown != downtown)
            runtimeSample.WithDownTownArea(newDowntown);

        runtimeSample.downTownSize = GUILayout.HorizontalSlider(runtimeSample.downTownSize, 50f, 200f);
        GUILayout.Label("Downtown Size: " + runtimeSample.downTownSize.ToString("0"));

        GUILayout.Space(8f);
        GUILayout.Label("Traffic", GUI.skin.box);
        bool rightHand = runtimeSample.IsRightHandTraffic();
        bool newRightHand = GUILayout.Toggle(rightHand, "Right Hand Traffic");
        if (newRightHand != rightHand)
            runtimeSample.RightHand(newRightHand);

        if (!newRightHand)
            runtimeSample.japanTrafficLight = GUILayout.Toggle(runtimeSample.japanTrafficLight, "Japan Traffic Light");

        GUILayout.Space(8f);
        GUILayout.Label("Presets", GUI.skin.box);
        GUILayout.Label("Path: " + presetPath);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Preset"))
            SavePreset();
        if (GUILayout.Button("Load Preset"))
            LoadPreset();
        if (GUILayout.Button("Reset"))
            ResetSettings();
        GUILayout.EndHorizontal();

        GUILayout.EndScrollView();

        GUI.DragWindow();
    }

    private RuntimePanelPreset CapturePreset()
    {
        RuntimePanelPreset preset = new RuntimePanelPreset();

        preset.citySize = runtimeSample.citySize;
        preset.clearBeforeGenerate = runtimeSample.clearBeforeGenerate;
        preset.autoGenerateBuildings = runtimeSample.autoGenerateBuildings;
        preset.autoAddTraffic = runtimeSample.autoAddTraffic;
        preset.useRequestObject = runtimeSample.useRequestObject;
        preset.requestFollowsInspector = runtimeSample.requestFollowsInspector;

        preset.withSatelliteCity = runtimeSample.withSatelliteCity;
        preset.borderFlat = runtimeSample.borderFlat;
        preset.satelliteCityCount = runtimeSample.satelliteCityCount;
        preset.connectSatellitesToMain = runtimeSample.connectSatellitesToMain;
        preset.connectSatellitesTogether = runtimeSample.connectSatellitesTogether;
        preset.satelliteConnectionMode = (int)runtimeSample.satelliteConnectionMode;
        preset.satelliteMaxNeighborLinks = runtimeSample.satelliteMaxNeighborLinks;
        preset.satelliteCloseLoop = runtimeSample.satelliteCloseLoop;
        preset.satelliteConnectionStep = runtimeSample.satelliteConnectionStep;
        preset.randomSatelliteLayout = runtimeSample.randomSatelliteLayout;
        preset.useCustomSatelliteOffsets = runtimeSample.useCustomSatelliteOffsets;
        preset.useSatelliteSeed = runtimeSample.useSatelliteSeed;
        preset.satelliteSeed = runtimeSample.satelliteSeed;
        preset.randomSatelliteMin = runtimeSample.randomSatelliteMin;
        preset.randomSatelliteMax = runtimeSample.randomSatelliteMax;
        preset.satelliteGlobalOffset = runtimeSample.satelliteGlobalOffset;
        preset.customSatelliteOffsets = (runtimeSample.customSatelliteOffsets != null)
            ? new List<Vector2>(runtimeSample.customSatelliteOffsets)
            : new List<Vector2>();
        preset.satelliteBuildingDensity = runtimeSample.satelliteBuildingDensity;

        preset.createCityAnchors = runtimeSample.createCityAnchors;
        preset.createConnectionDebugLines = runtimeSample.createConnectionDebugLines;
        preset.connectionDebugLineHeight = runtimeSample.connectionDebugLineHeight;

        preset.withDownTownArea = runtimeSample.IsDownTownAreaEnabled();
        preset.downTownSize = runtimeSample.downTownSize;
        preset.rightHand = runtimeSample.IsRightHandTraffic();
        preset.japanTrafficLight = runtimeSample.japanTrafficLight;

        return preset;
    }

    private void ApplyPreset(RuntimePanelPreset preset)
    {
        if (preset == null)
            return;

        runtimeSample.citySize = Mathf.Clamp(preset.citySize, 1, 4);
        runtimeSample.clearBeforeGenerate = preset.clearBeforeGenerate;
        runtimeSample.autoGenerateBuildings = preset.autoGenerateBuildings;
        runtimeSample.autoAddTraffic = preset.autoAddTraffic;
        runtimeSample.useRequestObject = preset.useRequestObject;
        runtimeSample.requestFollowsInspector = preset.requestFollowsInspector;

        runtimeSample.withSatelliteCity = preset.withSatelliteCity;
        runtimeSample.borderFlat = preset.borderFlat;
        runtimeSample.satelliteCityCount = Mathf.Clamp(preset.satelliteCityCount, 1, 64);
        runtimeSample.connectSatellitesToMain = preset.connectSatellitesToMain;
        runtimeSample.connectSatellitesTogether = preset.connectSatellitesTogether;
        runtimeSample.satelliteConnectionMode = (CityGenerator.SatelliteConnectionMode)Mathf.Clamp(preset.satelliteConnectionMode, 0, 3);
        runtimeSample.satelliteMaxNeighborLinks = Mathf.Clamp(preset.satelliteMaxNeighborLinks, 1, 6);
        runtimeSample.satelliteCloseLoop = preset.satelliteCloseLoop;
        runtimeSample.satelliteConnectionStep = Mathf.Max(0f, preset.satelliteConnectionStep);
        runtimeSample.randomSatelliteLayout = preset.randomSatelliteLayout;
        runtimeSample.useCustomSatelliteOffsets = preset.useCustomSatelliteOffsets;
        if (runtimeSample.randomSatelliteLayout)
            runtimeSample.useCustomSatelliteOffsets = false;
        runtimeSample.useSatelliteSeed = preset.useSatelliteSeed;
        runtimeSample.satelliteSeed = preset.satelliteSeed;
        runtimeSample.randomSatelliteMin = preset.randomSatelliteMin;
        runtimeSample.randomSatelliteMax = preset.randomSatelliteMax;
        runtimeSample.satelliteGlobalOffset = preset.satelliteGlobalOffset;
        runtimeSample.customSatelliteOffsets = (preset.customSatelliteOffsets != null)
            ? new List<Vector2>(preset.customSatelliteOffsets)
            : new List<Vector2>();
        runtimeSample.satelliteBuildingDensity = preset.satelliteBuildingDensity;

        runtimeSample.createCityAnchors = preset.createCityAnchors;
        runtimeSample.createConnectionDebugLines = preset.createConnectionDebugLines;
        runtimeSample.connectionDebugLineHeight = Mathf.Clamp(preset.connectionDebugLineHeight, 0f, 20f);

        runtimeSample.WithDownTownArea(preset.withDownTownArea);
        runtimeSample.downTownSize = Mathf.Clamp(preset.downTownSize, 50f, 200f);
        runtimeSample.RightHand(preset.rightHand);
        runtimeSample.japanTrafficLight = preset.japanTrafficLight;
    }

    private void SavePreset()
    {
        try
        {
            RuntimePanelPreset preset = CapturePreset();
            string json = JsonUtility.ToJson(preset, true);
            File.WriteAllText(presetPath, json);
            status = "Preset saved.";
        }
        catch (Exception ex)
        {
            status = "Save failed: " + ex.Message;
        }
    }

    private void LoadPreset()
    {
        try
        {
            if (!File.Exists(presetPath))
            {
                status = "Preset file not found.";
                return;
            }

            string json = File.ReadAllText(presetPath);
            RuntimePanelPreset preset = JsonUtility.FromJson<RuntimePanelPreset>(json);
            ApplyPreset(preset);
            status = "Preset loaded.";
        }
        catch (Exception ex)
        {
            status = "Load failed: " + ex.Message;
        }
    }

    private void ResetSettings()
    {
        ApplyPreset(new RuntimePanelPreset());
        status = "Reset to defaults.";
    }
}

