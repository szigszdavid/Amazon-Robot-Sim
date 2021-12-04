using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace project6.CIPersistence
{
    public class FileDataAccess : IDataAccess
    {
        public async Task<Table> LoadAsync(String path)
        {
            try
            {
                using (StreamReader reader = new StreamReader(path))
                {
                    String line = await reader.ReadLineAsync();
                    String[] current = line.Split(' ');
                    Int32 rowSize = Int32.Parse(current[0]);
                    Int32 columnSize = Int32.Parse(current[1]);

                    Int32 counter = 0;
                    List<Int32>[,] parcels = new List<Int32>[columnSize, rowSize];
                    
                    Table table = new Table(rowSize, columnSize);


                    line = await reader.ReadLineAsync();
                    current = line.Split(' ');
                    table.StepCount = Int32.Parse(current[0]);
                    table.StartCharge = Int32.Parse(current[1]);

                    line = await reader.ReadLineAsync();
                    current = line.Split(' ');
                    table.SimTime = UInt64.Parse(current[0]);

                    for (Int32 i = 0; i < rowSize; i++)
                    {
                        line = await reader.ReadLineAsync();
                        current = line.Split(' ');
                        
                        for (Int32 j = 0; j < columnSize; j++)
                        {
                            table.SetValue(i, j, Int32.Parse(current[j]));
                            if(Int32.Parse(current[j]) == 3)
                            {
                                counter++;
                            }
                        }
                    }

                    
                    for(Int32 ctr = 0; ctr < counter; ctr++)
                    {
                        for (Int32 i = 0; i < rowSize; i++)
                        {
                            line = await reader.ReadLineAsync();
                            current = line.Split(' ');

                            for (Int32 j = 0; j < columnSize; j++)
                            {
                                if(Int32.Parse(current[j]) != 0)
                                {
                                    table.SetParcels(i, j, Int32.Parse(current[j]));
                                   // Console.WriteLine("current[j]: " + Int32.Parse(current[j]));
                                   // parcels[i, j] = new List<Int32>();
                                   // parcels[i, j].Add(Int32.Parse(current[j]));
                                }
                                else
                                {
                                    table.SetParcels(i, j, 0);
                                }
                            }
                        }
                    }

                    table.Robots = new Fields.Robot[table.CountRobots()];

                    for(Int32 ctr = 0; ctr < table.CountRobots(); ctr++)
                    {
                        line = await reader.ReadLineAsync();
                        current = line.Split(' ');
                        //Console.WriteLine(table.Robots[ctr].Id);
                        table.Robots[ctr] = new Fields.Robot();
                        table.Robots[ctr].Id = Int32.Parse(current[0]);

                        line = await reader.ReadLineAsync();
                        current = line.Split(' ');
                        table.Robots[ctr].Target_x = Int32.Parse(current[0]);
                        table.Robots[ctr].Target_y = Int32.Parse(current[1]);
                        table.Robots[ctr].Dest_x = Int32.Parse(current[2]);
                        table.Robots[ctr].Dest_y = Int32.Parse(current[3]);
                        table.Robots[ctr].Start_x = Int32.Parse(current[4]);
                        table.Robots[ctr].Start_y = Int32.Parse(current[5]);
                        table.Robots[ctr].ExtraMove = Int32.Parse(current[6]);
                        table.Robots[ctr].Battery = Int32.Parse(current[7]);
                        table.Robots[ctr].Dir = Int32.Parse(current[8]);
                        table.Robots[ctr].IsCarrying = Boolean.Parse(current[9]);
                        table.Robots[ctr].Goback = Boolean.Parse(current[10]);
                        table.Robots[ctr].X = Int32.Parse(current[11]);
                        table.Robots[ctr].Y = Int32.Parse(current[12]);


                        line = await reader.ReadLineAsync();
                        current = line.Split(' ');
                        if (current[0] == "x")
                            table.Robots[ctr].DestXList = null;
                        else
                            for (Int32 i = 0; i < current.Length-1; i++)
                                //Console.WriteLine(current.Length);
                                table.Robots[ctr].DestXList.Add(Int32.Parse(current[i]));

                        line = await reader.ReadLineAsync();
                        current = line.Split(' ');
                        if (current[0] == "x")
                            table.Robots[ctr].DestYList = null;
                        else
                            for (Int32 i = 0; i < current.Length - 1; i++)
                                table.Robots[ctr].DestYList.Add(Int32.Parse(current[i]));

                        line = await reader.ReadLineAsync();
                        current = line.Split(' ');
                        if (current[0] == "x")
                            table.Robots[ctr].Travel_x = null;
                        else
                            for (Int32 i = 0; i < current.Length - 1; i++)
                                table.Robots[ctr].Travel_x.Add(Int32.Parse(current[i]));

                        line = await reader.ReadLineAsync();
                        current = line.Split(' ');
                        if (current[0] == "x")
                            table.Robots[ctr].Travel_y = null;
                        else
                            for (Int32 i = 0; i < current.Length - 1; i++)
                                table.Robots[ctr].Travel_y.Add(Int32.Parse(current[i]));
                    }

                    return table;   
                }
            }
            catch
            {
                throw new MyDataException();
            }
        }

        public async Task SaveAsync(String path, Table table)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(path))
                {

                    writer.Write(table.RowSize);
                    await writer.WriteAsync(" " + table.ColumnSize);
                    await writer.WriteLineAsync();
                    writer.Write(table.StepCount);
                    await writer.WriteAsync(" " + table.StartCharge);
                    await writer.WriteLineAsync();

                    writer.Write(table.SimTime);
                    await writer.WriteLineAsync();

                    for (Int32 i = 0; i < table.RowSize; i++)
                    {
                        for (Int32 j = 0; j < table.ColumnSize; j++)
                        {
                            await writer.WriteAsync(table[i, j] + " ");
                        }

                        await writer.WriteLineAsync();
                    }

                    for (Int32 ctr = 1; ctr <= table.CountGoals(); ctr++)
                    {
                        for (Int32 i = 0; i < table.RowSize; i++)
                        {

                            for (Int32 j = 0; j < table.ColumnSize; j++)
                            {
                                if(table.GetParcels(i, j).Contains(ctr))
                                {
                                    await writer.WriteAsync(ctr + " ");
                                }
                                else
                                {
                                    await writer.WriteAsync(0 + " ");
                                }
                            }
                            await writer.WriteLineAsync();
                        }
                    }

                    Int32 RobotCounter = 0;
                    for(Int32 i = 0; i < table.RowSize; i++)
                    {
                        for(Int32 j = 0; j < table.ColumnSize; j++)
                        {
                            if(table.GetField(i,j) == 4)
                            {
                                writer.Write(table.Robots[RobotCounter].Id);
                                await writer.WriteLineAsync();
                                await writer.WriteAsync(table.Robots[RobotCounter].Target_x + " " + table.Robots[RobotCounter].Target_y + " " +
                                                        table.Robots[RobotCounter].Dest_x + " " + table.Robots[RobotCounter].Dest_y + " " +
                                                        table.Robots[RobotCounter].Start_x + " " + table.Robots[RobotCounter].Start_y + " " +
                                                        table.Robots[RobotCounter].ExtraMove + " " + table.Robots[RobotCounter].Battery + " " + table.Robots[RobotCounter].Dir + " " +
                                                        table.Robots[RobotCounter].IsCarrying + " " + table.Robots[RobotCounter].Goback + " " +
                                                        table.Robots[RobotCounter].X + " " + table.Robots[RobotCounter].Y);
                                await writer.WriteLineAsync();

                                if(table.Robots[RobotCounter].DestXList.Count == 0)
                                {
                                    writer.Write("x");
                                    await writer.WriteLineAsync();
                                    await writer.WriteAsync("x");
                                    await writer.WriteLineAsync();
                                }
                                else
                                {
                                    for (Int32 curr = 0; curr < table.Robots[RobotCounter].DestXList.Count; curr++)
                                    {
                                        writer.Write(table.Robots[RobotCounter].DestXList[curr] + " ");
                                    }
                                    await writer.WriteLineAsync();
                                    for (Int32 curr = 0; curr < table.Robots[RobotCounter].DestYList.Count; curr++)
                                    {
                                        writer.Write(table.Robots[RobotCounter].DestYList[curr] + " ");
                                    }
                                    await writer.WriteLineAsync();
                                }

                                if (table.Robots[RobotCounter].Travel_x.Count == 0)
                                {
                                    writer.Write("x");
                                    await writer.WriteLineAsync();
                                    await writer.WriteAsync("x");
                                    await writer.WriteLineAsync();
                                }
                                else
                                {
                                    for (Int32 curr = 0; curr < table.Robots[RobotCounter].Travel_x.Count; curr++)
                                    {
                                        writer.Write(table.Robots[RobotCounter].Travel_x[curr] + " ");
                                    }
                                    await writer.WriteLineAsync();
                                    for (Int32 curr = 0; curr < table.Robots[RobotCounter].Travel_y.Count; curr++)
                                    {
                                        writer.Write(table.Robots[RobotCounter].Travel_y[curr] + " ");
                                    }
                                }
                                RobotCounter++;
                            }
                        }
                    }
                }
            }
            catch
            {
                throw new MyDataException();
            }
        }
    }
}
