using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HeroDefense.Benchmark
{
    /// <summary>
    /// Эпик 0.1, шаг 1: базовый замер.
    ///
    /// Спавнит N агентов по кругу, гонит их к центру ОДНИМ центральным циклом
    /// (без MonoBehaviour на каждом агенте). Это самый дешёвый по CPU вариант —
    /// значит, полученный FPS отражает почти чистую стоимость РЕНДЕРА.
    ///
    /// Это осознанно оптимистичный baseline: реальная игра добавит поиск цели,
    /// урон, анимацию. Но сначала надо знать потолок сверху.
    ///
    /// Клавиши: 1/2/3/4 — 100/250/500/1000 агентов. R — рестарт замера.
    /// </summary>
    public sealed class CrowdBenchmark : MonoBehaviour
    {
        [Header("Агенты")]
        [Tooltip("Префаб агента. Для baseline — примитив без Animator и без коллайдера.")]
        [SerializeField] private GameObject agentPrefab;

        [SerializeField] private int startCount = 100;

        [Header("Поле")]
        [Tooltip("Радиус кольца, с которого агенты стартуют.")]
        [SerializeField] private float spawnRadius = 40f;

        [Tooltip("Куда все идут. Пусто = мировой ноль.")]
        [SerializeField] private Transform target;

        [Header("Движение")]
        [SerializeField] private float moveSpeed = 3f;

        [Tooltip("Разброс скорости, чтобы толпа не двигалась как одно целое.")]
        [SerializeField] private float speedVariance = 0.3f;

        [Tooltip("Дистанция до цели, на которой агент респавнится обратно на кольцо.")]
        [SerializeField] private float reachDistance = 2f;

        [Header("Ссылки")]
        [SerializeField] private PerfHud perfHud;

        private readonly List<Transform> _agents = new();
        private readonly List<float> _speeds = new();

        private static readonly int[] TestCounts = { 100, 250, 500, 1000 };

        private void Start()
        {
            SetAgentCount(startCount);
        }

        private void Update()
        {
            HandleHotkeys();
            MoveAgents();
        }

        // ---------- Управление замером ----------

        private void HandleHotkeys()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null)
                return;

            if (kb.digit1Key.wasPressedThisFrame) SetAgentCount(TestCounts[0]);
            if (kb.digit2Key.wasPressedThisFrame) SetAgentCount(TestCounts[1]);
            if (kb.digit3Key.wasPressedThisFrame) SetAgentCount(TestCounts[2]);
            if (kb.digit4Key.wasPressedThisFrame) SetAgentCount(TestCounts[3]);

            if (kb.rKey.wasPressedThisFrame && perfHud != null)
                perfHud.ResetWorstFrame();
        }

        private void SetAgentCount(int count)
        {
            while (_agents.Count > count)
                RemoveLastAgent();

            while (_agents.Count < count)
                AddAgent();

            if (perfHud != null)
            {
                perfHud.AgentCount = _agents.Count;
                perfHud.ResetWorstFrame();
            }
        }

        private void AddAgent()
        {
            if (agentPrefab == null)
            {
                Debug.LogError("[Benchmark] Не назначен agentPrefab.", this);
                enabled = false;
                return;
            }

            GameObject go = Instantiate(agentPrefab, RandomSpawnPosition(), Quaternion.identity, transform);

            _agents.Add(go.transform);
            _speeds.Add(moveSpeed * Random.Range(1f - speedVariance, 1f + speedVariance));
        }

        private void RemoveLastAgent()
        {
            int last = _agents.Count - 1;

            Destroy(_agents[last].gameObject);
            _agents.RemoveAt(last);
            _speeds.RemoveAt(last);
        }

        // ---------- Движение ----------

        /// <summary>
        /// Один цикл на всю толпу. Никаких Update'ов на агентах,
        /// никаких NavMeshAgent, никакого поиска пути — только вектор к цели.
        /// </summary>
        private void MoveAgents()
        {
            Vector3 goal = TargetPosition;
            float deltaTime = Time.deltaTime;

            for (int i = 0; i < _agents.Count; i++)
            {
                Transform agent = _agents[i];
                Vector3 toGoal = goal - agent.position;
                toGoal.y = 0f;

                if (toGoal.sqrMagnitude < reachDistance * reachDistance)
                {
                    agent.position = RandomSpawnPosition();
                    continue;
                }

                Vector3 direction = toGoal.normalized;

                agent.position += direction * (_speeds[i] * deltaTime);
                agent.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }

        // ---------- Вспомогательное ----------

        private Vector3 TargetPosition => target != null ? target.position : Vector3.zero;

        private Vector3 RandomSpawnPosition()
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(spawnRadius * 0.8f, spawnRadius);

            return TargetPosition + new Vector3(
                Mathf.Cos(angle) * distance,
                0f,
                Mathf.Sin(angle) * distance);
        }
    }
}
