using System;

namespace V2Screenshot.Error
{
    [Serializable]
    class InvalidResponseException : ApiException
    {
        public InvalidResponseException() : base("Invalid response from server") { }
    }
}
