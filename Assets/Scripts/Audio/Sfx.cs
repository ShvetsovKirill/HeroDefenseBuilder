using UnityEngine;

namespace HeroDefense.Audio
{
    /// <summary>
    /// Короткий доступ к звуку из любого места.
    ///
    /// Зачем прослойка над <see cref="AudioService"/>: вызов должен быть
    /// одной строкой без проверок на null, иначе игровой код обрастёт
    /// защитой от отсутствующего проигрывателя. Здесь эта проверка одна
    /// на всю игру.
    /// </summary>
    public static class Sfx
    {
        /// <summary>Сыграть звук в точке события: удар, смерть, постройка.</summary>
        public static void PlayAt(SoundId id, Vector3 position)
        {
            AudioService service = AudioService.Instance;

            if (service != null)
                service.Play(id, position);
        }

        /// <summary>
        /// Сыграть звук без места: интерфейс, начало волны, итог забега.
        /// Такие события происходят «нигде» и слышны одинаково громко.
        /// </summary>
        public static void Play(SoundId id)
        {
            AudioService service = AudioService.Instance;

            if (service != null)
                service.Play(id);
        }
    }
}
