using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ACE.DB.Classes;

namespace ACE.Writers
{
    /// <summary>
    /// 
    /// This interface should be inherited by a class that will correctly implement the method(s)
    /// declared here.  Namely, when upserting a record, the implementating class should only 
    /// mark a database record as updated when there is an actual difference detected between
    /// the incoming record and the present record.
    ///     
    /// </summary>
    public interface IApplicable
    {
        bool InitPreparedStatements(AceAPIBucket poBucketConfiguration);

        bool CompareOldVersusNew(Hashtable poOldRecord, Hashtable poNewRecord);

        bool UpsertRecord(Hashtable poRecord);
    }
}
