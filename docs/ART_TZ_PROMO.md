# ТЗ на промо: обложка, логотип, арт кампании

**Дата:** 6 сентября 2026.

Четыре заказа. Первые три — промо: обложка, логотип и их сборка.
Четвёртый — то, что понадобилось коду прямо сейчас: карта кампании,
портреты командиров, значки владений.

Стиль и палитра — те же, что во всём заказе (см.
[ART_PROMPTS_REORDER.md](ART_PROMPTS_REORDER.md)): славянское тёмное
фэнтези, стилизованная низкополигональная графика в духе Bad North.

---

## Что промо обязано сообщить за одну секунду

Игрок, увидевший картинку в ленте Steam, должен понять три вещи:

1. **ты — король верхом**, а не безликий полководец сверху;
2. **ты защищаешь поселение**, а не штурмуешь;
3. **врагов много, и они уже здесь**.

Если из картинки не читается хотя бы одно — она не работает, какой бы
красивой ни была. Поэтому в промпте ниже композиция задана жёстко,
а не отдана на усмотрение модели.

---

## P1 — обложка без текста

Заказывается **без единой буквы**: логотип ставится отдельно, иначе
модель впишет в картинку кривые надписи, и переделывать придётся всё.

Верхняя треть намеренно оставляется пустой — туда ляжет логотип.

```
Create a key art illustration for a Slavic dark-fantasy tower-defense
roguelite game.

SUBJECT AND STORY. A lone king on horseback defends a small wooden
settlement against a horde pouring out of a black forest at dusk.
He is not a distant commander — he is in the fight himself.

COMPOSITION, strictly follow this.
- Foreground, lower right third: the mounted king seen from behind and
  slightly to the side, riding toward the battle. Crowned helmet, dark
  red cloak with a golden lion, spear or sword raised. He is the largest
  figure and the clear focal point, but we do not see his face.
- Middle ground, center: a small fortified settlement — timber palisade,
  a stone-and-wood hall with a steep roof, watchtowers, warm firelight
  in the windows, braziers along the wall. Two or three small squads of
  spearmen with blue banners hold the approaches.
- Background, upper left and left edge: a dense black pine forest with
  a torch-lit horde streaming out along two paths toward the settlement.
  Silhouettes and pinpoints of torchlight, not detailed faces.
- Upper third of the image: open dusk sky, clouds, empty of any object.
  Leave it clean — a logo will be placed there.

STYLE. Stylized low-poly 3D render in the spirit of Bad North: faceted
geometry, flat shading, matte materials, large simple shapes, minimal
small detail. Painterly sky. Readable silhouettes above all.

LIGHT AND COLOR. Cold blue dusk overall; the settlement is the only
source of warm orange light, and the enemy side is cold and dark.
The picture must split in two at a glance: warm home, cold threat.
Palette: player blue #3B6FA0, gold #C9A227, dark timber #6B4A2F,
bandit red #C0392B, cold night blue #2A3550.

FORMAT. Horizontal 16:9, at least 1920x1080. Cinematic but grounded —
this is a game about a small kingdom, not an epic army.

MUST NOT. No text, no letters, no numbers, no logo, no watermark,
no UI elements, no modern objects. Do not fill the upper sky with
dragons, birds or floating debris — it must stay empty.
```

**Приёмка.** Смотреть на картинку три секунды и ответить: кто главный,
что защищают, откуда опасность. Если хоть один ответ не приходит сразу —
переделывать композицию, а не детали.

---

## P2 — логотип

Название длинное — четыре слова, двадцать букв. Модели врут в длинных
надписях: съеденная перекладина, «RICHT» вместо «RIGHT». Поэтому
у заказа два пути, и первый надёжнее.

### Путь A, надёжный: эмблема без букв

Текст набирается шрифтом отдельно и совмещается с эмблемой — так уже
собран временный логотип в проекте.

