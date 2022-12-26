using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AgarmeServer.Others
{
    public static class TypeExtensions
    {
        //检测int数据是否溢出
        public static bool IsStackOverFlow(this int value) => (value > int.MaxValue || value < int.MinValue) ? Constant.CAL_SUCCESS : Constant.CAL_FAIL;
        //检测uint数据是否溢出
        public static bool IsStackOverFlow(this uint value) => (value > uint.MaxValue || value < uint.MinValue) ? Constant.CAL_SUCCESS : Constant.CAL_FAIL;
        //检测int数据是否溢出
        public static bool IsStackOverFlow(this ushort value) => (value > ushort.MaxValue || value < ushort.MinValue) ? Constant.CAL_SUCCESS : Constant.CAL_FAIL;
        //检测long数据是否溢出
        public static bool IsStackOverFlow(this long value)=> (value > long.MaxValue || value < long.MinValue) ? Constant.CAL_SUCCESS : Constant.CAL_FAIL;
        //检测float数据是否溢出
        public static bool IsStackOverFlow(this float value)=> (value > float.MaxValue || value < float.MinValue) ? Constant.CAL_SUCCESS : Constant.CAL_FAIL;
        //检测double数据是否溢出
        public static bool IsStackOverFlow(this double value)=> (value > double.MaxValue || value < double.MinValue) ? Constant.CAL_SUCCESS : Constant.CAL_FAIL;
    }
}
