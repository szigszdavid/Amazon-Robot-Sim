using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using project6.Persistence;
using project6.Persistence.Fields;

namespace project6.Model
{
    public class SimulationModel
    {
        #region Private variables

        private IDataAccess _dataAccess;    // A perzisztenciához szükséges változó.
        private Table _table;               // egy tábla típusba mentjük el a pályával kapcsolatos legfontosabb adatokat, például méretek, robotok, szekrények adatai.
        private Int32 _stepCount;
        private UInt64 _modelSimTime;       //A statisztikához nyilván kell tartani az eltelt időt, erre van ez a vátozó.
        private Int32 _rowSize = 10;        //Alapértelemezetten egy 10x10-es táblát jelenítünk meg, ez annak a sor mérete.
        private Int32 _columnSize = 10;     //Alapértelemezetten egy 10x10-es táblát jelenítünk meg, ez annak az oszlop mérete.
        public Int32 _robots = 0;           //Hasznos, ha tudjuk hány darab robot van a szimulációban, ez a változó a Simulation() függvény első for ciklusában is megjelenik, illetve a viewnak is nagy szüksége van rá.
        public Robot[] Robots;              //A robotokat egy Robots[]-ban tároljuk,ezekben Robot típusú elemek vannak, ennek pedig sok adattagja van.
        public Stand[] Stands;              //A szekrények tárolására használt tömb, vagyis a mi Stand típusunkkal létrehozott egy dimenziós tömb. 
        private Int32 _id = 1;              // A robotknak és célállomásoknak is kell egy azonosító.
        private Int32 GoalCtr = 0;          //Ebben a változóban tároljuk el a célállomások számát, ez későbbi ciklusokhoz kell.
        private Int32 Leftway;              //A forgáshoz felhasznált változó értéke = 1; 
        private Int32 RigthWay;             //A forgáshoz felhasznált változó értéke = 2;
        private Int32 Forward;              //A forgáshoz felhasznált változó értéke = 3;
        private Int32 Backward;             //A forgáshoz felhasznált változó értéke = 4;
        private Boolean firstround = true;  //Tudnunk kell, hogy még csak az első kör van, mivel akkor van van vége a szimulációnak, amikor már nics több szekrény és a robotok a kiindulási helyükön vannak.

        #endregion

        #region Public methods for other parts of the Solution

        public Table Table { get { return _table; } } // A viewnak lehet szüksége lesz a table-re
        public Int32 RowSize { get { return _rowSize; } set { _rowSize = value; } }
        public Int32 ColumnSize { get { return _columnSize; } set { _columnSize = value; } }

        public UInt64 ModelSimTime { get { return _modelSimTime; } set { _modelSimTime = value; } } //Át kell adni a viewnak az eltelt időt, erre való ez a függvény }

        public Boolean FirstRound { get { return firstround; } set { firstround = value; } } // A egyik tesztesetünkben ellenőrizzük, hogy első kör van-e

        #endregion

        #region Public events

        public event EventHandler<MyEventArgs> SimAdvanced;         //Ez egy olyan event, ami minden lépés után kiváltódik és azért vanrá szükség mert ezzel frissül később a view
        public event EventHandler<MyEventArgs> SimOver;             //Ez az event akkor váltódik ki amikor minden robot "hazament", vagyis vége a szimulációnak
        public event EventHandler<TimerEventArgs> TimerAdvanced;    //Az idő múlásának megjelenítéshez használt event.

        #endregion

        #region Constructor
        public SimulationModel(IDataAccess dataAccess) //Inicializáljuk a legfontosabb adatokat
        {
            _dataAccess = dataAccess;
            firstround = true;
            _table = new Table();
            Robots = new Robot[_robots];
            Stands = new Stand[0];
        }

        #endregion 

        #region NewSimulation
        public void NewSimulation() //Üres tablet készít
        {
            firstround = true;
            _stepCount = 0;
            _table = new Table(_rowSize, _columnSize);
            Initialize_Table();
            ResetTable();
            _modelSimTime = 0;
        }

        #endregion

        #region Check simulation over

        public void CheckSimOver() //Ez a függvény ellenőrzi, hogy minden Robot hazament-e már
        {


            for(int i = 0; i < _robots; i++)
            {
               
               if (Robots[i].X != Robots[i].Start_x || Robots[i].Y != Robots[i].Start_y) // A robot Start_X-e és Start_Y-ja,az a hely ahonnan indult. Ebben az if-ben az aktuális koordinátát hasonlítja össze a legelső koordinátákkal.
                        return;
            }

            OnSimOver(true); //Ebben a függvényben váltódik ki az az esemény, ami a szimuláció végét jelzi

        }

        #endregion

        #region RobotTarget
        public void RobotTarget(int a) // Ezzel a függvénnyel az dönti el a Robots[] a. tagja, hogy melyik szekrényért menjen el. 
        {
            for (int i = 0; i < _rowSize; i++)
            {
                for (int j = 0; j < _columnSize; j++)
                {
                    if (_table._fieldTypes[i, j].GetType() == typeof(Stand) && _table._fieldTypes[i, j].IsTargeted == false && _table.GetParcels(i, j).Count > 1) ///3 dolgot ellenőriz : 1. A tábala (i,j) koordinátájú pontja szekrény(Stand) típusú-e   2. Ezt a szekrényt más Robot már nem választotta ki    3. Ha több mint egy szám van a szekrényben, akkor az egy jó szekrény, mivel az üres szekrényben is van egy nulla szám, kell ennek az ellenőrzése.
                    {
                        Robots[a].Target_x = i;                     //Elmentjük a Robotba, hogy hol van aszekrénye
                        Robots[a].Target_y = j;
                        _table._fieldTypes[i, j].IsTargeted = true; // A robot is lefoglalja magának a szekrényt, ezt elmentjük a tablebe is, ez azért jó, mert 2 robot nem fog ugyanazért menni

                        return;
                    }
                }

            }

            Robots[a].Target_x = Robots[a].Start_x;

            Robots[a].Target_y = Robots[a].Start_y;

        }

        #endregion

        #region ShorterWay
        public Int32 XShorterWay(Int32 a, Int32 b) // Eldönti, hogy ha akadályba kerül a robot, hogy merre menjen. Például ha nem mehet előre akkor a bal vagy jobbra út a szerncsésebb.
        {
            if(a < b)
            {
                return a;
            }

            else
            {
                return b;
            }
        }

        #endregion

        #region ResetTable()
        public void ResetTable() // Visszaállítja a táblát alapbeállításokra
        {
            for(int i = 0; i < _robots; i++)
            {
                Robots[i].X = Robots[i].Start_x;
                Robots[i].Y = Robots[i].Start_y;
                Robots[i].IsCarrying = false;
                Robots[i].Goback = false;
                Robots[i].ExtraMove = 0;

                ChangingColors(i);
            }
        }

        #endregion