```
Design a heraldic emblem for a Slavic dark-fantasy strategy game.

WHAT TO DRAW. A royal crown seen straight on, with a sword and a
sceptre crossed behind it. Two golden heraldic lions rampant flank the
crown, facing inward, holding it up.

STYLE. Stylized low-poly with faceted geometry and flat shading, matte
gold, no gloss, no gradients on metal. Bold shapes readable at small
size. Light from upper left at 45 degrees.

COLOR. Gold #C9A227, player blue #3B6FA0, dark steel #4A4A50,
cream #F2E4C8.

COMPOSITION. Strictly symmetrical along the vertical axis. Fits into a
wide triangle, point up. Width about twice the height.

MUST NOT. No text, no letters, no numbers, no ribbons or banners with
writing, no empty scrolls — space for the title is added later.
Transparent background, or flat #FF00FF if transparency is unavailable.
No drop shadow outside the emblem.

Output PNG, at least 2048 px on the long side.
```

### Путь B, с текстом: только если проверишь каждую букву

```
Design a game logo for "THE KING IS ALWAYS RIGHT", a Slavic
dark-fantasy tower-defense roguelite.

TEXT LAYOUT, exactly three lines, centered:
line 1, small, widely letter-spaced: THE
line 2, very large, the dominant element: KING
line 3, medium: IS ALWAYS RIGHT

Spell every word exactly as written. Do not invent, translate or
abbreviate any word.

STYLE. Heavy geometric slab letterforms with faceted bevels, carved
in gold #C9A227 with a warm highlight on top edges and a dark outline
#16110C. Slight wear, no gloss. Above the word KING sits a small
faceted royal crown. A thin gold rule separates KING from the third line.

MUST NOT. No background — transparent, or flat #FF00FF. No extra words,
no taglines, no scrolls, no shields, no characters. Nothing may overlap
the letters.

Output PNG, at least 2560 px wide.
```

**Приёмка пути B.** Прочитать надпись по буквам вслух. Одна кривая
буква — брак: логотип видно в каждом скриншоте и в каждой ленте.

---

## P3 — сборка обложки с логотипом

Совмещение делается **не генерацией заново**, а поверх принятой обложки:
заново нарисованная картинка не совпадёт с той, что уже утверждена.

Если сборку всё-таки делает нейросеть, ей отдаётся готовая обложка
из P1 как исходное изображение:

```
Take the provided key art as the base image and do not redraw it.
Keep every element of the illustration exactly as it is.

Add the provided logo into the empty sky in the upper third, centered
horizontally. The logo occupies about 55 percent of the image width and
sits with its top edge about 8 percent below the top of the frame.

Blend it into the scene: a soft dark shadow under the letterforms so
they hold against the sky, and a faint warm rim on the top edges to
match the dusk light. Do not add glow, sparks, rays or particles.

Nothing must overlap the king, the settlement or the horde.

Output the same aspect ratio and resolution as the base image.
```

**Проще и надёжнее:** прислать мне обложку и логотип отдельными файлами.
Совмещу их сам — с точным позиционированием, тенью и без риска, что
модель перерисует иллюстрацию.

**Нужные размеры для Steam** (когда дойдёт до страницы):

| Что | Размер |
|---|---|
| Главная капсула | 1232×706 |
| Малая капсула | 462×174 |
| Вертикальная (библиотека) | 600×900 |
| Фон страницы | 1438×810 |

Все режутся из одной обложки, если она снята в 16:9 с запасом по краям.

---

## P4 — арт, которого требует код прямо сейчас

Появилась кампания: карта владений, командиры, разные типы поселений.
Код работает на заглушках, места под арт готовы.

### P4.1 — значки владений на карте

Шесть типов мест. Игрок выбирает маршрут по этим значкам, поэтому они
обязаны различаться силуэтом, а не подписью.

