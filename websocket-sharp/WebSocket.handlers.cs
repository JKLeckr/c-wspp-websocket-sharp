// Handlers triggered by native lib

using System;

namespace WebSocketSharp
{
    public partial class WebSocket : IDisposable
    {
        internal bool sequenceEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }
            for (int i=0; i<a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }

        private void OpenHandler()
        {
            debug("On Open");

            // ignore events that happen during shutdown of the socket
            if (wspp == null)
                return;

            debug("ReadyState = Open");
            readyState = WebSocketState.Open;
            EventArgs e = new EventArgs();
            // FIXME: on .net >=4.0 we could use an async task to fire from main thread
            WebSocketEventDispatcher activeDispatcher = dispatcher;
            if (activeDispatcher != null)
            {
                activeDispatcher.Enqueue(e);
            }
        }

        private void CloseHandler()
        {
            debug("On Close");

            // ignore events that happen during shutdown of the socket
            if (wspp == null)
                return;

            debug("ReadyState = Closed");
            readyState = WebSocketState.Closed; // TODO: move this after nulling dispatcher in WebSocket.cs to avoid a race if another thread polls ReadyState

            CloseEventArgs e = new CloseEventArgs(0, ""); // TODO: code and reason
            // FIXME: on .net >=4.0 we could use an async task to fire from main thread
            WebSocketEventDispatcher activeDispatcher = dispatcher;
            if (activeDispatcher != null)
            {
                activeDispatcher.Enqueue(e);
            }
        }

        private void MessageHandler(byte[] bytes, int opCode)
        {
            debug("On Message");

            if (wspp == null)
                return;

            MessageEventArgs e = new MessageEventArgs(bytes, (OpCode)opCode);
            // FIXME: on .net >=4.0 we could use an async task to fire from main thread
            WebSocketEventDispatcher activeDispatcher = dispatcher;
            if (activeDispatcher != null)
            {
                activeDispatcher.Enqueue(e);
            }
        }

        private void ErrorHandler(string msg)
        {
            debug("On Error");

            if (wspp == null)
                return;

            bool isConnectError = readyState == WebSocketState.Connecting;
            if (isConnectError)
            {
                // no need to close
                debug("ReadyState = Closed");
                readyState = WebSocketState.Closed; // TODO: move this after nulling dispatcher in WebSocket.cs to avoid a race if another thread polls ReadyState
            }
            else if (readyState == WebSocketState.Open)
            {
                debug("ReadyState = Closed");
                readyState = WebSocketState.Closed;
            }
            lastError = msg;
            error((isConnectError ? "Connect error: " : "WebSocket error: ") + msg);
        }

        private void PongHandler(byte[] bytes)
        {
            debug("On Pong");

            if (wspp == null)
                return;

            // look for internal ping
            lock (pings)
            {
                for (int i = 0; i < pings.Count; i++)
                {
                    byte[] b = pings[i];
                    if (sequenceEqual(bytes, b))
                    {
                        pings.RemoveAt(i);
                        lastPong = DateTime.UtcNow;
                        return;
                    }
                }
            }

            // emit event for external ping
            MessageEventArgs e = new MessageEventArgs(bytes, OpCode.Pong);
            // FIXME: on .net >=4.0 we could use an async task to fire from main thread
            WebSocketEventDispatcher activeDispatcher = dispatcher;
            if (activeDispatcher != null)
            {
                activeDispatcher.Enqueue(e);
            }
        }

        private void SetHandlers()
        {
            debug("Set Handlers");
            wspp.set_open_handler(OpenHandler);
            wspp.set_close_handler(CloseHandler);
            wspp.set_error_handler(ErrorHandler);
            wspp.set_message_handler(MessageHandler);
            wspp.set_pong_handler(PongHandler);
        }

        private void ClearHandlers()
        {
            debug("Clear Handlers");
            wspp.clear_handlers();
        }
    }
}
