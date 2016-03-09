using V2Screenshot.Model;
using System;

namespace V2Screenshot.Error
{
    [Serializable]
    class UserOfflineException : ApiException
    {
        public UserOfflineException(Player p) : base(String.Format("{0} is offline", p.Name)) { }
    }
}
