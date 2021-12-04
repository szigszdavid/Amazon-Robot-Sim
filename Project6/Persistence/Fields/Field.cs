using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace project6.Persistence
{
    public enum COLORS { WHITE, GREY, BLUE, GREEN, ORANGE }
    public abstract class Field
    {
        protected Int32 _id;                                                                        //Néhány mező külön id-t kap, hogy megkönnyítse a simulation lépéseit. Ilyen például a robot és a cél.
        protected Int32 _x;                                                                         
        protected Int32 _y;
        protected COLORS _color;                                                                    //Minden mező különböző színű, ezek be vannak állítva minden típus konstruktorában.
        private SortedSet<Int32> _parcels;                                                          //Ez amjd a szekrény típusnál kell csak majd. Ebben mentjük a termékeket.
        Boolean _isTargeted;                                                                        // Szintén csak a szekrénynek kell, ledönti, hogy megy-e hozzá már egy robot.

        public SortedSet<Int32> Parcels { get { return _parcels; } set { _parcels = value; } }

        public Int32 Id { get { return _id; }  set { _id = value;  } }
        
        public Int32 X { get { return _x; } set { _x = value; } }

        public Int32 Y { get { return _y; } set { _y = value; } }

        public COLORS Color { get { return _color; } set { _color = value; } }

        public Boolean IsTargeted { get { return _isTargeted; } set { _isTargeted = value; } }

    }
}
