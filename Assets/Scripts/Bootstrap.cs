using UnityEngine;

/// <summary>
/// Procedural 2D "hello world": builds a camera, a walled arena with a
/// checkerboard floor and scattered crates, and a player you can drive with
/// WASD / arrow keys. Every sprite is generated at runtime, so NO art assets
/// are required.
///
/// Setup: in an otherwise-empty scene, create one empty GameObject, add this
/// component, and press Play.
/// </summary>
public class Bootstrap : MonoBehaviour
{
    [Tooltip("Build the procedural box arena (floor/walls/crates). Turn this OFF " +
             "once you have a real Tilemap field so the two don't overlap.")]
    public bool buildArena = true;

    [Header("Arena size (in tiles)")]
    public int arenaWidth = 24;
    public int arenaHeight = 16;

    [Header("Obstacles")]
    public int crateCount = 14;

    [Header("Player")]
    [Tooltip("Your real animated player (SpriteRenderer + PlayerController2D + " +
             "CharacterAnimator2D). Leave empty to spawn the placeholder box.")]
    public GameObject playerOverride;

    void Start()
    {
        SetupCamera();
        if (buildArena) BuildArena();
        var player = playerOverride != null
            ? PlacePlayer(playerOverride, ResolveSpawn(playerOverride))
            : SpawnPlayer(ResolveSpawn(null));
        AttachCameraFollow(player.transform);
    }

    void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = go.AddComponent<Camera>();
        }
        cam.orthographic = true;
        if (buildArena) cam.orthographicSize = arenaHeight * 0.6f;  // only drive zoom for the procedural arena
        cam.backgroundColor = new Color(0.05f, 0.06f, 0.09f);
        cam.transform.position = new Vector3(0f, 0f, -10f);
    }

    void BuildArena()
    {
        var floorA = new Color(0.13f, 0.15f, 0.20f);
        var floorB = new Color(0.16f, 0.18f, 0.24f);
        var wallColor = new Color(0.30f, 0.33f, 0.42f);
        var crateColor = new Color(0.55f, 0.40f, 0.24f);

        int halfW = arenaWidth / 2;
        int halfH = arenaHeight / 2;

        var floorRoot = new GameObject("Floor").transform;

        // Checkerboard floor so movement is easy to read.
        for (int x = -halfW; x < halfW; x++)
        {
            for (int y = -halfH; y < halfH; y++)
            {
                var c = ((x + y) & 1) == 0 ? floorA : floorB;
                var tile = MakeSprite($"Floor_{x}_{y}", c, new Vector3(x + 0.5f, y + 0.5f, 0f), 0);
                tile.transform.SetParent(floorRoot);
            }
        }

        // Border walls (with colliders) so the player is boxed in.
        var wallRoot = new GameObject("Walls").transform;
        for (int x = -halfW - 1; x <= halfW; x++)
        {
            SpawnWall(new Vector3(x + 0.5f, halfH + 0.5f, 0f), wallColor, wallRoot);
            SpawnWall(new Vector3(x + 0.5f, -halfH - 0.5f, 0f), wallColor, wallRoot);
        }
        for (int y = -halfH; y <= halfH; y++)
        {
            SpawnWall(new Vector3(-halfW - 0.5f, y + 0.5f, 0f), wallColor, wallRoot);
            SpawnWall(new Vector3(halfW + 0.5f, y + 0.5f, 0f), wallColor, wallRoot);
        }

        // Crates: static obstacles to bump into. Keep the spawn point clear.
        var crateRoot = new GameObject("Crates").transform;
        for (int i = 0; i < crateCount; i++)
        {
            var pos = new Vector3(
                Random.Range(-halfW + 1, halfW - 1) + 0.5f,
                Random.Range(-halfH + 1, halfH - 1) + 0.5f,
                0f);
            if (pos.magnitude < 2f) continue;

            var crate = MakeSprite($"Crate_{i}", crateColor, pos, 1);
            crate.AddComponent<BoxCollider2D>();
            crate.transform.SetParent(crateRoot);
        }
    }

    void SpawnWall(Vector3 pos, Color color, Transform parent)
    {
        var go = MakeSprite("Wall", color, pos, 1);
        go.AddComponent<BoxCollider2D>();
        go.transform.SetParent(parent);
    }

    GameObject SpawnPlayer(Vector2 pos)
    {
        var go = MakeSprite("Player", new Color(1f, 0.80f, 0.30f), new Vector3(pos.x, pos.y, 0f), 10);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        go.AddComponent<BoxCollider2D>();
        go.AddComponent<PlayerController2D>();
        return go;
    }

    // Use an existing player GameObject (your real animated character) instead of
    // the generated box: drop it at the spawn point and hand it back.
    GameObject PlacePlayer(GameObject player, Vector2 pos)
    {
        player.transform.position = new Vector3(pos.x, pos.y, 0f);
        return player;
    }

    // Where the player should appear: at the SpawnPoint matching a pending travel
    // target (set by a Portal), otherwise wherever it already sits in the scene.
    Vector2 ResolveSpawn(GameObject player)
    {
        if (!string.IsNullOrEmpty(SceneTravel.Target))
        {
            foreach (var sp in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
            {
                if (sp.id == SceneTravel.Target)
                {
                    SceneTravel.Target = null;
                    return sp.transform.position;
                }
            }
            SceneTravel.Target = null;   // id not found in this scene; ignore it
        }
        return player != null ? (Vector2)player.transform.position : Vector2.zero;
    }

    void AttachCameraFollow(Transform target)
    {
        var cam = Camera.main;
        var follow = cam.GetComponent<CameraFollow2D>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow2D>();
        follow.target = target;
    }

    static GameObject MakeSprite(string name, Color color, Vector3 pos, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Solid(color);
        sr.sortingOrder = sortingOrder;
        return go;
    }
}
