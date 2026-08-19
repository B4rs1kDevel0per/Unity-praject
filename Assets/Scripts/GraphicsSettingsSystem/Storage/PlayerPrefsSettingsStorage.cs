using GameSettings.Data;
using UnityEngine;

namespace GameSettings.Storage
{
    /// <summary>
    /// Альтернативная реализация хранения — через PlayerPrefs (сериализованный JSON в одном ключе).
    /// Полезно на платформах, где запись в файловую систему нежелательна (например, WebGL),
    /// либо когда не нужен отдельный файл.
    /// </summary>
    public class PlayerPrefsSettingsStorage : ISettingsStorage
    {
        private readonly string _key;

        public PlayerPrefsSettingsStorage(string key = "GraphicsSettingsData")
        {
            _key = key;
        }

        public bool Exists()
        {
            return PlayerPrefs.HasKey(_key);
        }

        public GraphicSettingsData Load()
        {
            if (!Exists())
                return null;

            string json = PlayerPrefs.GetString(_key);
            if (string.IsNullOrEmpty(json))
                return null;

            return JsonUtility.FromJson<GraphicSettingsData>(json);
        }

        public void Save(GraphicSettingsData data)
        {
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(_key, json);
            PlayerPrefs.Save();
        }

        public void Delete()
        {
            if (Exists())
                PlayerPrefs.DeleteKey(_key);
        }
    }
}