        #region Initialize_Table()
        public void Initialize_Table() // Elkészítjük a táblát
        {
            for (int i = 0; i < _rowSize; i++)
            {
                for (int j = 0; j < _columnSize; j++)
                {
                    switch (_table.GetField(i, j)) // Minden mezőt beállítunk valamilyen típusra, ez lehet robot (Robot), szekrény (Stand), célállomás (Goal), töltő (Charger) vagy üres (Empty)
                    {
                        case 0:
                            _table._fieldTypes[i, j] = new Empty();
                            break;
                        case 1:
                            _table._fieldTypes[i, j] = new Charger();
                            break;
                        case 2:
                            _table._fieldTypes[i, j] = new Stand();
                            _table._fieldTypes[i, j].IsTargeted = false;
                            break;
                        case 3:
                            _table._fieldTypes[i, j] = new Goal();

                        
                            break;
                        case 4:
                            _table._fieldTypes[i, j] = new Robot();
                         
                            break;
                    }
                    _table._fieldTypes[i, j].X = i; //Koordinátkat is hasznos elmenteni, főleg a mentés miatt
                    _table._fieldTypes[i, j].Y = j;
                }
            }

            
            Int32 RobotCtr = 0; //Eltároljuk a Robotok számát
            Int32 StandCtr = 0; //Eltároljuk a szekrények számát
            GoalCtr = 0;

            for (int i = 0; i < RowSize; i++)
            {
                for (int j = 0; j < ColumnSize; j++) // A cél, a szekrény és a robotok is kapnak id-t 
                {
                    if (_table._fieldTypes[i, j].GetType() == typeof(Goal))
                    {
                        GoalCtr++;
                        _table._fieldTypes[i, j].Id = GoalCtr;
                    }
                    else if (_table._fieldTypes[i, j].GetType() == typeof(Robot))
                    {
                        RobotCtr++;
                        _table._fieldTypes[i, j].Id = RobotCtr;
                    }
                    else if (_table._fieldTypes[i, j].GetType() == typeof(Stand))
                    {
                        StandCtr++;
                        _table._fieldTypes[i, j].Id = StandCtr;
                    }
                }
            }

            _robots = RobotCtr;


            Robots = new Robot[RobotCtr];           //Létrehozzuk a Robotok és szekréynek tömbjét
            Table.Robots = new Robot[RobotCtr];
            Stands = new Stand[StandCtr];

            Int32 r = 0;
            Int32 s = 0;
            for(int i = 0; i < _rowSize; i++)
            {
                for(int j = 0; j < _columnSize; j++)
                {
                    if (_table._fieldTypes[i, j].GetType() == typeof(Robot))
                    {
                        Robots[r] = (Robot)_table._fieldTypes[i, j];
                        Robots[r].Start_x = i;                  //Minden robotba belerakjuk azt, hogy honnan indult, mert ide kell majd visszamennie, a szimuláció végén.
                        Robots[r].Start_y = j;
                        Robots[r].ExtraMove = 0;                
                        Robots[r].Goback = false;               //Ha beért a célba utána vissza kell vinnie a szekrényt a helyére ennek ellenőrzésére van ez az adattag
                        Robots[r].Battery = Table.StartCharge;  //Inicializáljuk az akkumulátort
                        r++;
                    }
                    else if(_table._fieldTypes[i, j].GetType() == typeof(Stand))
                    {
                        Stands[s] = (Stand)_table.FieldTypes[i, j];

                        s++;
                        _table.FieldTypes[i, j].Parcels = Table.GetParcels(i, j); // Itt írjuk a szekréynbe a termékeket
                    }
                }
            }

            for (int a = 0; a < _robots; a++)
            {
                RobotTarget(a); // Minden robot választ magnának egy szekrényt.
            }
        }

        #endregion

        #region DestXisBigger method
        public void DestXisBigger(int i)                                                                                                        // Akkor használjuk ezt a függvényt, ha arobot már cipel szekrényt és a célállomás felé szeretne menni.
        {
            if (Robots[i].X < Robots[i].Dest_x && _table.GetField(Robots[i].X + 1, Robots[i].Y) != 2)                                           // Ha az aktuális x koordináta kisebb, mint a kiválasztott cél x koordinátája, akkor jobbra lépünk. A következő lépésben nem léphet szekrényre,mert már cipel egyet. A table száma a 2 (a fájlban és innen olvasunk ki), ezért hasonlítjuk a koordináta értékét 2-vel
            {
                _table.SetValue(Robots[i].X + 1, Robots[i].Y, 4);
                _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                Robots[i].X++;

                Robots[i].Travel_x.Add(Robots[i].X);                                                                                            //El kell menteni, hogy merre volt, mert ezen az úton viszi majd vissza a szekrényt a helyére.
                Robots[i].Travel_y.Add(Robots[i].Y);

                Robots[i].StepCount++;                                                                                                          //Stattisztikához kell minden Robot lépészáma külön-külön.

                Robots[i].ExtraMove = 0;                                                                                                        // Az ExtraMove azért kell, hogy amikor a robot cipel szekrényt és kikerüli és újraszámolja a legrövidebb utat, akkor ne kapja azt az eredményt, hogy a rossz helyre kell visszalépnie.

         

            }
            else if (Robots[i].X < Robots[i].Dest_x && _table.GetField(Robots[i].X + 1, Robots[i].Y) == 2 && Robots[i].IsCarrying == false)     // Ha nem cipel szekrényt a robot és ha a következő lépésben már szekréynre lépne, akkor ráléphet
            {
                

                _table.SetValue(Robots[i].X + 1, Robots[i].Y, 2);                                                                               // Lecseréli a következő helyet szürkére
                _table.SetValue(Robots[i].X, Robots[i].Y, 0);                                                                                   // Lecseréli az aktuális helyet fehérre

                Robots[i].X++;

                Robots[i].Travel_x.Add(Robots[i].X);
                Robots[i].Travel_y.Add(Robots[i].Y);

                Robots[i].StepCount++;

                Robots[i].ExtraMove = 0;

               
            }

            else if (Robots[i].X < Robots[i].Dest_x && _table.GetField(Robots[i].X + 1, Robots[i].Y) == 2 && Robots[i].IsCarrying == true)  /// Ha a robot cipel szekrényt és szekrénnyel találja szembe magát, akkor eldönti, hogy balra vagy jobbra lépjen.
            {
                Leftway = Math.Abs(Robots[i].Dest_x - Robots[i].X) + Math.Abs(Robots[i].Dest_y - (Robots[i].Y - 1));
                RigthWay = Math.Abs(Robots[i].Dest_x - Robots[i].X) + Math.Abs(Robots[i].Dest_y - (Robots[i].Y + 1));

                if (XShorterWay(Leftway, RigthWay) == Leftway && _table.GetField(Robots[i].X, Robots[i].Y - 1) != 2)                        // Kiválasztjuk, hogy jelenesetben balra vagy jobbra érdemesebb egy lépést tenni, ha előttünk van egy szekrény és a robot is szekrényt cipel.
                {
                    _table.SetValue(Robots[i].X, Robots[i].Y - 1, 4);
                    _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                    Robots[i].Y--;

                    Robots[i].StepCount++;

                    Robots[i].Travel_x.Add(Robots[i].X);
                    Robots[i].Travel_y.Add(Robots[i].Y);
                }

                else
                {
                    _table.SetValue(Robots[i].X, Robots[i].Y + 1, 4);
                    _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                    Robots[i].Y++;

                    Robots[i].StepCount++;

                    Robots[i].Travel_x.Add(Robots[i].X);
                    Robots[i].Travel_y.Add(Robots[i].Y);
                }

                Robots[i].ExtraMove = 1;                                                                                                        // A fentiek alapján amikor eldönti, hogy ablra vagy jobbra lépjen és nem akarjuk, hogy végtelen ciklusban oda-vissza lépkedjen robot, muszáj hogy legyen egy extra lépés amit azért kell elkövessen a robot, hogy új legrövideb utat találjon
            }
        }

        #endregion

        #region DestXSmaller method

