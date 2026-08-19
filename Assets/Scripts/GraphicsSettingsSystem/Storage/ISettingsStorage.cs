using GameSettings.Data;

namespace GameSettings.Storage
{
    /// <summary>
    /// Абстракция слоя хранения. Позволяет подменить реализацию
    /// (JSON-файл, PlayerPrefs, облачное сохранение и т.д.), не трогая остальную систему.
    /// </summary>
    public interface ISettingsStorage
    {
        /// <summary>Есть ли сохранённые настройки.</summary>
        bool Exists();

        /// <summary>Загрузить настройки. Возвращает null, если файла/записи нет или данные повреждены.</summary>
        GraphicSettingsData Load();

        /// <summary>Сохранить настройки.</summary>
        void Save(GraphicSettingsData data);

        /// <summary>Удалить сохранённые настройки (сброс).</summary>
        void Delete();
    }
}
