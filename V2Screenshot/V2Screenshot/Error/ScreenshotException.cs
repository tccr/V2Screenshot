using System;

namespace V2Screenshot.Error
{
    [Serializable]
    class ScreenshotException : ApiException
    {
        public ScreenshotException(string msg) : base(msg) { }

    }
}
