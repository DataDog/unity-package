// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2023-Present Datadog, Inc.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Datadog.Unity.Rum
{
    /// <summary>
    /// DatadogTrackedWebRequest is a wrapper around <see cref="UnityWebRequest"/> that allows us to track the request.
    /// </summary>
    public class DatadogTrackedWebRequest : IDisposable
    {
        private readonly UnityWebRequest _innerRequest;

        public DatadogTrackedWebRequest()
        {
            _innerRequest = new UnityWebRequest();
        }

        ~DatadogTrackedWebRequest()
        {
            _innerRequest?.Dispose();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatadogTrackedWebRequest"/> class.
        /// Create a DatadogTrackedWebRequest from an existing <see cref="UnityWebRequest"/>. To ensure that this
        /// functions properly, the DatadogTrackedWebRequest should be created before any operations
        /// are performed on the wrapped request, and the wrapped request should not be used after.
        /// </summary>
        /// <param name="webRequest">The request to wrap.</param>
        public DatadogTrackedWebRequest(UnityWebRequest webRequest)
        {
            _innerRequest = webRequest;
        }

        public DatadogTrackedWebRequest(string url)
            : this(new UnityWebRequest(url))
        {
        }

        public DatadogTrackedWebRequest(Uri uri)
            : this(new UnityWebRequest(uri))
        {
        }

        public DatadogTrackedWebRequest(string url, string method)
            : this(new UnityWebRequest(url, method))
        {
        }

        public DatadogTrackedWebRequest(Uri uri, string method)
            : this(new UnityWebRequest(uri, method))
        {
        }

        public DatadogTrackedWebRequest(string url, string method, DownloadHandler downloadHandler, UploadHandler uploadHandler)
            : this(new UnityWebRequest(url, method, downloadHandler, uploadHandler))
        {
        }

        public DatadogTrackedWebRequest(Uri uri, string method, DownloadHandler downloadHandler, UploadHandler uploadHandler)
            : this(new UnityWebRequest(uri, method, downloadHandler, uploadHandler))
        {
        }

        public UnityWebRequest innerRequest => _innerRequest;

        public CertificateHandler certificateHandler
        {
            get => _innerRequest.certificateHandler;
            set => _innerRequest.certificateHandler = value;
        }

        public bool disposeCertificateHandlerOnDispose
        {
            get => _innerRequest.disposeCertificateHandlerOnDispose;
            set => _innerRequest.disposeCertificateHandlerOnDispose = value;
        }

        public bool disposeDownloadHandlerOnDispose
        {
            get => _innerRequest.disposeDownloadHandlerOnDispose;
            set => _innerRequest.disposeDownloadHandlerOnDispose = value;
        }

        public bool disposeUploadHandlerOnDispose
        {
            get => _innerRequest.disposeUploadHandlerOnDispose;
            set => _innerRequest.disposeUploadHandlerOnDispose = value;
        }

        public ulong downloadedBytes => _innerRequest.downloadedBytes;

        public DownloadHandler downloadHandler
        {
            get => _innerRequest.downloadHandler;
            set => _innerRequest.downloadHandler = value;
        }

        public float downloadProgress => _innerRequest.downloadProgress;

        public string error => _innerRequest.error;

        public bool isDone => _innerRequest.isDone;

        public bool isModifiable => _innerRequest.isModifiable;

        public string method
        {
            get => _innerRequest.method;
            set => _innerRequest.method = value;
        }

        public int redirectLimit
        {
            get => _innerRequest.redirectLimit;
            set => _innerRequest.redirectLimit = value;
        }

        public long responseCode => _innerRequest.responseCode;

        public UnityWebRequest.Result result => _innerRequest.result;

        public int timeout
        {
            get => _innerRequest.timeout;
            set => _innerRequest.timeout = value;
        }

        public ulong uploadedBytes => _innerRequest.uploadedBytes;

        public UploadHandler uploadHandler
        {
            get => _innerRequest.uploadHandler;
            set => _innerRequest.uploadHandler = value;
        }

        public float uploadProgress => _innerRequest.uploadProgress;

        public string url
        {
            get => _innerRequest.url;
            set => _innerRequest.url = value;
        }

        public Uri uri
        {
            get => _innerRequest.uri;
            set => _innerRequest.uri = value;
        }

        public bool useHttpContinue
        {
            get => _innerRequest.useHttpContinue;
            set => _innerRequest.useHttpContinue = value;
        }

        public void Abort()
        {
            _innerRequest.Abort();
        }

        public UnityWebRequestAsyncOperation SendWebRequest()
        {
            // Determine if the request we're about to send should have tracing headers injected
            var trackingHelper = DatadogSdk.Instance.ResourceTrackingHelper;
            var tracingHeaderType = trackingHelper?.HeaderTypesForHost(_innerRequest.uri) ?? TracingHeaderType.None;

            // Generate a RUM key, i.e. a globally unique value to identify this request as a "resource" in RUM:
            // so long as this value is non-null, our request corresponds to a RUM resource and should be tracked
            // as such
            var rumKey = Guid.NewGuid().ToString();

            // Preprocess the request before sending it, handling errors gracefully
            var attributes = new Dictionary<string, object>();
            try
            {
                // Inject tracing headers into the underlying HTTP request if needed
                if (tracingHeaderType != TracingHeaderType.None)
                {
                    var context = trackingHelper.GenerateTraceContext();
                    trackingHelper.GenerateDatadogAttributes(context, attributes);
                    var headers = new Dictionary<string, string>();
                    trackingHelper.GenerateTracingHeaders(context, tracingHeaderType, trackingHelper.TraceContextInjection, headers);

                    foreach (var header in headers)
                    {
                        SetRequestHeader(header.Key, header.Value);
                    }
                }

                // Begin a RUM "resource" to track this request in the context of the current RUM view
                DatadogSdk.Instance.Rum.StartResource(
                    rumKey,
                    EnumHelpers.HttpMethodFromString(_innerRequest.method),
                    _innerRequest.url,
                    attributes);
            }
            catch (Exception e)
            {
                // If we failed to start a RUM resource, clear the RUM key to abort any further tracking
                DatadogSdk.Instance.InternalLogger?.TelemetryError("Error starting RUM resource.", e);
                rumKey = null;
            }

            // Use Unity's implementation to send the underlying HTTP request, register our own on-complete
            // callback, and return the resulting AsyncOperation to the caller
            var operation = _innerRequest.SendWebRequest();
            operation.completed += (op) =>
            {
                // If we started a RUM resource for this request, attempt to record that it's now stopped
                if (rumKey != null)
                {
                    try
                    {
                        switch (result)
                        {
                            case UnityWebRequest.Result.Success:
                            case UnityWebRequest.Result.ProtocolError:
                                // ProtocolError indicates a valid response with an error-level HTTP status code:
                                // we consider a RUM resource completed successfully as long as it got a valid response
                                var contentType = GetResponseHeader("content-type");
                                DatadogSdk.Instance.Rum.StopResource(
                                    rumKey,
                                    EnumHelpers.ResourceTypeFromContentType(contentType),
                                    (int)responseCode,
                                    (long)downloadedBytes);
                                break;
                            case UnityWebRequest.Result.ConnectionError:
                            case UnityWebRequest.Result.DataProcessingError:
                                // We did not get a valid response; stop the resource and record error details
                                DatadogSdk.Instance.Rum.StopResourceWithError(rumKey, result.ToString(), error);
                                break;
                            default:
                                DatadogSdk.Instance.InternalLogger.TelemetryError(
                                    $"Unexpected result from UnityWebRequest: {result}", null);
                                break;
                        }
                    }
                    catch (NullReferenceException)
                    {
                        // If any attempt to access underlying request state resulted in NullReferenceException, it's
                        // likely that this request was disposed before the operation completed: this is not a
                        // telemetry error, but we should stop the resource
                        DatadogSdk.Instance.Rum.StopResourceWithError(rumKey, "RequestDisposed", "Web request was disposed before the operation completed.");
                    }
                    catch (Exception e)
                    {
                        // Any other unhandled error should be reported as an internal telemetry error
                        DatadogSdk.Instance.InternalLogger?.TelemetryError("Error stopping RUM resource.", e);
                    }
                }
            };

            return operation;
        }

        public string GetRequestHeader(string name)
        {
            return _innerRequest.GetRequestHeader(name);
        }

        public void SetRequestHeader(string name, string value)
        {
            _innerRequest.SetRequestHeader(name, value);
        }

        public string GetResponseHeader(string name)
        {
            return _innerRequest.GetResponseHeader(name);
        }

        public void Dispose()
        {
            _innerRequest?.Dispose();
            GC.SuppressFinalize(this);
        }

        public static DatadogTrackedWebRequest Delete(string url)
        {
            return new DatadogTrackedWebRequest(UnityWebRequest.Delete(url));
        }

        public static DatadogTrackedWebRequest Delete(Uri uri)
        {
            return new DatadogTrackedWebRequest(UnityWebRequest.Delete(uri));
        }

        public static DatadogTrackedWebRequest Get(string url)
        {
            return new DatadogTrackedWebRequest(UnityWebRequest.Get(url));
        }

        public static DatadogTrackedWebRequest Get(Uri uri)
        {
            return new DatadogTrackedWebRequest(UnityWebRequest.Get(uri));
        }

        public static DatadogTrackedWebRequest Head(string url)
        {
            return new DatadogTrackedWebRequest(UnityWebRequest.Head(url));
        }

        public static DatadogTrackedWebRequest Head(Uri uri)
        {
            return new DatadogTrackedWebRequest(UnityWebRequest.Head(uri));
        }
#if UNITY_2021
        public static DatadogTrackedWebRequest Post(string url, string postData)
        {
            return new DatadogTrackedWebRequest(UnityWebRequest.Post(url, postData));
        }

        public static DatadogTrackedWebRequest Post(Uri uri, string postData)
        {
            return new DatadogTrackedWebRequest(UnityWebRequest.Post(uri, postData));
        }

        public static DatadogTrackedWebRequest Post(string url, WWWForm form)
        {
            return new DatadogTrackedWebRequest(UnityWebRequest.Post(url, form));
        }

        public static DatadogTrackedWebRequest Post(Uri uri, WWWForm form)
        {
            return new DatadogTrackedWebRequest(UnityWebRequest.Post(uri, form));
        }

        public static DatadogTrackedWebRequest Post(string url, List<IMultipartFormSection> form)
        {
            return new DatadogTrackedWebRequest(UnityWebRequest.Post(url, form));
        }

        public static DatadogTrackedWebRequest Post(Uri url, List<IMultipartFormSection> form)
        {
            return new DatadogTrackedWebRequest(UnityWebRequest.Post(url, form));
        }

        public static DatadogTrackedWebRequest Post(string uri, List<IMultipartFormSection> form, byte[] boundary)
        {
            return new DatadogTrackedWebRequest(UnityWebRequest.Post(uri, form, boundary));
        }

        public static DatadogTrackedWebRequest Post(Uri uri, List<IMultipartFormSection> form, byte[] boundary)
        {
            return new DatadogTrackedWebRequest(UnityWebRequest.Post(uri, form, boundary));
        }
#elif UNITY_2022_1_OR_NEWER
        public static DatadogTrackedWebRequest Post(string url, string postData, string contentType)
        {
            return new DatadogTrackedWebRequest(UnityWebRequest.Post(url, postData, contentType));
        }

        public static DatadogTrackedWebRequest Post(Uri uri, string postData, string contentType)
        {
            return new DatadogTrackedWebRequest(UnityWebRequest.Post(uri, postData, contentType));
        }

        public static DatadogTrackedWebRequest PostWwwForm(string url, string form)
        {
            return new DatadogTrackedWebRequest(UnityWebRequest.PostWwwForm(url, form));
        }

        public static DatadogTrackedWebRequest PostWwwForm(Uri uri, string form)
        {
            return new DatadogTrackedWebRequest(UnityWebRequest.PostWwwForm(uri, form));
        }
#endif
    }
}
