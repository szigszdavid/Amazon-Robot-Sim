using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using project6.Model;
using project6.Persistence;
using project6.Persistence.Fields;

namespace project6
{
    public partial class Form : System.Windows.Forms.Form
    {
        private FileDataAccess _dataAccess;
        private SimulationModel _model;
        private Button[,] _buttonGrid;
        private Timer _timer;
        private Int32 RowSize = 10;
        private Int32 ColumnSize = 10;
        private Int32 StartStop = 0;
        private Int32 avg = 0;
        private Int32 sum = 0;

        public Form()
        {
            InitializeComponent();

            _timer = new Timer();                                               //Létrehozzuk a timert
            _timer.Interval = 1000;
            
            GenerateTable();
        }

        private void Timer_Tick(Object sender, EventArgs e)                     ///Minden másodpercben frissül az idő és a pálya és lefut egy lépés minde robotnak.
        {
            _model.AdvanceTime();
            _model.Simulation();
            RefreshTable();

        }

        private void GenerateTable()                                                                        //Létrehozzuk a buttongridet.
        {
            
            _buttonGrid = new Button[RowSize, ColumnSize];

            for (Int32 i = 0; i < RowSize; i++)
                for (Int32 j = 0; j < ColumnSize; j++)
                {
                    _buttonGrid[i, j] = new Button();
                    _buttonGrid[i, j].Location = new Point(5 + 50 * j, 25 + 50 * i); 
                    _buttonGrid[i, j].Size = new Size(50, 50); 
                    _buttonGrid[i, j].Font = new Font(FontFamily.GenericSansSerif, 7, FontStyle.Regular); 
                    _buttonGrid[i, j].Enabled = false;
                    _buttonGrid[i, j].TabIndex = 100 + i * 3 + j;
                    _buttonGrid[i, j].FlatStyle = FlatStyle.Flat; 

                    Controls.Add(_buttonGrid[i, j]);
                }
        }

        private void RefreshTable()                                                                                 ///Frissítkük a megjelénést
        {
            for(Int32 i = 0; i < _model.RowSize; i++)
            {
                for (Int32 j = 0; j < _model.ColumnSize; j++)
                {

                    if (_model.Table._fieldTypes[i, j].GetType() == typeof(Stand))
                    {
                        _buttonGrid[i, j].Text = String.Join(",", _model.Table._fieldTypes[i, j].Parcels);
                    }

                    

                    
                }
            }

            for (Int32 i = 0; i < _model.RowSize; i++)
                for (Int32 j = 0; j < _model.ColumnSize; j++)
                {

                    switch (_model.Table.GetField(i, j))
                    {
                        case 0:
                            _buttonGrid[i, j].Image = null;
                            _buttonGrid[i, j].BackColor = Color.White;
                            break;
                        case 1:
                            _buttonGrid[i, j].Image = null;
                            _buttonGrid[i, j].BackColor = Color.Blue;
                            break;
                        case 2:
                            _buttonGrid[i, j].Image = null;
                            _buttonGrid[i, j].BackColor = Color.Gray;
                            break;
                        case 3:
                            _buttonGrid[i, j].Image = null;
                            _buttonGrid[i, j].BackColor = Color.Green;
                            break;
                        case 4:
                            _buttonGrid[i, j].BackColor = Color.Orange;
                            break;
                    }

                    for(int a = 0; a < _model._robots; a++)
                    {
                        if(_model.Robots[a].IsCarrying == true && _model.Robots[a].X == i && _model.Robots[a].Y == j)
                        {
                            _buttonGrid[i, j].BackColor = Color.Red;                                                            //ha cipel  a robot akkor piros lesz
                        }
                    }
                }

            
        }


        private void SetupTable()
        {
            for (Int32 i = 0; i < _model.RowSize; i++)
                for (Int32 j = 0; j < _model.ColumnSize; j++)
                {
                    _buttonGrid[i, j].Text = "";
                    switch(_model.Table.GetField(i, j))
                    {
                        case 0:
                            _buttonGrid[i, j].BackColor = Color.White;
                            break;
                        case 1:
                            _buttonGrid[i, j].BackColor = Color.Blue;
                            break;
                        case 2:
                            _buttonGrid[i, j].BackColor = Color.Gray;
                            break;
                        case 3:
                            _buttonGrid[i, j].BackColor = Color.Green;
                            break;
                        case 4:
                            _buttonGrid[i, j].BackColor = Color.Orange;
                            break;
                    }
                }
            RefreshTable();
        }

        private async void LoadButtonClicked(object sender, EventArgs e)                //Load gombra nyomás eventje
        {
            _timer.Stop();
            

            if (_openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    
                    await _model.LoadSimAsync(_openFileDialog.FileName);

                    for (Int32 i = 0; i < RowSize; i++)
                        for (Int32 j = 0; j < ColumnSize; j++)
                        {
                            Controls.Remove(_buttonGrid[i, j]);
                           
                        }

                    RowSize = _model.RowSize;
                    ColumnSize = _model.ColumnSize;

                    GenerateTable();


                    SetupTable();
                    _timer.Start(); 
                }
                catch (MyDataException)
                {
                    MessageBox.Show("Játék betöltése sikertelen!" + Environment.NewLine + "Hibás az elérési út, vagy a fájlformátum.", "Hiba!", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    _model.NewSimulation();
                 
                }   
            }
        }

