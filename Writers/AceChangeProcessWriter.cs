using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ACE.DB.Classes;

namespace ACE.Writers
{
    /// <summary>
    /// 
    /// This class serves to record an instance of a particular job running against its 
    /// configured REST API.
    ///     
    /// </summary>
    public class AceChangeProcessWriter : IDisposable 
    {
        public AceChangeProcessWriter()
        { }

        public void Dispose()
        { }
    }
}
