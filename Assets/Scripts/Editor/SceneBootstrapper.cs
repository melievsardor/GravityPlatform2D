#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PlatformGravity.Editor
{
    /// <summary>
    /// One-click scene builder accessible via <b>Tools → Platform Gravity → Build Scene</b>.
    ///
    /// Creates the platform, player, camera, and mobile UI from scratch so the prototype
    /// can be tested immediately without any manual setup.
    ///
    /// Note on collider sizing: the platform uses <c>localScale = (6, 2.5, 1)</c> with a
    /// <c>BoxCollider2D.size = (1, 1)</c>. After scale, the world-space collider is 6 × 2.5,
    /// which matches the default value of <see cref="Player.PlatformBounds.Size"/>.
    /// </summary>
    public static class SceneBootstrapper
    {
        [MenuItem("Tools/Platform Gravity/Build Scene")]
        public static void BuildScene()
        {
            CreatePlatform(out var platformBounds);
            var player = CreatePlayer(platformBounds);
            CreateCamera(player.transform);
            CreateMobileUI(player.GetComponent<Input.TouchInputHandler>());

            Selection.activeGameObject = player;
            Debug.Log("[SceneBootstrapper] Scene ready.");
        }

        private static void CreatePlatform(out Player.PlatformBounds bounds)
        {
            var go = new GameObject("Platform");
            go.transform.position = Vector3.zero;
            go.transform.localScale = new Vector3(6f, 2.5f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = MakeSquareSprite(Color.white);
            sr.sortingOrder = 0;

            // Local size (1,1) × scale (6, 2.5) = world collider 6×2.5.
            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            bounds = go.AddComponent<Player.PlatformBounds>();
        }

        private static GameObject CreatePlayer(Player.PlatformBounds platformBounds)
        {
            var go = new GameObject("Player");
            go.transform.position = new Vector3(0f, 2f, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = MakeCircleSprite(new Color(0.2f, 0.6f, 1f));
            sr.sortingOrder = 1;

            go.AddComponent<Rigidbody2D>();

            var capsule = go.AddComponent<CapsuleCollider2D>();
            capsule.size = new Vector2(0.5f, 0.8f);

            // Wire the platform reference through SerializedObject so the private field is set.
            var controller = go.AddComponent<Player.PlayerController>();
            var controllerSO = new SerializedObject(controller);
            controllerSO.FindProperty("platform").objectReferenceValue = platformBounds;
            controllerSO.ApplyModifiedProperties();

            go.AddComponent<Player.PlayerVisuals>();
            go.AddComponent<Input.KeyboardInputHandler>();
            go.AddComponent<Input.TouchInputHandler>();

            return go;
        }

        private static void CreateCamera(Transform target)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";

            var cam = go.AddComponent<UnityEngine.Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f);

            var follow = go.AddComponent<Camera.CameraFollow>();
            var followSO = new SerializedObject(follow);
            followSO.FindProperty("target").objectReferenceValue = target;
            followSO.ApplyModifiedProperties();

            go.AddComponent<AudioListener>();
        }

        private static void CreateMobileUI(Input.TouchInputHandler handler)
        {
            var canvasGO = new GameObject("MobileUI_Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            EnsureEventSystem();

            // Movement buttons on the bottom-left.
            SpawnButton(canvasGO.transform, "Btn_Left",
                new Vector2(100, 100), new Vector2(120, 120),
                "◄", Input.MobileButton.ButtonAction.Left, handler);

            SpawnButton(canvasGO.transform, "Btn_Right",
                new Vector2(250, 100), new Vector2(120, 120),
                "►", Input.MobileButton.ButtonAction.Right, handler);

            // Jump button on the bottom-right.
            SpawnButton(canvasGO.transform, "Btn_Jump",
                new Vector2(-100, 100), new Vector2(130, 130),
                "▲", Input.MobileButton.ButtonAction.Jump, handler, anchorRight: true);
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        private static void SpawnButton(
            Transform parent, string name,
            Vector2 anchoredPos, Vector2 size, string label,
            Input.MobileButton.ButtonAction action,
            Input.TouchInputHandler handler,
            bool anchorRight = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchorMin = rt.anchorMax = rt.pivot =
                anchorRight ? new Vector2(1f, 0f) : new Vector2(0f, 0f);
            rt.anchoredPosition = anchoredPos;

            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.25f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var mb = go.AddComponent<Input.MobileButton>();
            var mbSO = new SerializedObject(mb);
            mbSO.FindProperty("action").enumValueIndex = (int)action;
            mbSO.FindProperty("target").objectReferenceValue = handler;
            mbSO.ApplyModifiedProperties();

            // Child text label.
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);

            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;

            var text = labelGO.AddComponent<Text>();
            text.text = label;
            text.fontSize = 36;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        // --- Procedural sprite helpers ---

        private static Sprite MakeSquareSprite(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private static Sprite MakeCircleSprite(Color color)
        {
            const int res = 64;
            var tex = new Texture2D(res, res);
            float radius = res * 0.5f;

            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float dx = x - radius + 0.5f;
                    float dy = y - radius + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    tex.SetPixel(x, y, dist < radius ? color : Color.clear);
                }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
        }
    }
}
#endif