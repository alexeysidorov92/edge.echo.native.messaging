var portFromCS = null;
var nativePort = null;

function connected(p) {
    portFromCS = p;
    portFromCS.onMessage.addListener(function (message) {
        try {
            if (nativePort === null) {
                nativePort = browser.runtime.connectNative('edge.echo.native.messaging.test');
                nativePort.onMessage.addListener(function (response) {
                    if (portFromCS !== null) {
                        portFromCS.postMessage(JSON.parse(response));
                    }
                });
                nativePort.onDisconnect.addListener(function () {
                    if (portFromCS !== null) {
                        portFromCS.postMessage({ error: 'native port disconnected' });
                    }
                    nativePort = null;
                });
            }
            nativePort.postMessage(message);
        } catch (ex) {
            portFromCS.postMessage({ error: ex });
        }       
    });
    portFromCS.onDisconnect.addListener(function () {
        if (nativePort !== null) {
            nativePort.disconnect();
        }
        portFromCS = null;
    });
}

browser.runtime.onConnect.addListener(connected);