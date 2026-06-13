using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-click enemy builders (beginner-friendly), mirroring <c>Build Player</c>.
/// Select a sliced monster SpriteSheet in the Project window, then run either:
///
///   • <b>Create Monster from selected SpriteSheet</b> — drops one enemy in the scene.
///   • <b>Create Monster Spawner from selected SpriteSheet</b> — saves the enemy as a
///     reusable prefab (Assets/Prefabs/Monsters/) and drops a <see cref="MonsterSpawner"/>
///     that keeps a pack of them alive.
///
/// Both build a Monster with a SpriteRenderer, physics body + collider,
/// <see cref="Health"/>, <see cref="MonsterAI"/>, and (for a 4-column walk sheet) a
/// <see cref="CharacterAnimator2D"/>, and make sure the scene's Player has a
/// <see cref="PlayerAttacker"/> so you can fight back.
/// </summary>
public static class MonsterBuilder
{
    const int CellSize = 16;   // Ninja Adventure art is 16 px per cell.
    const int WalkFrames = 4;  // walk-cycle frames per direction.
    const string PrefabDir = "Assets/Prefabs/Monsters";

    [MenuItem("Tools/unwritten/Create Monster from selected SpriteSheet")]
    static void CreateMonster()
    {
        var tex = Selection.activeObject as Texture2D;
        var go = BuildMonster(tex);
        if (go == null) return;

        var pc = Object.FindFirstObjectByType<PlayerController2D>();
        go.transform.position = NearPlayer(pc, 4f);

        Undo.RegisterCreatedObjectUndo(go, "Create Monster");
        string combat = EnsurePlayerAttacker(pc);

        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        Debug.Log($"[unwritten] Created Monster from '{tex.name}'.{combat} " +
                  "Press Play, walk near it (it chases), and press J to kill it.");
    }

    [MenuItem("Tools/unwritten/Create Monster Spawner from selected SpriteSheet")]
    static void CreateMonsterSpawner()
    {
        var tex = Selection.activeObject as Texture2D;
        var monster = BuildMonster(tex);
        if (monster == null) return;

        // Author the prefab from the freshly-built monster, then discard the temp
        // scene instance — the spawner will instantiate copies at runtime.
        EnsureFolder(PrefabDir);
        string prefabPath = $"{PrefabDir}/Monster_{tex.name}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(monster, prefabPath);
        Object.DestroyImmediate(monster);

        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Create Monster Spawner",
                $"Couldn't save the prefab to '{prefabPath}'.", "OK");
            return;
        }

        var go = new GameObject("MonsterSpawner_" + tex.name);
        var spawner = go.AddComponent<MonsterSpawner>();
        spawner.monsterPrefab = prefab;

        var pc = Object.FindFirstObjectByType<PlayerController2D>();
        go.transform.position = NearPlayer(pc, 8f);

        Undo.RegisterCreatedObjectUndo(go, "Create Monster Spawner");
        string combat = EnsurePlayerAttacker(pc);

        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        Debug.Log($"[unwritten] Saved prefab '{prefabPath}' and dropped MonsterSpawner_{tex.name} " +
                  $"(keeps up to {spawner.maxAlive} alive within {spawner.spawnRadius} units).{combat} " +
                  "Move the spawner where you want the pack, then press Play.");
    }

    /// <summary>
    /// Builds a Monster GameObject from a sliced sheet (no positioning / Undo /
    /// player wiring — the callers handle that). Returns null and shows a dialog
    /// if the sheet isn't usable.
    /// </summary>
    public static GameObject BuildMonster(Texture2D tex)
    {
        if (tex == null)
        {
            EditorUtility.DisplayDialog("Create Monster",
                "First click a sliced monster SpriteSheet texture in the Project window " +
                "(e.g. a Slime / Skull / Bear sheet), then run this menu again.",
                "OK");
            return null;
        }

        string path = AssetDatabase.GetAssetPath(tex);
        var frames = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderBy(FrameIndex)
            .ToArray();

        if (frames.Length == 0)
        {
            EditorUtility.DisplayDialog("Create Monster",
                $"'{tex.name}' has no sprites. Set Sprite Mode = Multiple and slice it " +
                "(Sprite Editor ▸ Slice ▸ Grid By Cell Size ▸ 16 × 16), then run this again.",
                "OK");
            return null;
        }

        int cols = Mathf.Max(1, tex.width / CellSize);
        int walk = Mathf.Min(WalkFrames, frames.Length / cols);

        var go = new GameObject("Monster_" + tex.name);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 9;          // NPC band, just under the player (10)
        sr.sprite = frames[0];

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var box = go.AddComponent<BoxCollider2D>();
        box.size = Vector2.one * 0.7f;

        go.AddComponent<Health>();
        go.AddComponent<MonsterAI>();

        if (cols >= 4 && walk >= 1)
        {
            Sprite[] Column(int c) =>
                Enumerable.Range(0, walk).Select(r => frames[c + r * cols]).ToArray();

            var anim = go.AddComponent<CharacterAnimator2D>();
            anim.down  = Column(0);
            anim.up    = Column(1);
            anim.left  = Column(2);
            anim.right = Column(3);
        }
        return go;
    }

    static Vector3 NearPlayer(PlayerController2D pc, float dx) =>
        (pc != null ? pc.transform.position : Vector3.zero) + new Vector3(dx, 0f, 0f);

    static string EnsurePlayerAttacker(PlayerController2D pc)
    {
        if (pc != null && pc.GetComponent<PlayerAttacker>() == null)
        {
            Undo.AddComponent<PlayerAttacker>(pc.gameObject);
            return " Added a PlayerAttacker to the Player (press J to attack).";
        }
        return "";
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    // Unity names sliced sprites "<sheet>_0" … "_27"; sort by that trailing number.
    static int FrameIndex(Sprite s)
    {
        int u = s.name.LastIndexOf('_');
        return (u >= 0 && int.TryParse(s.name.Substring(u + 1), out int n)) ? n : 0;
    }
}
