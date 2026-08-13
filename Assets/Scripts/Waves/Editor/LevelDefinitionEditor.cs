#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace HeroDefense.Waves.EditorTools
{
    /// <summary>
    /// Кнопка «Показать превью» в инспекторе карты.
    ///
    /// Цикл балансировки без неё: изменил ассет — запустил игру — досидел
    /// до седьмой волны — посмотрел. С ней: изменил — нажал — увидел.
    /// Секунды вместо минут.
    ///
    /// Лежит в #if UNITY_EDITOR — в билд не попадёт.
    /// </summary>
    [CustomEditor(typeof(LevelDefinition))]
    public sealed class LevelDefinitionEditor : Editor
    {
        private string _preview;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            if (GUILayout.Button("Показать превью волн", GUILayout.Height(30)))
                _preview = ((LevelDefinition)target).BuildPreview();

            if (string.IsNullOrEmpty(_preview))
                return;

            EditorGUILayout.Space();

            // Своего шрифта здесь намеренно нет: Font.CreateDynamicFontFromOSFont
            // создавал бы новый объект при каждой отрисовке инспектора,
            // а Unity чистит их при перезагрузке скриптов с предупреждениями.
            var style = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = false,
                richText = false
            };

            EditorGUILayout.TextArea(_preview, style);

            if (GUILayout.Button("Скопировать в буфер"))
                EditorGUIUtility.systemCopyBuffer = _preview;
        }
    }
}
#endif
