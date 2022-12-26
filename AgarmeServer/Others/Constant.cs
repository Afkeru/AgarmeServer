using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgarmeServer.Others
{
    public class Constant
    {
        /*细胞类型常量*/
        public const byte TYPE_FOOD = 0;
        public const byte TYPE_PLAYER = 1;
        public const byte TYPE_VIRUS = 2;
        public const byte TYPE_EJECT = 3;
        public const byte TYPE_BOT = 4;
        public const byte TYPE_ROBOT = 5;
        public const byte TYPE_RED_VIRUS = 6;

        public const int INT_TYPE = 0;
        public const long LONG_TYPE = 0;
        public const double DOUBLE_TYPE = 0.0d;
        public const float FLOAT_TYPE = 0.0f;

        public const float PI_F = 3.1415926f;
        public const double PI_D = 3.1415926535d;

        public const bool CAL_SUCCESS = true;
        public const bool CAL_FAIL = false;
    }
}
