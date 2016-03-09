using System;

namespace V2Screenshot.Error
{
    [Serializable]
    class UserNotFoundException : ApiException
    {

        public UserNotFoundException():base("User could not be found") { }

    }
}
