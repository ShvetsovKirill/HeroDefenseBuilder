using UnityEngine;
using HeroDefense.Core;

namespace HeroDefense.UI
{
    /// <summary>
    /// Полоска здоровья над объектом. Вешается на всё, что можно повредить:
    /// ратушу, башни, казармы, бойцов отряда.
    ///
    /// Почему в мире, а не в HUD: игрок должен видеть, ЧТО именно бьют,
    /// не переводя взгляд. Десяток полосок в углу экрана эту задачу
    /// не решает — по ним не понять, какая башня горит.
    ///
    /// Показывается только при неполном здоровье: целые постройки
    /// не должны засорять экран. Это и есть «громкая обратная связь»
    /// из D2 — сигнал появляется ровно тогда, когда есть о чём сигналить.
    ///
    /// Реализация на спрайтах, а не на Canvas: World Space Canvas на каждом
    /// объекте — это отдельный батч рендера на каждую полоску.
    /// </summary>
    public sealed class WorldHealthBar : MonoBehaviour
    {
        [Header("Что показываем")]
        [Tooltip("Пусто — берётся с этого же объекта или родителя.")]
        [SerializeField] private Health health;

        [Header("Части полоски")]
        [Tooltip("Подложка. Обычно тёмный Quad.")]
        [SerializeField] private Transform background;

        [Tooltip("Заполнение. Масштабируется по X от левого края.")]
        [SerializeField] private Transform fill;

        [SerializeField] private Renderer fillRenderer;

        [Header("Положение")]
        [Tooltip("Насколько выше объекта висит полоска.")]
        [SerializeField] private float heightOffset = 2f;

        [Header("Цвет")]
        [SerializeField] private Color healthyColor = new Color(0.35f, 0.8f, 0.35f);
        [SerializeField] private Color hurtColor = new Color(0.9f, 0.75f, 0.25f);
        [SerializeField] private Color criticalColor = new Color(0.9f, 0.25f, 0.25f);

        [Tooltip("Ниже этой доли здоровья полоска считается критической.")]
        [Range(0f, 1f)]
        [SerializeField] private float criticalThreshold = 0.3f;

        [Header("Поведение")]
        [Tooltip("Показывать, только когда здоровье не полное. " +
                 "Выключено — полоска висит всегда (для ратуши может быть уместно).")]
        [SerializeField] private bool hideWhenFull = true;

        [Tooltip("Как быстро заполнение догоняет реальное здоровье. " +
                 "Плавность делает удар заметнее, чем мгновенный скачок.")]
        [SerializeField] private float fillSpeed = 4f;

        private Transform _root;
        private Transform _cameraTransform;
        private MaterialPropertyBlock _propertyBlock;
        private float _displayedFraction = 1f;

        private MaterialPropertyBlock PropertyBlock =>
            _propertyBlock ??= new MaterialPropertyBlock();

        private void Awake()
        {
            _root = transform;

            if (health == null)
                health = GetComponentInParent<Health>();

            if (health == null)
            {
                Debug.LogError($"[WorldHealthBar] На {name} и его родителях нет Health.", this);
                enabled = false;
                return;
            }

            if (Camera.main != null)
                _cameraTransform = Camera.main.transform;

            _displayedFraction = health.Fraction;
            ApplyFill(_displayedFraction);
            UpdateVisibility();
        }

        private void LateUpdate()
        {
            if (health == null)
                return;

            FollowOwner();
            FaceCamera();
            UpdateFill();
            UpdateVisibility();
        }

        // ---------- Положение ----------

        /// <summary>
        /// Полоска висит над владельцем, но не наследует его поворот:
        /// иначе она крутилась бы вместе с врагом или башней.
        /// </summary>
        private void FollowOwner()
        {
            Vector3 position = health.transform.position;

            position.y += heightOffset;
            _root.position = position;
        }

        private void FaceCamera()
        {
            if (_cameraTransform == null)
                return;

            // Разворачиваем к камере целиком: при фиксированном угле обзора
            // этого достаточно, billboard-математика не нужна.
            _root.rotation = _cameraTransform.rotation;
        }

        // ---------- Заполнение ----------

        private void UpdateFill()
        {
            float target = health.Fraction;

            _displayedFraction = Mathf.MoveTowards(
                _displayedFraction, target, fillSpeed * Time.deltaTime);

            ApplyFill(_displayedFraction);
        }

        /// <summary>
        /// Масштабируем по X и сдвигаем на половину убыли: пивот у Quad
        /// в центре, поэтому без сдвига полоска убывала бы с обеих сторон.
        /// </summary>
        private void ApplyFill(float fraction)
        {
            if (fill == null)
                return;

            Vector3 scale = fill.localScale;
            float fullWidth = background != null ? background.localScale.x : 1f;

            scale.x = fullWidth * fraction;
            fill.localScale = scale;

            Vector3 position = fill.localPosition;
            position.x = -(fullWidth - scale.x) * 0.5f;
            fill.localPosition = position;

            ApplyColor(fraction);
        }

        private void ApplyColor(float fraction)
        {
            if (fillRenderer == null)
                return;

            Color color = fraction <= criticalThreshold
                ? criticalColor
                : fraction < 0.99f ? hurtColor : healthyColor;

            MaterialPropertyBlock block = PropertyBlock;

            fillRenderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            fillRenderer.SetPropertyBlock(block);
        }

        // ---------- Видимость ----------

        private void UpdateVisibility()
        {
            bool visible = !hideWhenFull || health.Fraction < 0.999f;

            // Прячем через рендереры, а не SetActive: выключенный объект
            // перестал бы получать LateUpdate и застыл бы в старом состоянии.
            SetRendererEnabled(background, visible);
            SetRendererEnabled(fill, visible);
        }

        private static void SetRendererEnabled(Transform target, bool visible)
        {
            if (target == null)
                return;

            var renderer = target.GetComponent<Renderer>();

            if (renderer != null && renderer.enabled != visible)
                renderer.enabled = visible;
        }
    }
}