        public void DestXisSmaller(int i)                                                                                                       // Teljesen ugyanúgy működik, mint a fenti DestXisBigger, azzal a különbséggel, hogy most lefelé megy a x tengely mentén
        {
            
            if (Robots[i].X > Robots[i].Dest_x && _table.GetField(Robots[i].X - 1, Robots[i].Y) != 2)
            {
                _table.SetValue(Robots[i].X - 1, Robots[i].Y, 4);
                _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                Robots[i].X--;

                Robots[i].StepCount++;

                Robots[i].Travel_x.Add(Robots[i].X);
                Robots[i].Travel_y.Add(Robots[i].Y);

                Robots[i].ExtraMove = 0;



            }
            else if (Robots[i].X > Robots[i].Dest_x && _table.GetField(Robots[i].X - 1, Robots[i].Y) == 2 && Robots[i].IsCarrying == false)
            {

                _table.SetValue(Robots[i].X - 1, Robots[i].Y, 2);
                _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                Robots[i].X--;

                Robots[i].StepCount++;

                Robots[i].Travel_x.Add(Robots[i].X);
                Robots[i].Travel_y.Add(Robots[i].Y);

                Robots[i].ExtraMove = 0;

            }

            else if (Robots[i].X > Robots[i].Dest_x && _table.GetField(Robots[i].X - 1, Robots[i].Y) == 2 && Robots[i].IsCarrying == true)
            {
                Leftway = Math.Abs(Robots[i].Dest_x - Robots[i].X) + Math.Abs(Robots[i].Dest_y - (Robots[i].Y - 1));
                RigthWay = Math.Abs(Robots[i].Dest_x - Robots[i].X) + Math.Abs(Robots[i].Dest_y - (Robots[i].Y + 1));

                if (XShorterWay(Leftway, RigthWay) == Leftway && _table.GetField(Robots[i].X, Robots[i].Y - 1) != 2)
                {
                    _table.SetValue(Robots[i].X, Robots[i].Y - 1, 4);
                    _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                    Robots[i].Y--;

                    Robots[i].StepCount++;

                    Robots[i].Travel_x.Add(Robots[i].X);
                    Robots[i].Travel_y.Add(Robots[i].Y);
                }

                else
                {
                    _table.SetValue(Robots[i].X, Robots[i].Y + 1, 4);
                    _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                    Robots[i].Y++;

                    Robots[i].StepCount++;

                    Robots[i].Travel_x.Add(Robots[i].X);
                    Robots[i].Travel_y.Add(Robots[i].Y);
                }

                Robots[i].ExtraMove = 2;
            }
        }

        #endregion

        #region DestYisBigger

        public void DestYisBigger(int i)                                                                                                        // Teljesen ugyanúgy működik, mint a DestXisBigger, csak itt már az y koordináta tengely mentén mozog a robot és felfelé megy ha meghívódik ez a függvény
        {
            if (Robots[i].Y < Robots[i].Dest_y && _table.GetField(Robots[i].X, Robots[i].Y + 1) != 2)
            {
                _table.SetValue(Robots[i].X, Robots[i].Y + 1, 4);
                _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                Robots[i].Y++;

                Robots[i].StepCount++;

                Robots[i].Travel_x.Add(Robots[i].X);
                Robots[i].Travel_y.Add(Robots[i].Y);

                Robots[i].ExtraMove = 0;
                
            }
            else if (Robots[i].Y < Robots[i].Dest_y && _table.GetField(Robots[i].X, Robots[i].Y + 1) == 2 && Robots[i].IsCarrying == false)
            {
                
                _table.SetValue(Robots[i].X, Robots[i].Y + 1, 2);
                _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                Robots[i].Y++;

                Robots[i].StepCount++;

                Robots[i].Travel_x.Add(Robots[i].X);
                Robots[i].Travel_y.Add(Robots[i].Y);

                Robots[i].ExtraMove = 0;
                
            }

            else if (Robots[i].Y < Robots[i].Dest_y && _table.GetField(Robots[i].X, Robots[i].Y + 1) == 2 && Robots[i].IsCarrying == true)
            {
                Forward = Math.Abs(Robots[i].Dest_x - (Robots[i].X + 1)) + Math.Abs(Robots[i].Dest_y - Robots[i].Y);
                Backward = Math.Abs(Robots[i].Dest_x - (Robots[i].X - 1)) + Math.Abs(Robots[i].Dest_y - Robots[i].Y);

                if (XShorterWay(Forward, Backward) == Forward && _table.GetField(Robots[i].X + 1, Robots[i].Y) != 2)
                {
                    _table.SetValue(Robots[i].X + 1, Robots[i].Y, 4);
                    _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                    Robots[i].X++;

                    Robots[i].StepCount++;

                    Robots[i].Travel_x.Add(Robots[i].X);
                    Robots[i].Travel_y.Add(Robots[i].Y);
                }
                else
                {
                    _table.SetValue(Robots[i].X - 1, Robots[i].Y, 4);
                    _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                    Robots[i].X--;

                    Robots[i].StepCount++;

                    Robots[i].Travel_x.Add(Robots[i].X);
                    Robots[i].Travel_y.Add(Robots[i].Y);
                }

                Robots[i].ExtraMove = 3;

            }
        }

        #endregion

        #region DestYisSmaller

        public void DestYisSmaller(int i)                                                                                                        // Teljesen ugyanúgy működik, mint a DestXisBigger, csak itt már az y koordináta tengely mentén mozog a robot és lefelé megy ha meghívódik ez a függvény
        {
            if (Robots[i].Y > Robots[i].Dest_y && _table.GetField(Robots[i].X, Robots[i].Y - 1) != 2)
            {
                _table.SetValue(Robots[i].X, Robots[i].Y - 1, 4);
                _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                Robots[i].Y--;

                Robots[i].StepCount++;

                Robots[i].Travel_x.Add(Robots[i].X);
                Robots[i].Travel_y.Add(Robots[i].Y);

                Robots[i].ExtraMove = 0;

                

            }
            else if (Robots[i].Y > Robots[i].Dest_y && _table.GetField(Robots[i].X, Robots[i].Y - 1) == 2 && Robots[i].IsCarrying == false)
            {

                _table.SetValue(Robots[i].X, Robots[i].Y - 1, 2);
                _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                Robots[i].Y--;

                Robots[i].StepCount++;

                Robots[i].Travel_x.Add(Robots[i].X);
                Robots[i].Travel_y.Add(Robots[i].Y);

                Robots[i].ExtraMove = 0;

                

            }

            else if (Robots[i].Y > Robots[i].Dest_y && _table.GetField(Robots[i].X, Robots[i].Y - 1) == 2 && Robots[i].IsCarrying == true)
            {
                Forward = Math.Abs(Robots[i].Dest_x - (Robots[i].X + 1)) + Math.Abs(Robots[i].Dest_y - Robots[i].Y);
                Backward = Math.Abs(Robots[i].Dest_x - (Robots[i].X - 1)) + Math.Abs(Robots[i].Dest_y - Robots[i].Y);

                if (XShorterWay(Forward, Backward) == Forward && _table.GetField(Robots[i].X + 1, Robots[i].Y) != 2)
                {
                    _table.SetValue(Robots[i].X + 1, Robots[i].Y, 4);
                    _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                    Robots[i].X++;

                    Robots[i].StepCount++;

                    Robots[i].Travel_x.Add(Robots[i].X);
                    Robots[i].Travel_y.Add(Robots[i].Y);
                }
                else
                {
                    _table.SetValue(Robots[i].X - 1, Robots[i].Y, 4);
                    _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                    Robots[i].X--;

                    Robots[i].StepCount++;

                    Robots[i].Travel_x.Add(Robots[i].X);
                    Robots[i].Travel_y.Add(Robots[i].Y);
                }

                Robots[i].ExtraMove = 4;
            }
        }

