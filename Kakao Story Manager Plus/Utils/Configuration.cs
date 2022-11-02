using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace KSMP.Utils;

public class Configuration
{
    private readonly static string BasePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    private const string ConfigurationDirectoryName = "KSMP";
    private const string ConfigurationFileName = "settings.json";

    private readonly static string ConfigurationDirectoryPath= Path.Combine(BasePath, ConfigurationDirectoryName);
    private readonly static string ConfigurationFilePath= Path.Combine(ConfigurationDirectoryPath, ConfigurationFileName);

    private static void ValidateConfigurationFile()
    {
        if (!Directory.Exists(ConfigurationDirectoryPath))
            Directory.CreateDirectory(ConfigurationDirectoryPath);
        if (!File.Exists(ConfigurationFilePath))
        {
            using var stream = File.Create(ConfigurationFilePath);
            stream.Close();
        }
    }

    private static string GetConfigurationFileContentString()
    {
        ValidateConfigurationFile();
        var content = File.ReadAllText(ConfigurationFilePath).Trim();
        if (string.IsNullOrEmpty(content)) content = "{}";
        return content;
    }
    private static Dictionary<string, object> GetConfigurationFileContent()
    {
        var configurationFileContentString = GetConfigurationFileContentString();
        var convertedFileContent = JsonConvert.DeserializeObject<Dictionary<string, object>>(configurationFileContentString);
        return convertedFileContent;
    }

    public static void SetValue(string key, object value)
    {
        var convertedFileContent = GetConfigurationFileContent();
        if (convertedFileContent.ContainsKey(key)) convertedFileContent[key] = value;
        else convertedFileContent.Add(key, value);
        var configurationFileContentStringToSave = JsonConvert.SerializeObject(convertedFileContent);
        File.WriteAllText(ConfigurationFilePath, configurationFileContentStringToSave);
    }
    public static object GetValue(string key)
    {
        var convertedFileContent = GetConfigurationFileContent();
        if (convertedFileContent.ContainsKey(key)) return convertedFileContent[key];
        return null;
    }
}
