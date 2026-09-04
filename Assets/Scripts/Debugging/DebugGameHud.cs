using UnityEngine;
using HeroDefense.Base;
using HeroDefense.Core;
using HeroDefense.Economy;
using HeroDefense.Enemies;

namespace HeroDefense.Debugging
{
    /// <summary>
    /// ВРЕМЕННЫЙ отладочный HUD на OnGUI.
    /// Заменится нормальным uGUI на этапе UI — сейчас важно видеть цифры,
    /// а не тратить время на интерфейс, который ещё десять раз изменится.
    /// </summary>
    public sealed class DebugGameHud : MonoBehaviour
    {
        [SerializeField] private TownHall townHall;
        [SerializeField] private EnemyManager enemyManager;
        [SerializeField] private GameLoop gameLoop;
        [SerializeField] private Wallet wallet;
        [SerializeField] private HeroDefense.Waves.WaveRunner waveRunner;

        private const int Padding = 10;
        private const int LineHeight = 26;

        private Health TownHallHealth => townHall != null ? townHall.Health : null;

        private void OnGUI()
        {
            GUIStyle style = CreateStyle(20, Color.white);

            DrawStats(style);

            if (gameLoop != null && gameLoop.IsGameOver)
                DrawGameOver();
        }

        private static GUIStyle CreateStyle(int fontSize, Color color)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                normal = { textColor = color }
            };
        }

        private void DrawStats(GUIStyle style)
        {
            GUI.Box(new Rect(Padding, Padding, 320, LineHeight * 4 + Padding), GUIContent.none);

            Health health = TownHallHealth;

            string healthText = health != null
                ? $"Ратуша: {health.Current:F0} / {health.Max:F0}"
                : "Ратуша: —";

            string enemyText = enemyManager != null
                ? $"Врагов: {enemyManager.AliveCount}"
                : "Врагов: —";

            string goldText = wallet != null
                ? $"Золото: {wallet.Gold}"
                : "Золото: —";

            GUI.Label(new Rect(Padding * 2, Padding, 310, LineHeight), healthText, style);
            GUI.Label(new Rect(Padding * 2, Padding + LineHeight, 310, LineHeight), enemyText, style);
            GUI.Label(new Rect(Padding * 2, Padding + LineHeight * 2, 310, LineHeight), goldText, style);
            GUI.Label(new Rect(Padding * 2, Padding + LineHeight * 3, 310, LineHeight), BuildWaveText(), style);
        }

        private string BuildWaveText()
        {
            if (waveRunner == null)
                return "Волна: —";

            if (waveRunner.IsBreak)
                return $"Волна {waveRunner.CurrentWaveNumber} отбита. Следующая через {waveRunner.BreakTimeLeft:F0}";

            return $"Волна: {waveRunner.CurrentWaveNumber} / {waveRunner.TotalWaves}";
        }

        private void DrawGameOver()
        {
            GUIStyle big = CreateStyle(48, Color.red);
            GUIStyle hint = CreateStyle(24, Color.white);

            big.alignment = TextAnchor.MiddleCenter;
            hint.alignment = TextAnchor.MiddleCenter;

            GUI.Label(new Rect(0f, Screen.height * 0.4f, Screen.width, 120f), "РАТУША РАЗРУШЕНА", big);
            GUI.Label(new Rect(0f, Screen.height * 0.5f, Screen.width, 60f), "Enter — начать заново", hint);
        }
    }
}