        #endregion

        #region ChangingColors

        public void ChangingColors(int a)                                                                                       //Ezzel a függvénnyel friissítjük a táblában lévő színeket: töltő - 1 (kék), szekrény - 2(szürke)
        {
            
            for(int i = 0; i < _rowSize; i++)
            {
                for(int j= 0; j < _columnSize; j++)
                {
                    if(_table._fieldTypes[i, j].GetType() == typeof(Stand) && Robots[a].X != i && Robots[a].Y != j)
                    {
                        _table.SetValue(i, j, 2);
                    }

                    if (_table._fieldTypes[i, j].GetType() == typeof(Goal) && Robots[a].X != i && Robots[a].Y != j)
                    {
                        _table.SetValue(i, j, 3);
                    }


                    if (_table._fieldTypes[i, j].GetType() == typeof(Charger) && Robots[a].X != i && Robots[a].Y != j)
                    {
                        _table.SetValue(i, j, 1);
                    }
                }
            }
        }

        #endregion

        #region CalculateDist
        public Int32 CalculateDist(Int32 curr_x, Int32 curr_y, Int32 targ_x, Int32 targ_y, Int32 dest_x, Int32 dest_y) //Kiszámolja, hogy mennyi lépéps kell a robotnak, míg eljutna a célba, ha kevesebb mint az akkumulátor szint, akkor elmegy tölteni
        {
            Int32 charger_x = -1, charger_y = -1;
            Int32 min = -1;
            for(int i = 0; i < _rowSize; i++)
            {
                for(int j = 0; j < _columnSize; j++)
                {
                    if(_table._fieldTypes[i,j].GetType() == typeof(Charger))
                    {
                        if (min == -1)
                        {
                            charger_x = i;
                            charger_y = j;
                            min = Math.Abs(targ_x - charger_x) + Math.Abs(targ_y - charger_y);      ///első chargerhez vezető út hossza a targettől
                        }
                        else
                        {
                            if(min > (Math.Abs(targ_x - i) + Math.Abs(targ_y - j)))
                            {
                                charger_x = i;
                                charger_y = j;
                                min = Math.Abs(targ_x - charger_x) + Math.Abs(targ_y - charger_y);
                            }
                        }
                    }
                }
            }

            Int32 curr_to_targ = Math.Abs(curr_x - targ_x) + Math.Abs(curr_y - targ_y);                     //kiszámoljuk, hogy milyen messze van a célbavett szekrény
            Int32 targ_to_dest = Math.Abs(targ_x - dest_x) + Math.Abs(targ_y - dest_y);                     //Kiszámoljuk milyen messze lenne a cél
            Int32 targ_to_charger = Math.Abs(targ_x - charger_x) + Math.Abs(targ_y - charger_y);            //Kiszámoljuk milyen messze lenne a töltő

            double f1 = _table.StartCharge / 10;
            double f2 = _table.RowSize / 10;
            double f3 = _table.ColumnSize / 10;

            Double fault = Math.Round(f1+f2+f3);

            return curr_to_targ + (2 * targ_to_dest) + targ_to_charger + (int)fault;
        }

        #endregion

        #region Charge
        public void Charge(Robot robot)                                                                             /// Ez a függvény választ töltőt a robotnak
        {
            for(int i = 0; i < _rowSize; i++)
            {
                for(int j = 0; j < _columnSize; j++)
                {
                    if(_table._fieldTypes[i,j].GetType() == typeof(Charger) && !_table.FieldTypes[i,j].IsTargeted) //Ha másik robot már nem akar odamenni akkor megy bele
                    {
                        robot.ChargeX = i;                                                                          //A töltő koordinátja el lesz mentve a robotba
                        robot.ChargeY = j;
                        robot.NeedToCharge = true;
                        _table.FieldTypes[i, j].IsTargeted = true;                                                  //Innentől ehhez a töltőhöz nem lehet jönni, amíg rá nem lép a megfelelő robot
                    }
                }
            }

                                                                                              
        }       /// az adott robot targetjét átállítja a chargerre

        #endregion

        #region CopyRobots
        public void CopyRobots(Int32 robots)                ///Belerakjuk a table-be a Robots[] tömböt, mivel a mentést és betöltést a a table-n végezzük. 
        {
            for(Int32 i = 0; i < robots; i++)
            {
                _table.Robots[i] = Robots[i];
            }
        }

        #endregion

