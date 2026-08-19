# Пошаговый гайд по сборке в Unity Editor

## 1. Импорт скриптов
Скопируйте папки Data/Storage/Core/UI в `Assets/Scripts/GraphicsSettings/` (сохраняя структуру).
Дайте Unity скомпилироваться, ошибок быть не должно (кроме отсутствия URP — см. README).

## 2. Объект-менеджер (не UI)
1. Create Empty -> назовите `SettingsSystem`.
2. Add Component -> `Graphics Applier`.
3. Add Component -> `Settings Manager`.
4. В инспекторе `Settings Manager` перетащите в поле `Applier` этот же объект
   (или оставьте пустым — компонент найдёт `GraphicsApplier` на себе автоматически).
5. В `Graphics Applier`:
   - `Target Camera` -> перетащите Main Camera.
   - `Global Volume` -> см. пункт 3 ниже.
   - `Ambient Occlusion Feature` -> см. пункт 3 ниже.
6. Этот объект не удаляйте между сценами — `SettingsManager` сам вызывает `DontDestroyOnLoad`.

## 3. Volume для постобработки (Bloom/Motion Blur/AO)
1. GameObject -> Volume -> Global Volume (создаст объект с компонентом `Volume`, `Is Global = true`).
2. В инспекторе Volume создайте новый `Profile` (кнопка New).
3. Add Override -> Post-processing -> Bloom (включите галочку Intensity).
4. Add Override -> Post-processing -> Motion Blur.
5. Перетащите этот Volume в поле `Global Volume` компонента `GraphicsApplier`.
6. Для AO: откройте ваш `Universal Renderer Data` asset (Project window, обычно
   `Assets/Settings/URP-*-Renderer.asset`), нажмите Add Renderer Feature ->
   Screen Space Ambient Occlusion (если её ещё нет).
7. Разверните стрелку слева от Renderer Data asset в Project window — там появится
   суб-ассет фичи. Перетащите его в поле `Ambient Occlusion Feature` компонента `GraphicsApplier`.

Если вы не используете URP — просто пропустите пункт 3 целиком, Bloom/AO/Motion Blur
работать не будут, но остальная система (экран, тени, текстуры, FPS) останется рабочей.

## 4. Создание UI-панели настроек
1. GameObject -> UI -> Canvas (если Canvas ещё нет).
2. Внутри Canvas: Create Empty -> `SettingsPanel` (это будет корневая панель,
   которую можно скрывать/показывать). Добавьте на неё Image (полупрозрачный фон) при желании.
3. Внутри `SettingsPanel` создавайте секции UI -> Panel для каждой группы:
   `Section_Screen`, `Section_Quality`, `Section_Shadows`, `Section_PostFX`.

### 4.1 Секция "Экран"
- UI -> Dropdown - Legacy -> назовите `ResolutionDropdown`.
- UI -> Dropdown - Legacy -> `RefreshRateDropdown`.
- UI -> Dropdown - Legacy -> `ScreenModeDropdown`.
- UI -> Toggle -> `VSyncToggle`.
- UI -> Dropdown - Legacy -> `FpsLimitDropdown`.

(Используйте обычный `Dropdown`, а не TMP-версию, чтобы код из архива работал без изменений.
Если хотите TMP — замените `UnityEngine.UI.Dropdown` на `TMPro.TMP_Dropdown` в соответствующих
скриптах, остальной API идентичен.)

### 4.2 Секция "Качество"
- UI -> Dropdown -> `TextureQualityDropdown`.
- UI -> Dropdown -> `AnisotropicDropdown`.
- UI -> Dropdown -> `AntiAliasingDropdown`.
- UI -> Slider -> `LodBiasSlider` (Min Value = 0.25, Max Value = 2, Whole Numbers = off).
- UI -> Text (опционально) -> `LodBiasValueLabel`.

### 4.3 Секция "Тени"
- UI -> Toggle -> `ShadowsEnabledToggle`.
- UI -> Dropdown -> `ShadowResolutionDropdown`.
- UI -> Slider -> `ShadowDistanceSlider` (Min 10, Max 300).
- UI -> Text (опц.) -> `ShadowDistanceValueLabel`.
- UI -> Dropdown -> `ShadowCascadesDropdown`.
- Create Empty с компонентом `Canvas Group` -> `ShadowsDependentGroup`, вложите в неё
  Dropdown/Slider/Dropdown из пунктов выше (кроме самого Toggle) — это позволит серому
  затемнению и блокировке при выключенных тенях.

