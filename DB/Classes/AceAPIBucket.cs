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
    /// This class serves as the representation of a record within the 
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
            AttrKeys = new HashSet<string>();

            SoughtAttributes    = new Dictionary<string, SqlDbType>();
            SoughtAttrKeys      = new Dictionary<string, bool>();
            SoughtAttrLengths   = new Dictionary<string, int>();
            SoughtAttrXPaths    = new Dictionary<string, string>();
            SoughtAttrXmlBodies = new Dictionary<string, bool>();
        }

        #region Properties

        public HashSet<string> AttrKeys { get; set; }

        public string BucketName { get; set; }

        public string TableName { get; set; }

        public Dictionary<string, SqlDbType> SoughtAttributes { get; set; }

        public Dictionary<string, bool> SoughtAttrKeys { get; set; }

        public Dictionary<string, int> SoughtAttrLengths { get; set; }

        public Dictionary<string, string> SoughtAttrXPaths { get; set; }

        public Dictionary<string, bool> SoughtAttrXmlBodies { get; set; }

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
        public void AddAttribute(string psName, SqlDbType poType, bool pbIsKey, int pnLength, string psXPath, bool pbIsXmlBody)
        {
            SoughtAttributes[psName]    = poType;
            SoughtAttrKeys[psName]      = pbIsKey;
            SoughtAttrLengths[psName]   = pnLength;
            SoughtAttrXPaths[psName]    = psXPath;
            SoughtAttrXmlBodies[psName] = pbIsXmlBody;

            if (pbIsKey)
                AttrKeys.Add(psName);
        }

        #endregion
    }
}