        #region Simulation
        public void Simulation()                                // A szimulációt végző függvény, közös minden lépés fajtában, hogy csökkentik az akkumulátor szintjét .
        {


            if (firstround == false)                            //Ha letelt az első kör utána minden függvény hívás elején ellenőrizzük, hogy vége van-e a szimulációnak.
            {
                CheckSimOver();
            }


            for (int i = 0; i < _robots; i++)                   //Az alapján, hogy az extra lépést melxik irányba kell megtenni újra kell tervezni az utat.
            {
                if (Robots[i].ExtraMove != 0)
                {
                    if (Robots[i].ExtraMove == 1)
                    {
                        DestXisBigger(i);
                    }

                    if (Robots[i].ExtraMove == 2)
                    {
                        DestXisSmaller(i);
                    }

                    if (Robots[i].ExtraMove == 3)
                    {
                        DestYisBigger(i);
                    }

                    if (Robots[i].ExtraMove == 4)
                    {
                        DestYisSmaller(i);
                    }

                    continue;
                }

                #region Go to Charger

                if (Robots[i].NeedToCharge == true)                                                 //Akkor lépünk ebbe az if-be ha kell tölteni a robot, ekkor a robot a megfelelő töltő állomáshoz megy
                {
                    if (Robots[i].X == Robots[i].ChargeX && Robots[i].Y == Robots[i].ChargeY)           
                    {
                        Robots[i].Battery = _table.StartCharge;                                     // ITT töltődik fel a robot
                        Robots[i].NeedToCharge = false;

                        Robots[i].Chargetime++;

                        continue;
                    }

                    if (Robots[i].X != Robots[i].ChargeX)
                    {
                        if (Robots[i].X < Robots[i].ChargeX && _table.GetField(Robots[i].X + 1, Robots[i].Y) != 2 && _table.GetField(Robots[i].X + 1, Robots[i].Y) != 4)
                        {
                            _table.SetValue(Robots[i].X + 1, Robots[i].Y, 4);
                            _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                            Robots[i].X++;
                            Robots[i].Battery--;

  
                        }

                        else if (Robots[i].X < Robots[i].ChargeX && _table.GetField(Robots[i].X + 1, Robots[i].Y) == 4)
                        {
                            continue;
                        }

                        else if (Robots[i].X < Robots[i].ChargeX && _table.GetField(Robots[i].X + 1, Robots[i].Y) == 2 && Robots[i].IsCarrying == false)
                        {
                            _table.SetValue(Robots[i].X + 1, Robots[i].Y, 2);
                            _table.SetValue(Robots[i].X, Robots[i].Y, 0);


                            Robots[i].X++;
                            Robots[i].Battery--;

   
                        }



                        if (Robots[i].X > Robots[i].ChargeX && _table.GetField(Robots[i].X - 1, Robots[i].Y) != 2 && _table.GetField(Robots[i].X - 1, Robots[i].Y) != 4)
                        {
                            _table.SetValue(Robots[i].X - 1, Robots[i].Y, 4);
                            _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                            Robots[i].X--;
                            Robots[i].Battery--;


                        }
                        else if (Robots[i].X > Robots[i].ChargeX && _table.GetField(Robots[i].X - 1, Robots[i].Y) == 2 && Robots[i].IsCarrying == false)
                        {
                            _table.SetValue(Robots[i].X - 1, Robots[i].Y, 2);
                            _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                            Robots[i].X--;
                            Robots[i].Battery--;


                        }

                        else if (Robots[i].X > Robots[i].ChargeX && _table.GetField(Robots[i].X - 1, Robots[i].Y) == 4)
                        {
                            continue;
                        }

                        continue;

                    }


                    if (Robots[i].Y != Robots[i].ChargeY)
                    {
                        if (Robots[i].Y < Robots[i].ChargeY && _table.GetField(Robots[i].X, Robots[i].Y + 1) != 2 && _table.GetField(Robots[i].X, Robots[i].Y + 1) != 4)
                        {
                            _table.SetValue(Robots[i].X, Robots[i].Y + 1, 4);
                            _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                            Robots[i].Y++;
                            Robots[i].Battery--;


                        }
                        else if (Robots[i].Y < Robots[i].ChargeY && _table.GetField(Robots[i].X, Robots[i].Y + 1) == 2 && Robots[i].IsCarrying == false)
                        {
                            _table.SetValue(Robots[i].X, Robots[i].Y + 1, 2);
                            _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                            Robots[i].Y++;
                            Robots[i].Battery--;


                        }

                        else if (Robots[i].Y < Robots[i].ChargeY && _table.GetField(Robots[i].X, Robots[i].Y + 1) == 4)
                            continue;

                        if (Robots[i].Y > Robots[i].ChargeY && _table.GetField(Robots[i].X, Robots[i].Y - 1) != 2 & _table.GetField(Robots[i].X, Robots[i].Y - 1) != 4)
                        {
                            _table.SetValue(Robots[i].X, Robots[i].Y - 1, 4);
                            _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                            Robots[i].Y--;
                            Robots[i].Battery--;

                        }
                        else if (Robots[i].Y > Robots[i].ChargeY && _table.GetField(Robots[i].X, Robots[i].Y - 1) == 2 && Robots[i].IsCarrying == false)
                        {



                            _table.SetValue(Robots[i].X, Robots[i].Y - 1, 2);
                            _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                            Robots[i].Y--;
                            Robots[i].Battery--;

                        }

                        else if (Robots[i].Y > Robots[i].ChargeY && _table.GetField(Robots[i].X, Robots[i].Y - 1) == 4)
                            continue;

                        continue;


                    }
                }


                #endregion

                if (Robots[i].Goback == true)                                                                                                               // A GoBack ronotonként akkor igaz, ha a robot a célba bevitte már a szekrényt, de azt vissza is kell vinnie azeredeti helyére, ekkor a robot elindul a szekrény régi helye felé
                {
                    if (Robots[i].Travel_x.Count >= 1 && Robots[i].Travel_y.Count >= 1)
                    {
                        if (_table.GetField(Robots[i].Travel_x[Robots[i].Travel_x.Count - 1], Robots[i].Travel_y[Robots[i].Travel_y.Count - 1]) != 4)
                        {
                            if (Robots[i].Travel_x.Count > 1 && Robots[i].Travel_y.Count > 1)
                            {
                                _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                                Robots[i].X = Robots[i].Travel_x[Robots[i].Travel_x.Count - 1];
                                Robots[i].Travel_x.RemoveAt(Robots[i].Travel_x.Count - 1);
                                Robots[i].Y = Robots[i].Travel_y[Robots[i].Travel_y.Count - 1];
                                Robots[i].Travel_y.RemoveAt(Robots[i].Travel_y.Count - 1);

                                ChangingColors(i);

                                _table.SetValue(Robots[i].X, Robots[i].Y, 4);

                                Robots[i].StepCount++;

                                continue;

                            }

                            if (Robots[i].Travel_x.Count == 1 && Robots[i].Travel_y.Count == 1)
                            {
                                _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                                Robots[i].X = Robots[i].Travel_x[Robots[i].Travel_x.Count - 1];
                                Robots[i].Travel_x.RemoveAt(Robots[i].Travel_x.Count - 1);
                                Robots[i].Y = Robots[i].Travel_y[Robots[i].Travel_y.Count - 1];
                                Robots[i].Travel_y.RemoveAt(Robots[i].Travel_y.Count - 1);

                                ChangingColors(i);

                                _table.SetValue(Robots[i].X, Robots[i].Y, 4);

                                Robots[i].IsCarrying = false;

                                Robots[i].StepCount++;

                                continue;
                            }
                        }
                        else
                            continue;
                    }



                }

                if (Robots[i].IsCarrying == false)                                                                                                                      //Ha az IsCarrying false, akkor a robot nem cipe szekrényt vagyis átmehet például más szekréynek alatt.
                {
                    ChangingColors(i);

                    if (Robots[i].X == Robots[i].Target_x && Robots[i].Y == Robots[i].Target_y)                                                                         //Itt veszi fel a robot a szekrényt, vagyis ha igaz az állítás vagyis most épp a kiszemelt szekrény alatt van.
                    {
                        if (Robots[i].Start_x == Robots[i].Target_x && Robots[i].Target_y == Robots[i].Start_y)                                                        //Ellenőrizzük, hogy nem a kiindulási pozíción állunk.
                        {
                            continue;
                        }

                        if(Robots[i].Goback == true)                                                                                                                    //Ha szekrényt a cálból visszahozzuk vagyis ha a GoABck true, akkor a robot ledobja magáól aszekrényt és újra nem cipel semmit, vgyis az IsCarrying már újra false lesz.
                        {
                            Robots[i].IsCarrying = false;
                            RobotTarget(i);                                                                                                                             //Kiválasztunk egy új szekrényt aminek termékeit le akarjuk szállítani.
                            if (Robots[i].Battery<CalculateDist(Robots[i].X, Robots[i].Y, Robots[i].Target_x, Robots[i].Target_y, Robots[i].Dest_x, Robots[i].Dest_y)) //Kiszámoljuk, hogy odaér-e a robot az akkumulátorával.
                                Charge(Robots[i]);
                            Robots[i].Goback = false;
                            continue;
                        }

                        Robots[i].Travel_x.Add(Robots[i].X);                                                                                                            //Elmentjük, hogy merre volt a robot, hogy könnyen visszatudjuk vinni a szekrényeket.
                        Robots[i].Travel_y.Add(Robots[i].Y);


                        for (Int32 j = 1; j <= GoalCtr; j++) 
                        {
                            if (_table.GetParcels(Robots[i].Target_x, Robots[i].Target_y).Contains(j))
                            {
                                for (int a = 0; a < _rowSize; a++)
                                {
                                    for (int b = 0; b < _columnSize; b++)
                                    {
                                        if (_table._fieldTypes[a, b].GetType() == typeof(Goal) && _table._fieldTypes[a, b].Id == j)
                                        {
                                            Robots[i].DestXList.Add(a);                                                                                                     //Kiolvassuk, hogy melyik célállomásokra kell mennie a robotnak, ezeket egy kkordináta tömbben mentjük.
                                            Robots[i].DestYList.Add(b);
                                        }
                                    }
                                }
                            }
                        }

                        Robots[i].Dest_x = Robots[i].DestXList[0];
                        Robots[i].DestXList.RemoveAt(0);
                        Robots[i].Dest_y = Robots[i].DestYList[0];
                        Robots[i].DestYList.RemoveAt(0);
                        Robots[i].IsCarrying = true;                                                                                                                    //Mivel mostantól cipelünk szekrény ezért az IsCarrying = true
                        continue;
                    }

                    if (Robots[i].X == Robots[i].Target_x && Robots[i].Y < Robots[i].Target_y && Robots[i].Dir != 2                                                     //Ebben az if-ben dől el, hogy kell-e forognia a robotnak, ha kell akkor jobbra fordul
                        || Robots[i].Y == Robots[i].Target_y && Robots[i].X > Robots[i].Target_x && Robots[i].Dir != 1
                        )
                    {
                        Robots[i].TurnRight();
                        continue;
                    }

                    if (Robots[i].X == Robots[i].Target_x && Robots[i].Y > Robots[i].Target_y && Robots[i].Dir != 4
                        || Robots[i].Y == Robots[i].Target_y && Robots[i].X < Robots[i].Target_x && Robots[i].Dir != 3
                        )
                    {
                        Robots[i].TurnLeft();                                                                                                                                    //Szintén ha a target y-ja kisebb mint a robot aktuális y-ja, de az x koordináták megegyeznek, akkor balra kell fordulnia.
                        continue;
                    }


                    if (Robots[i].X != Robots[i].Target_x)                                                                                                                          //Ebben az if-be akkor megy bele a program, ha robot nem cipel szekrényt és épp egy szekrényhez szeretne eljutni. Itt msot először az x koordinátát ellenőrizzük.
                    {
                        if (Robots[i].X < Robots[i].Target_x && _table.GetField(Robots[i].X + 1, Robots[i].Y) != 2 && _table.GetField(Robots[i].X + 1, Robots[i].Y) != 4)           //Ellenőrizzük, hogy a következő lépsben nem egy szekrényre vagy Robotra lépnénk, vagyis nem ütköznénk
                        {
                            _table.SetValue(Robots[i].X + 1, Robots[i].Y, 4);                                                                                                       //Kicseréli az üres és arobot mezőinek színét és a robto egygel feljebb lép
                            _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                            Robots[i].X++;

                            Robots[i].StepCount++;

                            Robots[i].Battery--;

                        }

                        else if (Robots[i].X < Robots[i].Target_x && _table.GetField(Robots[i].X + 1, Robots[i].Y) == 4)                                                            // Ha fölötte épp egy robot van, akkor vár, hogy az elmozduljon onnan
                        {
                            continue;
                        }

                        else if (Robots[i].X < Robots[i].Target_x && _table.GetField(Robots[i].X + 1, Robots[i].Y) == 2 && Robots[i].IsCarrying == false)                           // ha a robot épp nem cipel szekrény, akkor más szekrények alatt át tud menni
                        {
                            _table.SetValue(Robots[i].X + 1, Robots[i].Y, 2);
                            _table.SetValue(Robots[i].X, Robots[i].Y, 0);


                            Robots[i].X++;

                            Robots[i].StepCount++;

                            Robots[i].Battery--;

                        }



                        if (Robots[i].X > Robots[i].Target_x && _table.GetField(Robots[i].X - 1, Robots[i].Y) != 2 && _table.GetField(Robots[i].X - 1, Robots[i].Y) != 4)           // A formula ugyanaz, mint feljebb, csak itt az elleőrizzük, hogya target x kisebb-e mint az aktuális x. A különbség a fentiektől csak annyi, hogy most lefelé mozgunk.
                        {
                            _table.SetValue(Robots[i].X - 1, Robots[i].Y, 4);
                            _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                            Robots[i].X--;

                            Robots[i].StepCount++;

                            Robots[i].Battery--;


                        }
                        else if (Robots[i].X > Robots[i].Target_x && _table.GetField(Robots[i].X - 1, Robots[i].Y) == 2 && Robots[i].IsCarrying == false)
                        {
                            _table.SetValue(Robots[i].X - 1, Robots[i].Y, 2);
                            _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                            Robots[i].X--;

                            Robots[i].StepCount++;

                            Robots[i].Battery--;

                        }

                        else if (Robots[i].X > Robots[i].Target_x && _table.GetField(Robots[i].X - 1, Robots[i].Y) == 4)
                        {
                            continue;
                        }


                        continue;

                    }


                    if (Robots[i].Y != Robots[i].Target_y)                                                                                                                             //Y tengely ellenörzése, miután már jó x tengelyen vagyunk a szekrény felé.
                    {
                        if (Robots[i].Y < Robots[i].Target_y && _table.GetField(Robots[i].X, Robots[i].Y + 1) != 2 && _table.GetField(Robots[i].X, Robots[i].Y + 1) != 4)               // ha következő lépsben nem szekrényre lépnénk vagy másik robotra, akkor léphetünk.
                        {
                            _table.SetValue(Robots[i].X, Robots[i].Y + 1, 4);
                            _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                            Robots[i].Y++;

                            Robots[i].StepCount++;

                            Robots[i].Battery--;


                        }
                        else if (Robots[i].Y < Robots[i].Target_y && _table.GetField(Robots[i].X, Robots[i].Y + 1) == 2 && Robots[i].IsCarrying == false)                                   //Ha a robot nem cipel szekrényt, akkor átmehetünk alatta.
                        {
                            _table.SetValue(Robots[i].X, Robots[i].Y + 1, 2);
                            _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                            Robots[i].Y++;

                            Robots[i].StepCount++;

                            Robots[i].Battery--;


                        }

                        else if (Robots[i].Y < Robots[i].Target_y && _table.GetField(Robots[i].X, Robots[i].Y + 1) == 4)                                                                   //Ha robot lenne melletünk megvárjuk míg az elmozdul.
                            continue;




                        if (Robots[i].Y > Robots[i].Target_y && _table.GetField(Robots[i].X, Robots[i].Y - 1) != 2 & _table.GetField(Robots[i].X, Robots[i].Y - 1) != 4)        //Ugyanaz mint az előző formula csak most balra lépkedünk
                        {
                            _table.SetValue(Robots[i].X, Robots[i].Y - 1, 4);
                            _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                            Robots[i].Y--;

                            Robots[i].StepCount++;

                            Robots[i].Battery--;

                        }
                        else if (Robots[i].Y > Robots[i].Target_y && _table.GetField(Robots[i].X, Robots[i].Y - 1) == 2 && Robots[i].IsCarrying == false)
                        {



                            _table.SetValue(Robots[i].X, Robots[i].Y - 1, 2);
                            _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                            Robots[i].Y--;

                            Robots[i].StepCount++;

                            Robots[i].Battery--;



                        }

                        else if (Robots[i].Y > Robots[i].Target_y && _table.GetField(Robots[i].X, Robots[i].Y - 1) == 4)
                            continue;




                        continue;


                    }
                }

                else
                {
                    if (Robots[i].X == Robots[i].Dest_x && Robots[i].Y < Robots[i].Dest_y && Robots[i].Dir != 2
                        || Robots[i].Y == Robots[i].Dest_y && Robots[i].X > Robots[i].Dest_x && Robots[i].Dir != 1)
                    {
                        Robots[i].TurnRight();
                        continue;
                    }

                    if (Robots[i].X == Robots[i].Dest_x && Robots[i].Y > Robots[i].Dest_y && Robots[i].Dir != 4
                        || Robots[i].Y == Robots[i].Dest_y && Robots[i].X < Robots[i].Dest_x && Robots[i].Dir != 3)
                    {
                        Robots[i].TurnLeft();
                        continue;
                    }

                    ChangingColors(i);

                    if (Robots[i].X == Robots[i].Dest_x && Robots[i].Y == Robots[i].Dest_y)                                                             //Itt elelnőrizzük, hogy megérkeztünk-e a célba
                    {


                        _table.SetValue(Robots[i].Travel_x[Robots[i].Travel_x.Count - 1], Robots[i].Travel_y[Robots[i].Travel_y.Count - 1], 0);         //Ha igen, akkor elkezdhetünk visszafelé lépkedni a szekrény régi hely felé

                        Robots[i].worked++;                                                                                                             //Ha leadunk egy polcot, akkor növeljük a sikeres célba érések számát, ez kell a statisztikába
                                                    
                        if (Robots[i].DestXList.Count == 0)
                        {
                            Robots[i].Goback = true;                                                                                                    //Ha ebbe az if-be belemegyünk, akkor vissza kell vinni a szekréynt  arégi helyére
                            Robots[i].IsCarrying = true;
                        }




                        else
                        {
                            Robots[i].Dest_x = Robots[i].DestXList[0];
                            Robots[i].DestXList.RemoveAt(0);
                            Robots[i].Dest_y = Robots[i].DestYList[0];
                            Robots[i].DestYList.RemoveAt(0);
                        }




                        continue;
                        //}
                    }

                    if (Robots[i].X != Robots[i].Dest_x)                                                                                                                    //Ha még nem vagyunk a célállomáson, akkor bekerülünk ebeb az if-be.
                    {
                        if (Robots[i].X < Robots[i].Dest_x && _table.GetField(Robots[i].X + 1, Robots[i].Y) != 2 && _table.GetField(Robots[i].X + 1, Robots[i].Y) != 4)     // Ha nem szekrény vagy robot van a következő lépés helyén akkor odaléphetünk.
                        {
                            _table.SetValue(Robots[i].X + 1, Robots[i].Y, 4);
                            _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                            Robots[i].X++;
                            Robots[i].StepCount++;

                            Robots[i].Travel_x.Add(Robots[i].X);
                            Robots[i].Travel_y.Add(Robots[i].Y);

                            Robots[i].Battery--;

                            

                        }

                        else if (Robots[i].X < Robots[i].Dest_x && _table.GetField(Robots[i].X + 1, Robots[i].Y) == 4)                                                      // Ha robot van előttünk megvárjuk míg az elmozdul
                        {
                            continue;
                        }

                        else if (Robots[i].X < Robots[i].Dest_x && _table.GetField(Robots[i].X + 1, Robots[i].Y) == 2 && Robots[i].IsCarrying == false)                     // ha nem cipelünk szekrényt akkor átmehetünk alatta
                        {
                            
                            _table.SetValue(Robots[i].X + 1, Robots[i].Y, 2);
                            _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                            Robots[i].X++;

                            Robots[i].StepCount++;

                            Robots[i].Travel_x.Add(Robots[i].X);
                            Robots[i].Travel_y.Add(Robots[i].Y);

                            Robots[i].Battery--;

                        }



                        else if (Robots[i].X < Robots[i].Dest_x && _table.GetField(Robots[i].X + 1, Robots[i].Y) == 2 && Robots[i].IsCarrying == true)
                        {
                            Leftway = Math.Abs(Robots[i].Dest_x - Robots[i].X) + Math.Abs(Robots[i].Dest_y - (Robots[i].Y - 1));
                            RigthWay = Math.Abs(Robots[i].Dest_x - Robots[i].X) + Math.Abs(Robots[i].Dest_y - (Robots[i].Y + 1));

                            if (XShorterWay(Leftway, RigthWay) == Leftway && _table.GetField(Robots[i].X, Robots[i].Y - 1) != 2)
                            {
                                _table.SetValue(Robots[i].X, Robots[i].Y - 1, 4);
                                _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                                Robots[i].Y--;

                                Robots[i].StepCount++;

                                Robots[i].Travel_x.Add(Robots[i].X);
                                Robots[i].Travel_y.Add(Robots[i].Y);

                                Robots[i].Battery--;
                            }

                            else
                            {
                                _table.SetValue(Robots[i].X, Robots[i].Y + 1, 4);
                                _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                                Robots[i].Y++;

                                Robots[i].StepCount++;

                                Robots[i].Travel_x.Add(Robots[i].X);
                                Robots[i].Travel_y.Add(Robots[i].Y);

                                Robots[i].Battery--;
                            }

                            Robots[i].ExtraMove = 1;
                        }

                        if (Robots[i].X > Robots[i].Dest_x && _table.GetField(Robots[i].X - 1, Robots[i].Y) != 2 && _table.GetField(Robots[i].X - 1, Robots[i].Y) != 4)
                        {
                            _table.SetValue(Robots[i].X - 1, Robots[i].Y, 4);
                            _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                            Robots[i].X--;

                            Robots[i].StepCount++;

                            Robots[i].Travel_x.Add(Robots[i].X);
                            Robots[i].Travel_y.Add(Robots[i].Y);

                            Robots[i].Battery--;


                        }

                        else if (Robots[i].X > Robots[i].Dest_x && _table.GetField(Robots[i].X - 1, Robots[i].Y) == 4)
                            continue;

                        else if (Robots[i].X > Robots[i].Dest_x && _table.GetField(Robots[i].X - 1, Robots[i].Y) == 2 && Robots[i].IsCarrying == false)
                        {

                            _table.SetValue(Robots[i].X - 1, Robots[i].Y, 2);
                            _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                            Robots[i].X--;

                            Robots[i].StepCount++;

                            Robots[i].Travel_x.Add(Robots[i].X);
                            Robots[i].Travel_y.Add(Robots[i].Y);

                            Robots[i].Battery--;

                        }

                        else if (Robots[i].X > Robots[i].Dest_x && _table.GetField(Robots[i].X - 1, Robots[i].Y) == 2 && Robots[i].IsCarrying == true)
                        {
                            Leftway = Math.Abs(Robots[i].Dest_x - Robots[i].X) + Math.Abs(Robots[i].Dest_y - (Robots[i].Y - 1));
                            RigthWay = Math.Abs(Robots[i].Dest_x - Robots[i].X) + Math.Abs(Robots[i].Dest_y - (Robots[i].Y + 1));

                            if (XShorterWay(Leftway, RigthWay) == Leftway && _table.GetField(Robots[i].X, Robots[i].Y - 1) != 2)
                            {
                                _table.SetValue(Robots[i].X, Robots[i].Y - 1, 4);
                                _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                                Robots[i].Y--;

                                Robots[i].StepCount++;

                                Robots[i].Travel_x.Add(Robots[i].X);
                                Robots[i].Travel_y.Add(Robots[i].Y);

                                Robots[i].Battery--;
                            }

                            else
                            {
                                _table.SetValue(Robots[i].X, Robots[i].Y + 1, 4);
                                _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                                Robots[i].Y++;

                                Robots[i].StepCount++;

                                Robots[i].Travel_x.Add(Robots[i].X);
                                Robots[i].Travel_y.Add(Robots[i].Y);

                                Robots[i].Battery--;
                            }

                            Robots[i].ExtraMove = 2;
                        }

                        continue;

                    }




                    if (Robots[i].Y != Robots[i].Dest_y)
                    {
                        if (Robots[i].Y < Robots[i].Dest_y && _table.GetField(Robots[i].X, Robots[i].Y + 1) != 2 && _table.GetField(Robots[i].X, Robots[i].Y + 1) != 4)
                        {
                            _table.SetValue(Robots[i].X, Robots[i].Y + 1, 4);
                            _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                            Robots[i].Y++;

                            Robots[i].StepCount++;

                            Robots[i].Travel_x.Add(Robots[i].X);
                            Robots[i].Travel_y.Add(Robots[i].Y);

                            Robots[i].Battery--;


                        }

                        else if (Robots[i].Y < Robots[i].Dest_y && _table.GetField(Robots[i].X, Robots[i].Y + 1) == 4)
                            continue;

                        else if (Robots[i].Y < Robots[i].Dest_y && _table.GetField(Robots[i].X, Robots[i].Y + 1) == 2 && Robots[i].IsCarrying == false)
                        {

                            _table.SetValue(Robots[i].X, Robots[i].Y + 1, 2);
                            _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                            Robots[i].Y++;

                            Robots[i].StepCount++;

                            Robots[i].Travel_x.Add(Robots[i].X);
                            Robots[i].Travel_y.Add(Robots[i].Y);

                            Robots[i].Battery--;

                        }

                        else if (Robots[i].Y < Robots[i].Dest_y && _table.GetField(Robots[i].X, Robots[i].Y + 1) == 2 && Robots[i].IsCarrying == true)                  //Ha cipelünk szekrényt és a következő lépésben szekrényre lépnénk, akkor lefelé vagy felfeé kell tenni egy extra lépést
                        {
                            Forward = Math.Abs(Robots[i].Dest_x - (Robots[i].X + 1)) + Math.Abs(Robots[i].Dest_y - Robots[i].Y);
                            Backward = Math.Abs(Robots[i].Dest_x - (Robots[i].X - 1)) + Math.Abs(Robots[i].Dest_y - Robots[i].Y);

                            if (XShorterWay(Forward, Backward) == Forward && _table.GetField(Robots[i].X + 1, Robots[i].Y) != 2)
                            {
                                _table.SetValue(Robots[i].X + 1, Robots[i].Y, 4);
                                _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                                Robots[i].X++;

                                Robots[i].StepCount++;

                                Robots[i].Travel_x.Add(Robots[i].X);
                                Robots[i].Travel_y.Add(Robots[i].Y);

                                Robots[i].Battery--;
                            }
                            else
                            {
                                _table.SetValue(Robots[i].X - 1, Robots[i].Y, 4);
                                _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                                Robots[i].X--;

                                Robots[i].StepCount++;

                                Robots[i].Travel_x.Add(Robots[i].X);
                                Robots[i].Travel_y.Add(Robots[i].Y);

                                Robots[i].Battery--;
                            }

                            Robots[i].ExtraMove = 3;

                        }

                        if (Robots[i].Y > Robots[i].Dest_y && _table.GetField(Robots[i].X, Robots[i].Y - 1) != 2 && _table.GetField(Robots[i].X, Robots[i].Y - 1) != 4)
                        {
                            _table.SetValue(Robots[i].X, Robots[i].Y - 1, 4);
                            _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                            Robots[i].Y--;

                            Robots[i].StepCount++;

                            Robots[i].Travel_x.Add(Robots[i].X);
                            Robots[i].Travel_y.Add(Robots[i].Y);

                            Robots[i].Battery--;


                        }

                        else if (Robots[i].Y > Robots[i].Dest_y && _table.GetField(Robots[i].X, Robots[i].Y - 1) == 4)
                            continue;

                        else if (Robots[i].Y > Robots[i].Dest_y && _table.GetField(Robots[i].X, Robots[i].Y - 1) == 2 && Robots[i].IsCarrying == false)
                        {

                            _table.SetValue(Robots[i].X, Robots[i].Y - 1, 2);
                            _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                            Robots[i].Y--;

                            Robots[i].StepCount++;

                            Robots[i].Travel_x.Add(Robots[i].X);
                            Robots[i].Travel_y.Add(Robots[i].Y);

                            Robots[i].Battery--;



                        }

                        else if (Robots[i].Y > Robots[i].Dest_y && _table.GetField(Robots[i].X, Robots[i].Y - 1) == 2 && Robots[i].IsCarrying == true)
                        {
                            Forward = Math.Abs(Robots[i].Dest_x - (Robots[i].X + 1)) + Math.Abs(Robots[i].Dest_y - Robots[i].Y);
                            Backward = Math.Abs(Robots[i].Dest_x - (Robots[i].X - 1)) + Math.Abs(Robots[i].Dest_y - Robots[i].Y);

                            if (XShorterWay(Forward, Backward) == Forward && _table.GetField(Robots[i].X + 1, Robots[i].Y) != 2)
                            {
                                _table.SetValue(Robots[i].X + 1, Robots[i].Y, 4);
                                _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                                Robots[i].X++;

                                Robots[i].StepCount++;

                                Robots[i].Travel_x.Add(Robots[i].X);
                                Robots[i].Travel_y.Add(Robots[i].Y);

                                Robots[i].Battery--;
                            }
                            else
                            {
                                _table.SetValue(Robots[i].X - 1, Robots[i].Y, 4);
                                _table.SetValue(Robots[i].X, Robots[i].Y, 0);

                                Robots[i].X--;

                                Robots[i].StepCount++;

                                Robots[i].Travel_x.Add(Robots[i].X);
                                Robots[i].Travel_y.Add(Robots[i].Y);

                                Robots[i].Battery--;
                            }

                            Robots[i].ExtraMove = 4;
                        }
                    }

                }

            }

            firstround = false;                                                                                                                                     //Miután egyszer lefutott a simulation az elsőkörnek vége, vizsgálhatjuk, hogy vége van-e a programnak

        }