        private async void SaveButtonClicked(object sender, EventArgs e)                //Save gombara nyomás eseménye
        {
            Boolean restartTimer = _timer.Enabled;
            _timer.Stop();

            if (_saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    _model.CopyRobots(_model._robots);
                    await _model.SaveGameAsync(_saveFileDialog.FileName);
                }
                catch (MyDataException)
                {
                    MessageBox.Show("A szimuláció mentése sikertelen!" + Environment.NewLine + "Hibás az elérési út, vagy a könyvtár nem írható.", "Hiba!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            if (restartTimer)
                _timer.Start();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)                //Kilépés gombra nyomás
        {
            Boolean restartTimer = _timer.Enabled;
            _timer.Stop();

            if (MessageBox.Show("Biztosan ki szeretne lépni?", "Amazon Robot", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Close();
            }
            else
            {
                if (restartTimer)
                    _timer.Start();
            }
        }

        private void NewGameClicked(object sender, EventArgs e)             //Új szimuláció kezdése
        {
            try
            {
                _timer.Stop();
                _model.NewSimulation();
                SetupTable();

            }
            catch (NullReferenceException)
            {
                MessageBox.Show("A szimuláció már üres!", "Hiba!" , MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            _timer.Start();

        }

        private void TimeAdvanced(Object sender, TimerEventArgs e)                  //Frissül az idő.
        {

            _toolLabelSimTime.Text = TimeSpan.FromSeconds(e.SimTime).ToString("g");
            
                
            
        }

        private void Pause_Click(object sender, EventArgs e)                    //Pause gombra nyomás.
        {
            if(StartStop % 2 == 0)
            {
                _timer.Stop();
            }
            else
            {
                _timer.Start();
            }

            StartStop++;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)           //Sebbeség megvátozatása. Ehhez a hatványzoás műveletét alkamaztuk.
        {
            label1.Text = trackBar1.Value.ToString();

            double speed = trackBar1.Value;

            if(speed > 0)
            {
                _timer.Interval = (int)(1000 * Math.Pow(0.5, speed));
            }
            if(speed == 0)
            {
                _timer.Interval = 1000;
            }
            if(speed < 0)
            {
                speed = (-1) * speed;

                _timer.Interval = (int)(1000 * Math.Pow(2, speed));
            }
        }

        private void Form_Load_1(object sender, EventArgs e)
        {
            _dataAccess = new FileDataAccess();
            _model = new SimulationModel(_dataAccess);
            _model.TimerAdvanced += new EventHandler<TimerEventArgs>(TimeAdvanced);
            _model.SimOver += new EventHandler<MyEventArgs>(TheSimulationOver);

            _timer.Tick += new EventHandler(Timer_Tick);
        }

        public void TheSimulationOver(object sender, MyEventArgs e)                     //Szimuláció vége esemény lekezelése és statisztika kiírása.
        {
            _timer.Stop();
            StringBuilder MessageText = new StringBuilder();
            MessageText.AppendLine(string.Format("A szimulációnak vége!"));
            MessageText.AppendLine(string.Format("\n"));


            for (int i = 0; i < _model._robots; i++)
            {

                label2.Text = (_model.Robots[i].StepCount).ToString();
                MessageText.AppendLine(string.Format("Lépések: Robot{0} : ", i + 1)).AppendLine(string.Format("{0}", label2.Text));
                sum = sum + _model.Robots[i].StepCount;

            }

            avg = sum / _model._robots;

            MessageText.AppendLine(string.Format("\n"));

            MessageText.AppendLine(string.Format("Összesen ennyi lépés volt: {0}", sum.ToString()));

            MessageText.AppendLine(string.Format("\n"));
            MessageText.AppendLine(string.Format("Átlagosan egy robot ennyi lépést tett meg: {0}", avg.ToString()));
            MessageText.AppendLine(string.Format("\n"));


            for (int i = 0; i < _model._robots; i++)
            {

 
                MessageText.AppendLine(string.Format("Ennyiszer voltak tölteni a Robotok {0} :", i + 1)).AppendLine(string.Format("{0}", _model.Robots[i].Chargetime.ToString()));


            }

            MessageText.AppendLine(string.Format("\n"));

            for (int i = 0; i < _model._robots; i++)
            {


                MessageText.AppendLine(string.Format("Ennyi Terméket vitt a Robotok {0} :", i + 1)).AppendLine(string.Format("{0}", _model.Robots[i].worked));


            }

            MessageText.AppendLine(string.Format("\n"));

            for (int i = 0; i < _model._robots; i++)
            {
                MessageText.AppendLine(string.Format("Ennyi idő volt egy Robotnak: {0} ", i + 1)).AppendLine(string.Format(" {0}",TimeSpan.FromSeconds(_model.Robots[i].StepCount + 33).ToString("g")));
            }

           

            MessageText.AppendLine(string.Format("\n"));

            MessageText.AppendLine(string.Format("Idő: {0}", _toolLabelSimTime.Text));
            MessageBox.Show(MessageText.ToString(), "Simulation is over");
        }
    }
}
