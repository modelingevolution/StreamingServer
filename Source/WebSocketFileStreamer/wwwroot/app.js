var webSocket = new WebSocket("ws://localhost:1234/ws");
webSocket.onmessage = (event) => {
    console.log(event.data);
}

window.test = function () {
    webSocket.send('hello!');
}