using UnityEngine;

/// <summary>
/// Makes the game UI Canvas survive scene loads. It's a singleton: the first one
/// to wake becomes permanent (DontDestroyOnLoad) and any later copy that a scene
/// brings in destroys itself — so portalling Field↔Town keeps a single HUD/panels.
///
/// It also spawns itself once at startup from a prefab in a Resources folder, so
/// the UI shows up in whatever scene you press Play in, even one with no Canvas.
/// Build UI Shell saves that prefab to Assets/Resources/GameUICanvas.prefab.
/// </summary>
public class PersistentUI : MonoBehaviour
{
    public static PersistentUI Instance { get; private set; }

    const string ResourceName = "GameUICanvas";   // Assets/Resources/GameUICanvas.prefab

    void Awake()
    {
        // DontDestroyOnLoad only works on root objects.
        if (transform.parent != null) transform.SetParent(null);

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);   // a scene loaded its own copy — drop it, keep the original
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // If no UI exists when the game starts, spawn it from the prefab once.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        if (Instance != null) return;
        var prefab = Resources.Load<GameObject>(ResourceName);
        if (prefab != null) Instantiate(prefab);
        // else: not set up yet — run Tools ▸ unwritten ▸ Build UI Shell.
    }
}
