﻿using System;

namespace Lib.DataFlow
{
    public static class FuncEmpty<T>
    {
        public static readonly Func<T> Empty = () => default;
        public static readonly Func<T, bool> Empty_bool = _ => false;
    }

    public static class FuncEmpty
    {
        public static readonly Func<bool> Empty = () => false;
    }

    public static class ActionEmpty<T>
    {
        public static readonly Action<T> Empty = _ => { };
    }

    public static class ActionEmpty
    {
        public static readonly Action Empty = () => { };
    }
}