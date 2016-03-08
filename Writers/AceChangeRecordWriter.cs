﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

using ACE.DB.Classes;

namespace ACE.Writers
{
    /// <summary>
    /// 
    /// This class serves to insert an entry that notes the details about a certain record 
    /// being pulled down through the API.  (Usually, this is done after the enumeration of
    /// change manifest data, when we're in the enumeration of actual data.)  The record will
    /// include the raw payloads from the API in their original format (JSON, XML, etc.), being
    /// both the change manifest payload (if available) and the actual data payload.  In this way,
    /// we will archive an actual snapshot of the data at the time of the pull through the API.
    /// 
    /// NOTE: In this code, each record is identified through an EAN, which is a 
    /// standard identifier for books. However, it could easily be replaced by another identifier.
    ///     
    public class AceChangeRecordWriter : IDisposable
    {
        #region SQL

        #region Insert Record Instance

        private string msInsertProductInstanceSql =
@"
INSERT INTO
    ace_change_product(change_id, ean, notification, data)
VALUES
   (@cid, @ean, @notify_body, @data_body)
";

        #endregion

        #endregion

        public AceChangeRecordWriter()
        { }

        public void Dispose()
        { }
    }
}
