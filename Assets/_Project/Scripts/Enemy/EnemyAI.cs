using UnityEngine;
using UnityEngine.AI;

namespace EscapeGame
{
    public enum EnemyState { Patrol, Investigate, Chase }

    /// <summary>
    /// IA del enemigo con una máquina de estados sobre NavMeshAgent:
    ///
    ///   PATRULLA ──(te ve)──────────────▶ PERSECUCIÓN ──(te pierde 3 s)──▶ INVESTIGAR ──(nada)──▶ PATRULLA
    ///      ▲  └──(te oye correr)──▶ INVESTIGAR ──(te ve)──▶ PERSECUCIÓN        │
    ///      └────────────────────────────────────────────────────────────────────┘
    ///
    /// Percepción:  VISTA  = distancia + ángulo (cono) + Raycast contra paredes.
    ///              OÍDO   = distancia; correr hace más ruido que caminar.
    /// Optimización: la percepción (lo caro) se revisa cada "perceptionInterval" segundos, no en cada fotograma.
    /// Va en el prefab Enemy (que ya trae NavMeshAgent). Requiere un NavMesh horneado en la escena.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("Patrulla")]
        [Tooltip("Objeto PatrolRoute cuyos hijos son los waypoints. Si se deja vacío, vaga al azar.")]
        [SerializeField] PatrolRoute route;
        [SerializeField] float patrolSpeed = 1.8f;
        [SerializeField] float waitAtPoint = 2f;
        [Tooltip("Sin ruta, vaga por puntos al azar dentro de este radio alrededor de donde nació.")]
        [SerializeField] float wanderRadius = 12f;

        [Header("Visión")]
        [SerializeField] float sightRange = 15f;
        [SerializeField, Range(10f, 360f)] float fieldOfView = 110f;
        [SerializeField] float eyeHeight = 1.6f;
        [Tooltip("A esta distancia te detecta aunque estés a su espalda.")]
        [SerializeField] float closeDetectRange = 2.5f;
        [Tooltip("Capas que pueden tapar la visión (paredes, muebles...). Por defecto todas.")]
        [SerializeField] LayerMask sightBlockers = ~0;

        [Header("Oído")]
        [SerializeField] float hearingRangeWalk = 4f;
        [SerializeField] float hearingRangeSprint = 11f;

        [Header("Persecución")]
        [SerializeField] float chaseSpeed = 4.6f;
        [Tooltip("Segundos que sigue persiguiendo tras perderte de vista.")]
        [SerializeField] float loseSightDelay = 3f;
        [SerializeField] float catchDistance = 1.4f;

        [Header("Investigar")]
        [SerializeField] float investigateSpeed = 2.8f;
        [SerializeField] float lookAroundTime = 3.5f;

        [Header("Rendimiento")]
        [Tooltip("Cada cuántos segundos revisa si te ve u oye (el Raycast es lo más caro). 0.15 s = ~7 veces por segundo.")]
        [SerializeField] float perceptionInterval = 0.15f;

        [Header("Feedback (opcional)")]
        [Tooltip("Animator con un parámetro float llamado 'Speed'. Si no hay, se ignora.")]
        [SerializeField] Animator animator;
        [SerializeField] AudioClip alertClip;
        [SerializeField] AudioClip catchClip;
        [SerializeField] AudioClip[] growlClips;
        [SerializeField] Vector2 growlInterval = new Vector2(6f, 14f);
        [Tooltip("A esta distancia o menos empieza a sonar el latido de tensión.")]
        [SerializeField] float tensionDistance = 18f;

        public EnemyState CurrentState { get; private set; }
        public bool IsFrozen { get; private set; }

        NavMeshAgent agent;
        AudioSource audioSource;
        Transform player;
        PlayerMovement playerMovement;

        Vector3 homePosition;
        Vector3[] patrolPoints;
        int patrolIndex;
        bool waiting;
        float waitTimer;

        Vector3 lastKnownPosition;
        float timeSinceSeen;
        float lookTimer = -1f;
        float stuckTimer;
        float perceptionTimer;
        bool canSeePlayer;
        float growlTimer;
        bool animatorHasSpeed;

