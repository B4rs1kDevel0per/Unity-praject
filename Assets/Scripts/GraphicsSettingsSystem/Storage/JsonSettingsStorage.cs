using System;
using System.IO;
using GameSettings.Data;
using UnityEngine;

namespace GameSettings.Storage
{
    /// <summary>
    /// Хранит настройки в JSON-файле внутри Application.persistentDataPath.
    /// Плюс перед этой реализации: файл легко открыть/отредактировать вручную,
    /// удобно для дебага и для нескольких профилей (можно передать своё имя файла).
    /// </summary>
    public class JsonSettingsStorage : ISettingsStorage
    {
        private readonly string _filePath;

        public JsonSettingsStorage(string fileName = "graphics_settings.json")
        {
            _filePath = Path.Combine(Application.persistentDataPath, fileName);
        }

        public bool Exists()
        {
            return File.Exists(_filePath);
        }

        public GraphicSettingsData Load()
        {
            if (!Exists())
                return null;

            try
            {
                string json = File.ReadAllText(_filePath);
                var data = JsonUtility.FromJson<GraphicSettingsData>(json);
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[JsonSettingsStorage] Не удалось прочитать файл настроек: {e.Message}");
                return null;
            }
        }

        public void Save(GraphicSettingsData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonSettingsStorage] Не удалось сохранить настройки: {e.Message}");
            }
        }

        public void Delete()
        {
            if (Exists())
                File.Delete(_filePath);
        }
    }
}
