using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACE.DB.Classes
{
    /// <summary>
    /// 
    /// This class serves as the configuration data needed in order to establish
    /// a connection with the database.
    ///     
    /// </summary>
    public class AceConnectionMetadata
    {
        public AceConnectionMetadata()
        {

            DBUser     = "";
            DBPassword = "";
            DBTarget   = "";
            DBMachine  = "";

            DBConnectionString = "";
        }

        public string DBUser { get; set; }

        public string DBPassword { get; set; }

        public string DBTarget { get; set; }

        public string DBMachine { get; set; }

        public string DBConnectionString { get; set; }
    }
}

