using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace project6.CIPersistence
{
    public interface IDataAccess
    {
        Task<Table> LoadAsync(string path);

        Task SaveAsync(string path, Table table);
    }
}
