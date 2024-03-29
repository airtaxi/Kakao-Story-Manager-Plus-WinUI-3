﻿using Newtonsoft.Json;
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
    
    private static Dictionary<string, object> s_configuration;

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
        return convertedFileContent ?? new();
    }

    public static void SetValue(string key, object value)
    {
        if (s_configuration == null) s_configuration = GetConfigurationFileContent();
        if (s_configuration.ContainsKey(key)) s_configuration[key] = value;
        else s_configuration.Add(key, value);
        var configurationFileContentStringToSave = JsonConvert.SerializeObject(s_configuration);
        File.WriteAllText(ConfigurationFilePath, configurationFileContentStringToSave);
    }
    public static object GetValue(string key)
    {
        if (s_configuration == null) s_configuration = GetConfigurationFileContent();
        if (s_configuration.ContainsKey(key)) return s_configuration[key];
        return null;
    }
}
