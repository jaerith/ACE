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
        private const string CONST_PROCESS_STATUS_COMPLETED = "C";
        private const string CONST_PROCESS_STATUS_FAILED    = "F";

        #region SQL

        #region Gets Maximum Start Time

        private string msGetMaxStartDtimeSql =
@"
SELECT
    c.change_id, 
    c.last_anchor, 
    c.start_dtime as max_start_dtime
FROM
    ace_change c,     
    (SELECT max(change_id) as max_change_id  
       FROM ace_change 
      WHERE process_id = @pid
        AND status = @status
    ) c2
WHERE
    c2.max_change_id = c.change_id
";

        #endregion

        #region Gets Previous Process Instance Failure

        private string msGetPreviousProcessFailureSql =
@"
SELECT
    c.change_id, c.last_anchor
FROM
    ace_change c
WHERE
    c.process_id = @pid
AND
    (c.status = 'F' OR c.status = 'P')
";

        #endregion

        #region Insert Process Instance

        private string msInsertProcessInstanceSql =
@"
INSERT INTO
    ace_change(process_id, start_dtime)
VALUES
   (@pid, SYSDATE)
RETURNING 
   change_id into @cid
";

        #endregion

        #region Set Change Process Complete

        private string msSetProcessCompleteSql =
@"
UPDATE
    ace_change
SET
    end_dtime = SYSDATE
    , total_records = @total
    , status = 'C'
    , upd_dtime = SYSDATE
WHERE
    change_id = @cid
AND
    process_id = @pid
";

        #endregion

        #region Set Change Process Complete

        private string msSetProcessFailureSql =
@"
UPDATE
    ace_change
SET
    status = 'F'
    , last_anchor = @anchor
    , upd_dtime = SYSDATE
WHERE
    change_id = @cid
AND
    process_id = @pid
";

        #endregion

        #region Update Anchor

        private string msUpdateAnchorSql =
@"
UPDATE
    ace_change
SET
    last_anchor = @anchor
    , upd_dtime = SYSDATE
WHERE
    change_id = @cid
AND
    process_id = @pid    
";

        #endregion

        #endregion

        public AceChangeProcessWriter()
        { }

        public void Dispose()
        { }
    }
}
