// Copyright 2019 Eschryn/zCore (https://gist.github.com/Eschryn)
// MIT: https://gist.github.com/Eschryn/9f823508a823367ff99093ef18af8971

using System;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using DbgSe = OpenTK.Graphics.OpenGL.DebugSeverity;
using DbgSrc = OpenTK.Graphics.OpenGL.DebugSource;
using DbgSrcExt = OpenTK.Graphics.OpenGL.DebugSourceExternal;
using DbgTy = OpenTK.Graphics.OpenGL.DebugType;

namespace Example
{
    public enum DebugSeverity
    {
        High = DbgSe.DebugSeverityHigh,
        Medium = DbgSe.DebugSeverityMedium,
        Low = DbgSe.DebugSeverityLow,
        Notification = DbgSe.DebugSeverityNotification,
        DontCare = DbgSe.DontCare,
    }

    public enum DebugSource
    {
        Api = DbgSrc.DebugSourceApi,
        Application = DbgSrc.DebugSourceApplication,
        Other = DbgSrc.DebugSourceOther,
        ShaderCompiler = DbgSrc.DebugSourceShaderCompiler,
        ThirdParty = DbgSrc.DebugSourceThirdParty,
        WindowSystem = DbgSrc.DebugSourceWindowSystem,
        DontCare = DbgSrc.DontCare
    }

    public enum DebugSourceExternal
    {
        Application = DbgSrcExt.DebugSourceApplication,
        ThirdParty = DbgSrcExt.DebugSourceThirdParty
    }

    public enum DebugType
    {
        DeprecatedBehavior = DbgTy.DebugTypeDeprecatedBehavior,
        Error = DbgTy.DebugTypeError,
        Marker = DbgTy.DebugTypeMarker,
        Other = DbgTy.DebugTypeOther,
        Performance = DbgTy.DebugTypePerformance,
        PopGroup = DbgTy.DebugTypePopGroup,
        Portability = DbgTy.DebugTypePortability,
        PushGroup = DbgTy.DebugTypePushGroup,
        UndefinedBehavior = DbgTy.DebugTypeUndefinedBehavior,
        DontCare = DbgTy.DontCare
    }
    
    public sealed class DebugMessageEventArgs
    {
        public string Message { get; }
        public DebugSource Source { get; }
        public DebugType Type { get; }
        public int ID { get; }
        public DebugSeverity Severity { get; }
        public IntPtr UserParam { get; }

        public DebugMessageEventArgs(DebugSource source, DebugType type, int id, DebugSeverity severity, string msg, IntPtr userParam)
        {
            Source = source;
            Type = type;
            ID = id;
            Severity = severity;
            Message = msg;
            UserParam = userParam;
        }
    }

    public static class GLDebugLog
    {
        const int DEBUG_BIT = (int)ContextFlagMask.ContextFlagDebugBit;
        const int GLMaxDebugMessageLength = 0x9143;

        private static readonly int _maxMessageLength; 

        public delegate void DebugMessage(object sender, DebugMessageEventArgs e);
        public static event DebugMessage Message;
        
        private static readonly DebugProc _debugProcCallback;
        // ReSharper disable once NotAccessedField.Local
        private static readonly GCHandle _callbackHandle;
        private static void DebugCallback(DbgSrc source, DbgTy type, int id, DbgSe severity, int length, IntPtr message, IntPtr userParam)
        {
            var msg = Marshal.PtrToStringAnsi(message, length);
            Message?.Invoke(null, new DebugMessageEventArgs((DebugSource)source, (DebugType)type, id, (DebugSeverity)severity, msg, userParam));
        }

        static GLDebugLog()
        {
            _debugProcCallback = DebugCallback;
            _callbackHandle = GCHandle.Alloc(_debugProcCallback);
            
            GL.GetInteger(GetPName.ContextFlags, out var flags);
            if ((flags & DEBUG_BIT) != 0) {
                GL.Enable(EnableCap.DebugOutput);
            }

            GL.Enable(EnableCap.DebugOutputSynchronous);

            _maxMessageLength = GL.GetInteger((GetPName)GLMaxDebugMessageLength);

            GL.DebugMessageCallback(_debugProcCallback, IntPtr.Zero);
        }

        public static void Activate(DebugSourceControl dsrcc, DebugTypeControl dtc, DebugSeverityControl dsc)
        {
            GL.DebugMessageControl(dsrcc, dtc, dsc, 0, new int[0], true);
        }

        public static void Activate(DebugSourceControl dsrcc, DebugTypeControl dtc, int[] ids)
        {
            GL.DebugMessageControl(dsrcc, dtc, DebugSeverityControl.DontCare, ids.Length, ids, true);
        }

        public static void Activate(DebugSourceControl dsrcc, DebugTypeControl dtc, int length, ref int ids)
        {
            GL.DebugMessageControl(dsrcc, dtc, DebugSeverityControl.DontCare, length, ref ids, true);
        }

        public unsafe static void Activate(DebugSourceControl dsrcc, DebugTypeControl dtc, int length, int* ids)
        {
            GL.DebugMessageControl(dsrcc, dtc, DebugSeverityControl.DontCare, length, ids, true);
        }

        public static void Deactivate(DebugSourceControl dsrcc, DebugTypeControl dtc, DebugSeverityControl dsc)
        {
            GL.DebugMessageControl(dsrcc, dtc, dsc, 0, new int[0], false);
        }

        public static void Deactivate(DebugSourceControl dsrcc, DebugTypeControl dtc, int[] ids)
        {
            GL.DebugMessageControl(dsrcc, dtc, DebugSeverityControl.DontCare, ids.Length, ids, false);
        }

        public static void Deactivate(DebugSourceControl dsrcc, DebugTypeControl dtc, int length, ref int ids)
        {
            GL.DebugMessageControl(dsrcc, dtc, DebugSeverityControl.DontCare, length, ref ids, false);
        }

        public unsafe static void Deactivate(DebugSourceControl dsrcc, DebugTypeControl dtc, int length, int* ids)
        {
            GL.DebugMessageControl(dsrcc, dtc, DebugSeverityControl.DontCare, length, ids, false);
        }

        public static void PushGroup(DebugSourceExternal source, int id, string message)
        {
            GL.PushDebugGroup((DbgSrcExt)source, id, message.Length, message);
        }

        public static void PopGroup() => GL.PopDebugGroup();

        public static void EvokeMessage(DebugSourceExternal dse, DebugType dt, int id, DebugSeverity ds, string msg)
        {
            if (msg.Length > _maxMessageLength)
                throw new ArgumentException("Message is too long");

            GL.DebugMessageInsert((DbgSrcExt)dse, (DbgTy)dt, id, (DbgSe)ds, msg.Length, msg);
        }

        public static string GetLog(int count)
        {
            GL.GetDebugMessageLog(count, _maxMessageLength, out _, out _, out _, out _, out _, out var res);
            return res;
        }

    }
}
