using System.Collections.Generic;
using UnityEngine;
using HeroDefense.Core;

namespace HeroDefense.UI
{
    /// <summary>
    /// Вспышка при получении урона.
    ///
    /// Полоска здоровья показывает состояние, но не привлекает внимание.
    /// Вспышка отвечает на другой вопрос: «в меня попали прямо сейчас».
    /// При толпе врагов это единственный способ понять, что бьют именно
    /// эту башню, а не соседнюю.
    ///
    /// Работает через MaterialPropertyBlock: менять material.color означало бы
    /// плодить копию материала на каждый объект и ломать батчинг.
    /// </summary>
    public sealed class DamageFlash : MonoBehaviour
    {
        [Header("Что мигает")]
        [Tooltip("Пусто — соберутся все рендереры с этого объекта и детей.")]
        [SerializeField] private Renderer[] renderers;

        [Header("Ссылки")]
        [Tooltip("Пусто — берётся с этого объекта или родителя.")]
        [SerializeField] private Health health;

        [Header("Вспышка")]
        [SerializeField] private Color flashColor = Color.white;

        [Tooltip("Сколько секунд длится вспышка. Короткая читается лучше: " +
                 "длинная сливается в постоянное свечение при частых ударах.")]
        [SerializeField] private float duration = 0.08f;

        [Range(0f, 1f)]
        [Tooltip("Насколько сильно подмешивается цвет вспышки.")]
        [SerializeField] private float intensity = 0.7f;

        private readonly List<Color> _originalColors = new();

        private MaterialPropertyBlock _propertyBlock;
        private float _timer;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            if (health == null)
                health = GetComponentInParent<Health>();

            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>();

            _propertyBlock = new MaterialPropertyBlock();

            CacheOriginalColors();
        }

        private void OnEnable()
        {
            if (health != null)
                health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (health != null)
                health.Damaged -= OnDamaged;
        }

        /// <summary>
        /// Запоминаем исходные цвета один раз: после вспышки надо вернуть
        /// именно их, а не белый по умолчанию.
        /// </summary>
        private void CacheOriginalColors()
        {
            _originalColors.Clear();

            for (int i = 0; i < renderers.Length; i++)
            {
                Color color = Color.white;

                if (renderers[i] != null && renderers[i].sharedMaterial != null
                    && renderers[i].sharedMaterial.HasProperty(BaseColorId))
                {
                    color = renderers[i].sharedMaterial.GetColor(BaseColorId);
                }

                _originalColors.Add(color);
            }
        }

        private void OnDamaged(float amount)
        {
            _timer = duration;
            ApplyFlash(1f);
        }

        private void Update()
        {
            if (_timer <= 0f)
                return;

            _timer -= Time.deltaTime;

            // Гасим постепенно: резкое выключение выглядит как мигание лампы.
            float progress = Mathf.Clamp01(_timer / duration);

            ApplyFlash(progress);
        }

        private void ApplyFlash(float strength)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    continue;

                Color original = i < _originalColors.Count ? _originalColors[i] : Color.white;
                Color result = Color.Lerp(original, flashColor, strength * intensity);

                renderers[i].GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(BaseColorId, result);
                renderers[i].SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
