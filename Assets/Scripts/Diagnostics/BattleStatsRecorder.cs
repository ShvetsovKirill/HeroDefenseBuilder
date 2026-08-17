using UnityEngine;
using UnityEngine.InputSystem;
using HeroDefense.Base;
using HeroDefense.Core;
using HeroDefense.Economy;
using HeroDefense.Enemies;
using HeroDefense.Waves;

namespace HeroDefense.Diagnostics
{
    /// <summary>
    /// Связывает игру со сборщиком статистики и показывает отчёт.
    ///
    /// Отдельный компонент, а не вызовы по всему коду: замеры — это
    /// инструмент разработки, и его должно быть легко выключить одним
    /// объектом, не трогая геймплей.
    ///
    /// Клавиши:
    ///   F1 — показать/скрыть сводку
    ///   F2 — вывести сводку в консоль (оттуда удобно копировать)
    ///   F3 — сбросить накопленное
    /// </summary>
    public sealed class BattleStatsRecorder : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private WaveRunner waveRunner;

        [Header("Отображение")]
        [SerializeField] private bool showOnScreen;

        [Tooltip("Печатать сводку в консоль после каждой волны.")]
        [SerializeField] private bool logAfterEachWave = true;

        private Wallet Wallet => SceneContext.Current?.Wallet;
        private EnemyManager Enemies => SceneContext.Current?.EnemyManager;
        private TownHall Hall => SceneContext.Current?.TownHall;

        private int _goldBefore;

        private void Start()
        {
            int startingGold = Wallet != null ? Wallet.Gold : 0;

            BattleStats.Reset(startingGold);

            // Опорное значение берём сразу, а не с нуля: иначе первое же
            // поступление считалось приходом на весь стартовый кошелёк.
            _goldBefore = startingGold;

            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (waveRunner != null)
            {
                waveRunner.WaveStarted += OnWaveStarted;
                waveRunner.WaveCleared += OnWaveCleared;
            }

            if (Enemies != null)
            {
                Enemies.EnemyKilled += OnEnemyKilled;
                Enemies.StartedSiege += OnEnemyReachedBase;
            }

            if (Wallet != null)
                Wallet.GoldChanged += OnGoldChanged;

            if (Hall != null && Hall.Health != null)
                Hall.Health.Damaged += OnTownHallDamaged;
        }

        private void Unsubscribe()
        {
            if (waveRunner != null)
            {
                waveRunner.WaveStarted -= OnWaveStarted;
                waveRunner.WaveCleared -= OnWaveCleared;
            }

            if (Enemies != null)
            {
                Enemies.EnemyKilled -= OnEnemyKilled;
                Enemies.StartedSiege -= OnEnemyReachedBase;
            }

            if (Wallet != null)
                Wallet.GoldChanged -= OnGoldChanged;

            if (Hall != null && Hall.Health != null)
                Hall.Health.Damaged -= OnTownHallDamaged;
        }

        // ---------- События ----------

        private void OnWaveStarted(int waveNumber)
        {
            BattleStats.BeginWave(waveNumber);
        }

        private void OnWaveCleared(int waveNumber)
        {
            float health = Hall != null && Hall.Health != null ? Hall.Health.Current : 0f;

            BattleStats.EndWave(health);

            if (logAfterEachWave)
                Debug.Log($"[Замер] Волна {waveNumber} завершена.\n{BattleStats.BuildReport()}");
        }

        private void OnEnemyKilled(Enemy enemy)
        {
            DamageSource source = enemy != null
                ? enemy.LastDamageSource
                : DamageSource.Unknown;

            int reward = enemy != null ? enemy.GoldReward : 0;

            BattleStats.RegisterKill(source, reward);
        }

        /// <summary>
        /// Считаем прорыв, только если враг осадил именно ратушу.
        ///
        /// StartedSiege срабатывает на любую цель — постройку, бойца, —
        /// и без проверки число «дошло до базы» вырастало втрое против
        /// реального: 143 прорыва при ратуше без единой царапины.
        /// </summary>
        private void OnEnemyReachedBase(Enemy enemy)
        {
            if (enemy == null || Hall == null || Hall.Health == null)
                return;

            if (enemy.AttackTarget == Hall.Health)
                BattleStats.RegisterEnemyReachedBase();
        }

        /// <summary>
        /// Движение золота считаем по кошельку — он единственный источник
        /// правды. Перехватывать каждую покупку значило бы дописывать вызовы
        /// в постройку, пополнение отряда и всё остальное, что тратит золото.
        ///
        /// Приход тоже отсюда, а не из наград за убийства: пассивный доход
        /// ратуши и экономики (D45) идёт мимо наград, и «заработано» его
        /// не видело вовсе — остаток в отчёте не сходился с кошельком.
        /// </summary>
        private void OnGoldChanged(int gold)
        {
            int delta = gold - _goldBefore;

            if (delta > 0)
                BattleStats.RegisterGoldEarned(delta);
            else if (delta < 0)
                BattleStats.RegisterGoldSpent(-delta);

            _goldBefore = gold;
        }

        private void OnTownHallDamaged(float amount)
        {
            BattleStats.RegisterTownHallDamage(amount);
        }

        // ---------- Управление ----------

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
                return;

            if (keyboard.f1Key.wasPressedThisFrame)
                showOnScreen = !showOnScreen;

            if (keyboard.f2Key.wasPressedThisFrame)
                Debug.Log(BattleStats.BuildReport());

            if (keyboard.f3Key.wasPressedThisFrame)
            {
                int gold = Wallet != null ? Wallet.Gold : 0;

                BattleStats.Reset(gold);
                _goldBefore = gold;

                Debug.Log("[Замер] Статистика сброшена.");
            }
        }

        private void OnGUI()
        {
            if (!showOnScreen)
                return;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = Color.white },
                wordWrap = false
            };

            const int width = 620;
            const int height = 520;

            var area = new Rect(Screen.width - width - 20, 20, width, height);

            GUI.Box(area, GUIContent.none);
            GUI.Label(new Rect(area.x + 10, area.y + 10, width - 20, height - 20),
                BattleStats.BuildReport(), style);
        }
    }
}
