var myPort = browser.runtime.connect({ name: 'port-from-cs' });
var maxCount = 1000000;
var data = 'hello world';// { "type": "io.transfer", "source": { "type": "file", "path": "\\\\siberia\\kontur.Plugin\\tests\\samples\\small" }, "sink": { "type": "hex" }, "sessionId": "18d2b895-14bc-48ee-aff8-8c54d938ffed", "commandId": 3, "hostUri": "http://localhost:8123" }

myPort.onMessage.addListener(function (message) {
    if (message.error === undefined) {
        var count = message.count + 1;
        if (count < maxCount) {
            document.getElementById('Count').value = count.toString();
            start();
        } else {
            document.getElementById('Count').value = maxCount.toString() + ' is max value';
        }
    } else {
        document.getElementById('Count').value = message.error.toString();
    }
});

myPort.onDisconnect.addListener(function () {
    document.getElementById('Count').value = '0';
});

function start() {
    var count = parseInt(document.getElementById('Count').value);
    myPort.postMessage({ count: count, data: data });
}

document.getElementById('StartButton').onclick = start;

