using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACE.DB.Classes
{
    #region CONSTANTS
    public enum ContentType { XML = 1, JSON };
    #endregion

    /// <summary>
    /// 
    /// This class serves as the representation of a record within the 
    /// ACE_CFG_API table.  It will serve to provide the details that 
    /// power the way in which data will be pulled through an available API.
    ///     
    /// </summary>
    public class AceAPIConfiguration
    {
        #region Constructors

        public AceAPIConfiguration()
        {
            BaseURL            = SinceURLArg   = "";
            AnchorIndicator    = AnchorElement = "";
            ResponseFilterPath = TargetTag     = TargetKeyTag = "";

            DataContentType = ContentType.XML;

            KeyList           = new HashSet<string>();
            AnchorFilterArgs  = new Dictionary<string, string>();
            RequestFilterArgs = new Dictionary<string, string>();
            ApplyBuckets      = new Dictionary<string, AceAPIBucket>();
        }

        #endregion

        #region Properties

        public string AnchorElement { get; set; }

        public Dictionary<string, string> AnchorFilterArgs { get; set; }

        public string AnchorIndicator { get; set; }

        public Dictionary<string, AceAPIBucket> ApplyBuckets { get; set; }

        public string BaseURL { get; set; }

        public ContentType DataContentType { get; set; }

        public string CurrentAnchor { get; set; }

        public HashSet<string> KeyList { get; set; }

        public Dictionary<string, string> RequestFilterArgs { get; set; }

        public string ResponseFilterPath { get; set; }

        public string SinceURLArg { get; set; }

        public string TargetKeyTag { get; set; }

        public string TargetTag { get; set; }

        #endregion
    }

}
