using UnityEngine;

// Keeps the game smooth on every device: Windows, Android phones and
// tablets, and web browsers. Before the first scene loads it decides
// whether this is a low-end device; if so it switches to the "Low"
// quality level (no shadows), caps the frame rate at 30, lowers the
// screen resolution on big phone screens and asks the live 3D mTiny to
// draw a smaller picture less often.
public static class PerformanceMode
{
    public static bool IsLowEnd { get; private set; }

    // Editor only: mTiny > Preview > Simulate Low-End Device
    public const string SimulateKey = "mtiny.simulateLowEnd";

    public enum Kind { Desktop, Mobile, Web, MobileWeb }

    // The rule, kept apart so it can be tested. Sizes in MB; 0 = unknown.
    public static bool Decide(Kind kind, int memoryMB, int cores, int gpuMemoryMB)
    {
        switch (kind)
        {
            case Kind.MobileWeb:
                return true;                                       // a phone browser: always go easy
            case Kind.Mobile:
                return (memoryMB > 0 && memoryMB < 3000) || (cores > 0 && cores <= 4);
            case Kind.Web:
                return cores > 0 && cores <= 2;                    // browsers hide memory sizes
            default:
                return (memoryMB > 0 && memoryMB < 4000) || (cores > 0 && cores <= 2) ||
                       (gpuMemoryMB > 0 && gpuMemoryMB < 512);
        }
    }

    public static Kind CurrentKind()
    {
        bool web = Application.platform == RuntimePlatform.WebGLPlayer;
        if (web) return Application.isMobilePlatform ? Kind.MobileWeb : Kind.Web;
        return Application.isMobilePlatform ? Kind.Mobile : Kind.Desktop;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        Kind kind = CurrentKind();
        IsLowEnd = Decide(kind, SystemInfo.systemMemorySize, SystemInfo.processorCount, SystemInfo.graphicsMemorySize);
#if UNITY_EDITOR
        IsLowEnd = UnityEditor.EditorPrefs.GetBool(SimulateKey, false);
#endif

        if (IsLowEnd)
        {
            int low = System.Array.IndexOf(QualitySettings.names, "Low");
            if (low >= 0) QualitySettings.SetQualityLevel(low, true);
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 30;

            // Big phone screens: draw at 720p and let the screen scale it up
            if (kind == Kind.Mobile && Screen.height > 720)
            {
                float s = 720f / Screen.height;
                Screen.SetResolution(Mathf.RoundToInt(Screen.width * s), 720, true);
            }
        }
        else
        {
            // Phones and tablets stay at 30 for battery and heat; computers
            // and desktop browsers get 60 (vSync, where on, keeps it steady)
            Application.targetFrameRate = kind == Kind.Mobile || kind == Kind.MobileWeb ? 30 : 60;
        }
        Debug.Log("[mTiny] " + kind + (IsLowEnd ? ": low-end mode on" : ": normal mode") +
                  " (quality " + QualitySettings.names[QualitySettings.GetQualityLevel()] + ")");
    }
}
