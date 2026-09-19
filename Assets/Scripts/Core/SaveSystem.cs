using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Popayork.Core
{
    public static class SaveSystem
    {
        public static string SavePath
        {
            get { return Path.Combine(Application.persistentDataPath, GameConfig.SaveFileName); }
        }

        public static void Save(SaveData data)
        {
            File.WriteAllText(SavePath, ToJson(data));
        }

        public static SaveData Load()
        {
            if (!File.Exists(SavePath))
            {
                var fresh = SaveData.NewDefault();
                Save(fresh);
                return fresh;
            }

            SaveData data = FromJson(File.ReadAllText(SavePath));
            if (data == null || data.unlockedMissions == null)
            {
                data = SaveData.NewDefault();
            }
            return data;
        }

        public static SaveData LoadFreshWithoutWriting()
        {
            if (!File.Exists(SavePath))
            {
                return SaveData.NewDefault();
            }
            SaveData data = FromJson(File.ReadAllText(SavePath));
            if (data == null || data.unlockedMissions == null)
            {
                return SaveData.NewDefault();
            }
            return data;
        }

        internal static string ToJson(SaveData data)
        {
            var sb = new StringBuilder(256);
            sb.Append("{\"version\":");
            sb.Append(data.version.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"masterVolume\":");
            sb.Append(data.masterVolume.ToString("R", CultureInfo.InvariantCulture));
            sb.Append(",\"mouseSensitivity\":");
            sb.Append(data.mouseSensitivity.ToString("R", CultureInfo.InvariantCulture));
            sb.Append(",\"unlockedMissions\":");
            AppendStringList(sb, data.unlockedMissions);
            sb.Append(",\"completedMissions\":");
            AppendStringList(sb, data.completedMissions);
            sb.Append('}');
            return sb.ToString();
        }

        internal static SaveData FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }
            var data = new SaveData();
            data.unlockedMissions = new List<string>();
            data.completedMissions = new List<string>();
            data.version = ReadInt(json, "\"version\":", 1);
            data.masterVolume = ReadFloat(json, "\"masterVolume\":", GameConfig.DefaultVolume);
            data.mouseSensitivity = ReadFloat(json, "\"mouseSensitivity\":", GameConfig.DefaultSensitivity);
            ReadStringList(json, "\"unlockedMissions\":", data.unlockedMissions);
            ReadStringList(json, "\"completedMissions\":", data.completedMissions);
            return data;
        }

        private static void AppendStringList(StringBuilder sb, List<string> list)
        {
            sb.Append('[');
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }
                    sb.Append('\"');
                    sb.Append(list[i]);
                    sb.Append('\"');
                }
            }
            sb.Append(']');
        }

        private static int ReadInt(string json, string key, int fallback)
        {
            int idx = json.IndexOf(key);
            if (idx < 0)
            {
                return fallback;
            }
            idx += key.Length;
            int end = idx;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-'))
            {
                end++;
            }
            int value;
            if (int.TryParse(json.Substring(idx, end - idx), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return value;
            }
            return fallback;
        }

        private static float ReadFloat(string json, string key, float fallback)
        {
            int idx = json.IndexOf(key);
            if (idx < 0)
            {
                return fallback;
            }
            idx += key.Length;
            int end = idx;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-' || json[end] == '+' || json[end] == '.' || json[end] == 'e' || json[end] == 'E'))
            {
                end++;
            }
            float value;
            if (float.TryParse(json.Substring(idx, end - idx), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return value;
            }
            return fallback;
        }

        private static void ReadStringList(string json, string key, List<string> outList)
        {
            int idx = json.IndexOf(key);
            if (idx < 0)
            {
                return;
            }
            idx = json.IndexOf('[', idx);
            if (idx < 0)
            {
                return;
            }
            int end = json.IndexOf(']', idx);
            if (end < 0)
            {
                return;
            }
            int i = idx + 1;
            while (i < end)
            {
                int q1 = json.IndexOf('\"', i, end - i);
                if (q1 < 0)
                {
                    break;
                }
                int q2 = json.IndexOf('\"', q1 + 1, end - (q1 + 1));
                if (q2 < 0)
                {
                    break;
                }
                outList.Add(json.Substring(q1 + 1, q2 - q1 - 1));
                i = q2 + 1;
            }
        }
    }
}
