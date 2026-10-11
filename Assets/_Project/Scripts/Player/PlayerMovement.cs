using UnityEngine;

namespace EscapeGame
{
    /// <summary>
    /// Movimiento en primera persona con CharacterController: caminar, correr (con stamina), saltar y gravedad.
    /// Va en el objeto raíz "Player". El giro de la cámara está en PlayerLook.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Velocidad")]
        [SerializeField] float walkSpeed = 3.5f;
        [SerializeField] float sprintSpeed = 6.2f;
        [Tooltip("Qué tan rápido alcanza la velocidad objetivo (m/s²).")]
        [SerializeField] float acceleration = 30f;

        [Header("Salto y gravedad")]
        [SerializeField] float jumpHeight = 0.9f;
        [SerializeField] float gravity = -20f;

        [Header("Stamina (correr)")]
        [SerializeField] float maxStamina = 100f;
        [SerializeField] float sprintDrainPerSecond = 25f;
        [SerializeField] float regenPerSecond = 15f;
        [SerializeField] float regenDelay = 1.2f;
        [Tooltip("Tras agotarse, hace falta recuperar esta cantidad para volver a correr.")]
        [SerializeField] float minStaminaToSprint = 20f;

        public float Stamina01 { get { return maxStamina > 0f ? stamina / maxStamina : 0f; } }
        public bool IsSprinting { get; private set; }
        public bool IsGrounded { get { return controller.isGrounded; } }
        public float HorizontalSpeed { get; private set; }
        public bool IsMoving { get { return HorizontalSpeed > 0.15f; } }

        CharacterController controller;
        Vector3 horizontalVelocity;
        float verticalVelocity;
        float stamina;
        float regenTimer;
        bool exhausted;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            stamina = maxStamina;
        }

        void Update()
        {
            if (!GameManager.IsPlaying)
            {
                IsSprinting = false;
                HorizontalSpeed = 0f;
                return;
            }

            float dt = Time.deltaTime;
            Vector2 input = GameInput.Move;

            // 1) Correr (gasta stamina; si se agota hay que recuperar un mínimo para volver a correr)
            if (exhausted && stamina >= minStaminaToSprint) exhausted = false;
            bool wantsSprint = GameInput.Sprint && input.y > 0.1f && !exhausted;
            IsSprinting = wantsSprint && stamina > 0f;

            if (IsSprinting)
            {
                stamina = Mathf.Max(0f, stamina - sprintDrainPerSecond * dt);
                regenTimer = regenDelay;
                if (stamina <= 0f)
                {
                    exhausted = true;
                    IsSprinting = false;
                }
            }
            else
            {
                regenTimer -= dt;
                if (regenTimer <= 0f) stamina = Mathf.Min(maxStamina, stamina + regenPerSecond * dt);
            }

            // 2) Movimiento horizontal (relativo a hacia dónde mira el jugador)
            Vector3 direction = transform.right * input.x + transform.forward * input.y;
            Vector3 targetVelocity = direction * (IsSprinting ? sprintSpeed : walkSpeed);
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration * dt);

            // 3) Salto y gravedad
            bool grounded = controller.isGrounded;
            if (grounded && verticalVelocity < 0f) verticalVelocity = -2f;
            if (grounded && GameInput.JumpPressed) verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            verticalVelocity += gravity * dt;

            controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);

            // Velocidad REAL (si empujas una pared, es 0 aunque pulses W). La usan los pasos y el oído del enemigo.
            Vector3 v = controller.velocity;
            HorizontalSpeed = new Vector2(v.x, v.z).magnitude;
        }
    }
}
