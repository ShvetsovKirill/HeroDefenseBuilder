using System.Collections.Generic;
using UnityEngine;

namespace HeroDefense.Audio
{
    /// <summary>
    /// Проигрыватель звуков и музыки. Один на всю игру.
    ///
    /// Создаёт себя сам при первом обращении и переживает смену сцен.
    /// Так сделано намеренно: ставить объект со звуком в каждую из четырёх
    /// сцен — значит рано или поздно забыть в одной и получить тишину,
    /// которую не отличить от отсутствия файла. Тот же приём, что
    /// у <see cref="Localization.Loc"/>.
    ///
    /// Источники звука лежат в пуле: каждый эффект — это не новый
    /// объект, иначе на плотной волне сборщик мусора даст рывок.
    /// </summary>
    [DefaultExecutionOrder(-800)]
    public sealed class AudioService : MonoBehaviour
    {
        /// <summary>Имя ассета каталога внутри Resources. Без расширения.</summary>
        private const string CatalogPath = "AudioCatalog";

        /// <summary>
        /// Сколько эффектов может звучать одновременно.
        ///
        /// Больше — не громче, а грязнее: перекрывающиеся копии одного
        /// удара складываются в треск. Лишние вызовы отбрасываются,
        /// и это правильное поведение, а не потеря.
        /// </summary>
        private const int VoiceCount = 16;

        private static AudioService _instance;
        private static bool _catalogMissingReported;
        private static bool _quitting;

        private readonly Dictionary<SoundId, float> _lastPlayed = new();

        private AudioCatalog _catalog;
        private AudioSource[] _voices;
        private int _nextVoice;

        private AudioSource _musicSource;
        private AudioSource _musicFadeOut;
        private AudioClip _currentMusic;
        private float _musicFade;

        /// <summary>Сервис, создающий себя при первом обращении. Null только если игра выключается.</summary>
        public static AudioService Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                // Приложение закрывается — создавать объект уже нельзя:
                // Unity выбросит предупреждение о создании объекта в OnDestroy.
                if (_quitting || !Application.isPlaying)
                    return null;

                var host = new GameObject("AudioService");

                _instance = host.AddComponent<AudioService>();
                DontDestroyOnLoad(host);

