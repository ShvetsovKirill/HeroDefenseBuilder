using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using HeroDefense.Core;

namespace HeroDefense.Squads
{
    /// <summary>
    /// Единственный канал командования отрядами (D12).
    ///
    /// Схема (D51): 1–4 выбирают флаг, повторное нажатие убирает его в руки,
    /// F втыкает флаг в точку пивота коня. Переставить — отбежать и нажать F снова.
    ///
    /// Никаких выделений мышью и миникарты (D16): всё сводится к одиночным
    /// нажатиям, поэтому схема переносится на тач без переделки.
    ///
    /// Превью на земле сознательно нет (D52) — обратная связь встроена
    /// в самого короля: везёт знамя, значит флаг выбран и готов к установке.
    /// </summary>
    public sealed class FlagController : MonoBehaviour
    {
        [Header("Ссылки")]
        [Tooltip("Знамя в руках короля. Включается при выборе флага.")]
        [SerializeField] private Renderer carriedBanner;

        [Tooltip("Префаб флага на земле — чистый маркер без коллайдера (D53).")]
        [SerializeField] private GameObject flagMarkerPrefab;

        private readonly Dictionary<Barracks, GameObject> _markers = new();

        private int _selectedIndex = -1;
        private MaterialPropertyBlock _propertyBlock;

        private MaterialPropertyBlock PropertyBlock =>
            _propertyBlock ??= new MaterialPropertyBlock();

        /// <summary>Какой флаг сейчас в руках. -1 — ничего не выбрано.</summary>
        public int SelectedIndex => _selectedIndex;

        public int BarracksCount => SquadRegistry.Count;

        private static Transform King => SceneContext.Current?.King;

        private void Awake()
        {
            RefreshBarracksList();
            UpdateCarriedBanner();
        }

        private void OnEnable()
        {
            // Реестр сам скажет, когда казарм стало больше или меньше —
            // подписка на постройку зданий больше не нужна.
            SquadRegistry.Changed += RefreshBarracksList;
        }

        private void OnDisable()
        {
            SquadRegistry.Changed -= RefreshBarracksList;
        }

        /// <summary>
        /// Порядок казарм берётся из реестра, а не из поиска по сцене:
        /// FindObjectsByType возвращает объекты в неопределённом порядке,
        /// и после перестройки казармы клавиши 1–4 могли поменяться местами.
        /// Игрок нажимал «2» и переставлял не тот флаг.
        /// </summary>
        public void RefreshBarracksList()
        {
            CleanupOrphanMarkers();

            if (_selectedIndex >= SquadRegistry.Count)
                Deselect();
        }

        /// <summary>
        /// Убрать флаги казарм, которых больше нет.
        /// Без этого маркер разрушенной казармы остался бы висеть на карте.
        /// </summary>
        private void CleanupOrphanMarkers()
        {
            var orphans = new List<Barracks>();

            foreach (KeyValuePair<Barracks, GameObject> pair in _markers)
            {
                if (pair.Key == null || !SquadRegistry.All.Contains(pair.Key))
                    orphans.Add(pair.Key);
            }

            foreach (Barracks orphan in orphans)
            {
                if (_markers.TryGetValue(orphan, out GameObject marker) && marker != null)
                    Destroy(marker);

                _markers.Remove(orphan);
            }
        }

        private void Update()
        {
            if (!IsGameRunning)
                return;

            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
                return;

            HandleSelection(keyboard);
            HandlePlacement(keyboard);
        }

        private static bool IsGameRunning =>
            GameState.Current == null || GameState.Current.IsPlaying;

        // ---------- Выбор ----------

        private void HandleSelection(Keyboard keyboard)
        {
            if (keyboard.digit1Key.wasPressedThisFrame) ToggleSelection(0);
            if (keyboard.digit2Key.wasPressedThisFrame) ToggleSelection(1);
            if (keyboard.digit3Key.wasPressedThisFrame) ToggleSelection(2);
            if (keyboard.digit4Key.wasPressedThisFrame) ToggleSelection(3);
        }

        /// <summary>Повторное нажатие на ту же цифру убирает флаг обратно.</summary>
        private void ToggleSelection(int index)
        {
            if (index >= SquadRegistry.Count)
                return;

            _selectedIndex = _selectedIndex == index ? -1 : index;

            UpdateCarriedBanner();
        }

        private void Deselect()
        {
            _selectedIndex = -1;
            UpdateCarriedBanner();
        }

        // ---------- Установка ----------

        private void HandlePlacement(Keyboard keyboard)
        {
            if (!keyboard.fKey.wasPressedThisFrame || _selectedIndex < 0)
                return;

            PlaceFlag(SquadRegistry.GetAt(_selectedIndex));
        }

        private void PlaceFlag(Barracks barracks)
        {
            Transform monarch = King;

            if (barracks == null || barracks.Squad == null || monarch == null)
                return;

            Vector3 position = monarch.position;

            barracks.Squad.SetFlag(position);
            MoveMarker(barracks, position);

            // Флаг воткнут — король едет дальше налегке.
            Deselect();
        }

        // ---------- Визуал ----------

        private void MoveMarker(Barracks barracks, Vector3 position)
        {
            if (!_markers.TryGetValue(barracks, out GameObject marker) || marker == null)
            {
                if (flagMarkerPrefab == null)
                    return;

                marker = Instantiate(flagMarkerPrefab);
                _markers[barracks] = marker;

                ApplyColor(marker.GetComponentInChildren<Renderer>(), FlagColorOf(barracks));
            }

            marker.transform.position = position;
        }

        private void UpdateCarriedBanner()
        {
            if (carriedBanner == null)
                return;

            bool hasSelection = _selectedIndex >= 0 && _selectedIndex < SquadRegistry.Count;

            carriedBanner.gameObject.SetActive(hasSelection);

            if (hasSelection)
                ApplyColor(carriedBanner, FlagColorOf(SquadRegistry.GetAt(_selectedIndex)));
        }

        private void ApplyColor(Renderer target, Color color)
        {
            if (target == null)
                return;

            MaterialPropertyBlock block = PropertyBlock;

            target.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            target.SetPropertyBlock(block);
        }

        private static Color FlagColorOf(Barracks barracks)
        {
            return barracks != null && barracks.Definition != null
                ? barracks.Definition.flagColor
                : Color.white;
        }
    }
}
