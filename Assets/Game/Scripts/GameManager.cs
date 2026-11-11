using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MichiCosmico
{
    /// <summary>
    /// "Michi Cósmico" — un nivel: atrapa las 12 estrellas en 45 segundos
    /// mientras esquivas los meteoritos. Todo se genera por código.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public const int StarCount = 12;
        public const float LevelTime = 45f;
        public const int MaxHearts = 3;

        enum State { Title, Playing, Won, Lost }

        public static GameManager Instance { get; private set; }

        State state = State.Title;
        public bool IsPlaying => state == State.Playing;

        Camera cam;
        CameraShake shake;
        Transform world;
        PlayerController player;
        Vector2 bounds;

        readonly List<StarPickup> stars = new List<StarPickup>();
        readonly List<Asteroid> asteroids = new List<Asteroid>();
        int heartsLeft = MaxHearts;
        float timeLeft = LevelTime;
        bool restarting;

        // UI
        Canvas canvas;
        Transform overlayRoot;
        Text scoreText;
        Text timerText;
        Text overlayTitle;
        Text overlaySubtitle;
        Text overlayPrompt;
        readonly List<Image> heartIcons = new List<Image>();
        Image flashImage;

        static readonly Color Gold = new Color(1f, 0.83f, 0.30f);
        static readonly Color Pink = new Color(1f, 0.55f, 0.85f);
        static readonly Color Danger = new Color(1f, 0.30f, 0.45f);
        static readonly Color TextColor = new Color(0.92f, 0.94f, 1f);

        // ------------------------------------------------------------------ arranque

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBoot()
        {
            if (Object.FindAnyObjectByType<GameManager>() != null) return;
            var go = new GameObject("Michi Cosmico");
            go.AddComponent<GameManager>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            SetupCamera();
            BuildUI();
            BuildWorld();
            ShowTitle();
            UpdateHUD();
        }

        void SetupCamera()
        {
            cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }

            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.08f);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;

            shake = cam.GetComponent<CameraShake>();
            if (shake == null) shake = cam.gameObject.AddComponent<CameraShake>();

            var audio = cam.GetComponent<AudioSource>();
            if (audio == null) audio = cam.gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0f;
            audio.volume = 1f;
            Sfx.Init(audio);

            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            bounds = new Vector2(halfWidth - 1.0f, halfHeight - 0.9f);
        }

        // ------------------------------------------------------------------ mundo

        void BuildWorld()
        {
            var worldGo = new GameObject("World");
            world = worldGo.transform;
            world.SetParent(transform, false);

            var starfieldGo = new GameObject("Starfield");
            starfieldGo.transform.SetParent(world, false);
            var starfield = starfieldGo.AddComponent<Starfield>();
            starfield.Build(bounds, starfieldGo.transform);

            BuildArena();

            // Michi
            var playerGo = new GameObject("Michi");
            playerGo.transform.SetParent(world, false);
            playerGo.transform.position = new Vector3(0f, -2.3f, 0f);
            playerGo.transform.localScale = Vector3.one * 1.9f;

            var playerSprite = playerGo.AddComponent<SpriteRenderer>();
            playerSprite.sprite = GameArt.Cat();
            playerSprite.color = Color.white;
            playerSprite.sortingOrder = 12;
            playerSprite.sharedMaterial = GameArt.Unlit;

            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(playerGo.transform, false);
            glowGo.transform.localScale = Vector3.one * 1.5f;
            var playerGlow = glowGo.AddComponent<SpriteRenderer>();
            playerGlow.sprite = GameArt.Soft();
            playerGlow.color = new Color(1f, 0.62f, 0.88f, 0.22f);
            playerGlow.sortingOrder = 11;
            playerGlow.sharedMaterial = GameArt.Unlit;

            var body = playerGo.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var collider = playerGo.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.36f;

            var trail = playerGo.AddComponent<TrailRenderer>();
            trail.sharedMaterial = GameArt.Unlit;
            trail.time = 0.32f;
            trail.startWidth = 0.42f;
            trail.endWidth = 0f;
            trail.numCapVertices = 4;
            trail.sortingOrder = 10;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.72f, 0.92f), 0f) },
                new[] { new GradientAlphaKey(0.65f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;

            player = playerGo.AddComponent<PlayerController>();
            player.Setup(this, bounds);

            BuildAsteroids();
            BuildStars();
        }

        void BuildArena()
        {
            float x = bounds.x + 0.55f;
            float y = bounds.y + 0.55f;
            var edge = new Color(0.45f, 0.85f, 1.00f, 0.30f);

            Bar(new Vector3(0f, y, 0f), new Vector3(x * 2f + 0.3f, 0.06f, 1f), edge);
            Bar(new Vector3(0f, -y, 0f), new Vector3(x * 2f + 0.3f, 0.06f, 1f), edge);
            Bar(new Vector3(-x, 0f, 0f), new Vector3(0.06f, y * 2f + 0.3f, 1f), edge);
            Bar(new Vector3(x, 0f, 0f), new Vector3(0.06f, y * 2f + 0.3f, 1f), edge);

            var corner = new Color(0.55f, 0.95f, 1.00f, 0.55f);
            foreach (var sx in new[] { -1f, 1f })
            foreach (var sy in new[] { -1f, 1f })
                Bar(new Vector3(sx * x, sy * y, 0f), new Vector3(0.22f, 0.22f, 1f), corner);
        }

        void Bar(Vector3 position, Vector3 scale, Color color)
        {
            var go = new GameObject("Borde");
            go.transform.SetParent(world, false);
            go.transform.position = position;
            go.transform.localScale = scale;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = GameArt.Square();
            renderer.color = color;
            renderer.sortingOrder = -5;
            renderer.sharedMaterial = GameArt.Unlit;
        }

        void BuildAsteroids()
        {
            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject("Meteorito_" + i);
                go.transform.SetParent(world, false);

                var scale = Random.Range(0.95f, 1.45f);

                // Nacen lejos del centro, donde aparece Michi
                Vector2 position;
                int attempts = 0;
                do
                {
                    position = new Vector2(Random.Range(-bounds.x, bounds.x), Random.Range(-bounds.y, bounds.y));
                    attempts++;
                } while (position.sqrMagnitude < 16f && attempts < 40);

                go.transform.position = position;
                go.transform.localScale = Vector3.one * scale;

                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = GameArt.Asteroid(i + 1);
                renderer.color = new Color(0.92f, 0.88f, 1.00f);
                renderer.sortingOrder = 6;
                renderer.sharedMaterial = GameArt.Unlit;

                var collider = go.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
                collider.radius = 0.44f;

                float angle = Random.value * Mathf.PI * 2f;
                float speed = Random.Range(1.6f, 2.6f);
                var asteroid = go.AddComponent<Asteroid>();
                asteroid.Setup(
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed,
                    Random.Range(-70f, 70f),
                    bounds,
                    0.44f * scale);

                asteroids.Add(asteroid);
            }
        }

        void BuildStars()
        {
            var taken = new List<Vector2>();

            for (int i = 0; i < StarCount; i++)
            {
                Vector2 position;
                int attempts = 0;
                bool ok;
                do
                {
                    position = new Vector2(
                        Random.Range(-bounds.x + 1f, bounds.x - 1f),
                        Random.Range(-bounds.y + 1f, bounds.y - 1f));
                    ok = position.sqrMagnitude > 9f;
                    foreach (var other in taken)
                        if ((position - other).sqrMagnitude < 2.4f) ok = false;
                    attempts++;
                } while (!ok && attempts < 60);

                taken.Add(position);

                var go = new GameObject("Estrella_" + i);
                go.transform.SetParent(world, false);
                go.transform.position = position;

                var glowGo = new GameObject("Glow");
                glowGo.transform.SetParent(go.transform, false);
                glowGo.transform.localScale = Vector3.one * 2.2f;
                var glow = glowGo.AddComponent<SpriteRenderer>();
                glow.sprite = GameArt.Soft();
                glow.color = new Color(1f, 0.85f, 0.35f, 0.38f);
                glow.sortingOrder = 4;
                glow.sharedMaterial = GameArt.Unlit;

                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = GameArt.Star();
                renderer.color = Gold;
                renderer.sortingOrder = 5;
                renderer.sharedMaterial = GameArt.Unlit;

                var collider = go.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
                collider.radius = 0.42f;

                stars.Add(go.AddComponent<StarPickup>());
            }
        }

        // ------------------------------------------------------------------ interfaz

        void BuildUI()
        {
            var canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = canvasGo.transform;
            var rootRect = canvasGo.GetComponent<RectTransform>();
            Stretch(rootRect);

            // Destello rojo al recibir daño
            flashImage = MakeImage("Flash", root, null, Color.clear);
            Stretch(flashImage.rectTransform);

            // HUD
            var hudGo = new GameObject("HUD", typeof(RectTransform));
            hudGo.transform.SetParent(root, false);
            Stretch(hudGo.GetComponent<RectTransform>());

            var starIcon = MakeImage("IconoEstrella", hudGo.transform, GameArt.Star(), Gold);
            Place(starIcon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(44f, -40f), new Vector2(58f, 58f));

            scoreText = MakeText("Puntuacion", hudGo.transform, "0 / " + StarCount, 46, TextAnchor.MiddleLeft, Gold);
            Place(scoreText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(116f, -44f), new Vector2(320f, 64f));
            scoreText.fontStyle = FontStyle.Bold;

            timerText = MakeText("Tiempo", hudGo.transform, Mathf.CeilToInt(LevelTime).ToString(), 52, TextAnchor.MiddleRight, TextColor);
            Place(timerText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-52f, -46f), new Vector2(220f, 66f));
            timerText.fontStyle = FontStyle.Bold;

            var timerLabel = MakeText("EtiquetaTiempo", hudGo.transform, "SEG", 20, TextAnchor.MiddleRight, new Color(0.65f, 0.72f, 0.85f));
            Place(timerLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-52f, -104f), new Vector2(220f, 32f));

            for (int i = 0; i < MaxHearts; i++)
            {
                var heart = MakeImage("Corazon_" + i, hudGo.transform, GameArt.Heart(), new Color(1f, 0.30f, 0.45f));
                Place(heart.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(56f + i * 52f, -124f), new Vector2(44f, 44f));
                heartIcons.Add(heart);
            }

            var hint = MakeText("Ayuda", hudGo.transform,
                "WASD / Flechas  moverse      SHIFT  dash      R  reiniciar", 24, TextAnchor.MiddleCenter,
                new Color(0.70f, 0.76f, 0.90f, 0.8f));
            Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(1400f, 40f));

            // Panel central (título / victoria / derrota)
            var overlayGo = new GameObject("Overlay", typeof(RectTransform));
            overlayGo.transform.SetParent(root, false);
            overlayRoot = overlayGo.transform;
            Stretch(overlayGo.GetComponent<RectTransform>());

            var dim = MakeImage("Oscurecer", overlayRoot, null, new Color(0f, 0f, 0.03f, 0.55f));
            Stretch(dim.rectTransform);

            var panelGlow = MakeImage("PanelBrillo", overlayRoot, GameArt.Soft(), new Color(0.62f, 0.40f, 1.00f, 0.30f));
            Place(panelGlow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1500f, 760f));

            var panel = MakeImage("Panel", overlayRoot, GameArt.Panel(), new Color(0.07f, 0.05f, 0.14f, 0.92f));
            panel.type = Image.Type.Sliced;
            Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(960f, 400f));

            overlayTitle = MakeText("Titulo", overlayRoot, "MICHI CÓSMICO", 88, TextAnchor.MiddleCenter, Pink);
            Place(overlayTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 265f), new Vector2(900f, 130f));
            overlayTitle.fontStyle = FontStyle.Bold;

            overlaySubtitle = MakeText("Subtitulo", overlayRoot, "", 32, TextAnchor.MiddleCenter, TextColor);
            Place(overlaySubtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 135f), new Vector2(880f, 120f));

            overlayPrompt = MakeText("Continuar", overlayRoot, "", 30, TextAnchor.MiddleCenter, Gold);
            Place(overlayPrompt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -5f), new Vector2(880f, 60f));
            overlayPrompt.fontStyle = FontStyle.Bold;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        static Image MakeImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static Text MakeText(string name, Transform parent, string content, int size, TextAnchor alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = GameArt.UIFont;
            text.text = content;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
            shadow.effectDistance = new Vector2(2f, -2f);

            return text;
        }

        // ------------------------------------------------------------------ bucle

        void Update()
        {
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;

            bool confirm = (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)) ||
                           (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);
            bool retry = (keyboard != null && keyboard.rKey.wasPressedThisFrame) ||
                         (gamepad != null && gamepad.buttonEast.wasPressedThisFrame);

            switch (state)
            {
                case State.Title:
                    if (confirm) StartGame();
                    break;
                case State.Playing:
                    timeLeft -= Time.deltaTime;
                    UpdateHUD();
                    if (timeLeft <= 0f)
                    {
                        timeLeft = 0f;
                        Lose("¡SE ACABÓ EL TIEMPO!");
                    }
                    break;
                case State.Won:
                case State.Lost:
                    if (confirm || retry) Restart();
                    break;
            }

            if (flashImage != null && flashImage.color.a > 0f)
            {
                var color = flashImage.color;
                color.a = Mathf.MoveTowards(color.a, 0f, Time.unscaledDeltaTime * 2.4f);
                flashImage.color = color;
            }

            if (overlayRoot != null && overlayRoot.gameObject.activeSelf && overlayPrompt != null)
            {
                var color = overlayPrompt.color;
                color.a = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 3.2f);
                overlayPrompt.color = color;
            }
        }

        void UpdateHUD()
        {
            int collected = StarCount - stars.Count;
            scoreText.text = collected + " / " + StarCount;

            int seconds = Mathf.CeilToInt(Mathf.Max(0f, timeLeft));
            timerText.text = seconds.ToString();
            timerText.color = seconds <= 10 ? Danger : TextColor;

            for (int i = 0; i < heartIcons.Count; i++)
                heartIcons[i].color = i < heartsLeft ? new Color(1f, 0.30f, 0.45f) : new Color(0.30f, 0.30f, 0.40f, 0.45f);
        }

        // ------------------------------------------------------------------ eventos de juego

        public void CollectStar(StarPickup star)
        {
            if (state != State.Playing || star == null || !stars.Contains(star)) return;

            var position = star.transform.position;
            stars.Remove(star);
            Destroy(star.gameObject);

            SpawnBurst(position, Gold, 16);
            shake.Add(0.16f);
            Sfx.Play("pickup", 0.55f, 1f + (StarCount - stars.Count) * 0.03f);
            SpeedUpAsteroids();
            UpdateHUD();

            if (stars.Count == 0) Win();
        }

        public void HitPlayer(Asteroid asteroid)
        {
            if (state != State.Playing || player == null || player.Invulnerable) return;

            heartsLeft--;
            UpdateHUD();
            shake.Add(0.55f);
            Flash(new Color(1f, 0.25f, 0.35f, 0.45f));
            Sfx.Play("hit", 0.9f);
            SpawnBurst(player.transform.position, new Color(1f, 0.45f, 0.55f), 12);

            if (asteroid != null)
                player.Knockback(player.transform.position - asteroid.transform.position);
            else
                player.Knockback(Vector2.up);

            if (heartsLeft <= 0) Lose("¡TE ATRAPARON!");
            else player.StartInvulnerability(1.7f);
        }

        public void OnPlayerDashed(Vector3 position)
        {
            Sfx.Play("dash", 0.45f);
            SpawnBurst(position, new Color(0.75f, 0.85f, 1f), 6);
        }

        void SpeedUpAsteroids()
        {
            float multiplier = 1f + 0.07f * (StarCount - stars.Count);
            foreach (var asteroid in asteroids) asteroid.SetSpeedMultiplier(multiplier);
        }

        void StartGame()
        {
            state = State.Playing;
            timeLeft = LevelTime;
            heartsLeft = MaxHearts;
            overlayRoot.gameObject.SetActive(false);
            UpdateHUD();
            SpawnBurst(player != null ? player.transform.position : Vector3.zero, Pink, 18);
            Sfx.Play("dash", 0.5f, 1.4f);
        }

        void Win()
        {
            state = State.Won;
            Sfx.Play("win", 0.9f);
            shake.Add(0.35f);
            SpawnBurst(player != null ? player.transform.position : Vector3.zero, Gold, 30);
            ShowOverlay("¡NIVEL COMPLETADO!",
                "Michi volvió a casa con todas las estrellas\nTiempo restante: " + timeLeft.ToString("0.0") + " s",
                "R  o  ESPACIO  —  jugar otra vez",
                new Color(0.65f, 1f, 0.75f));
        }

        void Lose(string title)
        {
            state = State.Lost;
            Sfx.Play("lose", 0.9f);
            ShowOverlay(title,
                "Estrellas: " + (StarCount - stars.Count) + " de " + StarCount,
                "R  o  ESPACIO  —  reintentar",
                Danger);
        }

        void ShowTitle()
        {
            ShowOverlay("MICHI CÓSMICO",
                "Atrapa las " + StarCount + " estrellas en " + Mathf.RoundToInt(LevelTime) + " s\nesquivando los meteoritos",
                "Pulsa  ESPACIO  para empezar",
                Pink);
        }

        void ShowOverlay(string title, string subtitle, string prompt, Color titleColor)
        {
            overlayRoot.gameObject.SetActive(true);
            overlayTitle.text = title;
            overlayTitle.color = titleColor;
            overlaySubtitle.text = subtitle;
            overlayPrompt.text = prompt;
        }

        void Flash(Color color)
        {
            flashImage.color = color;
        }

        IEnumerator RestartRoutine()
        {
            restarting = true;
            overlayRoot.gameObject.SetActive(false);
            state = State.Title;

            if (world != null) Destroy(world.gameObject);
            yield return null;

            stars.Clear();
            asteroids.Clear();
            heartsLeft = MaxHearts;
            timeLeft = LevelTime;
            BuildWorld();
            ShowTitle();
            UpdateHUD();
            restarting = false;
        }

        public void Restart()
        {
            if (restarting) return;
            StartCoroutine(RestartRoutine());
        }

        // ------------------------------------------------------------------ partículas

        void SpawnBurst(Vector3 position, Color color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Chispa");
                go.transform.SetParent(world != null ? world : transform, false);
                go.transform.position = position;

                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = GameArt.Spark();
                renderer.color = color;
                renderer.sortingOrder = 20;
                renderer.sharedMaterial = GameArt.Unlit;

                var spark = go.AddComponent<Spark>();
                float angle = Random.value * Mathf.PI * 2f;
                spark.Setup(
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(2.5f, 6.5f),
                    Random.Range(0.25f, 0.6f),
                    Random.Range(0.35f, 0.8f),
                    color);
            }
        }
    }
}