        #endregion

        #region AdvanceTime 
        public void AdvanceTime()               //Ezzel afügvénynel növeljük az időt minden tickelés után
        {
            _modelSimTime++;
            _table.SimTime = _modelSimTime;     //Elmentjük a table-be az időt mert kell a mentéshez
            OnGameAdvanced();
        }

        #endregion

        #region Load & Save
        public async Task LoadSimAsync(String path)                                         //Fájlból olvasást elvégző metódus
        {

            if (_dataAccess == null)
                throw new InvalidOperationException("No data access is provided.");

            _table = await _dataAccess.LoadAsync(path);                                     //Létrehoz egy megadott table-t és beállítja az értékeit

            firstround = true;
            _stepCount = _table.StepCount;
            _rowSize = _table.RowSize;
            _columnSize = _table.ColumnSize;
            _modelSimTime = _table.SimTime;
            Robots = Table.Robots;

            Initialize_Table();
        }

        public async Task SaveGameAsync(String path)                                        //Fájlba mentést elvégző függvény
        {
            if (_dataAccess == null)
                throw new InvalidOperationException("No data access is provided.");

            await _dataAccess.SaveAsync(path, _table);
        }

        #endregion

        #region Simulation events
        private void OnSimAdvanced()                                                        //itt küldünk egy eventet, amivel friisítjük a megjelenést
        {
            if (SimAdvanced != null)
                SimAdvanced(this, new MyEventArgs(_stepCount, false, _modelSimTime));
            _table.StepCount = _stepCount;
        }

        private void OnSimOver(Boolean isFinished)
        {
            if (SimOver != null)
                SimOver(this, new MyEventArgs(_stepCount, isFinished, _modelSimTime));          ///itt küldünk egy eventet, a,ivel jelezzük, hogy vége a szimuzlációnak
        }

        private void OnGameAdvanced()
        {   
            if (TimerAdvanced != null)
                TimerAdvanced(this, new TimerEventArgs(_modelSimTime));                         //Frissítjük az időkiírást a képernyőn
        }

        #endregion
    }
}