### 4.4 Секция "Постобработка"
- UI -> Toggle -> `BloomToggle`, UI -> Slider -> `BloomIntensitySlider` (0..5).
- UI -> Toggle -> `AoToggle`, UI -> Slider -> `AoIntensitySlider` (0..1).
- UI -> Toggle -> `MotionBlurToggle`, UI -> Slider -> `MotionBlurIntensitySlider` (0..1).

### 4.5 Кнопки управления
Внизу `SettingsPanel`: UI -> Button для каждой:
`ApplyButton` ("Применить"), `CancelButton` ("Отмена"), `ResetButton` ("Сброс"),
`CloseButton` ("Закрыть").

## 5. Развешивание скриптов-биндеров
Каждый биндер вешается на объект соответствующей секции (или на сам Canvas — не критично,
главное перетащить правильные ссылки).

| Скрипт | Куда повесить | Что перетащить в поля |
|---|---|---|
| `ResolutionSettingsBinder` | Section_Screen | ResolutionDropdown, RefreshRateDropdown |
| `ScreenModeBinder` | Section_Screen | ScreenModeDropdown |
| `VSyncBinder` | Section_Screen | VSyncToggle |
| `FpsLimitBinder` | Section_Screen | FpsLimitDropdown |
| `TextureQualityBinder` | Section_Quality | TextureQualityDropdown |
| `AnisotropicFilteringBinder` | Section_Quality | AnisotropicDropdown |
| `AntiAliasingBinder` | Section_Quality | AntiAliasingDropdown |
| `LodBiasBinder` | Section_Quality | LodBiasSlider, LodBiasValueLabel |
| `ShadowsSettingsBinder` | Section_Shadows | все Shadow*-элементы + ShadowsDependentGroup |
| `PostProcessingBinder` | Section_PostFX | Bloom/Ao/MotionBlur Toggle+Slider (6 полей) |
| `SettingsPanelController` | SettingsPanel (корень) | Apply/Cancel/Reset/Close кнопки, сам SettingsPanel |

**ВАЖНО:** Ничего вручную подписывать через инспектор `OnValueChanged()` НЕ нужно —
все `AddListener(...)` уже прописаны в коде каждого биндера в `OnEnable()`. Просто
перетащите нужные UI-компоненты в соответствующие публичные поля скрипта в инспекторе,
и всё заработает автоматически при запуске сцены.

## 6. Проверка
1. Нажмите Play.
2. Откройте `SettingsPanel` (например, повесив на кнопку в главном меню вызов
   `SettingsPanelController.Open()`).
3. Меняйте Dropdown/Slider/Toggle — изменения должны применяться сразу же (live-preview),
   т.к. вызывается `applier.ApplyX()` при каждом изменении.
4. Нажмите `ApplyButton` — настройки сохранятся в
   `%userprofile%\AppData\LocalLow\<CompanyName>\<ProductName>\graphics_settings.json`
   (Windows) или в аналогичной `persistentDataPath` на других платформах.
5. Измените что-то и нажмите `CancelButton` — значения должны откатиться к последнему
   сохранённому состоянию.
6. Нажмите `ResetButton` — вернутся заводские значения (не сохраняются, пока не нажмёте Apply).

## 7. Частые ошибки
- **NullReferenceException на SettingsManager.Instance** — убедитесь, что объект
  `SettingsSystem` есть на самой первой загружаемой сцене и не удаляется раньше времени.
- **Postprocessing не действует** — проверьте, что `Volume.isGlobal = true` и в URP Asset
  включён Post Processing (URP Renderer Data -> Rendering -> Post Processing).
- **AntiAliasing (FXAA/SMAA/TAA) не виден** — на камере должен быть компонент
  `Universal Additional Camera Data` (добавляется автоматически при использовании URP)
  и включён Post Processing на самой камере (Camera -> Rendering -> Post Processing = on).
