using System;

namespace V2Screenshot.Error
{
    [Serializable]
    class ExceptionBase : Exception
    {
        public ExceptionBase(string msg) : base(msg) { }
        public ExceptionBase() { }
    }
}
