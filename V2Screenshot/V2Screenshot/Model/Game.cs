using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using V2Screenshot.ViewModel;

namespace V2Screenshot.Model
{
    class Game : ModelBase
    {
        private int netCode;
        private string displayName;

        public int NetCode
        {
            get
            {
                return netCode;
            }
        }

        public string DisplayName
        {
            get
            {
                return displayName;
            }
        }

        public Game(int netcode, string displayname)
        {
            netCode = netcode;
            displayName = displayname;
        }

        public override bool Update(ModelBase m)
        {
            return false;
        }

        public ObservableCollection<ServerViewModel> Servers { get; set; }
    }
}
