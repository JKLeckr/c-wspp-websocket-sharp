# c-wspp websocket-sharp

Fake websocket-sharp.dll that partially implements the API of
[websocket-sharp](https://github.com/sta/websocket-sharp),
but uses a native lib
[c-wspp-rs](https://github.com/JKLeckr/c-wspp-rs),
which is a wrapper around [yawc](https://github.com/infinitefield/yawc) for better WebSocket support for programs that use older versions of mono. Supports newer TLS, and compression.

On macOS, this project expects the native library as a universal build:
`c-wspp-macos-universal.dylib`.

## How to build:
#### Run dotnet build on the websocket-sharp project:
```sh
dotnet build ./websocket-sharp/websocket-sharp.csproj
```

## How to test:
#### 1. Run the mini websocket server:
```sh
dotnet run --project ./test/wsmini.csproj --framework net8.0
```
#### 2. Run the test suite:
```sh
dotnet run --project ./test/wstest.csproj -f net8 -- "ws://127.0.0.1:18765/ws"
```
