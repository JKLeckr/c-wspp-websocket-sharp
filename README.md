# c-wspp websocket-sharp

Fake websocket-sharp.dll that partially implements the API of
[websocket-sharp](https://github.com/sta/websocket-sharp),
but uses a native lib
[c-wspp-rs](https://github.com/JKLeckr/c-wspp-rs),
which is a wrapper around [yawc](https://github.com/infinitefield/yawc) for better WebSocket support for programs that use older versions of mono. Supports newer TLS, and compression.

On macOS, this project expects the native library as a universal build:
`c-wspp-macos-universal.dylib`.

## How to build:
#### Use just to build on the websocket-sharp project:
```sh
just build
```

## How to test:
#### 1. Run the mini websocket server:
```sh
just run-test-server
```
#### 2. Run the test suite:
```sh
just test
```
