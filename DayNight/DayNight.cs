using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Rendering;

public class DayNight : MonoBehaviour
{
    //  In the 2 fields below, only the materials that will be alternated in the day/night exchange are registered
    //  When adding your buildings(which will have their own materials), you can register the day and night versions of the materials here.
    //  The index of the daytime version of the material must match the index of the nighttime version of the material
    //  Example: When switching to night scene, materialDay[1] will be replaced by materialNight[1]
    //  (Materials that will be used both night and day do not need to be here)
    public Material[] materialDay;    // Add materials that are only used in the day scene, and are substituted in the night scene
    public Material[] materialNight;  // Add night scene materials that will replace day scene materials. (The sequence must be respected)

    //Here are the skyboxes for day and night.
    //You can replace with other skyboxes
    public Material skyBoxDay;
    public Material skyBoxNight;

    //Don't forget to add the Directional Light here
    public Light directionalLight;

    [HideInInspector]
    public bool isNight;

    [HideInInspector]
    public bool isMoonLight;

    [HideInInspector]
    public bool isSpotLights;

    [HideInInspector]
    public bool isStreetLights;

    [HideInInspector]
    public bool night;

    [HideInInspector]
    public float intenseMoonLight = 0.2f;

    [HideInInspector]
    public float _intenseMoonLight;

    [HideInInspector]
    public float intenseSunLight = 1f;

    [HideInInspector]
    public float _intenseSunLight;


    [HideInInspector]
    public Color skyColorDay = new Color(0.74f, 0.62f, 0.60f);
    [HideInInspector]
    public Color equatorColorDay = new Color(0.74f, 0.74f, 0.74f);


    [HideInInspector]
    public Color _skyColorDay;
    [HideInInspector]
    public Color _equatorColorDay;


    [HideInInspector]
    public Color skyColorNight = new Color(0.78f, 0.72f, 0.72f);
    [HideInInspector]
    public Color equatorColorNight = new Color(0.16f, 0.16f, 0.16f);

    [HideInInspector]
    public Color _skyColorNight;
    [HideInInspector]
    public Color _equatorColorNight;

    [HideInInspector]
    public Color sunLightColor;
    [HideInInspector]
    public Color _sunLightColor;

    [HideInInspector]
    public Color moonLightColor;
    [HideInInspector]
    public Color _moonLightColor;

    private readonly Dictionary<Material, Material> dayToNight = new Dictionary<Material, Material>();
    private readonly Dictionary<Material, Material> nightToDay = new Dictionary<Material, Material>();
    private bool materialMapReady = false;

    private void OnValidate()
    {
        materialMapReady = false;
    }

    private void BuildMaterialMap()
    {
        dayToNight.Clear();
        nightToDay.Clear();
        materialMapReady = true;

        if (materialDay == null || materialNight == null)
        {
            Debug.LogWarning("DayNight: materialDay/materialNight arrays are not assigned.");
            return;
        }

        if (materialDay.Length != materialNight.Length)
            Debug.LogWarning("DayNight: materialDay and materialNight sizes differ. Mapping by minimum length.");

        int count = Mathf.Min(materialDay.Length, materialNight.Length);
        for (int i = 0; i < count; i++)
        {
            Material d = materialDay[i];
            Material n = materialNight[i];
            if (!d || !n) continue;

            if (!dayToNight.ContainsKey(d))
                dayToNight.Add(d, n);
            if (!nightToDay.ContainsKey(n))
                nightToDay.Add(n, d);
        }
    }