```
Draw a set of map icons for a Slavic dark-fantasy strategy game.

STYLE. Stylized low-poly, faceted geometry, flat shading, matte
materials, light from upper left at 45 degrees. Readable as a silhouette
at 64 pixels.

PALETTE. Player blue #3B6FA0, gold #C9A227, dark timber #6B4A2F,
stone #8A8580, cream #F2E4C8.

LAYOUT. One image 1536x1024, grid of 3 columns by 2 rows, cell 512x512.
Each object centered in its cell with 8-10 percent margin. No dividing
lines, no labels, no numbers. Order left to right, top to bottom.

CELLS.
1. Hamlet: three small thatched huts around an elder's house, low
   wooden fence.
2. Village: a cluster of timber houses with a taller town hall in the
   middle, steep shingled roof.
3. Port town: a wooden pier with a small warehouse and a moored boat.
4. Barony: a stone keep with a square donjon and a blue banner.
5. Castle: a larger fortress with two towers, curtain wall and a gate.
6. War camp: three tents around a campfire, a spear stuck in the ground
   with a pennant.

Sizes must read as a hierarchy: hamlet smallest, castle largest.

MUST NOT. Transparent background, or flat #FF00FF. No text, no numbers,
no shadows outside the object, no circular badges under the icons.

Output PNG.
```

### P4.2 — портреты командиров

Люди, ради которых игрок идёт в тяжёлое баронство. Их гибель
необратима, поэтому лица должны запоминаться.

```
Draw a sheet of character portraits for a Slavic dark-fantasy strategy
game, in the flat illustrated style of Bad North.

STYLE. Flat vector-like illustration with heavy black outlines, simple
geometric faces, long noses, small eyes, calm and slightly weary
expressions. Flat two-tone background behind each head, different for
each portrait. No shading gradients, no realism.

CHARACTERS, six busts, all wearing mail or padded armor of a Slavic
druzhina, no crowns:
1. Old veteran, grey beard, scar across the brow, steel helmet with
   a nasal guard.
2. Woman warrior, dark braid over the shoulder, mail coif, hard stare.
3. Lame young man, thin face, leather cap, bow across the back.
4. Broad-shouldered smith turned soldier, thick beard, no helmet.
5. Grim archer with a hood and a leather bracer.
6. Young spearman, freckles, oversized helmet, uncertain look.

LAYOUT. One image 1536x1024, grid of 3 columns by 2 rows, cell 512x512.
Each bust centered, cropped at the chest.

MUST NOT. No text, no names, no frames, no crowns, no modern clothing.
Flat background inside each cell is fine — the portraits are shown in
frames in-game.

Output PNG.
```

### P4.3 — значки фракций и наград

Шесть фракций и четыре типа награды. Показываются на карте до входа
во владение — по ним игрок решает, куда свернуть.

```
Draw a set of icons for a Slavic dark-fantasy strategy game.

STYLE. Stylized low-poly, faceted, flat shading, matte, light from upper
left. Bold shapes readable at 32 pixels.

PALETTE. Gold #C9A227, steel #8A8580, blood red #C0392B, forest green
#6A9E3F, cold violet #6B5B8A, sea blue #2E6E8E.

LAYOUT. One image 2048x1024, grid of 5 columns by 2 rows, cell 400x400,
centered objects, 10 percent margin. No lines, labels or numbers.

CELLS, order left to right, top to bottom.
1. Bandits: a crude notched axe crossed with a torch.
2. Beasts: a wolf skull in profile with bared fangs.
3. Monsters: a heavy clawed hand gripping a broken tree trunk.
4. Undead: a cracked human skull with faint cold light in the sockets.
5. Vikings: a round shield with a horned dragon-head prow behind it.
6. Rival lord: a closed knight helm above two crossed lances with
   pennants.
7. Prestige reward: a laurel wreath around a small crown.
8. Commander reward: a raised gauntlet holding a commander's baton.
9. Building card reward: a rolled blueprint with a mason's hammer.
10. Boon reward: a small vial of glowing liquid with a leather cord.

MUST NOT. Transparent background, or flat #FF00FF. No text, no numbers,
no badges or circles under the icons, no shadows outside the object.

Output PNG.
```

### P4.4 — фон карты кампании

