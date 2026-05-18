using UnityEngine;
using System;

public class DayNightCycle : MonoBehaviour
{
    [Header("Time Settings")]
    [Tooltip("How many real-time minutes a full 24 in-game hours should take.")]
    public float dayDurationInMinutes = 2f;
    [Range(0, 24)]
    [Tooltip("Current time of day in hours (0-24).")]
    public float timeOfDay = 8f;

    [Header("Night Freeze")]
    [Tooltip("When time reaches this hour, the clock stops and night begins. Default = 24 (Midnight).")]
    public float nightStartHour = 24f;

    // ─── Public State ────────────────────────────────────────────────
    /// <summary>True once the clock has frozen at nightStartHour.</summary>
    public bool IsNight { get; private set; } = false;

    /// <summary>Fired exactly once when night begins. Subscribe to react (close shop, trigger go-home sequence, etc.).</summary>
    public static event Action OnNightHasFallen;

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
    public Material afternoonSkybox; // 06:00 to 18:00 (Chiều)
    public Material eveningSkybox;   // 18:00 to 20:00 (Tối)
    public Material nightSkybox;     // 20:00 to 06:00 (Đêm)

    [Header("Fog Settings (Sương mù)")]
    [Tooltip("Bật tắt sương mù để che viền thế giới")]
    public bool enableFog = true;
    [Tooltip("Độ đặc của sương mù (thử 0.01 đến 0.05)")]
    public float fogDensity = 0.02f;
    
    [Header("Fog Colors (Màu sương mù)")]
    public bool autoChangeFogColor = true;
    [Tooltip("Tốc độ chuyển màu sương mù mượt mà")]
    public float fogColorBlendSpeed = 1f;
    public Color afternoonFogColor = new Color(0.6f, 0.7f, 0.8f);
    public Color eveningFogColor = new Color(0.8f, 0.4f, 0.2f);
    public Color nightFogColor = new Color(0.02f, 0.02f, 0.05f);

    [Header("Street Lights (Tự động đèn đường)")]
    public bool autoControlStreetLights = true;
    [Tooltip("Hệ số nhân độ sáng của tất cả các đèn khi trời tối")]
    public float nightIntensityMultiplier = 8f;
    [Tooltip("Hệ số nhân tầm chiếu xa (range) của các đèn khi trời tối")]
    public float nightRangeMultiplier = 2f;

    // Lưu trữ đèn và intensity gốc
    private Light[]  _streetLights;     // tất cả đèn (không phải directional)
    private float[]  _baseLightIntensity; // intensity gốc ban ngày
    private float[]  _baseLightRange;     // range gốc

    private float timeMultiplier;
    private bool  _lightsScanned = false;

    void Start()
    {
        timeMultiplier = 24f / (dayDurationInMinutes * 60f);
        ScanSceneLights();
    }

    void ScanSceneLights()
    {
        var allLights = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var lights   = new System.Collections.Generic.List<Light>();
        var intensities = new System.Collections.Generic.List<float>();
        var ranges      = new System.Collections.Generic.List<float>();

        foreach (Light l in allLights)
        {
            if (l == sunLight) continue;
            if (l.type == LightType.Directional) continue;
            lights.Add(l);
            intensities.Add(l.intensity);
            ranges.Add(l.range);
        }

        _streetLights        = lights.ToArray();
        _baseLightIntensity  = intensities.ToArray();
        _baseLightRange      = ranges.ToArray();
        _lightsScanned       = true;
        Debug.Log($"[DayNight] Scanned {_streetLights.Length} street/point lights.");
    }

    void Update()
    {
        UpdateTime();
        UpdateSun();
    }

    void UpdateTime()
    {
        // If it's already night, freeze the clock and do nothing
        if (IsNight) return;

        timeOfDay += Time.deltaTime * timeMultiplier;

        // Check if we just crossed into night
        if (timeOfDay >= nightStartHour)
        {
            timeOfDay = nightStartHour; // Pin time exactly at nightStartHour
            IsNight = true;

            Debug.Log("[DayNight] Night has fallen! Clock is now frozen.");

            // Fire the event so other systems can react
            OnNightHasFallen?.Invoke();
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

        // Ambient: giữ tối thiểu 0.15 để không đen hoàn toàn
        float ambientMin = 0.15f;
        RenderSettings.ambientIntensity = Mathf.Max(ambientMin, Mathf.Clamp01(sunLight.intensity));

        // Điều khiển đèn đường
        if (autoControlStreetLights && _lightsScanned && _streetLights != null)
        {
            // darkness: 0 = ban ngày, 1 = hoàn toàn tối đêm
            float sunI    = Mathf.Clamp01(sunLight.intensity);
            float darkness = 1f - sunI;

            for (int i = 0; i < _streetLights.Length; i++)
            {
                if (_streetLights[i] == null) continue;
                // Ban ngày = intensity gốc (không tắt hoàn toàn)
                // Ban đêm  = intensity gốc × multiplier
                float targetIntensity = Mathf.Lerp(
                    _baseLightIntensity[i],                                  // ban ngày
                    _baseLightIntensity[i] * nightIntensityMultiplier,       // ban đêm
                    darkness);
                _streetLights[i].intensity = targetIntensity;
                _streetLights[i].range     = _baseLightRange[i]; // giữ range gốc
            }
        }

        // Update Skybox smoothly based on time
        Material targetSkybox = null;

        if (timeOfDay >= 6f && timeOfDay < 18f)
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

        // Cập nhật Sương mù (Fog) liên tục
        if (enableFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = fogDensity;
            
            // Tự động đổi màu sương mù cho tiệp với các Skybox
            if (autoChangeFogColor)
            {
                Color targetFogColor;
                if (timeOfDay >= 6f && timeOfDay < 18f)
                    targetFogColor = afternoonFogColor;
                else if (timeOfDay >= 18f && timeOfDay < 20f)
                    targetFogColor = eveningFogColor;
                else
                    targetFogColor = nightFogColor;

                // Chuyển màu từ từ (mượt) thay vì giật cục
                RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, targetFogColor, Time.deltaTime * fogColorBlendSpeed);
            }
        }
        else
        {
            RenderSettings.fog = false;
        }
    }
}
