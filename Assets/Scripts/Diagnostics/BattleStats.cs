using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HeroDefense.Diagnostics
{
    /// <summary>
    /// Кто нанёс урон. Нужно, чтобы понять, работает ли эскалация:
    /// если к третьему акту больше половины убийств за королём,
    /// значит башни и отряды остались декорацией (D107).
    /// </summary>
    public enum DamageSource
    {
        King,
        Tower,
        Squad,
        Unknown
    }

    /// <summary>
    /// Сбор боевой статистики за забег.
    ///
    /// Зачем: на глаз баланс не читается. «Кажется, король слишком силён»
    /// и «король наносит 68% урона» — разные утверждения, и второе
    /// можно проверить.
    ///
    /// Статический сборщик без сцены: подписываться на него из десятка
    /// мест было бы дороже, чем просто позвать метод.
    /// </summary>
    public static class BattleStats
    {
        public sealed class WaveRecord
        {
            public int WaveNumber;
            public float Duration;

            public int EnemiesKilled;
            /// <summary>
            /// Дошли именно до РАТУШИ, а не до любой цели. Раньше считались
            /// все, кто начал что-то бить — постройку, бойца, — и число
            /// вырастало втрое против реального.
            /// </summary>
            public int EnemiesReachedBase;

            public int GoldEarned;
            public int GoldSpent;

            public int UnitsLost;
            public int BuildingsLost;

            public float TownHallDamage;
            public float TownHallHealthAfter;

            public readonly Dictionary<DamageSource, int> KillsBySource = new();
            public readonly Dictionary<DamageSource, float> DamageBySource = new();
        }

        private static readonly List<WaveRecord> Waves = new();
        private static WaveRecord _current;
        private static float _waveStartedAt;

        /// <summary>
        /// Золото на старте забега. Без него баланс уходил в минус:
        /// траты считались от нуля, хотя игрок начинал не с пустым кошельком.
        /// </summary>
        private static int _startingGold;

        /// <summary>Идёт ли запись. Ложь между волнами.</summary>
        public static bool IsRecording => _current != null;

        public static IReadOnlyList<WaveRecord> AllWaves => Waves;

        // ---------- Управление записью ----------

        public static void Reset(int startingGold = 0)
        {
            Waves.Clear();
            _current = null;
            _startingGold = startingGold;
        }

        public static void BeginWave(int waveNumber)
        {
            _current = new WaveRecord { WaveNumber = waveNumber };
            _waveStartedAt = Time.time;
        }

        public static void EndWave(float townHallHealth)
        {
            if (_current == null)
                return;

            _current.Duration = Time.time - _waveStartedAt;
            _current.TownHallHealthAfter = townHallHealth;

            Waves.Add(_current);
            _current = null;
        }

        // ---------- Регистрация событий ----------

        public static void RegisterKill(DamageSource source, int goldReward)
        {
            if (_current == null)
                return;

            _current.EnemiesKilled++;
            _current.GoldEarned += goldReward;

            _current.KillsBySource.TryGetValue(source, out int kills);
            _current.KillsBySource[source] = kills + 1;
        }

        public static void RegisterDamage(DamageSource source, float amount)
        {
            if (_current == null)
                return;

            _current.DamageBySource.TryGetValue(source, out float total);
            _current.DamageBySource[source] = total + amount;
        }

        public static void RegisterEnemyReachedBase() => Add(r => r.EnemiesReachedBase++);
        public static void RegisterUnitLost() => Add(r => r.UnitsLost++);
        public static void RegisterBuildingLost() => Add(r => r.BuildingsLost++);
        public static void RegisterGoldSpent(int amount) => Add(r => r.GoldSpent += amount);
        public static void RegisterTownHallDamage(float amount) => Add(r => r.TownHallDamage += amount);

        private static void Add(Action<WaveRecord> action)
        {
            if (_current != null)
                action(_current);
        }

        // ---------- Сводка ----------

        /// <summary>
        /// Таблица по волнам плюс итоги. Главное, на что смотреть,
        /// вынесено в конец отдельным блоком.
        /// </summary>
        public static string BuildReport()
        {
            if (Waves.Count == 0)
                return "Данных нет: ни одна волна не завершена.";

            var text = new StringBuilder();

            text.AppendLine("ВОЛНА | ВРЕМЯ | УБИТО | ДОШЛО | ЗОЛОТО +/- | ПОТЕРИ | РАТУША");
            text.AppendLine(new string('-', 70));

            foreach (WaveRecord wave in Waves)
            {
                text.AppendLine(
                    $"{wave.WaveNumber,5} | " +
                    $"{wave.Duration,5:F0}с | " +
                    $"{wave.EnemiesKilled,5} | " +
                    $"{wave.EnemiesReachedBase,5} | " +
                    $"{wave.GoldEarned,4}/{wave.GoldSpent,-4} | " +
                    $"{wave.UnitsLost,2}б {wave.BuildingsLost,1}з | " +
                    $"{wave.TownHallHealthAfter,5:F0}");
            }

            AppendTotals(text);

            return text.ToString();
        }

        private static void AppendTotals(StringBuilder text)
        {
            int killsTotal = 0;
            int reached = 0;
            int unitsLost = 0;
            int goldEarned = 0;
            int goldSpent = 0;

            var killsBySource = new Dictionary<DamageSource, int>();

            foreach (WaveRecord wave in Waves)
            {
                killsTotal += wave.EnemiesKilled;
                reached += wave.EnemiesReachedBase;
                unitsLost += wave.UnitsLost;
                goldEarned += wave.GoldEarned;
                goldSpent += wave.GoldSpent;

                foreach (KeyValuePair<DamageSource, int> pair in wave.KillsBySource)
                {
                    killsBySource.TryGetValue(pair.Key, out int current);
                    killsBySource[pair.Key] = current + pair.Value;
                }
            }

            text.AppendLine();
            text.AppendLine($"Волн пройдено: {Waves.Count}");
            text.AppendLine($"Убито врагов: {killsTotal}, прорвались к ратуше: {reached}");
            float townHallDamage = 0f;

            foreach (WaveRecord wave in Waves)
                townHallDamage += wave.TownHallDamage;

            text.AppendLine($"Потеряно бойцов: {unitsLost}");
            text.AppendLine($"Урон по ратуше за забег: {townHallDamage:F0}");
            text.AppendLine(
                $"Золото: старт {_startingGold}, заработано {goldEarned}, " +
                $"потрачено {goldSpent}, остаток {_startingGold + goldEarned - goldSpent}");
            text.AppendLine();

            text.AppendLine("КТО УБИВАЕТ:");

            foreach (KeyValuePair<DamageSource, int> pair in killsBySource)
            {
                float percent = killsTotal > 0 ? pair.Value * 100f / killsTotal : 0f;

                text.AppendLine($"  {Localize(pair.Key),-8} {pair.Value,4} ({percent,5:F1}%)");
            }

            AppendVerdict(text, killsBySource, killsTotal);
        }

        /// <summary>
        /// Автоматические выводы по ключевым порогам из плана.
        /// Смысл не в том, чтобы решить за разработчика, а в том,
        /// чтобы не пропустить очевидное в столбцах цифр.
        /// </summary>
        private static void AppendVerdict(
            StringBuilder text, Dictionary<DamageSource, int> kills, int total)
        {
            if (total == 0)
                return;

            text.AppendLine();
            text.AppendLine("НА ЧТО ОБРАТИТЬ ВНИМАНИЕ:");

            kills.TryGetValue(DamageSource.King, out int kingKills);
            kills.TryGetValue(DamageSource.Squad, out int squadKills);
            kills.TryGetValue(DamageSource.Tower, out int towerKills);

            float kingShare = kingKills * 100f / total;

            if (kingShare > 50f)
                text.AppendLine($"  ⚠ Король убивает {kingShare:F0}% — эскалация не работает (D107).");
            else if (kingShare < 15f)
                text.AppendLine($"  ⚠ Король убивает всего {kingShare:F0}% — он перестал быть нужен.");
            else
                text.AppendLine($"  ✓ Доля короля {kingShare:F0}% — в норме.");

            if (squadKills == 0)
                text.AppendLine("  ⚠ Отряды не убили никого. Роль затычки не работает.");

            if (towerKills == 0)
                text.AppendLine("  ⚠ Башни не убили никого. Либо не построены, либо стоят не там.");

            // Экономика: если остаток огромный, деньги некуда девать.
            int goldEarned = 0;
            int goldSpent = 0;

            foreach (WaveRecord wave in Waves)
            {
                goldEarned += wave.GoldEarned;
                goldSpent += wave.GoldSpent;
            }

            int leftover = _startingGold + goldEarned - goldSpent;

            if (goldEarned > 0 && leftover > goldEarned * 0.5f)
                text.AppendLine($"  ⚠ Не потрачено {leftover} золота — строить нечего или некуда.");
        }

        private static string Localize(DamageSource source)
        {
            return source switch
            {
                DamageSource.King => "Король",
                DamageSource.Tower => "Башни",
                DamageSource.Squad => "Отряды",
                _ => "Прочее"
            };
        }
    }
}
