using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;

namespace ACE.DB.Classes
{
    /// <summary>
    /// 
    /// This class serves as the representation of a record set within the 
    /// ACE_CFG_BUCKET table.  It will serve to map the tags of a logical
    /// bucket (i.e., the entire set or a subset of a XML composite) within
    /// the pulled Xml to the columns within a target table.
    ///     
    /// </summary>
    public class AceAPIBucket
    {
        public AceAPIBucket()
        {
            BucketName = "";
            TableName  = "";

            Init();
        }

        public AceAPIBucket(string psBucketName, string psTableName)
        {
            BucketName = psBucketName;
            TableName  = psTableName;

            Init();
        }

        public void Init()
        {
            ColKeys = new HashSet<string>();

            SoughtColumns      = new Dictionary<string, SqlDbType>();
            SoughtColKeys      = new Dictionary<string, bool>();
            SoughtColLengths   = new Dictionary<string, int>();
            SoughtColXPaths    = new Dictionary<string, string>();
            SoughtColXmlBodies = new Dictionary<string, bool>();
        }

        #region Properties

        public HashSet<string> ColKeys { get; set; }

        public string BucketName { get; set; }

        public string TableName { get; set; }

        public Dictionary<string, SqlDbType> SoughtColumns { get; set; }

        public Dictionary<string, bool> SoughtColKeys { get; set; }

        public Dictionary<string, int> SoughtColLengths { get; set; }

        public Dictionary<string, string> SoughtColXPaths { get; set; }

        public Dictionary<string, bool> SoughtColXmlBodies { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// 
        /// This method will map another tag within the pulled Xml to a column of the target table.
        /// 
        /// <param name="psName">The column name of the target table</param>
        /// <param name="poType">The data type of the column in the target table</param>
        /// <param name="pbIsKey">The indicator of whether the target table's column is part of the primary key</param>
        /// <param name="pnLength">The length of the table's column (if the type is text)</param>
        /// <param name="psXPath">The XPath of the tag mapped to the target table's column</param>
        /// <param name="pbIsXmlBody">The indicator of whether the XPath points to a single node's value or a composite that should be serialized</param>
        /// <returns>None.</returns>
        /// </summary>
        public void AddTargetColumn(string psName, SqlDbType poType, bool pbIsKey, int pnLength, string psXPath, bool pbIsXmlBody)
        {
            SoughtColumns[psName]      = poType;
            SoughtColKeys[psName]      = pbIsKey;
            SoughtColLengths[psName]   = pnLength;
            SoughtColXPaths[psName]    = psXPath;
            SoughtColXmlBodies[psName] = pbIsXmlBody;

            if (pbIsKey)
                ColKeys.Add(psName);
        }

        #endregion
    }
}