                return _instance;
            }
        }

        /// <summary>Каталог звуков. Может быть null — тогда игра просто молчит.</summary>
        public AudioCatalog Catalog => _catalog;

        // ---------- Эффекты ----------

        /// <summary>
        /// Сыграть звук. Позиция нужна только пространственным звукам —
        /// у интерфейса и волн её нет.
        /// </summary>
        public void Play(SoundId id, Vector3 position)
        {
            AudioCatalog.Entry entry = _catalog != null ? _catalog.Find(id) : null;

            if (entry == null || entry.clips.Length == 0)
                return;

            if (!AllowedNow(id, entry))
                return;

            AudioClip clip = entry.clips[Random.Range(0, entry.clips.Length)];

            if (clip == null)
                return;

            AudioSource voice = TakeVoice();

            voice.transform.position = position;
            voice.clip = clip;
            voice.volume = entry.volume * AudioOptions.EffectiveSfx;
            voice.pitch = 1f + Random.Range(-entry.pitchSpread, entry.pitchSpread);
            voice.spatialBlend = entry.spatial ? 1f : 0f;
            voice.Play();
        }

        /// <summary>Сыграть звук без привязки к точке: интерфейс, волны, итог забега.</summary>
        public void Play(SoundId id) => Play(id, Vector3.zero);

        /// <summary>
        /// Не слишком ли часто. Считаем по неотмасштабированному времени:
        /// на паузе и в замедлении звук должен вести себя одинаково.
        /// </summary>
        private bool AllowedNow(SoundId id, AudioCatalog.Entry entry)
        {
            if (entry.minInterval <= 0f)
                return true;

            float now = Time.unscaledTime;

            if (_lastPlayed.TryGetValue(id, out float last) && now - last < entry.minInterval)
                return false;

            _lastPlayed[id] = now;

            return true;
        }

        /// <summary>
        /// Взять источник из пула по кругу. Самый старый звук обрывается —
        /// на слух это незаметнее, чем пропуск нового.
        /// </summary>
        private AudioSource TakeVoice()
        {
            AudioSource voice = _voices[_nextVoice];

            _nextVoice = (_nextVoice + 1) % _voices.Length;

            return voice;
        }

        // ---------- Музыка ----------

        /// <summary>
        /// Включить трек. Тот же самый трек повторно не перезапускается:
        /// иначе музыка обрывалась бы на каждом возврате в замок.
        /// </summary>
        public void PlayMusic(AudioClip clip)
        {
            if (_currentMusic == clip)
                return;

            _currentMusic = clip;

            // Прошлый трек уводим в затухание, новый поднимаем с нуля.
            (_musicSource, _musicFadeOut) = (_musicFadeOut, _musicSource);

            _musicSource.clip = clip;
            _musicSource.volume = 0f;
            _musicSource.loop = true;

            if (clip != null)
                _musicSource.Play();

            _musicFade = 0f;
        }

        /// <summary>Заглушить музыку с тем же переходом, что и смена трека.</summary>
        public void StopMusic() => PlayMusic(null);

        // ---------- Жизненный цикл ----------

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _quitting = false;

            LoadCatalog();
            CreateVoices();
            CreateMusicSources();

            AudioOptions.Changed += OnVolumeChanged;
        }

        private void OnApplicationQuit()
        {
            _quitting = true;
        }

        private void OnDestroy()
        {
            AudioOptions.Changed -= OnVolumeChanged;

            if (_instance == this)
                _instance = null;
        }

        private void Update()
        {
            UpdateMusicFade();
        }

        private void LoadCatalog()
        {
            _catalog = Resources.Load<AudioCatalog>(CatalogPath);

            if (_catalog != null || _catalogMissingReported)
                return;

            // Ровно один раз за запуск: иначе каждый удар писал бы строку в лог.
            _catalogMissingReported = true;

            Debug.LogWarning($"[Звук] Не найден ассет Resources/{CatalogPath}. " +
                             "Игра будет молчать. Создай каталог через меню " +
                             "HeroDefense и положи его в папку Resources.");
        }

        private void CreateVoices()
        {
            _voices = new AudioSource[VoiceCount];

            for (int i = 0; i < VoiceCount; i++)
            {
                var host = new GameObject($"Voice_{i}");

                host.transform.SetParent(transform, false);

                AudioSource source = host.AddComponent<AudioSource>();

                source.playOnAwake = false;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 5f;
                source.maxDistance = 60f;

                _voices[i] = source;
            }
        }

        private void CreateMusicSources()
        {
            _musicSource = CreateMusicSource("Music_A");
            _musicFadeOut = CreateMusicSource("Music_B");
        }

        private AudioSource CreateMusicSource(string sourceName)
        {
            var host = new GameObject(sourceName);

            host.transform.SetParent(transform, false);

            AudioSource source = host.AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;

            return source;
        }

        /// <summary>
        /// Плавный переход между треками. Идёт по неотмасштабированному
        /// времени: на паузе музыка не должна замирать на полуслове.
        /// </summary>
        private void UpdateMusicFade()
        {
            if (_catalog == null)
                return;

            float duration = Mathf.Max(0.01f, _catalog.musicFadeTime);

            _musicFade = Mathf.MoveTowards(_musicFade, 1f, Time.unscaledDeltaTime / duration);

            float target = _catalog.musicVolume * AudioOptions.EffectiveMusic;

            _musicSource.volume = target * _musicFade;
            _musicFadeOut.volume = target * (1f - _musicFade);

            if (_musicFade >= 1f && _musicFadeOut.isPlaying)
                _musicFadeOut.Stop();
        }

        /// <summary>
        /// Ползунок в настройках подвинули. Уже звучащие эффекты не трогаем:
        /// они длятся доли секунды, а вот музыку надо подхватить немедленно,
        /// иначе игрок решит, что настройка не работает.
        /// </summary>
        private void OnVolumeChanged()
        {
            UpdateMusicFade();
        }
    }
}
