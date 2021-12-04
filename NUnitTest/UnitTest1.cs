using NUnit.Framework;
using project6.Model;
using project6.Persistence;
using Moq;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace NUnitTest
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public void b()
        {
            Assert.AreEqual(1, 1);
        }

        private Mock<IDataAccess> _mock;
        private SimulationModel _model;

        [SetUp]
        public void Setup()
        {
            _mock = new Mock<IDataAccess>();

            _model = new SimulationModel(_mock.Object);
        }
        
        [Test]
        public void ConstructorTest()
        {
            _model.NewSimulation();
            for (Int32 i = 0; i < _model.RowSize; i++)
            {
                for (Int32 j = 0; j < _model.ColumnSize; j++)
                {
                    Assert.AreEqual(0, _model.Table.FieldTValues[i, j]);
                }
            }
        }

        [Test]
        public void StepSim()
        {
            _model.NewSimulation();
            Assert.AreEqual(0, _model.Table.CountRobots());
            Assert.AreEqual(0, _model.Table.CountGoals());
            _model.Table.FieldTValues[0, 0] = 4;
            _model.Table.FieldTypes[0, 0] = new project6.Persistence.Fields.Robot();
            _model.Table.FieldTValues[0, 5] = 3;
            _model.Table.FieldTValues[9, 9] = 3;
            _model.Table.FieldTypes[0, 5] = new project6.Persistence.Fields.Goal { Id = 1 };
            _model.Table.FieldTypes[9, 9] = new project6.Persistence.Fields.Goal { Id = 2 };
            _model.Table.FieldTValues[1, 3] = 2;
            _model.Robots = new project6.Persistence.Fields.Robot[1];
            _model.Robots[0] = (project6.Persistence.Fields.Robot)_model.Table.FieldTypes[0, 0];
            _model.Robots[0].Battery = 100;
            _model.Table.FieldTypes[1, 3] = new project6.Persistence.Fields.Stand
            {
                Parcels = new System.Collections.Generic.SortedSet<int>(),
                IsTargeted = false
            };
            _model.Table.FieldTypes[1, 3].Parcels.Add(0);
            _model.Table.FieldTypes[1, 3].Parcels.Add(1);

            Assert.AreEqual(1, _model.Table.CountRobots());
            Assert.AreEqual(2, _model.Table.CountGoals());
            _model.Robots[0].Target_x = 1;
            _model.Robots[0].Target_y = 3;


            _model.Simulation();
            _model.Table.SetValue(0, 0, 0);
            _model.Table.SetValue(1, 0, 4);

            Console.WriteLine(_model.Robots[0].Target_x);
            Console.WriteLine(_model.Robots[0].Target_y);

            for (int i = 0; i < _model.Table.RowSize; i++)
            {
                for (int j = 0; j < _model.Table.ColumnSize; j++)
                    Console.Write(_model.Table.FieldTValues[i, j] + " ");
                Console.WriteLine();
            }
            Assert.AreEqual(0, _model.Table.FieldTValues[0, 0]);
            Assert.AreEqual(4, _model.Table.FieldTValues[1, 0]);
        }

        [Test]
        public async Task SimLoadAsyncTest()
        {
            Boolean caughtEx = false;
            _model.NewSimulation();
            try
            {
                await _model.LoadSimAsync(String.Empty);
            }
            catch
            {
                caughtEx = true;
            }

            Assert.AreEqual(caughtEx, true);
            _mock.Verify(dataAccess => dataAccess.LoadAsync(String.Empty), Times.Once());
        }

        [Test]
        public async Task SimSaveAsyncTest()
        {

            Int32[,] values = new Int32[10, 10];

            for (Int32 i = 0; i < 10; i++)
                for (Int32 j = 0; j < 10; j++)
                    values[i, j] = _model.Table.FieldTValues[i, j];

            await _model.SaveGameAsync(String.Empty);


            for (Int32 i = 0; i < 10; i++)
                for (Int32 j = 0; j < 10; j++)
                    Assert.AreEqual(values[i, j], _model.Table.FieldTValues[i, j]);
            _mock.Verify(mock => mock.SaveAsync(String.Empty, It.IsAny<Table>()), Times.Once());
        }

        [Test]
        public void CalculateDist()
        {
            _model.NewSimulation();
            _model.Table.FieldTypes[9, 9] = new project6.Persistence.Fields.Charger();
            Int32 dist = _model.CalculateDist(0, 0, 0, 9, 9, 0);
            Assert.IsTrue(dist > 0);
            Assert.AreEqual(56, dist);      //56 == 9 + 2*18 + 9 + 2
        }

        [Test]
        public void CalculateDist2()
        {
            _model.NewSimulation();
            _model.Table.FieldTypes[0, 0] = new project6.Persistence.Fields.Charger();
            Int32 dist = _model.CalculateDist(0, 0, 0, 0, 0, 0);
            Assert.AreEqual(2, dist);       //2 == fault in _model.CalculateDist
        }

        [Test]
        public void Charge()
        {
            _model.NewSimulation();
            _model.Table.FieldTypes[9, 9] = new project6.Persistence.Fields.Charger();
            _model.Table.FieldTypes[0, 0] = new project6.Persistence.Fields.Robot();
            _model.Robots = new project6.Persistence.Fields.Robot[1];
            _model.Robots[0] = (project6.Persistence.Fields.Robot)_model.Table.FieldTypes[0, 0];
            _model.Charge(_model.Robots[0]);
            Assert.IsTrue(_model.Robots[0].NeedToCharge);
            Assert.IsTrue(_model.Table.FieldTypes[9, 9].IsTargeted);
            Assert.AreEqual(_model.Robots[0].ChargeX, 9);
            Assert.AreEqual(_model.Robots[0].ChargeY, 9);
        }

        [Test]
        public void NewSimulation()
        {
            _model.NewSimulation();
            _model.Table.FieldTValues[0, 0] = 1;
            Assert.AreEqual(_model.Table.FieldTValues[0, 0], 1);
            _model.NewSimulation();
            Assert.AreEqual(_model.Table.FieldTValues[0, 0], 0);
        }

        [Test]
        public void RobotTarget()
        {
            _model.NewSimulation();
            _model.Table.FieldTypes[9, 9] = new project6.Persistence.Fields.Stand
            {
                Parcels = new System.Collections.Generic.SortedSet<int>(),
                IsTargeted = false
            };
            _model.Table._parcels = new SortedSet<Int32>[10, 10];
            _model.Table.SetParcels(9, 9, 0);
            _model.Table.SetParcels(9, 9, 1);
            _model.Table.FieldTypes[0, 0] = new project6.Persistence.Fields.Robot();
            _model.Robots = new project6.Persistence.Fields.Robot[1];
            _model.Robots[0] = (project6.Persistence.Fields.Robot)_model.Table.FieldTypes[0, 0];
            Assert.IsNotNull(_model.Robots[0]);
            _model.RobotTarget(0);
            Assert.IsTrue(_model.Table.FieldTypes[9, 9].IsTargeted);
            Assert.AreEqual(_model.Robots[0].Target_x, 9);
            Assert.AreEqual(_model.Robots[0].Target_y, 9);
        }

        [Test]
        public void Simulation()
        {
            _model.ColumnSize = 3;
            _model.RowSize = 3;
            _model.NewSimulation();
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    _model.Table.FieldTValues[i, j] = 0;
                    _model.Table.FieldTypes[i, j] = new project6.Persistence.Fields.Empty();
                }
            }
            _model.Table.FieldTValues[0, 0] = 4;
            _model.Table.FieldTValues[0, 2] = 2;
            _model.Table.FieldTValues[2, 2] = 3;
            _model.Table.FieldTypes[0, 0] = new project6.Persistence.Fields.Robot
            {
                Battery = 50,
                NeedToCharge = false
            };
            _model.Table.FieldTypes[0, 2] = new project6.Persistence.Fields.Stand
            {
                Parcels = new System.Collections.Generic.SortedSet<int>(),
                IsTargeted = false
            };
            _model.Table.FieldTypes[2, 2] = new project6.Persistence.Fields.Goal { Id = 1 };
            _model.Table._parcels = new SortedSet<Int32>[3, 3];
            _model.Table.SetParcels(0, 2, 0);
            _model.Table.SetParcels(0, 2, 1);
            _model.Robots = new project6.Persistence.Fields.Robot[1];
            _model.Robots[0] = (project6.Persistence.Fields.Robot)_model.Table.FieldTypes[0, 0];
            _model.RobotTarget(0);
            _model.FirstRound = true;

            _model.Simulation();

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                    Console.Write(_model.Table.FieldTValues[i, j] + " ");
                Console.WriteLine();
            }
            //Assert.AreEqual(_model.Table.FieldTValues[0, 1], 4);
            Assert.AreEqual(_model.FirstRound, false);
            Assert.AreEqual(_model.Robots[0].Goback, false);
            Assert.AreEqual(_model.Robots[0].Target_x, 0);
            Assert.AreEqual(_model.Robots[0].Target_y, 2);
            Assert.AreEqual(_model.Robots[0].Dest_x, 0);
            Assert.AreEqual(_model.Robots[0].Dest_y, 0);
            Assert.AreEqual(_model.Robots[0].Battery, 50);
            Assert.AreEqual(_model.Robots[0].Dir, 1);
        }

        [Test]
        public void CountRobot()
        {
            _model.NewSimulation();
            Assert.AreEqual(0, _model.Table.CountRobots());
            _model.Table.FieldTValues[0, 0] = 4;
            Assert.AreEqual(1, _model.Table.CountRobots());
        }
        [Test]
        public void CountGoal()
        {
            _model.NewSimulation();
            Assert.AreEqual(0, _model.Table.CountGoals());
            _model.Table.FieldTValues[0, 0] = 3;
            Assert.AreEqual(1, _model.Table.CountGoals());
        }
        
    }
}