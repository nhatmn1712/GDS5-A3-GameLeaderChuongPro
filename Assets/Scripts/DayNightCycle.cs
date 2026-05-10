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

    private float timeMultiplier;

    void Start()
    {
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
    }
}
