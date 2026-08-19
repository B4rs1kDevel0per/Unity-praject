# Graphics Settings System (Unity, C#)

Модульная система настроек графики, разбитая на 4 независимых слоя:

```
Data/     -> GraphicSettingsData.cs           (только данные, никакой логики)
Storage/  -> ISettingsStorage.cs
             JsonSettingsStorage.cs           (сохранение в JSON-файл)
             PlayerPrefsSettingsStorage.cs     (альтернатива через PlayerPrefs)
Core/     -> GraphicsApplier.cs               (применение к QualitySettings/Screen/URP)
             SettingsManager.cs               (синглтон-фасад для UI)
UI/       -> EnumDropdownHelper.cs
             ResolutionSettingsBinder.cs
             ScreenModeBinder.cs
             VSyncBinder.cs
             FpsLimitBinder.cs
             TextureQualityBinder.cs
             AnisotropicFilteringBinder.cs
             AntiAliasingBinder.cs
             ShadowsSettingsBinder.cs
             LodBiasBinder.cs
             PostProcessingBinder.cs
             SettingsPanelController.cs
```

Подробный пошаговый гайд по сборке в Unity Editor — в основном ответе чата
(или см. SETUP_GUIDE.md рядом с этим файлом).

Требования:
- Unity 2022.2+ рекомендуется (используется Screen.SetResolution с RefreshRate и
  QualitySettings.globalTextureMipmapLimit). Для более старых версий см. комментарии
  в коде — указаны фоллбэки.
- Universal Render Pipeline (URP) для блоков AntiAliasing/Shadows(URP asset)/PostProcessing.
  Если используете Built-in RP — просто уберите/закомментируйте помеченные [URP] участки
  в GraphicsApplier.cs, остальная система продолжит работать.
