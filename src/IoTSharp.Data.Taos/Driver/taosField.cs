
using IoTSharp.Data.Taos.Driver;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace TDengineDriver
{

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4, Size = 72)]
    public struct taosField_E
    {

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 65)]
        public byte[] _name;
        [MarshalAs(UnmanagedType.U1, SizeConst = 1)]
        public byte type;
        [MarshalAs(UnmanagedType.U1, SizeConst = 1)]
        public byte precision;
        [MarshalAs(UnmanagedType.U1, SizeConst = 1)]
        public byte scale;
        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        public int bytes;
#if NET5_0_OR_GREATER
        public string Name => Encoding.UTF8.GetString(_name, 0, _name.AsSpan().IndexOf((byte)0));
#else
        public string Name => Encoding.UTF8.GetString(_name, 0, Array.IndexOf( _name,(byte)0));
#endif
        public TDengineDataType DataType => (TDengineDataType)type;
        public Type CrlType => DataType.ToCrlType();
        public string TypeName => DataType.ToTypeName();
        public int Size => bytes;
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4, Size = 72)]
    public struct taosField
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 65)]
        public byte[] _name;
        [MarshalAs(UnmanagedType.U1, SizeConst = 1)]
        public byte type;
        //  int32_t bytes;
        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        public int bytes;
#if NET5_0_OR_GREATER
        public string Name => Encoding.UTF8.GetString(_name, 0, _name.AsSpan().IndexOf((byte)0));
#else
        public string Name => Encoding.UTF8.GetString(_name, 0, Array.IndexOf( _name,(byte)0));
#endif
        public TDengineDataType DataType => (TDengineDataType)type;
        public Type CrlType => DataType.ToCrlType();

        public string TypeName =>DataType.ToTypeName();
     
        public int  Size => bytes;
    }
}