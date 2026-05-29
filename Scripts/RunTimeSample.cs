using System.Collections.Generic;
using UnityEngine;
using FCG;

public class RunTimeSample : MonoBehaviour
{
    [Header("References")]
    public GameObject cg;
    public TrafficSystem trafficSystem;
    public CityGenerationProfile profile;
    public bool applyProfileOnAwake = false;

    [Header("Pipeline")]
    [Range(1, 4)] public int citySize = 3;
    public bool clearBeforeGenerate = true;
    public bool autoGenerateBuildings = true;
    public bool autoAddTraffic = true;
    public bool useRequestObject = true;
    public bool requestFollowsInspector = true;
    public CityGenerationRequest request = new CityGenerationRequest();

    [Header("Streets")]
    public bool withSatelliteCity = false;
    public bool borderFlat = false;
    [Range(1, 64)] public int satelliteCityCount = 1;
    public bool connectSatellitesToMain = true;
    public bool connectSatellitesTogether = true;
    public bool randomSatelliteSizes = false;
    [Range(1, 4)] public int satelliteCityMinSize = 1;
    [Range(1, 4)] public int satelliteCityMaxSize = 1;
    public CityGenerator.SatelliteConnectionMode satelliteConnectionMode = CityGenerator.SatelliteConnectionMode.Chain;
    [Range(1, 6)] public int satelliteMaxNeighborLinks = 1;
    public bool satelliteCloseLoop = false;
    public float satelliteConnectionStep = 0f;
    public bool randomSatelliteLayout = false;
    public bool useCustomSatelliteOffsets = false;
    public bool useSatelliteSeed = false;
    public int satelliteSeed = 12345;
    public bool useCitySeed = false;
    public int citySeed = 123456;
    public bool autoSatelliteCount = false;
    public Vector2 randomSatelliteMin = new Vector2(-1000f, -2200f);
    public Vector2 randomSatelliteMax = new Vector2(1000f, -1200f);
    public Vector2 satelliteGlobalOffset = Vector2.zero;
    public List<Vector2> customSatelliteOffsets = new List<Vector2>();
    public bool createCityAnchors = true;
    public bool createConnectionDebugLines = false;
    [Range(0f, 20f)] public float connectionDebugLineHeight = 3f;
    [Range(0f,1f)] public float satelliteBuildingDensity = 1f;

    [Header("Highways")]
    public bool generateHighways = true;
    [Range(8f, 40f)] public float highwayWidth = 18f;
    [Range(0.05f, 1f)] public float highwayThickness = 0.2f;

    [Header("Buildings")]
    [SerializeField] private bool withDownTownArea = true;
    [Range(50, 200)] public float downTownSize = 100;

    [Header("Traffic")]
    [SerializeField] private bool rightHand = true;
    public bool japanTrafficLight = false;

    private CityGenerator generator;

    private int CurrentTrafficHand()
    {
        if (rightHand) return 0;
        return japanTrafficLight ? 2 : 1;
    }

    private bool TryResolveGenerator()
    {
        if (!cg)
        {
            Debug.LogError("RunTimeSample: City Generator reference (cg) is missing.");
            return false;
        }

        if (!generator)
            generator = cg.GetComponent<CityGenerator>();

        if (!generator)
        {
            Debug.LogError("RunTimeSample: CityGenerator component was not found on cg.");
            return false;
        }

        return true;
    }

    private List<Vector2> BuildCustomOffsets()
    {
        if (customSatelliteOffsets == null || customSatelliteOffsets.Count == 0)
            return null;

        List<Vector2> valid = new List<Vector2>(customSatelliteOffsets.Count);
        for (int i = 0; i < customSatelliteOffsets.Count; i++)
            valid.Add(customSatelliteOffsets[i]);

        return valid;
    }

    private void ClearCars()
    {
        GameObject carContainer = GameObject.Find("CarContainer");
        if (carContainer)
            Destroy(carContainer);
    }

