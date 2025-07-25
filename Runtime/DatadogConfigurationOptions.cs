﻿// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2023-Present Datadog, Inc.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Datadog.Unity
{
    public enum DatadogSite
    {
        [InspectorName("us1")]
        Us1,
        [InspectorName("us3")]
        Us3,
        [InspectorName("us5")]
        Us5,
        [InspectorName("eu1")]
        Eu1,
        [InspectorName("us1Fed")]
        Us1Fed,
        [InspectorName("ap1")]
        Ap1,
        [InspectorName("ap2")]
        Ap2,
    }

    /// <summary>
    /// Defines whether the trace context should be injected into all requests or
    /// only into requests that are sampled in.
    /// </summary>
    public enum TraceContextInjection
    {
        /// <summary>
        /// Injects trace context into all requests regardless of the sampling decision.
        /// </summary>
        [InspectorName("All")]
        All,

        /// <summary>
        /// Injects trace context only into sampled requests.
        /// </summary>
        [InspectorName("Only Sampled")]
        Sampled,
    }

    [Flags]
    public enum TracingHeaderType
    {
        /// <summary>
        /// Do not add tracing headers
        /// </summary>
        None,

        /// <summary>
        /// Datadog's 'x-datadog-*' header
        /// </summary>
        Datadog = 1 << 1,

        /// <summary>
        /// Open telemetry B3 Single header
        /// </summary>
        B3 = 1 << 2,

        /// <summary>
        /// Open telemetry B3 multiple headers
        /// </summary>
        B3Multi = 1 << 3,

        /// <summary>
        /// W3C Trace Context header
        /// </summary>
        TraceContext = 1 << 4,
    }

    /// <summary>
    /// Defines the policy when batching data together.
    /// Smaller batches will means smaller but more network requests,
    /// whereas larger batches will mean fewer but larger network requests.
    /// </summary>
    public enum BatchSize
    {
        Small,
        Medium,
        Large,
    }

    /// <summary>
    /// Defines the frequency at which batch uploads are tried.
    /// </summary>
    public enum UploadFrequency
    {
        Frequent,
        Average,
        Rare,
    }

    /// <summary>
    /// Defines the maximum amount of batches processed sequentially without a delay within one reading/uploading cycle.
    /// High level will mean that more data will be sent in a single upload cycle but more CPU and memory
    /// will be used to process the data.
    /// Low level will mean that less data will be sent in a single upload cycle but less CPU and memory
    /// will be used to process the data.
    /// </summary>
    public enum BatchProcessingLevel
    {
        Low,
        Medium,
        High,
    }

    /// <summary>
    /// The Consent enum class providing the possible values for the Data Tracking Consent flag.
    /// </summary>
    public enum TrackingConsent
    {
        /// <summary>
        /// The permission to persist and dispatch data to the Datadog Endpoints was granted.
        /// Any previously stored pending data will be marked as ready for sent.
        /// </summary>
        Granted,

        /// <summary>
        /// Any previously stored pending data will be deleted and any Log, Rum, Trace event will
        /// be dropped from now on without persisting it in any way.
        /// </summary>
        NotGranted,

        /// <summary>
        /// Any Log, Rum, Trace event will be persisted in a special location and will be pending there
        /// until we will receive one of the [TrackingConsent.Granted] or
        /// [TrackingConsent.NotGranted] flags.
        /// Based on the value of the consent flag we will decide what to do
        /// with the pending stored data.
        /// </summary>
        Pending,
    }

    /// <summary>
    /// Options for detecting non-fatal ANRs on Android. The Android SDK can make a decision about whether to track non-fatal
    /// ANRs based on the version of Android.
    /// </summary>
    public enum NonFatalAnrDetectionOption
    {
        /// <summary>
        /// Use the default behavior for the version of Android. On Android 30+, the default is set to disabled
        /// because it would create too much noise over fatal ANRs. On Android 29 and below, however, the
        /// reporting of non-fatal ANRs is enabled by default, as fatal ANRs cannot be reported on those versions.
        /// </summary>
        [InspectorName("SDK Default")]
        SdkDefault,

        /// <summary>
        /// Always enable non-fatal ANR tracking, regardless of Android version.
        /// </summary>
        Enabled,

        /// <summary>
        /// Always disable non-fatal ANR tracking, regardless of Android version. (This is the Unity default)
        /// </summary>
        Disabled,
    }

    /// <summary>
    /// The frequency at which Datadog samples mobile vitals (FPS, CPU Usage, Memory Usage).
    /// </summary>
    public enum VitalsUpdateFrequency
    {
        /// <summary>
        /// Disable mobile vitals collection.
        /// </summary>
        None,

        /// <summary>
        /// Collect mobile vitals every ~100ms.
        /// </summary>
        Frequent,

        /// <summary>
        /// Collect mobile vitals every ~500ms.
        /// </summary>
        Average,

        /// <summary>
        /// Collect mobile vitals every ~1000ms.
        /// </summary>
        Rare,
    }

    [Serializable]
    public class FirstPartyHostOption
    {
        public string Host;
        public TracingHeaderType TracingHeaderType;

        public FirstPartyHostOption()
        {
            Host = "";
            TracingHeaderType = TracingHeaderType.Datadog | TracingHeaderType.TraceContext;
        }

        public FirstPartyHostOption(string host, TracingHeaderType tracingHeaderType)
        {
            Host = host;
            TracingHeaderType = tracingHeaderType;
        }
    }

    public class DatadogConfigurationOptions : ScriptableObject
    {
        public static readonly string DefaultDatadogSettingsPath = $"Assets/Resources/{_DefaultDatadogSettingsFileName}.asset";

        // Field should be private
#pragma warning disable SA1401

        // Base Config
        public bool Enabled;
        public CoreLoggerLevel SdkVerbosity = CoreLoggerLevel.Warn;
        public bool OutputSymbols;
        public bool PerformNativeStackMapping = true;
        public string ClientToken;
        public DatadogSite Site;
        public string Env;
        public string ServiceName;
        public string CustomEndpoint;
        public BatchSize BatchSize;
        public UploadFrequency UploadFrequency;
        public BatchProcessingLevel BatchProcessingLevel = BatchProcessingLevel.Medium;
        public bool CrashReportingEnabled = true;

        // Logging
        public bool ForwardUnityLogs;
        public LogType RemoteLogThreshold;

        // RUM
        public bool RumEnabled;
        public string RumApplicationId;
        public bool AutomaticSceneTracking;
        public VitalsUpdateFrequency VitalsUpdateFrequency = VitalsUpdateFrequency.Average;
        public float SessionSampleRate = 100.0f;
        public float TraceSampleRate = 20.0f;
        public TraceContextInjection TraceContextInjection = TraceContextInjection.All;
        public List<FirstPartyHostOption> FirstPartyHosts = new ();
        public NonFatalAnrDetectionOption TrackNonFatalAnrs = NonFatalAnrDetectionOption.Disabled;
        public bool TrackNonFatalAppHangs = false;
        public float NonFatalAppHangThreshold = 0.25f;

        // Advanced RUM
        public float TelemetrySampleRate;

        private const string _DefaultDatadogSettingsFileName = "DatadogSettings";

        public static DatadogConfigurationOptions Load()
        {
            return Resources.Load<DatadogConfigurationOptions>(_DefaultDatadogSettingsFileName);
        }
    }
}
