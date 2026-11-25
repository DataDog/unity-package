// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.
"use strict";

const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const test = require('node:test');

test('logs an error with a custom fingerprint', () => {
    const library = {};
    const context = {
        LibraryManager: { library },
        mergeInto: Object.assign,
        UTF8ToString: value => value,
    };
    const source = fs.readFileSync(path.join(__dirname, '../../Plugins/WebGL/DatadogLogs.jslib'), 'utf8');
    vm.runInNewContext(source, context);

    let logged;
    library._activeLoggers = { logger: { log: (...args) => { logged = args; } } };
    library.DDLogs_Log('logger', 'message', 'error', 'Error', 'failure', 'stack', JSON.stringify({
        '_dd.error.fingerprint': 'test-fingerprint',
        other: 'value',
    }));

    assert.ok(logged, 'the logger should receive the error');
    assert.equal(logged[0], 'message');
    assert.equal(logged[1]['error.fingerprint'], 'test-fingerprint');
    assert.equal(Object.hasOwn(logged[1], '_dd.error.fingerprint'), false);
    assert.equal(logged[1].other, 'value');
    assert.equal(logged[2], 'error');
    assert.equal(logged[3].message, 'failure');
    assert.equal(logged[3].name, 'Error');
    assert.equal(logged[3].stack, 'stack');
});
