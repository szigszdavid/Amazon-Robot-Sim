using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using project6.Persistence.Fields;

namespace project6.Persistence
{
    public class Table
    {
        private Int32[,] _fieldValues;                                                              //Számok alapjána az egyes mezők típusai pl.: szekréyn - 2, robot - 4 
        public Field[,] _fieldTypes;                                                                //Típus alapján a mezők
        private Int32 _stepCount;                                                                   //Lépészám
        private Int32 _rowSize;                                                                     //Sor mérete
        private Int32 _columnSize;                                                                  //oszlop mérete
        private Int32 _startCharge;                                                                 //Kezdőtöltés
        private Int32 _SCount;
        private Int32 _RCount;
        private UInt64 _simtime;                                                                    //Eltelt idő
        public SortedSet<Int32>[,] _parcels;                                                        //Termékek számai egy listában
        public Robot[] Robots;                                                                      //Robotok minden tulajdonságukkal

        public Int32 StepCount { get { return _stepCount; } set { _stepCount = value; } }
        public Int32 RowSize { get { return _rowSize; } set { _rowSize = value; } }
        public Int32 ColumnSize { get { return _columnSize; } set { _columnSize = value; } }
        public Int32 StartCharge { get { return _startCharge; } set { _startCharge = value; } }
        public UInt64 SimTime { get { return _simtime; } set { _simtime = value; } }

        public Int32[,] FieldTValues { get { return _fieldValues; } }
        public Field[,] FieldTypes { get { return _fieldTypes; } }

        public Table()
        {
            _fieldValues = new Int32[10, 10];
            _fieldTypes = new Field[10, 10];
            _parcels = new SortedSet<Int32>[10, 10];
            Robots = new Robot[1];
            _rowSize = 10;
            _columnSize = 10;
            _simtime = 0;
        }

        public Table(Int32 row, Int32 column)
        {

            _fieldValues = new Int32[row, column];
            _fieldTypes = new Field[row, column];
            _parcels = new SortedSet<Int32>[row, column];
            _rowSize = row;
            _columnSize = column;
            Robots = new Robot[CountRobots()];
        }

        public Int32 CountGoals()                                       //Megszámoljuk a célállomásokat
        {
            _SCount = 0;
            for(Int32 i = 0; i < RowSize; i++)
            {
                for(Int32 j = 0; j < ColumnSize; j++)
                {
                    if (GetField(i, j) == 3)
                        _SCount++;
                }
            }
            return _SCount;
        }

        public Int32 CountRobots()                                          //Megszámoljuk a robotokat
        {
            _RCount = 0;
            for (Int32 i = 0; i < RowSize; i++)
            {
                for (Int32 j = 0; j < ColumnSize; j++)
                {
                    if (GetField(i, j) == 4)
                        _RCount++;
                }
            }
            return _RCount;
        }

        public Int32 this[Int32 x, Int32 y] { get { return GetField(x, y); } }
        public Int32 GetField(Int32 x, Int32 y)
        {
            return _fieldValues[x, y];
        }

        public void SetValue(Int32 x, Int32 y, Int32 v)
        {
            _fieldValues[x, y] = v;
        }

        public SortedSet<Int32> GetParcels(Int32 x, Int32 y)
        {
            return _parcels[x, y];
        }

        public void SetParcels(Int32 x, Int32 y, Int32 v)                       //Beállítjuk a termékeket.
        {
            if(_parcels[x, y] == null)
                _parcels[x, y] = new SortedSet<Int32>();
            _parcels[x, y].Add(v);
        }

 

    }
}
