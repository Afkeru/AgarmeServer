﻿namespace AgarmeServer.HKObj
{
    interface IRgba<T> : IRgb<T>
    {
        T A { get; set; }//透明度
    }
}
