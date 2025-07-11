// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2023-Present Datadog, Inc.

using System;
using System.Collections.Generic;
using Datadog.Unity.Worker;

namespace Datadog.Unity.Rum
{
    internal class DdWorkerProxyRum : IDdRum
    {
        private readonly DatadogWorker _worker;
        private readonly IDateProvider _dateProvider;

        public DdWorkerProxyRum(DatadogWorker worker, IDateProvider dateProvider = null)
        {
            _dateProvider = dateProvider ?? new DefaultDateProvider();
            _worker = worker;
        }

        public void StartView(string key, string name = null, Dictionary<string, object> attributes = null)
        {
            if (key == null)
            {
                LogNullWarning("StartView", "key");
                return;
            }

            InternalHelpers.Wrap("StartView",
                () =>
                {
                    _worker.AddMessage(DdRumProcessor.StartViewMessage.Create(_dateProvider.Now, key, name, attributes));
                });
        }

        public void StopView(string key, Dictionary<string, object> attributes = null)
        {
            if (key == null)
            {
                LogNullWarning("StopView", "key");
                return;
            }

            InternalHelpers.Wrap("StopView",
                () => { _worker.AddMessage(DdRumProcessor.StopViewMessage.Create(_dateProvider.Now, key, attributes)); });
        }

        public void AddAction(RumUserActionType type, string name, Dictionary<string, object> attributes = null)
        {
            if (name == null)
            {
                LogNullWarning("AddAction", "name");
                return;
            }

            InternalHelpers.Wrap("AddAction",
                () =>
                {
                    _worker.AddMessage(
                        DdRumProcessor.AddUserActionMessage.Create(_dateProvider.Now, type, name, attributes));
                });
        }

        public void StartAction(RumUserActionType type, string name, Dictionary<string, object> attributes = null)
        {
            if (name == null)
            {
                LogNullWarning("StartAction", "name");
                return;
            }

            InternalHelpers.Wrap("StartAction",
                () =>
                {
                    _worker.AddMessage(
                        DdRumProcessor.StartUserActionMessage.Create(_dateProvider.Now, type, name, attributes));
                });
        }

        public void StopAction(RumUserActionType type, string name, Dictionary<string, object> attributes = null)
        {
            if (name == null)
            {
                LogNullWarning("StopAction", "name");
                return;
            }

            InternalHelpers.Wrap("StopAction",
                () =>
                {
                    _worker.AddMessage(
                        DdRumProcessor.StopUserActionMessage.Create(_dateProvider.Now, type, name, attributes));
                });
        }

        public void AddError(ErrorInfo error, RumErrorSource source, Dictionary<string, object> attributes = null)
        {
            if (error == null)
            {
                LogNullWarning("AddError", "error");
                return;
            }

            InternalHelpers.Wrap("StopView",
                () =>
                {
                    _worker.AddMessage(
                        DdRumProcessor.AddErrorMessage.Create(_dateProvider.Now, error, source, attributes));
                });
        }

        public void StartResource(string key, RumHttpMethod httpMethod, string url,
            Dictionary<string, object> attributes = null)
        {
            if (key == null)
            {
                LogNullWarning("StartResource", "key");
                return;
            }

            if (url == null)
            {
                LogNullWarning("StartResource", "url");
                return;
            }

            InternalHelpers.Wrap("StartResource",
                () =>
                {
                    _worker.AddMessage(
                        DdRumProcessor.StartResourceLoadingMessage.Create(_dateProvider.Now, key, httpMethod, url,
                            attributes));
                });
        }

        public void StopResource(string key, RumResourceType kind, int? statusCode = null, long? size = null,
            Dictionary<string, object> attributes = null)
        {
            if (key == null)
            {
                LogNullWarning("StopResource", "key");
                return;
            }

            InternalHelpers.Wrap("StopResource",
                () =>
                {
                    _worker.AddMessage(
                        DdRumProcessor.StopResourceLoadingMessage.Create(_dateProvider.Now, key, kind, statusCode, size,
                            attributes));
                });
        }

        public void StopResourceWithError(string key, string errorType, string errorMessage,
            Dictionary<string, object> attributes = null)
        {
            string stackTrace = null;
            StopResourceWithError(key, new ErrorInfo(errorType, errorMessage, stackTrace));
        }

        public void StopResource(string key, Exception error, Dictionary<string, object> attributes = null)
        {
            StopResourceWithError(key, error, attributes);
        }

        public void StopResourceWithError(string key, ErrorInfo error, Dictionary<string, object> attributes = null)
        {
            if (key == null)
            {
                LogNullWarning("StopResourceWithError", "key");
                return;
            }

            InternalHelpers.Wrap("StopResourceWithError",
                () =>
                {
                    _worker.AddMessage(DdRumProcessor.StopResourceLoadingWithErrorMessage.Create(
                        _dateProvider.Now, key, error, attributes));
                });
        }

        public void AddAttribute(string key, object value)
        {
            if (key == null)
            {
                LogNullWarning("AddAttribute", "key");
                return;
            }

            InternalHelpers.Wrap("AddAttribute",
                () => { _worker.AddMessage(DdRumProcessor.AddAttributeMessage.Create(key, value)); });
        }

        public void RemoveAttribute(string key)
        {
            if (key == null)
            {
                LogNullWarning("RemoveAttribute", "key");
                return;
            }

            InternalHelpers.Wrap("RemoveAttribute",
                () => { _worker.AddMessage(DdRumProcessor.RemoveAttributeMessage.Create(key)); });
        }

        public void AddFeatureFlagEvaluation(string key, object value)
        {
            if (key == null)
            {
                LogNullWarning("AddFeatureFlagEvaluation", "key");
                return;
            }

            InternalHelpers.Wrap("AddFeatureFlagEvaluation",
                () => { _worker.AddMessage(DdRumProcessor.AddFeatureFlagEvaluationMessage.Create(key, value)); });
        }

        public void StopSession()
        {
            InternalHelpers.Wrap("StopSession",
                () => { _worker.AddMessage(new DdRumProcessor.StopSessionMessage()); });
        }

        private void LogNullWarning(string methodName, string parameter)
        {
            UnityEngine.Debug.LogWarning($"[Datadog] {methodName} called with null parameter: {parameter}.");
        }
    }
}
