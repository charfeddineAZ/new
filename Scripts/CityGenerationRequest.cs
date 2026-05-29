using System.Collections.Generic;
using UnityEngine;

namespace FCG
{
    [System.Serializable]
    public class CityGenerationRequest
    {
        [Range(1, 4)] public int citySize = 3;
        public bool withSatelliteCity = false;
        public bool borderFlat = false;
        [Range(1, 64)] public int satelliteCityCount = 1;
        public bool connectSatellitesToMain = true;
        public bool connectSatellitesTogether = true;
        public bool randomSatelliteSizes = false;
        [Range(1, 4)] public int satelliteCityMinSize = 1;
        [Range(1, 4)] public int satelliteCityMaxSize = 1;
        public bool randomSatelliteLayout = false;
        public bool useCitySeed = false;
        public int citySeed = 123456;
        public bool autoSatelliteCount = false;
        public bool useSatelliteSeed = false;
        public int satelliteSeed = 12345;
        public Vector2 randomSatelliteMin = new Vector2(-1000f, -2200f);
        public Vector2 randomSatelliteMax = new Vector2(1000f, -1200f);
        public List<Vector2> customSatelliteOffsets = new List<Vector2>();
        public bool useCustomSatelliteOffsets = false;
        public Vector2 satelliteGlobalOffset = Vector2.zero;
        public CityGenerator.SatelliteConnectionMode satelliteConnectionMode = CityGenerator.SatelliteConnectionMode.Chain;
        [Range(1, 6)] public int satelliteMaxNeighborLinks = 1;
        public bool satelliteCloseLoop = false;
        public float connectionStepOverride = 0f;
        public bool createCityAnchors = true;
        public bool autoGenerateBuildings = true;
        public bool withDownTownArea = true;
        [Range(50, 200)] public float downTownSize = 100f;
        public bool createConnectionDebugLines = false;
        public float connectionDebugLineHeight = 3f;
        [Range(0f,1f)] public float satelliteBuildingDensity = 1f;

        public void Normalize()
        {
            citySize = Mathf.Clamp(citySize, 1, 4);
            satelliteCityCount = Mathf.Clamp(satelliteCityCount, 1, 64);
            satelliteMaxNeighborLinks = Mathf.Clamp(satelliteMaxNeighborLinks, 1, 6);
            satelliteCityMinSize = Mathf.Clamp(satelliteCityMinSize, 1, 4);
            satelliteCityMaxSize = Mathf.Clamp(satelliteCityMaxSize, 1, 4);
            connectionStepOverride = Mathf.Max(0f, connectionStepOverride);
            connectionDebugLineHeight = Mathf.Clamp(connectionDebugLineHeight, 0f, 20f);

            if (randomSatelliteMin.x > randomSatelliteMax.x)
            {
                float t = randomSatelliteMin.x;
                randomSatelliteMin.x = randomSatelliteMax.x;
                randomSatelliteMax.x = t;
            }
            // Ensure seed values are non-negative
            if (citySeed < 0) citySeed = 0;
            if (satelliteSeed < 0) satelliteSeed = 0;

            if (satelliteCityMinSize > satelliteCityMaxSize)
            {
                int t = satelliteCityMinSize;
                satelliteCityMinSize = satelliteCityMaxSize;
                satelliteCityMaxSize = t;
            }

            if (randomSatelliteMin.y > randomSatelliteMax.y)
            {
                float t = randomSatelliteMin.y;
                randomSatelliteMin.y = randomSatelliteMax.y;
                randomSatelliteMax.y = t;
            }
            if (satelliteCityMinSize > satelliteCityMaxSize)
            {
                int t = satelliteCityMinSize;
                satelliteCityMinSize = satelliteCityMaxSize;
                satelliteCityMaxSize = t;
            }

            downTownSize = Mathf.Clamp(downTownSize, 50f, 200f);
        }

        public static CityGenerationRequest FromProfile(CityGenerationProfile profile)
        {
            if (!profile)
                return new CityGenerationRequest();

            CityGenerationRequest request = new CityGenerationRequest();
            request.citySize = profile.citySize;
            request.withSatelliteCity = profile.withSatelliteCity;
            request.borderFlat = profile.borderFlat;
            request.satelliteCityCount = profile.satelliteCityCount;
            request.connectSatellitesToMain = profile.connectSatellitesToMain;
            request.connectSatellitesTogether = profile.connectSatellitesTogether;
            request.satelliteConnectionMode = profile.satelliteConnectionMode;
            request.satelliteMaxNeighborLinks = profile.satelliteMaxNeighborLinks;
            request.satelliteCloseLoop = profile.satelliteCloseLoop;
            request.connectionStepOverride = profile.satelliteConnectionStep;
            request.createCityAnchors = profile.createCityAnchors;
            request.autoGenerateBuildings = profile.autoGenerateBuildings;
            request.withDownTownArea = profile.withDownTownArea;
            request.downTownSize = profile.downTownSize;
            request.createConnectionDebugLines = profile.createConnectionDebugLines;
            request.connectionDebugLineHeight = profile.connectionDebugLineHeight;
            request.satelliteBuildingDensity = profile.satelliteBuildingDensity;
            request.randomSatelliteLayout = profile.randomSatelliteLayout;
            request.useSatelliteSeed = profile.useSatelliteSeed;
            request.satelliteSeed = profile.satelliteSeed;
            request.useCitySeed = profile.useCitySeed;
            request.citySeed = profile.citySeed;
            request.autoSatelliteCount = profile.autoSatelliteCount;
            request.randomSatelliteMin = profile.randomSatelliteMin;
            request.randomSatelliteMax = profile.randomSatelliteMax;
            request.satelliteGlobalOffset = profile.satelliteGlobalOffset;
            request.customSatelliteOffsets = (profile.customSatelliteOffsets != null)
                ? new List<Vector2>(profile.customSatelliteOffsets)
                : new List<Vector2>();
            request.useCustomSatelliteOffsets = !request.randomSatelliteLayout && request.customSatelliteOffsets.Count > 0;
            request.randomSatelliteSizes = profile.randomSatelliteSizes;
            request.satelliteCityMinSize = profile.satelliteCityMinSize;
            request.satelliteCityMaxSize = profile.satelliteCityMaxSize;
            request.Normalize();
            return request;
        }
    }
}