    public void ChangeMaterial()
    {

        //Switching the skyboxes according to day/ night
        RenderSettings.skybox = (isNight) ? skyBoxNight : skyBoxDay;


        /*
        Setting the ambient (gradient) light to fit day/night
        These are properties of the "Lighting" window and Directional Light
        */
        UpdateColor();



        //Configuring the Directional Light as it is day or night (sun/moon)
        SetDirectionalLight();

        /*
        Substituting Night materials for Day materials (or vice versa) in all Mesh Renders within City-Maker
        Only materials that have been added in "materialDay" and "materialNight" Array
        */

        GameObject GmObj = GameObject.Find("City-Maker"); ;

        if (GmObj == null) return;

        if (!materialMapReady)
            BuildMaterialMap();

        Renderer[] children = GmObj.GetComponentsInChildren<Renderer>(true);

        Material[] myMaterials;

        for (int i = 0; i < children.Length; i++)
        {
            Renderer r = children[i];
            if (!r) continue;
            myMaterials = r.sharedMaterials;

            for (int m = 0; m < myMaterials.Length; m++)
            {
                Material current = myMaterials[m];
                if (!current) continue;

                if (isNight)
                {
                    if (dayToNight.TryGetValue(current, out Material nightMat) && nightMat)
                        myMaterials[m] = nightMat;
                }
                else
                {
                    if (nightToDay.TryGetValue(current, out Material dayMat) && dayMat)
                        myMaterials[m] = dayMat;
                }
            }

            r.sharedMaterials = myMaterials;

        }

        //Toggles street lamp lights on/off
        SetStreetLights();





    }
    public void UpdateColor()
    {
        /*
       Setting the ambient (gradient) light to fit day/night
       These are properties of the "Lighting" window and Directional Light
       */

        if (isNight)
        {

            //During the Night

            if (directionalLight)
                directionalLight.GetComponent<Light>().color = moonLightColor;

            RenderSettings.ambientMode = AmbientMode.Trilight;

            RenderSettings.ambientSkyColor = skyColorNight;  // Floor color/brightness (any face up) - no moonlight

            RenderSettings.ambientEquatorColor = equatorColorNight;              // Wall color/ luminosity(any side facing)
            RenderSettings.ambientGroundColor = new Color(0.07f, 0.07f, 0.07f);  // Ceiling color/brightness (any face down)
        }
        else
        {
            //During the day
            RenderSettings.ambientMode = AmbientMode.Trilight;

            RenderSettings.ambientSkyColor = skyColorDay;                        // Floor color/brightness (any face up) 
            RenderSettings.ambientEquatorColor = equatorColorDay;                // Wall color/ luminosity(any side facing)
            RenderSettings.ambientGroundColor = new Color(0.4f, 0.4f, 0.4f);     // Ceiling color/brightness (any face down)

        }

    }

    public void SetDirectionalLight() //Configuring the Directional Light as it is day or night (sun/moon)
    {

        if (directionalLight)
        {
            directionalLight.GetComponent<Light>().enabled = (!isNight || isMoonLight);
            directionalLight.intensity = (isNight) ? intenseMoonLight / 100 : intenseSunLight / 100;
        }

    }
    public void SetStreetLights()  //Toggles street lamp lights on/off
    {
        GameObject[] tempArray = GameObject.FindObjectsOfType(typeof(GameObject)).Select(g => g as GameObject).Where(g => g.name == ("_LightV")).ToArray();
        foreach (GameObject lines in tempArray)
        {
            MeshRenderer mr = lines.GetComponent<MeshRenderer>();
            if (mr) mr.enabled = isNight;
        }
    }

    /*
    public void ShiftStreetLights(bool night)
    {

        GameObject[] tempArray = GameObject.FindObjectsOfType(typeof(GameObject)).Select(g => g as GameObject).Where(g => g.name == ("_LightV")).ToArray();

        foreach (GameObject lines in tempArray)
        {
            lines.GetComponent<MeshRenderer>().enabled = night;
            if(lines.transform.GetChild(0))
                lines.transform.GetChild(0).GetComponent<Light>().enabled = (isStreetLights && night);
        }



    }

    public void ShiftSpotLights(bool night)
    {
        
        GameObject[]  tempArray = GameObject.FindObjectsOfType(typeof(GameObject)).Select(g => g as GameObject).Where(g => g.name == ("_Spot_Light")).ToArray();

        foreach (GameObject lines in tempArray)
            lines.GetComponent<Light>().enabled = (isSpotLights && night);

    }
    */
}
