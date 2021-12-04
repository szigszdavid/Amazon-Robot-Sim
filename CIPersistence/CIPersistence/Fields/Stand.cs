using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace project6.CIPersistence.Fields
{
    public class Stand : Field
    {
        public Stand()
        {
            this._color = COLORS.GREY;
        }

        private Boolean _isTargeted;                                                                    //Eldönti, hogy megy-e már hozzá robot

        public Boolean IsTargeted { get { return _isTargeted; } set { _isTargeted = value; } }

        public Boolean IsEmpty()
        {
            return !Parcels.Any();
        }
    }
}
