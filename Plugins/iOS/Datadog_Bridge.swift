// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2023-Present Datadog, Inc.

import Foundation
import DatadogCore
import DatadogLogs
import DatadogInternal

@_cdecl("Datadog_SetSdkVerbosity")
func Datadog_SetSdkVerbosity(sdkVerbosityInt: Int) {
    let verbosity = CoreLoggerLevel(rawValue: sdkVerbosityInt)
    Datadog.verbosityLevel = verbosity
}

@_cdecl("Datadog_SetTrackingConsent")
func Datadog_SetTrackingConsent(trackingConsentInt: Int) {
    let trackingConsent: TrackingConsent?
    switch trackingConsentInt {
    case 0: trackingConsent = .granted
    case 1: trackingConsent = .notGranted
    case 2: trackingConsent = .pending
    default: trackingConsent = nil
    }

    if let trackingConsent = trackingConsent {
        Datadog.set(trackingConsent: trackingConsent)
    }
}

@_cdecl("Datadog_AddLogsAttributes")
func Datadog_AddLogsAttributes(jsonAttributes: CString?) {
    guard let jsonAttributes = jsonAttributes else {
        return
    }

    let decodedAttributes = decodeJsonAttributes(fromCString: jsonAttributes);
    for attr in decodedAttributes {
        Logs.addAttribute(forKey: attr.key, value: attr.value)
    }
}

@_cdecl("Datadog_RemoveLogsAttributes")
func Datadog_RemoveLogsAttributes(key: CString?) {
    guard let key = key else {
        return
    }

    if let swiftKey = String(cString: key, encoding: .utf8) {
        Logs.removeAttribute(forKey: swiftKey)
    }
}

@_cdecl("Datadog_SetUserInfo")
func Datadog_SetUserInfo(
    id: CString?,
    name: CString?,
    email: CString?,
    extraInfo: CString?
) {
    let idString = decodeCString(cString: id)
    let nameString = decodeCString(cString: name)
    let emailString = decodeCString(cString: email)
    let decodedExtraInfo = decodeJsonAttributes(fromCString: extraInfo)

    Datadog.setUserInfo(id: idString, name: nameString, email: emailString, extraInfo: decodedExtraInfo)
}

@_cdecl("Datadog_AddUserExtraInfo")
func Datadog_AddUserExtraInfo(extraInfo: CString) {
    let decodedExtraInfo = decodeJsonAttributes(fromCString: extraInfo)

    Datadog.addUserExtraInfo(decodedExtraInfo)
}

@_cdecl("Datadog_SendDebugTelemetry")
func Datadog_SendDebugTelemetry(message: UnsafeMutablePointer<CChar>?) {
    guard let message = message else {
        return
    }

    if let messageString = String(cString: message, encoding: .utf8) {
        Datadog._internal.telemetry.debug(id: "datadog_unity:\(messageString)", message: messageString)
    }
}

@_cdecl("Datadog_SendErrorTelemetry")
func Datadog_SendErrorTelemetry(
    message: UnsafeMutablePointer<CChar>?,
    stack: UnsafeMutablePointer<CChar>?,
    kind: UnsafeMutablePointer<CChar>?
) {
    guard let message = message else {
        return
    }

    if let messageString = String(cString: message, encoding: .utf8) {
        var errorStack: String?
        var errorKind: String?

        if let stack = stack {
            errorStack = String(cString: stack, encoding: .utf8)
        }
        if let kind = kind {
            errorKind = String(cString: kind, encoding: .utf8)
        }

        Datadog._internal.telemetry.error(id: "datadog_unity:\(messageString)", message: messageString, kind: errorKind, stack: errorStack)
    }
}

@_cdecl("Datadog_ClearAllData")
func Datadog_ClearAllData() {
    Datadog.clearAllData()
}

@_cdecl("Datadog_UpdateTelemetryConfiguration")
func Datadog_UpdateTelemetryConfiguration(unityVersion: CString) {
    guard let unityVersion = decodeCString(cString: unityVersion) else {
        return
    }

    let core = CoreRegistry.default
    core.telemetry.configuration(unityVersion: unityVersion)
}