    private void SyncRequestFromInspector()
    {
        if (request == null)
            request = new CityGenerationRequest();

        bool useCustom = withSatelliteCity
            && !randomSatelliteLayout
            && useCustomSatelliteOffsets
            && customSatelliteOffsets != null
            && customSatelliteOffsets.Count > 0;
        List<Vector2> customOffsets = useCustom ? BuildCustomOffsets() : null;
        int effectiveSatelliteCount = useCustom ? Mathf.Max(1, customOffsets.Count) : Mathf.Clamp(satelliteCityCount, 1, 64);

        request.citySize = Mathf.Clamp(citySize, 1, 4);
        request.withSatelliteCity = withSatelliteCity;
        request.borderFlat = borderFlat;
        request.satelliteCityCount = effectiveSatelliteCount;
        request.connectSatellitesToMain = connectSatellitesToMain;
        request.connectSatellitesTogether = connectSatellitesTogether;
        request.randomSatelliteSizes = randomSatelliteSizes;
        request.satelliteCityMinSize = satelliteCityMinSize;
        request.satelliteCityMaxSize = satelliteCityMaxSize;
        request.randomSatelliteLayout = randomSatelliteLayout;
        request.useSatelliteSeed = useSatelliteSeed;
        request.satelliteSeed = satelliteSeed;
        request.useCitySeed = useCitySeed;
        request.citySeed = citySeed;
        request.autoSatelliteCount = autoSatelliteCount;
        request.autoGenerateBuildings = autoGenerateBuildings;
        request.withDownTownArea = withDownTownArea;
        request.downTownSize = downTownSize;
        request.randomSatelliteMin = randomSatelliteMin;
        request.randomSatelliteMax = randomSatelliteMax;
        request.customSatelliteOffsets = customOffsets ?? new List<Vector2>();
        request.useCustomSatelliteOffsets = useCustom;
        request.satelliteGlobalOffset = satelliteGlobalOffset;
        request.satelliteConnectionMode = satelliteConnectionMode;
        request.satelliteMaxNeighborLinks = satelliteMaxNeighborLinks;
        request.satelliteCloseLoop = satelliteCloseLoop;
        request.connectionStepOverride = satelliteConnectionStep;
        request.createCityAnchors = createCityAnchors;
        request.createConnectionDebugLines = createConnectionDebugLines;
        request.connectionDebugLineHeight = connectionDebugLineHeight;
        request.satelliteBuildingDensity = satelliteBuildingDensity;
        request.generateHighways = generateHighways;
        request.highwayWidth = highwayWidth;
        request.highwayThickness = highwayThickness;
        request.Normalize();
    }

    void Awake()
    {
        if (applyProfileOnAwake)
            ApplyProfile();

        TryResolveGenerator();
    }

    public void ApplyProfile()
    {
        if (!profile)
            return;

        citySize = Mathf.Clamp(profile.citySize, 1, 4);
        clearBeforeGenerate = profile.clearBeforeGenerate;
        autoGenerateBuildings = profile.autoGenerateBuildings;
        autoAddTraffic = profile.autoAddTraffic;

        withSatelliteCity = profile.withSatelliteCity;
        borderFlat = profile.borderFlat;
        satelliteCityCount = Mathf.Clamp(profile.satelliteCityCount, 1, 64);
        connectSatellitesToMain = profile.connectSatellitesToMain;
        connectSatellitesTogether = profile.connectSatellitesTogether;
        randomSatelliteSizes = profile.randomSatelliteSizes;
        satelliteCityMinSize = Mathf.Clamp(profile.satelliteCityMinSize, 1, 4);
        satelliteCityMaxSize = Mathf.Clamp(profile.satelliteCityMaxSize, 1, 4);
        satelliteConnectionMode = profile.satelliteConnectionMode;
        satelliteMaxNeighborLinks = Mathf.Clamp(profile.satelliteMaxNeighborLinks, 1, 6);
        satelliteCloseLoop = profile.satelliteCloseLoop;
        satelliteConnectionStep = Mathf.Max(0f, profile.satelliteConnectionStep);
        randomSatelliteLayout = profile.randomSatelliteLayout;
        useSatelliteSeed = profile.useSatelliteSeed;
        satelliteSeed = profile.satelliteSeed;
        randomSatelliteMin = profile.randomSatelliteMin;
        randomSatelliteMax = profile.randomSatelliteMax;
        satelliteGlobalOffset = profile.satelliteGlobalOffset;
        createCityAnchors = profile.createCityAnchors;
        createConnectionDebugLines = profile.createConnectionDebugLines;
        connectionDebugLineHeight = Mathf.Clamp(profile.connectionDebugLineHeight, 0f, 20f);
        generateHighways = profile.generateHighways;
        highwayWidth = Mathf.Clamp(profile.highwayWidth, 8f, 40f);
        highwayThickness = Mathf.Clamp(profile.highwayThickness, 0.05f, 1f);

        customSatelliteOffsets = new List<Vector2>();
        if (profile.customSatelliteOffsets != null)
        {
            for (int i = 0; i < profile.customSatelliteOffsets.Count; i++)
                customSatelliteOffsets.Add(profile.customSatelliteOffsets[i]);
        }
        useCustomSatelliteOffsets = !randomSatelliteLayout && customSatelliteOffsets.Count > 0;

        withDownTownArea = profile.withDownTownArea;
        downTownSize = Mathf.Clamp(profile.downTownSize, 50f, 200f);
        rightHand = profile.rightHand;
        japanTrafficLight = profile.japanTrafficLight;

        if (request == null)
            request = new CityGenerationRequest();

        request = CityGenerationRequest.FromProfile(profile);
        request.citySize = citySize;
        request.borderFlat = borderFlat;
        request.useCitySeed = profile.useCitySeed;
        request.citySeed = profile.citySeed;
        request.autoSatelliteCount = profile.autoSatelliteCount;
        request.autoGenerateBuildings = profile.autoGenerateBuildings;
        request.withDownTownArea = profile.withDownTownArea;
        request.downTownSize = profile.downTownSize;
    }

