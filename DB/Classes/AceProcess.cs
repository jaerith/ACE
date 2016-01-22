using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACE.DB.Classes
{
    /// <summary>
    /// 
    /// This class serves as the representation of a record within the 
    /// ACE_CFG_PROCESS table.  It will serve to provide the details that 
    /// power the two-step approach for iterating through an online data set:
    /// 
    /// 1.) Use an API configuration (for metadata) to pull manifest data 
    /// that describes "deltas" that we might be interested in
    /// 2.) With the manifest data in hand, use an API configuration 
    /// (for a catalog) to pull actual data
    ///     
    /// </summary>
    public class AceProcess
    {
        public AceProcess(int pnProcessID)
        {
            Init(pnProcessID, "");

            ChangeAPIConfiguration = null;
            DataAPIConfiguration   = null;
        }

        public AceProcess(int pnProcessID, string psProcessName)
        {
            Init(pnProcessID, psProcessName);

            ChangeAPIConfiguration = null;
            DataAPIConfiguration   = null;
        }

        public AceProcess(int pnProcessID, string psProcessName, string psChangeBaseURL, string psDataBaseURL)
        {
            Init(pnProcessID, psProcessName);

            ChangeAPIConfiguration = new AceAPIConfiguration() { BaseURL = psChangeBaseURL };
            DataAPIConfiguration   = new AceAPIConfiguration() { BaseURL = psDataBaseURL };
        }

        private void Init(int pnProcessID, string psProcessName)
        {
            ProcessID   = pnProcessID;
            ProcessName = psProcessName;
            ChangeSeq   = -1;
            Anchor      = "";
        }

        public long ChangeSeq { get; set; }

        public int ProcessID { get; set; }

        public string Anchor { get; set; }

        public string ProcessName { get; set; }

        public AceAPIConfiguration ChangeAPIConfiguration { get; set; }

        public AceAPIConfiguration DataAPIConfiguration { get; set; }

    }
}
