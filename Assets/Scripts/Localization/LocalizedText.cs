using TMPro;
using UnityEngine;

namespace HeroDefense.Localization
{
    /// <summary>
    /// Вешается на надпись, текст которой не меняется по ходу игры:
    /// подписи кнопок, заголовки панелей, названия разделов.
    ///
    /// Надписи с числами (золото, номер волны) этим компонентом не
    /// закрываются — они собираются в коде и берут строку через
    /// <see cref="Loc.Get(string, object[])"/>.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [DisallowMultipleComponent]
    public sealed class LocalizedText : MonoBehaviour
    {
        [Tooltip("Ключ из таблицы переводов. Если ключа нет в таблице, " +
                 "надпись покажет сам ключ — так пропуск виден сразу.")]
        [SerializeField] private string key;

        private TMP_Text _text;

        /// <summary>Сменить ключ из кода. Текст обновится сразу.</summary>
        public void SetKey(string newKey)
        {
            key = newKey;
            Refresh();
        }

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            Loc.LanguageChanged += OnLanguageChanged;
            Refresh();
        }

        private void OnDisable()
        {
            Loc.LanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged(Language language)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_text == null)
                _text = GetComponent<TMP_Text>();

            if (_text != null && !string.IsNullOrEmpty(key))
                _text.text = Loc.Get(key);
        }
    }
}
