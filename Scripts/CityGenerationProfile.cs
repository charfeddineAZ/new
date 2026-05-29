using System.Collections.Generic;
using UnityEngine;

namespace FCG
{
    [CreateAssetMenu(fileName = "CityGenerationProfile", menuName = "Fantastic City Generator/City Generation Profile")]
    public class CityGenerationProfile : ScriptableObject
    {
        [Header("Pipeline")]
        [Range(1, 4)] public int citySize = 3;
        public bool clearBeforeGenerate = true;
        public bool autoGenerateBuildings = true;
        public bool autoAddTraffic = true;

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
        public bool useCitySeed = false;
        public int citySeed = 123456;
        public bool autoSatelliteCount = false;
        public bool useSatelliteSeed = false;
        public int satelliteSeed = 12345;
        public Vector2 randomSatelliteMin = new Vector2(-1000f, -2200f);
        public Vector2 randomSatelliteMax = new Vector2(1000f, -1200f);
        public Vector2 satelliteGlobalOffset = Vector2.zero;
        public List<Vector2> customSatelliteOffsets = new List<Vector2>();
        public bool createCityAnchors = true;
        public bool createConnectionDebugLines = false;
        [Range(0f, 20f)] public float connectionDebugLineHeight = 3f;
        [Range(0f,1f)] public float satelliteBuildingDensity = 1f;

        [Header("Buildings")]
        public bool withDownTownArea = true;
        [Range(50, 200)] public float downTownSize = 100;

        [Header("Traffic")]
        public bool rightHand = true;
        public bool japanTrafficLight = false;
    }
}
