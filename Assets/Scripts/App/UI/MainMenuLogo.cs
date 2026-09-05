using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using HeroDefense.UI;

namespace HeroDefense.App.UI
{
    /// <summary>
    /// Название игры над блоком кнопок в главном меню.
    ///
    /// Меню без логотипа безымянно: игрок отыграет забег и не вспомнит,
    /// во что играл. Для страницы в Steam название нужно и подавно.
    ///
    /// Логотип встаёт не по центру экрана, а над самим блоком кнопок:
    /// иллюстрация меню занята королём и поселением, и центр там не пустой.
    ///
    /// Спрайт берётся из <see cref="UiSkin"/>, поэтому вёрстка не нужна:
    /// положил картинку в ассет — она появилась. Нет картинки — экран
    /// выглядит как раньше, без пустого места на месте логотипа.
    /// </summary>
    [DefaultExecutionOrder(-600)]
    public sealed class MainMenuLogo : MonoBehaviour
    {
        /// <summary>
        /// Насколько логотип может быть шире блока кнопок. Заметно шире —
        /// иначе он читается как ещё одна кнопка, а не как заголовок.
        /// </summary>
        private const float WidthFactor = 1.9f;

        /// <summary>Отступ от верхнего края блока кнопок.</summary>
        private const float BottomMargin = 36f;

        /// <summary>Отступ от верхнего края экрана.</summary>
        private const float TopMargin = 36f;

        /// <summary>Предел ширины: доля от ширины родителя.</summary>
        private const float MaxWidthShare = 0.46f;

        private GameObject _buttons;
        private GameObject _logo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single)
                return;

            // Признак главного меню — его собственный экран. Проверка по имени
            // сцены сломалась бы от переименования, а по типу — нет.
            if (FindFirstObjectByType<MainMenuView>() == null)
                return;

            var host = new GameObject("MainMenuLogo");

            host.AddComponent<MainMenuLogo>();
        }

        private void Start()
        {
            UiSkin skin = RuntimeUi.Skin;
            MainMenuView menu = FindFirstObjectByType<MainMenuView>();

            _buttons = menu != null ? menu.ButtonsRoot : null;

            if (skin == null || skin.logo == null || _buttons == null)
            {
                // Молча уходим: отсутствие логотипа — обычное состояние проекта,
                // пока картинка не нарисована, и ругаться тут не на что.
                Destroy(gameObject);
                return;
            }

            Build(skin.logo);
        }

        /// <summary>
        /// Логотип виден ровно тогда, когда видны кнопки.
        ///
        /// Иначе он остаётся висеть поверх настроек и налезает на их
        /// заголовок — своей раскладки у настроек нет, они просто
        /// подменяют собой блок кнопок.
        /// </summary>
        private void Update()
        {
            if (_logo == null || _buttons == null)
                return;

            bool visible = _buttons.activeInHierarchy;

            if (_logo.activeSelf != visible)
                _logo.SetActive(visible);
        }

        private void Build(Sprite logo)
        {
            var buttonsRect = _buttons.GetComponent<RectTransform>();
            var parentRect = buttonsRect != null ? buttonsRect.parent as RectTransform : null;

            if (buttonsRect == null || parentRect == null)
            {
                Destroy(gameObject);
                return;
            }

            // Раскладка на этом кадре ещё не посчитана, а нам нужны позиции
            // кнопок, чтобы встать точно над ними.
            Canvas.ForceUpdateCanvases();

            // Меряем по самим кнопкам, а не по их контейнеру: контейнер часто
            // растянут на весь экран, и «верхний край блока» оказывается
            // верхом монитора — логотип уезжает за кадр.
            if (!TryMeasureButtons(parentRect, out Rect area))
            {
                Destroy(gameObject);
                return;
            }

            var host = new GameObject("Logo", typeof(RectTransform));

            var rect = host.GetComponent<RectTransform>();

            // Логотип живёт в том же канвасе, что и кнопки: свой канвас поверх
            // пришлось бы вручную держать в нужном порядке отрисовки.
            rect.SetParent(parentRect, false);

            // Якорь в центре родителя, а координаты считаем сами: копировать
            // якоря кнопок нельзя, они могут быть растянутыми.
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            // Свободная полоса между верхом кнопок и верхом экрана.
            float bottom = area.yMax + BottomMargin;
            float top = parentRect.rect.yMax - TopMargin;
            float availableHeight = top - bottom;

            if (availableHeight <= 1f)
            {
                Debug.LogWarning("[Меню] Над кнопками нет места под логотип. " +
                                 "Опусти блок кнопок или уменьши логотип.", this);

                Destroy(gameObject);
                return;
            }

            // Логотип занимает эту полосу целиком, насколько позволяют
            // пропорции: заголовок должен быть заметно крупнее кнопок,
            // иначе он читается как их продолжение.
            float availableWidth = Mathf.Min(area.width * WidthFactor,
                                             parentRect.rect.width * MaxWidthShare);

            float aspect = logo.rect.height > 0f ? logo.rect.width / logo.rect.height : 1f;

            float width = Mathf.Min(availableWidth, availableHeight * aspect);
            float height = width / aspect;

            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(area.center.x, bottom + availableHeight * 0.5f);

            var image = host.AddComponent<Image>();

            image.sprite = logo;
            image.preserveAspect = true;

            // Логотип не должен ловить клики: под ним кнопки меню.
            image.raycastTarget = false;

            _logo = host;
        }

        /// <summary>
        /// Прямоугольник, который занимают кнопки меню, в координатах их
        /// родителя. Считается по углам каждой кнопки: это единственные
        /// объекты, чьё положение на экране заведомо совпадает с видимым.
        /// </summary>
        private bool TryMeasureButtons(RectTransform space, out Rect area)
        {
            area = default;

            Button[] buttons = _buttons.GetComponentsInChildren<Button>(false);

            if (buttons.Length == 0)
                return false;

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            var corners = new Vector3[4];

            foreach (Button button in buttons)
            {
                var rect = button.transform as RectTransform;

                if (rect == null)
                    continue;

                rect.GetWorldCorners(corners);

                foreach (Vector3 corner in corners)
                {
                    Vector2 local = space.InverseTransformPoint(corner);

                    min = Vector2.Min(min, local);
                    max = Vector2.Max(max, local);
                }
            }

            if (min.x > max.x)
                return false;

            area = new Rect(min, max - min);

            return true;
        }
    }
}
