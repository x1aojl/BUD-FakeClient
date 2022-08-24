using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

public class CsvReader
{
    private const string CFG_DIR = "Configs";

    private const string CFG_SUFFIX = ".csv";

    public static Dictionary<string, T> ReadCsv<T>(string name)
    {
        Dictionary<string, T> dic = new Dictionary<string, T>();

        Type t = typeof(T);
        string[] keys = null;
        string[] lines = ReadAllLines(GetCsvPath(name));
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrEmpty(line) || line[0] == '#')
                continue;

            string[] paras = line.Split(',');
            if (keys == null)
            {
                keys = paras;
                continue;
            }

            T obj = Activator.CreateInstance<T>();
            for (int j = 0; j < paras.Length; j++)
            {
                PropertyInfo info = t.GetProperty(keys[j]);
                if (info == null)
                    continue;

                string value = paras[j];
                if (info.PropertyType.FullName.IndexOf("Boolean") > 0)
                    info.SetValue(obj, Convert.ChangeType(Convert.ToInt16(value), (Nullable.GetUnderlyingType(info.PropertyType) ?? info.PropertyType)), null);
                else
                    info.SetValue(obj, Convert.ChangeType(value, (Nullable.GetUnderlyingType(info.PropertyType) ?? info.PropertyType)), null);
            }

            string id = paras[0];
            if (dic.ContainsKey(id))
                continue;

            dic[id] = obj;
        }

        return dic;
    }

    private static string GetCsvPath(string name)
    {
        return string.Format("{0}/{1}{2}", CFG_DIR, name, CFG_SUFFIX);
    }

    private static string[] ReadAllLines(string path)
    {
        string url = Environment.CurrentDirectory + "/" + path;
        return File.ReadAllLines(url);
    }
}