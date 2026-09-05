using System;
using UnityEngine;

namespace HeroDefense.Audio
{
    /// <summary>
    /// Громкости, выбранные игроком.
    ///
    /// Статический, как <see cref="Localization.Loc"/>: настройка живёт
    /// в меню, а слушают её все сцены, то есть она обязана переживать
    /// их смену.
    ///
    /// Три канала, а не один: музыку выключают гораздо чаще, чем эффекты, —
    /// человек хочет слышать, что происходит на поле, но не хочет фона.
    /// </summary>
    public static class AudioOptions
    {
        private const string MasterKey = "audio.master";
        private const string MusicKey = "audio.music";
        private const string SfxKey = "audio.sfx";

        private static float _master = 1f;
        private static float _music = 1f;
        private static float _sfx = 1f;
        private static bool _loaded;

        /// <summary>Любая громкость изменилась. Проигрыватели пересчитывают себя.</summary>
        public static event Action Changed;

        /// <summary>Общая громкость, 0..1.</summary>
        public static float Master
        {
            get { EnsureLoaded(); return _master; }
            set => Set(MasterKey, ref _master, value);
        }

        /// <summary>Громкость музыки, 0..1. Умножается на общую.</summary>
        public static float Music
        {
            get { EnsureLoaded(); return _music; }
            set => Set(MusicKey, ref _music, value);
        }

        /// <summary>Громкость эффектов, 0..1. Умножается на общую.</summary>
        public static float Sfx
        {
            get { EnsureLoaded(); return _sfx; }
            set => Set(SfxKey, ref _sfx, value);
        }

        /// <summary>Итоговая громкость эффектов с учётом общей.</summary>
        public static float EffectiveSfx => Master * Sfx;

        /// <summary>Итоговая громкость музыки с учётом общей.</summary>
        public static float EffectiveMusic => Master * Music;

        private static void Set(string key, ref float field, float value)
        {
            EnsureLoaded();

            float clamped = Mathf.Clamp01(value);

            // Ползунок шлёт значение каждый кадр перетаскивания.
            // Без этой проверки PlayerPrefs писались бы шестьдесят раз в секунду.
            if (Mathf.Approximately(field, clamped))
                return;

            field = clamped;

            PlayerPrefs.SetFloat(key, clamped);
            PlayerPrefs.Save();

            Changed?.Invoke();
        }

        private static void EnsureLoaded()
        {
            if (_loaded)
                return;

            _loaded = true;

            _master = PlayerPrefs.GetFloat(MasterKey, 1f);
            _music = PlayerPrefs.GetFloat(MusicKey, 1f);
            _sfx = PlayerPrefs.GetFloat(SfxKey, 1f);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Сброс статики при выходе из Play Mode: без него отключённая
        /// перезагрузка домена сохранила бы подписки прошлого запуска.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _loaded = false;
            Changed = null;
        }
#endif
    }
}
