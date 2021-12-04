using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace project6.CIPersistence.Fields
{
    public class Robot : project6.CIPersistence.Field
    {
        public Int32 _target_x;                                             //A szekrény x koordinátája
        public Int32 _target_y;                                             //A célba vett szekrény y-ja
        public Int32 _dest_x;                                               //A célállomás x-je
        public Int32 _dest_y;                                               //A célállomás y-ja
        public Int32 _start_x;                                              //Kiindulási koordináták
        public Int32 _start_y;
        public Int32 _extraMove;                                            //Az extra lépés száma, ez bővebben ki van fejteve a modelben
        private Int32 _battery;                                             //Akkumulátor
        private Int32 _dir;                                                 //forgáshoz használt változó
        public Boolean _iscarrying;                                         //Cipel- már szekrényt a robot
        public Boolean _goback;                                             //Vissza kell-e vinnie a szekrényt
        public List<Int32> DestXList;                                       //Listában a célállomások koordinátái
        public List<Int32> DestYList;
        public List<Int32> Travel_x;                                        //Listéban, hogy milyen úton kell visszvinni a szekrényt
        public List<Int32> Travel_y;
        public Int32 ChargeX;                                               //Töltő koordinátái
        public Int32 ChargeY;
        public Boolean NeedToCharge;                                        //Eldönti, hogy kell-e tölteni
        public Int32 StepCount;                                             //Számolni kell a lépés számot a statisztikához 
        public Int32 Chargetime;                                            //Hányszor kellett töltenie
        public Int32 worked;                                                //Számoljuk hány szekréynt szállított ki


        public Robot()
        {
            this._color = COLORS.ORANGE;
            DestXList = new List<Int32>();
            DestYList = new List<Int32>();
            Travel_x = new List<Int32>();
            Travel_y = new List<Int32>();
            NeedToCharge = false;
            Chargetime = 0;
            _dir = 1;
            StepCount = 0;
            worked = 0;
        }

        
        public Int32 Battery { get { return _battery; } set { _battery = value; } }

        public Int32 Dir { get { return _dir; } set { _dir = value; } }

        public Boolean IsCarrying { get { return _iscarrying; } set { _iscarrying = value; } }

        public Int32 Target_x { get { return _target_x; } set { _target_x = value; } }

        public Int32 Target_y { get { return _target_y; } set { _target_y = value; } }

        public Int32 Dest_x { get { return _dest_x; } set { _dest_x = value; } }

        public Int32 Dest_y { get { return _dest_y; } set { _dest_y = value; } }

        public Int32 Start_x { get { return _start_x; } set { _start_x = value; } }

        public Int32 Start_y { get { return _start_y; } set { _start_y = value; } }

        public Int32 ExtraMove { get { return _extraMove; } set { _extraMove = value; } }

        public Boolean Goback { get { return _goback; } set { _goback = value; } }

        public Boolean Step(Int32 x, Int32 y)
        {
            return false;
        }

        public Boolean Put()
        {
            return false;
        }

        public Boolean Drop()
        {
            return false;
        }

        public void Turn(Int32 dir)
        {
            // -1 - turn left 
            // 1 - turn right
            // (?)
            _dir = (_dir % 4) + dir;
        }

        public void TurnLeft()
        {
            _dir -= 1;
            if (_dir == 0)
                _dir = 4;
            _battery--;
        }
        public void TurnRight()
        {
            _dir = (_dir % 4) + 1;
            _battery--;
        }
    }
}
