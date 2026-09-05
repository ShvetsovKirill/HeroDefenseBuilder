using System;
using UnityEngine;

namespace HeroDefense.Audio
{
    /// <summary>
    /// Список звуков игры: какой файл играет на каком событии.
    ///
    /// ScriptableObject, а не ссылки в компонентах: звук — контент (D48),
    /// и подбирать его будет не программист. Заменить удар меча на другой
    /// файл нужно в одном месте, а не в пяти префабах.
    ///
    /// Ассет обязан лежать в Resources и называться так, как ждёт
    /// <see cref="AudioService"/> — иначе игра будет молчать без единой ошибки.
    /// </summary>
    [CreateAssetMenu(menuName = "HeroDefense/Каталог звуков", fileName = "AudioCatalog")]
    public sealed class AudioCatalog : ScriptableObject
    {
        /// <summary>Один звук: событие и что на нём играет.</summary>
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("На каком событии играет.")]
            public SoundId id = SoundId.None;

            [Tooltip("Варианты файла. Больше одного — движок выберет случайный, " +
                     "и повторяющийся удар перестаёт резать слух. Пусто — тишина.")]
            public AudioClip[] clips = Array.Empty<AudioClip>();

            [Range(0f, 1f)]
            [Tooltip("Громкость этого звука относительно остальных. " +
                     "Крутится здесь, а не в файле: перезаписывать wav ради " +
                     "минус трёх децибел неразумно.")]
            public float volume = 1f;

            [Range(0f, 0.5f)]
            [Tooltip("Разброс высоты тона. 0.1 означает ±10%. " +
                     "Без разброса десять одинаковых ударов звучат как машина.")]
            public float pitchSpread = 0.08f;

            [Min(0f)]
            [Tooltip("Сколько секунд этот звук не повторяется.\n\n" +
                     "Главная ручка против каши: сорок врагов умирают в одну " +
                     "секунду, и без ограничения сорок копий одного файла " +
                     "складываются в треск. 0.05–0.1 — разумно для частых звуков.")]
            public float minInterval = 0.05f;

            [Tooltip("Пространственный ли звук. Включено — слышен из точки события " +
                     "и тише издалека. Для интерфейса и волн выключено: " +
                     "они происходят «нигде».")]
            public bool spatial = true;
        }

        [Tooltip("Все звуки игры. Одно событие — одна строка; " +
                 "дубликат сообщит о себе предупреждением.")]
        public Entry[] entries = Array.Empty<Entry>();

        [Header("Музыка")]
        [Tooltip("Трек главного меню и замка.")]
        public AudioClip menuMusic;

        [Tooltip("Трек боя.")]
        public AudioClip battleMusic;

        [Range(0f, 1f)]
        [Tooltip("Громкость музыки относительно эффектов. Обычно заметно тише: " +
                 "музыка играет непрерывно, а эффекты — вспышками.")]
        public float musicVolume = 0.5f;

        [Min(0f)]
        [Tooltip("Секунды перехода между треками. Резкая смена слышна как обрыв.")]
        public float musicFadeTime = 1.5f;

        /// <summary>Найти запись по событию. Возвращает null, если звук не заведён.</summary>
        public Entry Find(SoundId id)
        {
            if (id == SoundId.None)
                return null;

            foreach (Entry entry in entries)
            {
                if (entry != null && entry.id == id)
                    return entry;
            }

            return null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Дубликат события означает, что вторая строка никогда не сыграет,
        /// а какая именно — зависит от порядка в массиве.
        /// </summary>
        private void OnValidate()
        {
            var seen = new System.Collections.Generic.HashSet<SoundId>();

            foreach (Entry entry in entries)
            {
                if (entry == null || entry.id == SoundId.None)
                    continue;

                if (!seen.Add(entry.id))
                    Debug.LogWarning($"[Звук] Событие «{entry.id}» заведено дважды.", this);
            }
        }
#endif
    }
}