```
Draw a stylized campaign map background for a Slavic dark-fantasy game.

WHAT IT IS. A hand-drawn regional map on aged parchment: forests, hills,
a river running from the upper left to the lower right, a coastline on
the right edge, roads winding from left to right across the map.

STYLE. Muted parchment tones, ink linework, low saturation. It is a
BACKGROUND — settlement icons and route lines are drawn on top by the
game, so the map must stay calm and uncluttered in the middle band.

COMPOSITION. Horizontal 1920x1080. The central horizontal band, about
60 percent of the height, must be visually quiet: no large forests or
mountains there, only light texture. Decorative density belongs at the
top and bottom edges.

MUST NOT. No text, no place names, no compass rose with letters, no
grid, no existing settlement markers, no route lines.

Output PNG or JPG.
```

---

## Порядок

| Очередь | Что | Почему |
|---|---|---|
| 1 | **P1 обложка** | без неё нечего показывать вообще |
| 2 | **P2 логотип** (путь A) | ложится на обложку и в главное меню |
| 3 | **P4.1 значки владений** | карта кампании уже работает на заглушках |
| 4 | **P4.4 фон карты** | тот же экран |
| 5 | **P4.2 портреты** | командиры выдаются в награду, лица нужны |
| 6 | **P4.3 значки фракций** | сейчас показываются словами, это терпимо |

P3 делается последним и, скорее всего, мной — не нейросетью.

---

## Приёмка 6 сентября 2026

**Весь заказ P1–P4 закрыт с одного захода.** Пришло 13 файлов: десять
по первому кругу и три доработки.

### Что принято

| Заказ | Файл в `References` | Состояние |
|---|---|---|
| P1 обложка | `01_Promo/KeyArt_Clean` | принята |
| P3 сборка | `01_Promo/KeyArt_WithLogo` | принята |
| P2 эмблема | `01_Promo/Logo_Emblem` | принята |
| P2 логотип с текстом | `01_Promo/Logo_Text` | принят, буквы проверены |
| P4.1 значки владений | `05_Campaign/HoldingIcons` | принят **второй вариант** |
| P4.2 портреты | `05_Campaign/CommanderPortraits` | принят **второй вариант** |
| P4.3 фракции и награды | `05_Campaign/FactionAndRewardIcons` | принят, ещё не нарезан |
| P4.4 фон карты | `05_Campaign/MapBackground_Parchment` и `_Green` | два варианта, оба годны |

### Почему по значкам и портретам взят второй вариант

Сравнивал не в полном размере, а в рабочем — как с кнопками.

**Значки владений.** У первого варианта больше деталей, но в 64 пикселя
они съедаются. У второго проще формы и чётче различия: порт узнаётся
по лодке, лагерь по шатрам, замок по двум башням. Для карты, где игрок
выбирает маршрут по силуэту, это решает.

**Портреты.** Первый вариант живописнее, но выпадает из стиля: у нас
плоский интерфейс поверх низкополигональной сцены. Второй — плоский,
с толстым контуром, ровно как просило ТЗ и как сделано в Bad North.

Отклонённые лежат рядом с суффиксом `_alt` — на случай, если решение
переиграется.

### Что нарезано и подключено

В `Assets/ART/Sprites/Campaign` лежат 12 спрайтов:

- `holding_village`, `holding_town`, `holding_port`, `holding_barony`,
  `holding_castle`, `holding_warcamp` — фон снят, вписаны в 256×256;
- `portrait_veteran`, `portrait_shieldmaiden`, `portrait_archer`,
  `portrait_smith`, `portrait_hooded`, `portrait_youth` — подрезаны
  до квадрата, 256×256, цветной фон оставлен как часть портрета.

Всё импортировано как `Sprite` через API импортёра, а не правкой `.meta`.

**Разложено по ассетам:** семь владений получили значки по своему типу,
четыре командира — портреты. Экран карты показывает значок в карточке.

Проверено в игре: на развилке две карточки 320×320, в каждой значок,
название, тип, число волн, фракция и награда — всё до входа во владение.

### Что осталось из промо

- `FactionAndRewardIcons` — десять значков, ещё не нарезаны: на карте
  фракции пока выводятся словами, это терпимо.
- Фон карты — не подключён: экран пока рисует ровную заливку. Подключать
  вместе с настоящей раскладкой узлов, а не списком карточек.
- Обложка и логотип — лежат готовые, ставить некуда до страницы Steam.
