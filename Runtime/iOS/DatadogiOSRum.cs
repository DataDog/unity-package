// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2023-Present Datadog, Inc.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Datadog.Unity.Rum;
using Newtonsoft.Json;

namespace Datadog.Unity.iOS
{
    internal class DatadogiOSRum : IDdRum
    {
        private readonly DatadogiOSPlatform _platform;

        public DatadogiOSRum(DatadogiOSPlatform platform)
        {
            _platform = platform;
        }

        public void StartView(string key, string name = null, Dictionary<string, object> attributes = null)
        {
            attributes ??= new Dictionary<string, object>();
            var jsonAttributes = JsonConvert.SerializeObject(attributes);

            DatadogRumBridge.DatadogRum_StartView(key, name, jsonAttributes);
        }

        public void StopView(string key, Dictionary<string, object> attributes = null)
        {
            attributes ??= new Dictionary<string, object>();
            var jsonAttributes = JsonConvert.SerializeObject(attributes);

            DatadogRumBridge.DatadogRum_StopView(key, jsonAttributes);
        }

        public void AddAction(RumUserActionType type, string name, Dictionary<string, object> attributes = null)
        {
            attributes ??= new Dictionary<string, object>();
            var jsonAttributes = JsonConvert.SerializeObject(attributes);

            DatadogRumBridge.DatadogRum_AddAction(type.ToString(), name, jsonAttributes);
        }

        public void StartAction(RumUserActionType type, string name, Dictionary<string, object> attributes = null)
        {
            attributes ??= new Dictionary<string, object>();
            var jsonAttributes = JsonConvert.SerializeObject(attributes);

            DatadogRumBridge.DatadogRum_StartAction(type.ToString(), name, jsonAttributes);
        }

        public void StopAction(RumUserActionType type, string name, Dictionary<string, object> attributes = null)
        {
            attributes ??= new Dictionary<string, object>();
            var jsonAttributes = JsonConvert.SerializeObject(attributes);

            DatadogRumBridge.DatadogRum_StopAction(type.ToString(), name, jsonAttributes);
        }

        public void AddError(ErrorInfo error, RumErrorSource source, Dictionary<string, object> attributes = null)
        {
            string stackTrace = null;
            if (error != null)
            {
                var nativeStackTrace = error.Exception != null ? _platform.GetNativeStack(error.Exception) : null;
                if (nativeStackTrace != null)
                {
                    attributes = attributes == null ? new () : new (attributes);
                    attributes["_dd.error.include_binary_images"] = true;
                    attributes[DatadogSdk.ConfigKeys.ErrorSourceType] = "ios+il2cpp";
                    stackTrace = nativeStackTrace;
                }
                else
                {
                    stackTrace = error.StackTrace ?? string.Empty;
                }
            }

            attributes ??= new Dictionary<string, object>();

            var jsonAttributes = JsonConvert.SerializeObject(attributes);

            var errorType = error?.Type;
            var errorMessage = error?.Message;

            DatadogRumBridge.DatadogRum_AddError(errorMessage, source.ToString(), errorType, stackTrace, jsonAttributes);
        }

        public void StartResource(string key, RumHttpMethod httpMethod, string url, Dictionary<string, object> attributes = null)
        {
            attributes ??= new Dictionary<string, object>();
            var jsonAttributes = JsonConvert.SerializeObject(attributes);

            DatadogRumBridge.DatadogRum_StartResource(key, httpMethod.ToString(), url, jsonAttributes);
        }

        public void StopResource(string key, RumResourceType kind, int? statusCode = null, long? size = null,
            Dictionary<string, object> attributes = null)
        {
            attributes ??= new Dictionary<string, object>();
            var jsonAttributes = JsonConvert.SerializeObject(attributes);

            // Note - using -1 as a special value to mean null, as sending optionals to C from C# is... difficult
            DatadogRumBridge.DatadogRum_StopResource(key, kind.ToString(), statusCode ?? -1, size ?? -1, jsonAttributes);
        }

        public void StopResource(string key, Exception error, Dictionary<string, object> attributes = null)
        {
            StopResourceWithError(key, error, attributes);
        }

        public void StopResourceWithError(string key, string errorType, string errorMessage, Dictionary<string, object> attributes = null)
        {
            var error = new ErrorInfo(errorType, errorMessage);
            StopResourceWithError(key, error, attributes);
        }

        public void StopResourceWithError(string key, ErrorInfo error, Dictionary<string, object> attributes = null)
        {
            // NOTE: We don't pass a stack trace to dd-sdk-ios here because the API doesn't support it. (RUM-10504)
            // If `stopResourceWithError` were updated to accept a string stack trace parameter, we would want
            // to attempt to recover a native stack trace with IL2CPP before passing it along.
            attributes ??= new Dictionary<string, object>();
            var jsonAttributes = JsonConvert.SerializeObject(attributes);

            var errorType = error?.Type;
            var errorMessage = error?.Message;

            DatadogRumBridge.DatadogRum_StopResourceWithError(key, errorType, errorMessage, jsonAttributes);
        }

        public void AddAttribute(string key, object value)
        {
            var valueDict = new Dictionary<string, object>()
            {
                { "value", value },
            };
            var encodedValue = JsonConvert.SerializeObject(valueDict);
            DatadogRumBridge.DatadogRum_AddAttribute(key, encodedValue);
        }

        public void RemoveAttribute(string key)
        {
            DatadogRumBridge.DatadogRum_RemoveAttribute(key);
        }

        public void AddFeatureFlagEvaluation(string key, object value)
        {
            DatadogRumBridge.DatadogRum_AddFeatureFlagEvaluation(key, value?.ToString() ?? "null");
        }

        public void StopSession()
        {
            DatadogRumBridge.DatadogRum_StopSession();
        }
    }

    internal static class DatadogRumBridge
    {
        [DllImport("__Internal")]
        public static extern void DatadogRum_StartView(string key, string name, string attributes);

        [DllImport("__Internal")]
        public static extern void DatadogRum_StopView(string key, string attributes);

        [DllImport("__Internal")]
        public static extern void DatadogRum_AddAction(string type, string name, string attributes);

        [DllImport("__Internal")]
        public static extern void DatadogRum_StartAction(string type, string name, string attributes);

        [DllImport("__Internal")]
        public static extern void DatadogRum_StopAction(string type, string name, string attributes);

        [DllImport("__Internal")]
        public static extern void DatadogRum_AddError(string message, string source, string type, string stack, string attributes);

        [DllImport("__Internal")]
        public static extern void DatadogRum_AddAttribute(string key, string value);

        [DllImport("__Internal")]
        public static extern void DatadogRum_RemoveAttribute(string key);

        [DllImport("__Internal")]
        public static extern void DatadogRum_StartResource(string key, string httpMethod, string url, string attributes);

        [DllImport("__Internal")]
        public static extern void DatadogRum_StopResource(string key, string kind, int statusCode, long size, string attributes);

        [DllImport("__Internal")]
        public static extern void DatadogRum_StopResourceWithError(string key, string errorType,
            string errorMessage, string jsonAttributes);

        [DllImport("__Internal")]
        public static extern void DatadogRum_AddFeatureFlagEvaluation(string key, string value);

        [DllImport("__Internal")]
        public static extern void DatadogRum_StopSession();
    }
}
