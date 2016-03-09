using System;

namespace V2Screenshot.Error
{
    [Serializable]
    class ApiException : ExceptionBase
    {
        public ApiException(string msg) : base(msg) { }
    }
}