    public void GenerateCityAtRuntime(int citySize)
    {
        this.citySize = Mathf.Clamp(citySize, 1, 4);
        GenerateCityAtRuntime();
    }

    public void GenerateCityAtRuntime()
    {
        if (!TryResolveGenerator())
            return;

        if (clearBeforeGenerate)
            generator.ClearCity();

        ClearCars();

        if (useRequestObject && request != null)
        {
            if (requestFollowsInspector)
                SyncRequestFromInspector();
            else
                request.citySize = Mathf.Clamp(this.citySize, 1, 4);
            request.Normalize();
            generator.GenerateCity(request);
        }
        else
        {
            bool useCustom = withSatelliteCity
                && !randomSatelliteLayout
                && useCustomSatelliteOffsets
                && customSatelliteOffsets != null
                && customSatelliteOffsets.Count > 0;
            List<Vector2> customOffsets = useCustom ? BuildCustomOffsets() : null;
            int effectiveSatelliteCount = useCustom ? Mathf.Max(1, customOffsets.Count) : Mathf.Clamp(satelliteCityCount, 1, 64);

            generator.GenerateCity(
                Mathf.Clamp(this.citySize, 1, 4),
                withSatelliteCity,
                borderFlat,
                effectiveSatelliteCount,
                connectSatellitesToMain,
                connectSatellitesTogether,
                randomSatelliteLayout,
                useSatelliteSeed,
                satelliteSeed,
                randomSatelliteMin.x,
                randomSatelliteMax.x,
                randomSatelliteMin.y,
                randomSatelliteMax.y,
                customOffsets,
                useCustom,
                satelliteGlobalOffset,
                satelliteConnectionMode,
                satelliteMaxNeighborLinks,
                satelliteCloseLoop,
                useCitySeed,
                citySeed,
                autoSatelliteCount,
                randomSatelliteSizes,
                satelliteCityMinSize,
                satelliteCityMaxSize,
                satelliteBuildingDensity,
                autoGenerateBuildings,
                withDownTownArea,
                downTownSize,
                satelliteConnectionStep,
                createCityAnchors,
                createConnectionDebugLines,
                connectionDebugLineHeight,
                generateHighways,
                highwayWidth,
                highwayThickness);
        }
    }

    public void GenerateFullPipeline()
    {
        GenerateCityAtRuntime();

        if (autoGenerateBuildings)
            GenerateBuildings();

        if (autoAddTraffic)
            AddTrafficSystem();
    }

    public void WithDownTownArea(bool value)
    {
        withDownTownArea = value;
    }

    public bool IsDownTownAreaEnabled()
    {
        return withDownTownArea;
    }

    public void RightHand(bool value)
    {
        rightHand = value;
    }

    public bool IsRightHandTraffic()
    {
        return rightHand;
    }

    public void GenerateBuildings()
    {
        if (!TryResolveGenerator())
            return;

        generator.GenerateAllBuildings(withDownTownArea, downTownSize);
    }

    public void AddTrafficSystem()
    {
        if (!trafficSystem)
            trafficSystem = FindObjectOfType<TrafficSystem>();

        if (trafficSystem)
        {
            trafficSystem.LoadCars(CurrentTrafficHand());
            Debug.LogWarning("Move the camera to the streets so that vehicles are generated around it");
        }
        else
        {
            Debug.LogError("Traffic System prefab not found in Hierarchy");
        }
    }

    public void SetNight(bool value)
    {
        DayNight dn = FindObjectOfType<DayNight>();
        if (!dn)
        {
            Debug.LogWarning("DayNight component not found in scene.");
            return;
        }

        dn.isNight = value;
        dn.ChangeMaterial();
    }

    public string GetLastGenerationSummary()
    {
        if (!TryResolveGenerator())
            return "Generator not ready.";

        return generator.GetLastGenerationSummary();
    }

    public string GetLastGenerationNetworkSummary()
    {
        if (!TryResolveGenerator())
            return "Generator not ready.";

        return generator.GetLastGenerationNetworkSummary();
    }

    public CityGenerator.GenerationNetwork GetLastGenerationNetwork()
    {
        if (!TryResolveGenerator())
            return null;

        return generator.GetLastGenerationNetworkClone();
    }
}
