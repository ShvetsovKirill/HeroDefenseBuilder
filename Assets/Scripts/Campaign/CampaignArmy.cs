using UnityEngine;
using HeroDefense.Combat;
using HeroDefense.Core;
using HeroDefense.Squads;

namespace HeroDefense.Campaign
{
    /// <summary>
    /// Раздаёт казармам отряды из армии похода.
    ///
    /// Смысл в одном: **казарма — это место для отряда, а не фабрика
    /// безымянных бойцов.** Построил казарму — вышел твой отряд, тот
    /// самый, что уцелел в прошлом владении, с тем же командиром
    /// и специализацией.
    ///
    /// Отряды раздаются по порядку слотов: первая построенная казарма
    /// получает первый отряд. Выбирать, кого куда, игрок будет тогда,
    /// когда специализации начнут отличаться в бою — сейчас разница
    /// только в названии, и спрашивать не о чем.
    ///
    /// Вне кампании класс молчит: боевую сцену запускают напрямую
    /// из редактора, и она должна работать без похода.
    /// </summary>
    public static class CampaignArmy
    {
        /// <summary>Во сколько раз командир живучее рядового бойца.</summary>
        private const float CommanderHealthFactor = 2.5f;

        /// <summary>Во сколько раз он сильнее бьёт.</summary>
        private const float CommanderDamageFactor = 1.5f;

        /// <summary>Насколько он крупнее. Заметно, но не карикатурно.</summary>
        private const float CommanderScale = 1.15f;

        /// <summary>Сколько отрядов уже выдано в этом бою.</summary>
        private static int _handedOut;

        private static Material _crownMaterial;

        /// <summary>Начать бой заново: следующая казарма снова получит первый отряд.</summary>
        public static void ResetForBattle() => _handedOut = 0;

        /// <summary>
        /// Взять следующий отряд из армии. Null означает «отрядов больше
        /// нет» — казарма тогда работает по-старому, как источник ополчения.
        /// </summary>
        public static SquadRecord TakeNext()
        {
            if (!CampaignRun.IsActive)
                return null;

            CampaignState state = CampaignRun.State;
            SquadRecord next = null;

            // Ищем наименьший слот из ещё не выданных, а не первую подходящую
            // запись по порядку списка: новый отряд встаёт в конец, но занимает
            // первый свободный слот, и после потери среднего порядок в списке
            // перестаёт совпадать с порядком слотов. Перебор подряд тогда
            // проскакивал бы отряд, и часть армии не выходила бы в бой вовсе.
            foreach (SquadRecord squad in state.squads)
            {
                if (squad == null || squad.wipedOut)
                    continue;

                if (squad.slot <= _handedOut)
                    continue;

                if (next == null || squad.slot < next.slot)
                    next = squad;
            }

            if (next != null)
                _handedOut = next.slot;

            return next;
        }

        /// <summary>
        /// Применить к бойцу то, что даёт его командир.
        ///
        /// Бонусы кладутся поверх прокачки, а не вместо неё: улучшения
        /// из лагеря общие для всей армии, а командир — надбавка именно
        /// этому отряду.
        /// </summary>
        public static void ApplyCommander(SquadUnit unit, SquadRecord record)
        {
            if (unit == null || record == null || !record.HasCommander)
                return;

            CampaignRules rules = CampaignRules.Current;
            CommanderDefinition commander = rules != null ? rules.FindCommander(record.commanderId) : null;

            if (commander == null)
                return;

            var health = unit.GetComponent<Health>();

            if (health != null)
                health.SetMaxHealth(health.Max + commander.healthBonus, true);

            var attacker = unit.GetComponent<AutoAttacker>();

            if (attacker != null)
                attacker.AddDamage(commander.damageBonus);
        }

        /// <summary>
        /// Сделать бойца самим командиром.
        ///
        /// Командир идёт **сверх** штата отряда и заметно живучее своих:
        /// он не должен погибать первым в случайной стычке, иначе отряд
        /// теряет имя из-за ерунды, а не из-за проигранного боя.
        ///
        /// Отличается и внешне. Пока это простой венец над головой:
        /// настоящий шлем появится с моделью, но игрок обязан видеть,
        /// кого именно бьют, уже сейчас.
        /// </summary>
        public static void MakeCommanderUnit(SquadUnit unit, SquadRecord record)
        {
            if (unit == null || record == null || !record.HasCommander)
                return;

            var health = unit.GetComponent<Health>();

            if (health != null)
                health.SetMaxHealth(health.Max * CommanderHealthFactor, true);

            var attacker = unit.GetComponent<AutoAttacker>();

            if (attacker != null)
                attacker.AddDamage(attacker.Damage * (CommanderDamageFactor - 1f));

            unit.transform.localScale *= CommanderScale;

            AddCrown(unit.transform);

            // Метка следит за его смертью: погибший командир не возвращается
            // ни в этом бою, ни в следующем владении.
            unit.gameObject.AddComponent<CommanderMark>().Bind(record);
        }

        /// <summary>
        /// Венец над головой — метка «это командир».
        ///
        /// Примитив, а не префаб: до появления моделей ставить нечего,
        /// а различать бойцов надо уже сегодня. Коллайдер снимается —
        /// венец ни во что не должен упираться.
        /// </summary>
        private static void AddCrown(Transform unit)
        {
            float top = 2f;

            foreach (Renderer renderer in unit.GetComponentsInChildren<Renderer>())
                top = Mathf.Max(top, renderer.bounds.max.y - unit.position.y);

            var crown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            crown.name = "CommanderCrown";
            crown.transform.SetParent(unit, false);
            crown.transform.localPosition = new Vector3(0f, top + 0.15f, 0f);
            crown.transform.localScale = new Vector3(0.32f, 0.09f, 0.32f);

            Object.Destroy(crown.GetComponent<Collider>());

            crown.GetComponent<Renderer>().sharedMaterial = CrownMaterial;
        }

        /// <summary>Золото венца. Материал один на всех: командиров будут десятки.</summary>
        private static Material CrownMaterial
        {
            get
            {
                if (_crownMaterial != null)
                    return _crownMaterial;

                _crownMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    name = "CommanderCrown"
                };

                _crownMaterial.SetColor("_BaseColor", new Color(0.79f, 0.64f, 0.15f));
                _crownMaterial.SetFloat("_Smoothness", 0.3f);

                return _crownMaterial;
            }
        }

        /// <summary>Имя отряда для сцены и отладки: «Копейщики сэра Родерика».</summary>
        public static string DescribeSquad(SquadRecord record)
        {
            if (record == null)
                return "Squad";

            CampaignRules rules = CampaignRules.Current;
            CommanderDefinition commander = rules != null ? rules.FindCommander(record.commanderId) : null;

            return commander != null
                ? $"Squad_{record.specialization}_{commander.commanderName}"
                : $"Squad_{record.slot}_Militia";
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _handedOut = 0;
            _crownMaterial = null;
        }
#endif
    }
}
