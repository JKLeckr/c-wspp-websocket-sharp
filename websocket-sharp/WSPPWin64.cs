using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WebSocketSharp
{
    /// <summary>
    /// Wrapper for native c-wspp on Non-Windows.
    /// </summary>
    internal class WSPPWin64 : IWSPP
    {
        internal const CallingConvention CALLING_CONVENTION = CallingConvention.Winapi;

        internal const string DLL_NAME = "c-wspp-win64.dll";

        [UnmanagedFunctionPointer(CALLING_CONVENTION)]
        internal delegate void OnOpenCallback();
        [UnmanagedFunctionPointer(CALLING_CONVENTION)]
        internal delegate void OnCloseCallback(); // TODO: code, reason
        [UnmanagedFunctionPointer(CALLING_CONVENTION)]
        internal delegate void OnErrorCallback(IntPtr msg); // TODO: errorCode
        [UnmanagedFunctionPointer(CALLING_CONVENTION)]
        internal delegate void OnMessageCallback(IntPtr data, ulong len, int opCode);
        [UnmanagedFunctionPointer(CALLING_CONVENTION)]
        internal delegate void OnPongCallback(IntPtr data, ulong len);
        [UnmanagedFunctionPointer(CALLING_CONVENTION)]
        internal delegate void OnLogCallback(int level, IntPtr msg);

        [DllImport(DLL_NAME, CharSet=CharSet.Ansi, CallingConvention=CALLING_CONVENTION)]
        internal static extern UIntPtr wspp_new(IntPtr uri);
        [DllImport(DLL_NAME, CallingConvention=CALLING_CONVENTION)]
        internal static extern void wspp_delete(UIntPtr ws);
        [DllImport(DLL_NAME, CallingConvention=CALLING_CONVENTION)]
        internal static extern ulong wspp_poll(UIntPtr ws);
        [DllImport(DLL_NAME, CallingConvention=CALLING_CONVENTION)]
        internal static extern ulong wspp_run(UIntPtr ws);
        [DllImport(DLL_NAME, CallingConvention=CALLING_CONVENTION)]
        internal static extern bool wspp_stopped(UIntPtr ws);
        [DllImport(DLL_NAME, CallingConvention=CALLING_CONVENTION)]
        internal static extern int wspp_connect(UIntPtr ws);
        [DllImport(DLL_NAME, CallingConvention=CALLING_CONVENTION)]
        internal static extern int wspp_close(UIntPtr ws, ushort code, IntPtr reason);
        [DllImport(DLL_NAME, CallingConvention=CALLING_CONVENTION)]
        internal static extern int wspp_send_text(UIntPtr ws, IntPtr message);
        [DllImport(DLL_NAME, CallingConvention=CALLING_CONVENTION)]
        internal static extern int wspp_send_binary(UIntPtr ws, byte[] data, ulong len);
        [DllImport(DLL_NAME, CallingConvention=CALLING_CONVENTION)]
        internal static extern int wspp_ping(UIntPtr ws, byte[] data, ulong len);
        [DllImport(DLL_NAME, CallingConvention=CALLING_CONVENTION)]
        internal static extern void wspp_set_open_handler(UIntPtr ws, OnOpenCallback f);
        [DllImport(DLL_NAME, CallingConvention=CALLING_CONVENTION)]
        internal static extern void wspp_set_close_handler(UIntPtr ws, OnCloseCallback f);
        [DllImport(DLL_NAME, CallingConvention=CALLING_CONVENTION)]
        internal static extern void wspp_set_error_handler(UIntPtr ws, OnErrorCallback f);
        [DllImport(DLL_NAME, CallingConvention=CALLING_CONVENTION)]
        internal static extern void wspp_set_message_handler(UIntPtr ws, OnMessageCallback f);
        [DllImport(DLL_NAME, CallingConvention=CALLING_CONVENTION)]
        internal static extern void wspp_set_pong_handler(UIntPtr ws, OnPongCallback f);
        [DllImport(DLL_NAME, CallingConvention=CALLING_CONVENTION, EntryPoint="wspp_set_log_handler")]
        internal static extern void wspp_set_log_handler(OnLogCallback f);
        [DllImport(DLL_NAME, CallingConvention=CALLING_CONVENTION, EntryPoint="wspp_set_loglevel")]
        internal static extern void wspp_set_loglevel(int level);
        [DllImport(DLL_NAME, CallingConvention=CALLING_CONVENTION, EntryPoint="wspp_abi_version")]
        internal static extern ulong wspp_abi_version();

        UIntPtr _ws;

#pragma warning disable 0414
        OnOpenCallback _openHandler;
        OnCloseCallback _closeHandler;
        OnErrorCallback _errorHandler;
        OnMessageCallback _messageHandler;
        OnPongCallback _pongHandler;
#pragma warning disable 0414

        static readonly object _nativeLogLock = new object();
        static bool _nativeLoggingUnavailable = false;
        static OnLogCallback _nativeLogCallback;
        static OnNativeLogHandler _managedNativeLogHandler;

        static public bool IsActivePlatform()
        {
            int platformId = (int)Environment.OSVersion.Platform;
            return (platformId < 4 || platformId == 5) && IntPtr.Size == 8;
        }

        public WSPPWin64(string uriString)
        {
            IntPtr uriUTF8 = Native.StringToHGlobalUTF8(uriString);
            try {
                _ws = wspp_new(uriUTF8);
            } finally {
                Marshal.FreeHGlobal(uriUTF8);
            }
        }

        private void validate()
        {
            if (_ws == UIntPtr.Zero)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        public void set_open_handler(OnOpenHandler callback)
        {
            _openHandler = null;
            if (callback != null)
            {
                _openHandler = delegate {
                    try { callback(); } catch (Exception) { }
                };
            }
            wspp_set_open_handler(_ws, _openHandler);
        }

        public void set_close_handler(OnCloseHandler callback)
        {
            _closeHandler = null;
            if (callback != null)
            {
                _closeHandler = delegate {
                    try { callback(); } catch (Exception) { }
                };
            }
            wspp_set_close_handler(_ws, _closeHandler);
        }

        public void set_error_handler(OnErrorHandler callback)
        {
            _errorHandler = null;
            if (callback != null)
            {
                _errorHandler = delegate (IntPtr data) {
                    if (_ws == UIntPtr.Zero)
                        return;

                    try { callback(Native.ToString(data, "Unknown")); } catch (Exception) { }
                };
            }
            wspp_set_error_handler(_ws, _errorHandler);
        }

        public void set_message_handler(OnMessageHandler callback)
        {
            _messageHandler = null;
            if (callback != null)
            {
                _messageHandler = delegate (IntPtr data, ulong len, int opCode) {
                    if (_ws == UIntPtr.Zero)
                        return;

                    if (len > Int32.MaxValue)
                        return;

                    try { callback(Native.ToByteArray(data, (int)len), opCode); } catch (Exception) { }
                };
            }
            wspp_set_message_handler(_ws, _messageHandler);
        }

        public void set_pong_handler(OnPongHandler callback)
        {
            _pongHandler = null;
            if (callback != null)
            {
                _pongHandler = delegate (IntPtr data, ulong len) {
                    if (_ws == UIntPtr.Zero)
                        return;

                    if (len > Int32.MaxValue)
                        return;

                    try { callback(Native.ToByteArray(data, (int)len)); } catch (Exception) { }
                };
            }
            wspp_set_pong_handler(_ws, _pongHandler);
        }

        private static void HandleNativeLog(int level, IntPtr msg)
        {
            OnNativeLogHandler managed = _managedNativeLogHandler;
            if (managed == null)
            {
                return;
            }
            try
            {
                managed(level, Native.ToString(msg, ""));
            }
            catch (Exception)
            {
            }
        }

        public ulong abi_version()
        {
            return wspp_abi_version();
        }

        public WsppRes connect()
        {
            validate();
            return (WsppRes) wspp_connect(_ws);
        }

        public void delete()
        {
            validate();
            // TODO: finalizer/Dispose
            wspp_delete(_ws);
            _ws = UIntPtr.Zero;
        }

        public WsppRes close(ushort code, string reason)
        {
            validate();
            IntPtr reasonUTF8 = Native.StringToHGlobalUTF8(reason);
            try
            {
                return (WsppRes) wspp_close(_ws, code, reasonUTF8);
            }
            finally
            {
                Marshal.FreeHGlobal(reasonUTF8);
            }
        }

        public WsppRes send(string message)
        {
            validate();
            IntPtr p = Native.StringToHGlobalUTF8(message);
            try
            {
                return (WsppRes) wspp_send_text(_ws, p);
            }
            finally
            {
                Marshal.FreeHGlobal(p);
            }
        }

        public WsppRes send(byte[] data)
        {
            validate();
            return (WsppRes) wspp_send_binary(_ws, data, (ulong)data.Length);
        }

        public WsppRes ping(byte[] data)
        {
            validate();
            return (WsppRes) wspp_ping(_ws, data, (ulong)data.Length);
        }

        public ulong poll()
        {
            validate();
            return wspp_poll(_ws);
        }

        public bool stopped()
        {
            validate();
            return wspp_stopped(_ws);
        }

        public bool supports_native_logging()
        {
            return !_nativeLoggingUnavailable;
        }

        public bool try_set_log_handler(OnNativeLogHandler callback)
        {
            lock (_nativeLogLock)
            {
                _managedNativeLogHandler = callback;
                if (_nativeLoggingUnavailable)
                {
                    return false;
                }

                try
                {
                    if (callback == null)
                    {
                        wspp_set_log_handler(null);
                    }
                    else
                    {
                        if (_nativeLogCallback == null)
                        {
                            _nativeLogCallback = HandleNativeLog;
                        }
                        wspp_set_log_handler(_nativeLogCallback);
                    }
                    return true;
                }
                catch (EntryPointNotFoundException)
                {
                    _nativeLoggingUnavailable = true;
                    return false;
                }
            }
        }

        public bool try_set_loglevel(int level)
        {
            if (_nativeLoggingUnavailable)
            {
                return false;
            }
            try
            {
                wspp_set_loglevel(level);
                return true;
            }
            catch (EntryPointNotFoundException)
            {
                _nativeLoggingUnavailable = true;
                return false;
            }
        }

        public void clear_handlers()
        {
            // clear native callbacks
            set_open_handler(null);
            set_close_handler(null);
            set_error_handler(null);
            set_message_handler(null);
            set_pong_handler(null);
        }
    }
}
