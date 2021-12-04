using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace project6.Model
{
    public class MyEventArgs
    {
        private Int32 _steps;
        private Boolean _isFinished;
        private UInt64 _modelSimTime;

        public Int32 StepCount { get { return _steps; } }                           //Lépés számlálás a statisztikához

        public Boolean IsFinished { get { return _isFinished;  } }                  //Errea szimuláció végén van szükség -> SimOver eventhez

        public UInt64 SimTime { get { return _modelSimTime; } }                     

        public MyEventArgs(Int32 stepCount, Boolean isFinished, UInt64 simTime)
        {
            _steps = stepCount;
            _isFinished = isFinished;
            _modelSimTime = simTime;
        }
    }
}
