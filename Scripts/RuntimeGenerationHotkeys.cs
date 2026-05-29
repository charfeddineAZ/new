using UnityEngine;

public class RuntimeGenerationHotkeys : MonoBehaviour
{
    public RunTimeSample runtimeSample;
    public KeyCode generateFullPipelineKey = KeyCode.F5;
    public KeyCode regenerateTrafficKey = KeyCode.F6;
    public KeyCode toggleNightKey = KeyCode.F7;

    private bool isNight = false;

    private void Awake()
    {
        if (!runtimeSample)
            runtimeSample = FindObjectOfType<RunTimeSample>();
    }

    private void Update()
    {
        if (!runtimeSample)
            return;

        if (Input.GetKeyDown(generateFullPipelineKey))
        {
            runtimeSample.GenerateFullPipeline();
            Debug.Log(runtimeSample.GetLastGenerationSummary());
            Debug.Log(runtimeSample.GetLastGenerationNetworkSummary());
        }

        if (Input.GetKeyDown(regenerateTrafficKey))
            runtimeSample.AddTrafficSystem();

        if (Input.GetKeyDown(toggleNightKey))
        {
            isNight = !isNight;
            runtimeSample.SetNight(isNight);
        }
    }
}
