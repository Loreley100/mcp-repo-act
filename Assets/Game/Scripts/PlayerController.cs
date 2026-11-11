using UnityEngine;
using UnityEngine.InputSystem;

namespace MichiCosmico
{
    /// <summary>Michi, la protagonista: movimiento con aceleración, dash y parpadeo de invulnerabilidad.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        public Vector2 Bounds = new Vector2(9f, 5f);
        public float MaxSpeed = 7.2f;
        public float Acceleration = 60f;
        public float DashSpeed = 19f;
        public float DashCooldown = 1.05f;

        GameManager game;
        Rigidbody2D body;
        SpriteRenderer spriteRenderer;
        Vector2 input;
        float dashTimer;
        float invulnTimer;
        float squash;
        Vector3 baseScale;

        public bool Invulnerable => invulnTimer > 0f;

        public void Setup(GameManager manager, Vector2 arenaBounds)
        {
            game = manager;
            Bounds = arenaBounds;
        }

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            baseScale = transform.localScale;
        }

        void Update()
        {
            if (game == null || !game.IsPlaying)
            {
                input = Vector2.zero;
                return;
            }

            input = ReadMoveInput();

            dashTimer -= Time.deltaTime;
            bool dashPressed = ReadDashInput();
            if (dashPressed && dashTimer <= 0f && input.sqrMagnitude > 0.04f) Dash();

            // Inclinación según la velocidad horizontal (efecto "volando")
            float tilt = Mathf.Clamp(-body.linearVelocity.x * 2.2f, -16f, 16f);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, 0f, tilt), 8f * Time.deltaTime);

            // Squash & stretch
            squash = Mathf.MoveTowards(squash, 0f, Time.deltaTime * 5f);
            transform.localScale = new Vector3(
                baseScale.x * (1f + squash * 0.22f),
                baseScale.y * (1f - squash * 0.22f),
                1f);

            // Parpadeo mientras es invulnerable
            if (invulnTimer > 0f)
            {
                invulnTimer -= Time.deltaTime;
                var color = spriteRenderer.color;
                color.a = Mathf.PingPong(Time.time * 11f, 1f) * 0.55f + 0.45f;
                spriteRenderer.color = color;
            }
            else
            {
                var color = spriteRenderer.color;
                color.a = 1f;
                spriteRenderer.color = color;
            }
        }

        void FixedUpdate()
        {
            if (game == null || !game.IsPlaying)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            var target = input * MaxSpeed;
            body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, target, Acceleration * Time.fixedDeltaTime);

            var position = body.position;
            position.x = Mathf.Clamp(position.x, -Bounds.x, Bounds.x);
            position.y = Mathf.Clamp(position.y, -Bounds.y, Bounds.y);
            body.position = position;
        }

        Vector2 ReadMoveInput()
        {
            float x = 0f;
            float y = 0f;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                var stick = gamepad.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.04f)
                {
                    x += stick.x;
                    y += stick.y;
                }
            }

            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }

        bool ReadDashInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame)) return true;

            var gamepad = Gamepad.current;
            if (gamepad != null && gamepad.buttonEast.wasPressedThisFrame) return true;

            return false;
        }

        void Dash()
        {
            dashTimer = DashCooldown;
            squash = 1f;
            body.linearVelocity = input.normalized * DashSpeed;
            game?.OnPlayerDashed(transform.position);
        }

        public void StartInvulnerability(float seconds)
        {
            invulnTimer = seconds;
        }

        public void Knockback(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.001f) direction = Vector2.up;
            body.linearVelocity = direction.normalized * 11f;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (game == null || !game.IsPlaying) return;

            var star = other.GetComponentInParent<StarPickup>();
            if (star != null)
            {
                game.CollectStar(star);
                return;
            }

            var asteroid = other.GetComponentInParent<Asteroid>();
            if (asteroid != null) game.HitPlayer(asteroid);
        }
    }
}
