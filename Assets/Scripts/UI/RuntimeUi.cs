using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HeroDefense.UI
{
    /// <summary>
    /// Сборка простых панелей кодом.
    ///
    /// Зачем: экран паузы и подсказка управления нужны в каждой боевой
    /// сцене, а вёрстки у них почти нет — рамка, заголовок, список строк.
    /// Собранные в сцене, они требовали бы повторения при каждой новой
    /// карте и молча ломались бы, если про них забыть.
    ///
    /// Это не замена вёрстке. Как только у экрана появится оформление —
    /// рамка, фон, иконки, — он переезжает в префаб, а этот класс остаётся
    /// для служебных панелей.
    ///
    /// Шрифт не задаётся: TMP берёт его из настроек проекта, где уже стоит
    /// Russo One. Прописать шрифт здесь — значит завести второе место,
    /// которое придётся править при следующей смене.
    /// </summary>
    public static class RuntimeUi
    {
        /// <summary>Тёмная подложка модального экрана.</summary>
        public static readonly Color OverlayColor = new(0.03f, 0.03f, 0.05f, 0.82f);

        /// <summary>Фон панели.</summary>
        public static readonly Color PanelColor = new(0.10f, 0.09f, 0.11f, 0.96f);

        /// <summary>Основной текст: тёплый светлый, как в остальных экранах.</summary>
        public static readonly Color TextColor = new(0.95f, 0.89f, 0.78f);

        /// <summary>Акцент: золото заголовков и выбранного.</summary>
        public static readonly Color AccentColor = new(0.79f, 0.64f, 0.15f);

        /// <summary>Имя ассета оформления внутри Resources. Без расширения.</summary>
        private const string SkinPath = "UiSkin";

        private static UiSkin _skin;
        private static bool _skinLoaded;
        private static TMP_FontAsset _font;

        /// <summary>
        /// Оформление, если оно заведено. Грузится один раз за запуск;
        /// его отсутствие — не ошибка, а «рисуй заливкой».
        /// </summary>
        public static UiSkin Skin
        {
            get
            {
                if (_skinLoaded)
                    return _skin;

                _skinLoaded = true;
                _skin = Resources.Load<UiSkin>(SkinPath);

                return _skin;
            }
        }

        /// <summary>
        /// Канвас поверх остальных. Порядок задаётся явно: у HUD он нулевой,
        /// и без запаса модальный экран оказался бы под ним.
        /// </summary>
        public static Canvas CreateCanvas(GameObject host, int sortingOrder)
        {
            var canvas = host.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = host.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            // Половина: иначе на широком мониторе интерфейс раздувается
            // по ширине, а на узком — сжимается по высоте.
            scaler.matchWidthOrHeight = 0.5f;

            host.AddComponent<GraphicRaycaster>();

            return canvas;
        }

        /// <summary>Прямоугольник во весь родитель. Основа подложек.</summary>
        public static RectTransform CreateStretched(string objectName, Transform parent)
        {
            var host = new GameObject(objectName, typeof(RectTransform));

            var rect = host.GetComponent<RectTransform>();

            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return rect;
        }

        /// <summary>Заливка во весь родитель: затемнение или фон панели.</summary>
        public static Image CreateFill(string objectName, Transform parent, Color color)
        {
            RectTransform rect = CreateStretched(objectName, parent);

            var image = rect.gameObject.AddComponent<Image>();

            image.color = color;

            return image;
        }

        /// <summary>
        /// Панель по центру экрана с вертикальной раскладкой.
        /// Высота подгоняется под содержимое: заранее её знать нельзя —
        /// список управления зависит от языка.
        /// </summary>
        public static RectTransform CreatePanel(Transform parent, float width, RectOffset padding, float spacing)
        {
            var host = new GameObject("Panel", typeof(RectTransform));

            var rect = host.GetComponent<RectTransform>();

            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, 0f);

            var background = host.AddComponent<Image>();

            ApplyPanelLook(background, Skin != null ? Skin.panel : null);

            var layout = host.AddComponent<VerticalLayoutGroup>();

            layout.padding = padding;
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            var fitter = host.AddComponent<ContentSizeFitter>();

            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return rect;
        }

        /// <summary>
        /// Одеть фон панели: спрайт с 9-slice, если он задан, иначе заливка.
        ///
        /// Отдельным методом, потому что панелей три вида — модальная,
        /// компактная и ложе полоски, — и правило подстановки у них одно.
        /// </summary>
        public static void ApplyPanelLook(Image image, Sprite sprite)
        {
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.color = Skin != null ? Skin.panelTint : Color.white;
                return;
            }

            image.color = PanelColor;
        }

        /// <summary>
        /// Шрифт для надписей, собранных кодом.
        ///
        /// Новый TextMeshProUGUI берёт шрифт из настроек TMP, а там он может
        /// оказаться пустым или указывать на удалённый ассет: проект перешёл
        /// на Russo One, и LiberationSans из настроек исчез. Тогда надпись
        /// молча не рисуется — текст есть, а на экране пусто.
        ///
        /// Поэтому шрифт назначается явно: из оформления, из настроек TMP,
        /// а в крайнем случае одалживается у любой существующей надписи сцены.
        /// </summary>
        public static TMP_FontAsset ResolveFont()
        {
            if (_font != null)
                return _font;

            if (Skin != null && Skin.font != null)
                return _font = Skin.font;

            if (TMP_Settings.defaultFontAsset != null)
                return _font = TMP_Settings.defaultFontAsset;

            foreach (TMP_Text existing in Object.FindObjectsByType<TMP_Text>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (existing.font != null)
                    return _font = existing.font;
            }

            Debug.LogWarning("[Интерфейс] Не нашёл ни одного шрифта TMP. " +
                             "Служебные экраны будут без текста.");

            return null;
        }

        /// <summary>Надпись внутри раскладки.</summary>
        public static TMP_Text CreateText(Transform parent, string text, float size,
                                          Color color, TextAlignmentOptions alignment)
        {
            var host = new GameObject("Text", typeof(RectTransform));

            host.transform.SetParent(parent, false);

            var label = host.AddComponent<TextMeshProUGUI>();

            TMP_FontAsset font = ResolveFont();

            if (font != null)
                label.font = font;

            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.richText = true;

            return label;
        }

        /// <summary>
        /// Вертикальная колонка внутри панели.
        ///
        /// ContentSizeFitter здесь СОЗНАТЕЛЬНО не ставится. Фиттер внутри
        /// чужой раскладки спорит с ней за высоту, и объект схлопывается
        /// в ноль: на экране от списка остаётся одна последняя строка,
        /// а от панели — угол рамки. Высоту колонки считает родительская
        /// группа — она умеет спрашивать вложенную.
        /// </summary>
        public static Transform CreateColumn(Transform parent, float spacing)
        {
            var host = new GameObject("Column", typeof(RectTransform));

            host.transform.SetParent(parent, false);

            var layout = host.AddComponent<VerticalLayoutGroup>();

            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            return host.transform;
        }

        /// <summary>
        /// Список управления: строка на клавишу.
        ///
        /// С картинками клавиш, если они заведены в оформлении, иначе —
        /// одним текстовым блоком, как было. Собран здесь, а не в трёх
        /// экранах: подсказка в бою, пауза и окно из меню показывают
        /// одно и то же, и расходиться они не должны.
        /// </summary>
        public static void CreateControlsList(Transform parent, float fontSize)
        {
            UiSkin skin = Skin;

            if (skin == null || skin.keyIcons.Length == 0)
            {
                CreateText(parent, ControlsInfo.BuildText(), fontSize,
                           TextColor, TextAlignmentOptions.Left);
                return;
            }

            foreach (ControlsInfo.Line line in ControlsInfo.Lines)
                CreateControlsRow(parent, line, fontSize, skin.FindKey(line.IconId));

            // Подсказка о строительстве идёт отдельной строкой без клавиши:
            // у неё нет своей кнопки, и картинку рисовать нечем.
            CreateText(parent, ControlsInfo.BuildHint, fontSize * 0.92f,
                       TextColor, TextAlignmentOptions.Left);
        }

        /// <summary>Одна строка списка: картинка клавиши слева, описание справа.</summary>
        private static void CreateControlsRow(Transform parent, ControlsInfo.Line line,
                                              float fontSize, Sprite keySprite)
        {
            var host = new GameObject("ControlRow", typeof(RectTransform));

            host.transform.SetParent(parent, false);

            var layout = host.AddComponent<HorizontalLayoutGroup>();

            layout.spacing = fontSize * 0.6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            float iconHeight = fontSize * 1.9f;

            if (keySprite != null)
            {
                var iconHost = new GameObject("Key", typeof(RectTransform));

                iconHost.transform.SetParent(host.transform, false);

                var icon = iconHost.AddComponent<Image>();

                icon.sprite = keySprite;

                // Ширина считается из пропорций спрайта: блок WASD шире
                // одиночной клавиши, и растягивать его до квадрата нельзя.
                icon.preserveAspect = true;

                var element = iconHost.AddComponent<LayoutElement>();

                element.minHeight = iconHeight;
                element.preferredHeight = iconHeight;
                element.minWidth = iconHeight;
                element.preferredWidth = iconHeight * AspectOf(keySprite);
            }
            else
            {
                TMP_Text keys = CreateText(host.transform, line.Keys, fontSize,
                                           AccentColor, TextAlignmentOptions.Left);

                keys.gameObject.AddComponent<LayoutElement>().preferredWidth = fontSize * 5f;
            }

            TMP_Text description = CreateText(host.transform, line.Description, fontSize,
                                              TextColor, TextAlignmentOptions.Left);

            // Гибкая ширина: описание занимает остаток строки и переносится,
            // а не вылезает за край панели.
            description.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private static float AspectOf(Sprite sprite)
        {
            Rect rect = sprite.rect;

            return rect.height > 0f ? rect.width / rect.height : 1f;
        }

        /// <summary>Кнопка с подписью. Высота фиксирована — иначе её не нажать.</summary>
        public static Button CreateButton(Transform parent, string label, float height)
        {
            var host = new GameObject("Button", typeof(RectTransform));

            host.transform.SetParent(parent, false);

            var image = host.AddComponent<Image>();

            if (Skin != null && Skin.button != null)
            {
                image.sprite = Skin.button;
                image.type = Image.Type.Sliced;
                image.color = Skin.buttonTint;
            }
            else
            {
                image.color = new Color(0.18f, 0.16f, 0.19f, 1f);
            }

            var button = host.AddComponent<Button>();

            button.targetGraphic = image;

            var element = host.AddComponent<LayoutElement>();

            element.minHeight = height;
            element.preferredHeight = height;

            TMP_Text text = CreateText(host.transform, label, height * 0.42f,
                                       TextColor, TextAlignmentOptions.Center);

            RectTransform textRect = text.rectTransform;

            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            ButtonTextFitter.Apply(button);

            return button;
        }
    }
}
