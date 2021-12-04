using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace project6.Model
{
    public class TimerEventArgs                                     //Itt kerül elmentésre az idó
    {
        private UInt64 _modelSimTime;
        private Int32 _step;
        public UInt64 SimTime { get { return _modelSimTime; } }

        public Int32 Step { get { return _step; } }

        public TimerEventArgs(UInt64 gameTime)
        {
            _modelSimTime = gameTime;
        }
    }
}
