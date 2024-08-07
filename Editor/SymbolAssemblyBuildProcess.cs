// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2023-Present Datadog, Inc.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.Device;
using Debug = UnityEngine.Debug;

namespace Datadog.Unity.Editor
{
    public class SymbolAssemblyBuildProcess : IPostprocessBuildWithReport, IPostGenerateGradleAndroidProject
    {
        internal const string IosDatadogSymbolsDir = "datadogSymbols";
        internal const string AndroidSymbolsDir = "symbols";
        internal const string AndroidLineNumberMappingsOutputPath = "symbols";

        // Relative to the output directory
        internal const string IosLineNumberMappingsLocation =
            "Il2CppOutputProject/Source/il2cppOutput/Symbols/LineNumberMappings.json";

        // Relative to the gradle output directory
        private static readonly List<string> AndroidLineNumberMappingsLocations = new ()
        {
            "../../IL2CppBackup/il2cppOutput/Symbols/LineNumberMappings.json",
            "src/main/il2CppOutputProject/Source/il2cppOutput/Symbols/LineNumberMappings.json",
        };


        // Make sure this is the last possible thing that's run
        public int callbackOrder => int.MaxValue;

        private IBuildFileSystemProxy _fileSystemProxy = new DefaultBuildFileSystemProxy();

        internal IBuildFileSystemProxy fileSystemProxy
        {
            get => _fileSystemProxy;
            set => _fileSystemProxy = value;
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            var options = DatadogConfigurationOptionsExtensions.GetOrCreate();
            if (report.summary.platformGroup == BuildTargetGroup.iOS)
            {
                WriteBuildId(options, report.summary.platformGroup, report.summary.guid.ToString(), report.summary.outputPath);
            }

            CopySymbols(options, report.summary.platformGroup, report.summary.guid.ToString(),
                    report.summary.outputPath);
        }

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var options = DatadogConfigurationOptionsExtensions.GetOrCreate();
            // Since Unity doesn't give us a copy of a buildGUID at this stage, generate our own.
            var buildGuid = Guid.NewGuid().ToString();
            WriteBuildId(options, BuildTargetGroup.Android, buildGuid, path);
            AndroidCopyMappingFile(options, path);
        }

        internal void AndroidCopyMappingFile(DatadogConfigurationOptions options, string path)
        {
            if (!options.Enabled || !options.OutputSymbols)
            {
                return;
            }

            bool foundFile = false;
            try
            {
                // Find the line number mapping file and copy it to the proper location
                // for the Datadog Gradle plugin to find
                foreach (var mappingLocation in AndroidLineNumberMappingsLocations)
                {
                    var mappingsSrcPath = Path.Join(path, mappingLocation);
                    if (_fileSystemProxy.FileExists(mappingsSrcPath))
                    {
                        var mappingsDestPath = Path.Join(path, AndroidLineNumberMappingsOutputPath);
                        if (!_fileSystemProxy.DirectoryExists(mappingsDestPath))
                        {
                            _fileSystemProxy.CreateDirectory(mappingsDestPath);
                        }

                        Debug.Log("Copying IL2CPP mappings file...");
                        _fileSystemProxy.CopyFile(mappingsSrcPath, mappingsDestPath);
                        foundFile = true;
                        break;
                    }
                }

                if (!foundFile)
                {
                    Debug.LogWarning("Could not find IL2CPP mappings file.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to copy IL2CPP mappings file");
                Debug.LogException(e);
            }
        }

        internal void WriteBuildId(DatadogConfigurationOptions options,
            BuildTargetGroup platformGroup,
            string buildGuid,
            string outputPath)
        {
            if (platformGroup is not (BuildTargetGroup.Android or BuildTargetGroup.iOS))
            {
                // Only copy symbols for Android and iOS
                return;
            }

            if (options.Enabled && options.OutputSymbols)
            {
                var outputDir = platformGroup switch
                {
                    BuildTargetGroup.Android => AndroidSymbolsDir,
                    BuildTargetGroup.iOS => IosDatadogSymbolsDir,
                    _ => ""
                };

                var symbolsDir = Path.Join(outputPath, outputDir);
                if (!_fileSystemProxy.DirectoryExists(symbolsDir))
                {
                    _fileSystemProxy.CreateDirectory(symbolsDir);
                }

                var buildIdPath = Path.Join(symbolsDir, "build_id");
                _fileSystemProxy.WriteAllText(buildIdPath, buildGuid);

                if (platformGroup == BuildTargetGroup.Android)
                {
                    // Write the build id to the Android assets directory
                    var androidAssetsDir = Path.Join(outputPath, "src/main/assets");
                    var androidBuildIdPath = Path.Join(androidAssetsDir, "datadog.buildId");
                    _fileSystemProxy.WriteAllText(androidBuildIdPath, buildGuid);
                }
            }
        }

        internal void CopySymbols(DatadogConfigurationOptions options, BuildTargetGroup platformGroup, string buildGuid, string outputPath)
        {
            if (platformGroup is not (BuildTargetGroup.Android or BuildTargetGroup.iOS))
            {
                // Only copy symbols for Android and iOS
                return;
            }

            if (options.Enabled && options.OutputSymbols)
            {
                switch (platformGroup)
                {
                    case BuildTargetGroup.iOS:
                        CopyIosSymbolFiles(outputPath);
                        break;
                    default:
                        break;
                }
            }
        }

        private void CopyIosSymbolFiles(string outputPath)
        {
            var mappingsSrcPath = Path.Join(outputPath, IosLineNumberMappingsLocation);
            var mappingsDestPath = Path.Join(outputPath, IosDatadogSymbolsDir, "LineNumberMappings.json");
            if (_fileSystemProxy.FileExists(mappingsSrcPath))
            {
                Debug.Log("Copying IL2CPP mappings file...");
                if (_fileSystemProxy.FileExists(mappingsDestPath))
                {
                    _fileSystemProxy.DeleteFile(mappingsDestPath);
                }
                _fileSystemProxy.CopyFile(mappingsSrcPath, mappingsDestPath);
            }
            else
            {
                Debug.LogWarning("Could not find iOS IL2CPP mappings file.");
            }
        }
    }
}