        static readonly int SpeedHash = Animator.StringToHash("Speed");

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            audioSource = GetComponent<AudioSource>();
            homePosition = transform.position;
        }

        void Start()
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
            {
                Debug.LogError("[EnemyAI] No hay ningún objeto con el tag 'Player'.", this);
                enabled = false;
                return;
            }
            player = playerObject.transform;
            playerMovement = playerObject.GetComponent<PlayerMovement>();

            // El agente debe estar sobre el NavMesh. Si nació un poco flotando, lo pegamos al suelo.
            if (!agent.isOnNavMesh)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, 4f, NavMesh.AllAreas)) agent.Warp(hit.position);
            }
            if (!agent.isOnNavMesh)
            {
                Debug.LogError("[EnemyAI] '" + name + "' no está sobre un NavMesh. Haz Bake del NavMeshSurface " +
                               "(objeto Environment) y coloca al enemigo sobre el suelo.", this);
                enabled = false;
                return;
            }

            if (animator != null && animator.runtimeAnimatorController != null) animatorHasSpeed = HasParameter(animator, "Speed");

            BuildPatrolPoints();
            perceptionTimer = Random.value * perceptionInterval; // varios enemigos no revisan todos en el mismo fotograma
            growlTimer = Random.Range(growlInterval.x, growlInterval.y);
            EnterPatrol();
        }

        void OnDisable()
        {
            if (CurrentState == EnemyState.Chase && GameManager.Instance != null)
                GameManager.Instance.ReportChase(this, false);
        }

        void Update()
        {
            if (!GameManager.IsPlaying || player == null || IsFrozen) return;

            float dt = Time.deltaTime;

            perceptionTimer -= dt;
            if (perceptionTimer <= 0f)
            {
                perceptionTimer = perceptionInterval;
                Perceive();
            }

            switch (CurrentState)
            {
                case EnemyState.Patrol: UpdatePatrol(dt); break;
                case EnemyState.Investigate: UpdateInvestigate(dt); break;
                case EnemyState.Chase: UpdateChase(dt); break;
            }

            UpdateAnimator();
            UpdateAudio(dt);
        }

        /// <summary>Lo usa DebugTools para congelar al enemigo mientras pruebas.</summary>
        public void SetFrozen(bool frozen)
        {
            IsFrozen = frozen;
            if (agent != null && agent.isOnNavMesh) agent.isStopped = frozen;
        }

        // ======================= PERCEPCIÓN =======================

        void Perceive()
        {
            Vector3 toPlayer = player.position - transform.position;
            float sqrDist = toPlayer.sqrMagnitude; // sqrMagnitude evita una raíz cuadrada

            canSeePlayer = CanSee(toPlayer, sqrDist);

            if (canSeePlayer)
            {
                lastKnownPosition = player.position;
                timeSinceSeen = 0f;

                if (CurrentState != EnemyState.Chase) EnterChase();
                else if (agent.isOnNavMesh) agent.SetDestination(lastKnownPosition);
                return;
            }

            if (CurrentState != EnemyState.Chase && CanHear(sqrDist))
            {
                bool newNoise = CurrentState != EnemyState.Investigate ||
                                (player.position - lastKnownPosition).sqrMagnitude > 4f;
                if (newNoise)
                {
                    lastKnownPosition = player.position;
                    EnterInvestigate(lastKnownPosition);
                }
            }
        }

        bool CanSee(Vector3 toPlayer, float sqrDist)
        {
            // 1) Distancia: barata, descarta casi todo.
            if (sqrDist > sightRange * sightRange) return false;

            // 2) Ángulo: ¿está dentro del cono de visión? (muy cerca te detecta igual)
            bool veryClose = sqrDist <= closeDetectRange * closeDetectRange;
            if (!veryClose)
            {
                Vector3 flat = new Vector3(toPlayer.x, 0f, toPlayer.z);
                if (Vector3.Angle(transform.forward, flat) > fieldOfView * 0.5f) return false;
            }

            // 3) Raycast: lo más caro, solo si pasó lo anterior. ¿Hay una pared en medio?
            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            Vector3 target = player.position + Vector3.up * 1.0f;
            Vector3 direction = target - eye;
            float distance = direction.magnitude;
            if (distance < 0.01f) return true;

            RaycastHit hit;
            if (Physics.Raycast(eye, direction / distance, out hit, distance + 0.1f, sightBlockers, QueryTriggerInteraction.Ignore))
                return hit.collider.CompareTag("Player");

            return false;
        }

        bool CanHear(float sqrDist)
        {
            if (playerMovement == null) return false;

            float range = 0f;
            if (playerMovement.IsSprinting) range = hearingRangeSprint;
            else if (playerMovement.IsMoving) range = hearingRangeWalk;

            return range > 0f && sqrDist <= range * range;
        }

        // ======================= ESTADOS =======================

        void SetState(EnemyState newState)
        {
            bool wasChasing = CurrentState == EnemyState.Chase;
            CurrentState = newState;
            bool isChasing = newState == EnemyState.Chase;

            if (wasChasing != isChasing && GameManager.Instance != null)
                GameManager.Instance.ReportChase(this, isChasing);
        }

        // ---- Patrulla ----

        void EnterPatrol()
        {
            SetState(EnemyState.Patrol);
            agent.speed = patrolSpeed;
            agent.isStopped = false;
            waiting = false;
            lookTimer = -1f;
            GoToNextPatrolPoint(true);
        }

        void UpdatePatrol(float dt)
        {
            if (waiting)
            {
                waitTimer -= dt;
                if (waitTimer <= 0f)
                {
                    waiting = false;
                    GoToNextPatrolPoint(false);
                }
                return;
            }

            if (HasArrived())
            {
                waiting = true;
                waitTimer = waitAtPoint;
            }
        }

        void GoToNextPatrolPoint(bool pickClosest)
        {
            Vector3 destination;

            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                patrolIndex = pickClosest ? ClosestPatrolPointIndex() : (patrolIndex + 1) % patrolPoints.Length;
                destination = patrolPoints[patrolIndex];
            }
            else
            {
                destination = RandomWanderPoint();
            }

            agent.SetDestination(destination);
        }

        // ---- Investigar ----

        void EnterInvestigate(Vector3 position)
        {
            SetState(EnemyState.Investigate);
            agent.speed = investigateSpeed;
            agent.isStopped = false;
            waiting = false;
            lookTimer = -1f; // todavía no llegó
            agent.SetDestination(SnapToNavMesh(position));
        }

        void UpdateInvestigate(float dt)
        {
            if (lookTimer < 0f)
            {
                if (HasArrived()) lookTimer = lookAroundTime; // llegó: se queda mirando alrededor
                return;
            }

            transform.Rotate(0f, 70f * dt, 0f);
            lookTimer -= dt;
            if (lookTimer <= 0f) EnterPatrol();
        }

        // ---- Persecución ----

        void EnterChase()
        {
            SetState(EnemyState.Chase);
            agent.speed = chaseSpeed;
            agent.isStopped = false;
            waiting = false;
            lookTimer = -1f;
            timeSinceSeen = 0f;
            agent.SetDestination(lastKnownPosition);

            if (audioSource != null && alertClip != null) audioSource.PlayOneShot(alertClip);
        }

        void UpdateChase(float dt)
        {
            if (canSeePlayer) timeSinceSeen = 0f;
            else timeSinceSeen += dt;

            // ¿Ya lo alcanzó? (solo cuenta si lo ve: no atrapa a través de una pared)
            Vector3 flat = player.position - transform.position;
            flat.y = 0f;
            if (canSeePlayer && flat.sqrMagnitude <= catchDistance * catchDistance)
            {
                Catch();
                return;
            }

            // Lo perdió de vista demasiado tiempo: va a investigar donde lo vio por última vez.
            if (timeSinceSeen > loseSightDelay) EnterInvestigate(lastKnownPosition);
        }

        void Catch()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            gm.PlayerCaught();

            if (gm.State == GameState.Lost)
            {
                if (agent.isOnNavMesh) agent.isStopped = true;
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(catchClip, 1f);
            }
        }

        // ======================= NAVEGACIÓN =======================

        void BuildPatrolPoints()
        {
            if (route == null || route.Count == 0)
            {
                patrolPoints = null;
                return;
            }

            patrolPoints = new Vector3[route.Count];
            for (int i = 0; i < patrolPoints.Length; i++)
                patrolPoints[i] = SnapToNavMesh(route.GetPoint(i).position);
        }

        int ClosestPatrolPointIndex()
        {
            int best = 0;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                float d = (patrolPoints[i] - transform.position).sqrMagnitude;
                if (d < bestSqr)
                {
                    bestSqr = d;
                    best = i;
                }
            }
            return best;
        }

        Vector3 RandomWanderPoint()
        {
            for (int attempt = 0; attempt < 6; attempt++)
            {
                Vector3 candidate = homePosition + Random.insideUnitSphere * wanderRadius;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(candidate, out hit, 4f, NavMesh.AllAreas)) return hit.position;
            }
            return homePosition;
        }

        Vector3 SnapToNavMesh(Vector3 position)
        {
            NavMeshHit hit;
            return NavMesh.SamplePosition(position, out hit, 3f, NavMesh.AllAreas) ? hit.position : position;
        }

        bool HasArrived()
        {
            if (agent.pathPending) return false;
            if (agent.pathStatus == NavMeshPathStatus.PathInvalid) return true; // no hay camino: pasa al siguiente

            if (agent.remainingDistance <= agent.stoppingDistance + 0.35f)
            {
                stuckTimer = 0f;
                return true;
            }

            // Atascado mucho rato sin moverse: damos el punto por alcanzado para no bloquearnos para siempre.
            if (agent.velocity.sqrMagnitude < 0.01f) stuckTimer += Time.deltaTime;
            else stuckTimer = 0f;

            if (stuckTimer > 2.5f)
            {
                stuckTimer = 0f;
                return true;
            }
            return false;
        }

        // ======================= FEEDBACK =======================

        void UpdateAnimator()
        {
            if (animator == null || !animatorHasSpeed) return;
            animator.SetFloat(SpeedHash, agent.velocity.magnitude, 0.1f, Time.deltaTime);
        }

        void UpdateAudio(float dt)
        {
            AudioManager audioManager = AudioManager.Instance;
            if (audioManager != null)
            {
                float distance = Vector3.Distance(transform.position, player.position);
                float tension = Mathf.Clamp01(1f - distance / Mathf.Max(1f, tensionDistance));
                if (CurrentState == EnemyState.Chase) tension = Mathf.Max(tension, 0.6f);
                audioManager.ReportTension(tension);
            }

            if (audioSource != null && growlClips != null && growlClips.Length > 0 && CurrentState != EnemyState.Chase)
            {
                growlTimer -= dt;
                if (growlTimer <= 0f)
                {
                    growlTimer = Random.Range(growlInterval.x, growlInterval.y);
                    AudioClip clip = growlClips[Random.Range(0, growlClips.Length)];
                    if (clip != null) audioSource.PlayOneShot(clip);
                }
            }
        }

        static bool HasParameter(Animator anim, string parameterName)
        {
            AnimatorControllerParameter[] parameters = anim.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == parameterName) return true;
            }
            return false;
        }

        // ======================= GIZMOS (para el video y para ajustar valores) =======================

        void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position;
            Vector3 eye = origin + Vector3.up * eyeHeight;

            // Amarillo: alcance de la vista y cono de visión
            Gizmos.color = new Color(1f, 0.9f, 0.1f, 0.9f);
            Gizmos.DrawWireSphere(origin, sightRange);
            Vector3 left = Quaternion.Euler(0f, -fieldOfView * 0.5f, 0f) * transform.forward;
            Vector3 right = Quaternion.Euler(0f, fieldOfView * 0.5f, 0f) * transform.forward;
            Gizmos.DrawLine(eye, eye + left * sightRange);
            Gizmos.DrawLine(eye, eye + right * sightRange);

            // Naranja: te detecta aunque estés a su espalda
            Gizmos.color = new Color(1f, 0.55f, 0f, 0.9f);
            Gizmos.DrawWireSphere(origin, closeDetectRange);

            // Celeste: oído caminando. Azul: oído corriendo
            Gizmos.color = new Color(0f, 0.9f, 1f, 0.9f);
            Gizmos.DrawWireSphere(origin, hearingRangeWalk);
            Gizmos.color = new Color(0.2f, 0.4f, 1f, 0.9f);
            Gizmos.DrawWireSphere(origin, hearingRangeSprint);

            // Rojo: distancia a la que te atrapa
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(origin, catchDistance);
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // Etiqueta con el estado actual sobre la cabeza del enemigo
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2.4f, CurrentState.ToString());

            // Magenta: última posición donde creyó verte
            if (CurrentState != EnemyState.Patrol)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position + Vector3.up, lastKnownPosition + Vector3.up);
                Gizmos.DrawWireSphere(lastKnownPosition, 0.4f);
            }
        }
#endif
    }
}
