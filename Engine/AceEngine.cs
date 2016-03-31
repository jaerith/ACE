using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACE
{
    /// <summary>
    /// 
    /// This class serves as the main body of functionality, in which it will iterate through
    /// different Process configurations in the metadata.  For each Process, it will then:
    /// 
    /// 1.) If applicable, it will point to a target REST API in order to download the change manifest(s) and
    /// persist them to a table.
    /// If not applicable, it will assume that it needs to pull down the entire record catalog and skip to Step 3.
    /// 2.) It will extract information from the change manifest(s) and then know which actual records to
    /// pull through the REST API.
    /// 3.) The records of interest will be pulled down and persisted to a table in their raw form.
    /// 4.) The raw payloads (for the records of interest) will then be parsed and then loaded into a staging table.
    ///     
    /// </summary>
    public class AceEngine
    {
    }
}
