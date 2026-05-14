using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Time Settings")]
    [Tooltip("How many real-time minutes a full 24 in-game hours should take.")]
    public float dayDurationInMinutes = 2f;
    [Range(0, 24)]
    [Tooltip("Current time of day in hours (0-24).")]
    public float timeOfDay = 8f;

    [Header("Sun / Light Settings")]
    [Tooltip("The Directional Light in your scene representing the Sun.")]
    public Light sunLight;
    
    [Tooltip("The Y rotation of the sun (affects which direction shadows fall).")]
    public float sunYRotation = -30f;
    
    [Tooltip("Controls the color of the sun over the 24 hours. (Left edge = Midnight, Middle = Noon, Right edge = Midnight)")]
    public Gradient sunColor;
    
    [Tooltip("Controls the brightness of the sun over the 24 hours.")]
    public AnimationCurve sunIntensity;

    [Header("Skybox Settings")]
    public Material morningSkybox;   // 06:00 to 12:00
    public Material noonSkybox;      // 12:00 to 16:00
    public Material afternoonSkybox; // 16:00 to 18:00
    public Material eveningSkybox;   // 18:00 to 20:00
    public Material nightSkybox;     // 20:00 to 06:00

    private float timeMultiplier;

    void Start()
    {
        // Ensure fog is turned off completely
        RenderSettings.fog = false;

        // Calculate how much the timeOfDay should increase per real-time second
        timeMultiplier = 24f / (dayDurationInMinutes * 60f);
    }

    void Update()
    {
        UpdateTime();
        UpdateSun();
    }

    void UpdateTime()
    {
        timeOfDay += Time.deltaTime * timeMultiplier;

        // Reset to midnight once we hit 24 hours
        if (timeOfDay >= 24f)
        {
            timeOfDay %= 24f;
        }
    }

    void UpdateSun()
    {
        if (sunLight == null) return;

        // Calculate a 0 to 1 percentage of the day (0 = midnight, 0.5 = noon, 1 = midnight)
        float timePercent = timeOfDay / 24f;
        
        // (timePercent * 360) - 90 ensures that 0 (midnight) is pointing straight up (-90X),
        // 6 (sunrise) is horizontal (0X), 12 (noon) is straight down (90X), 18 (sunset) is horizontal (180X)
        float sunRotation = (timePercent * 360f) - 90f;
        
        // Rotate the Directional Light
        sunLight.transform.localRotation = Quaternion.Euler(sunRotation, sunYRotation, 0f);

        // Update Sun Color and Intensity using the Gradient and AnimationCurve
        sunLight.color = sunColor.Evaluate(timePercent);
        sunLight.intensity = sunIntensity.Evaluate(timePercent);

        // Update Skybox smoothly based on time
        Material targetSkybox = null;

        if (timeOfDay >= 6f && timeOfDay < 12f)
            targetSkybox = morningSkybox;
        else if (timeOfDay >= 12f && timeOfDay < 16f)
            targetSkybox = noonSkybox;
        else if (timeOfDay >= 16f && timeOfDay < 18f)
            targetSkybox = afternoonSkybox;
        else if (timeOfDay >= 18f && timeOfDay < 20f)
            targetSkybox = eveningSkybox;
        else
            targetSkybox = nightSkybox;

        // Only change the skybox if we have one assigned and it's different from the current one
        if (targetSkybox != null && RenderSettings.skybox != targetSkybox)
        {
            RenderSettings.skybox = targetSkybox;
            DynamicGI.UpdateEnvironment(); // Update the scene lighting
        }
    }
}
