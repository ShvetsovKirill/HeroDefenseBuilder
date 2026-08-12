using UnityEngine;
using HeroDefense.Base;
using HeroDefense.Core;
using HeroDefense.Enemies;

namespace HeroDefense.Debugging
{
    /// <summary>
    /// ВРЕМЕННЫЙ отладочный HUD на OnGUI.
    /// Заменится нормальным uGUI на этапе UI — сейчас важно только видеть цифры,
    /// а не тратить время на интерфейс, который ещё десять раз изменится.
    /// </summary>
    public sealed class DebugGameHud : MonoBehaviour
    {
        [SerializeField] private TownHall townHall;
        [SerializeField] private EnemyManager enemyManager;
        [SerializeField] private GameLoop gameLoop;

        private const int Padding = 10;
        private const int LineHeight = 26;

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
            GUI.Box(new Rect(Padding, Padding, 300, LineHeight * 2 + Padding), GUIContent.none);

            string healthText = townHall != null
                ? $"Ратуша: {townHall.CurrentHealth:F0} / {townHall.MaxHealth:F0}"
                : "Ратуша: —";

            string enemyText = enemyManager != null
                ? $"Врагов: {enemyManager.AliveCount}"
                : "Врагов: —";

            GUI.Label(new Rect(Padding * 2, Padding, 290, LineHeight), healthText, style);
            GUI.Label(new Rect(Padding * 2, Padding + LineHeight, 290, LineHeight), enemyText, style);
        }

        private void DrawGameOver()
        {
            GUIStyle big = CreateStyle(48, Color.red);
            GUIStyle hint = CreateStyle(24, Color.white);

            var area = new Rect(0f, Screen.height * 0.4f, Screen.width, 120f);

            big.alignment = TextAnchor.MiddleCenter;
            hint.alignment = TextAnchor.MiddleCenter;

            GUI.Label(area, "РАТУША РАЗРУШЕНА", big);
            GUI.Label(new Rect(0f, Screen.height * 0.5f, Screen.width, 60f),
                "Enter — начать заново", hint);
        }
    }
}
