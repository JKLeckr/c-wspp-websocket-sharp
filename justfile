# justfile

set windows-shell := ["powershell.exe", "-NoLogo", "-Command"]

proj_name := 'websocket-sharp'
native_proj_args := '' # If building release use --release
test_dir := 'test'
test_proj_name := 'wstest'
test_server_name := 'wsmini'

default:
    @just --list

[working-directory: 'c-wspp-rs']
setup-native:
    cargo fetch

setup:
    @just setup-native
    dotnet restore

[working-directory: 'c-wspp-rs']
build-native:
    cargo build {{ native_proj_args }}

build:
    @just build-native
    dotnet build {{ proj_name }}

build-tests:
    @just build-native
    dotnet build {{ test_proj_name }}

build-all:
    @just build-native
    dotnet build

[working-directory: 'c-wspp-rs']
clean-native:
    cargo clean

clean:
    @just clean-native
    dotnet clean

run-test-server:
    dotnet run --project ./{{ test_dir }}/{{ test_server_name }}.csproj --framework net8.0

test:
    dotnet run --project ./{{ test_dir }}/{{ test_proj_name }}.csproj -f net8 -- "ws://127.0.0.1:18765/ws"
