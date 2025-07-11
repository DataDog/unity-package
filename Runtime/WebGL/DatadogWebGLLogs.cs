// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Datadog.Unity.Logs;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Datadog.Unity.WebGL
{
    public class DatadogWebGLLogs
    {
        public void Init(DatadogConfigurationOptions options)
        {
            var logConfig = new LoggingInitOptions()
            {
                clientToken = options.ClientToken,
                env = options.Env,
                proxy = string.IsNullOrEmpty(options.CustomEndpoint) ? null : options.CustomEndpoint,
                site = options.Site.ToWebValue(),
                service = options.ServiceName,
                // TODO: Version
            };

            var jsConfig = JsonConvert.SerializeObject(logConfig, new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            DDLogs_InitLogs(jsConfig);
        }

        public DatadogWebGLLogger CreateLogger(DatadogLoggingOptions options)
        {
            var loggerId = Guid.NewGuid().ToString();
            var logger = new DatadogWebGLLogger(options.RemoteLogThreshold, options.RemoteSampleRate, loggerId);
            var webLoggerConfig = new LoggerConfiguration()
            {
                name = options.Name ?? "default",
                service = options.Service,
            };
            var jsonConfig = JsonConvert.SerializeObject(webLoggerConfig, new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
            });
            DDLogs_CreateLogger(loggerId, jsonConfig);
            return logger;
        }

        private class LoggingInitOptions
        {
            public string clientToken;
            public string env;
            public string proxy;
            public string site;
            public string service;
            public string version;
        }

        private class LoggerConfiguration
        {
            public string name;
            public string service;
        }

        [DllImport("__Internal")]
        private static extern void DDLogs_InitLogs(string jsonConfiguration);

        [DllImport("__Internal")]
        private static extern void DDLogs_CreateLogger(string loggerId, string configuration);
    }
}
