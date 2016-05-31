
CREATE TABLE [dbo].[ACE_CHANGE]
(
  [CHANGE_ID]           [numeric]   (10, 0)   NOT NULL AUTO_INCREMENT,
  [PROCESS_ID]          [numeric]   (6, 0)    NOT NULL,
  [START_DTIME]         [datetime]            NOT NULL,
  [END_DTIME]           [datetime]            NOT NULL,
  [TOTAL_RECORDS]       [numeric]   (7, 0)    NOT NULL,
  [LAST_ANCHOR]         [varchar]   (512)     NOT NULL,
  [STATUS]              [char]      (1)       NOT NULL,
  [STATUS_MESSAGE]      [varchar]   (4000)    NOT NULL,
  [ADD_DTIME]           [datetime]            NOT NULL,
  [UPD_DTIME]           [datetime]            NOT NULL,
CONSTRAINT [PK_ACE_CHANGE] PRIMARY KEY CLUSTERED 
(
	[CHANGE_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[ACE_CHANGE] ADD  CONSTRAINT [DF_AC_ADD_DTIME]  DEFAULT (getdate()) FOR [ADD_DTIME]
GO

ALTER TABLE [dbo].[ACE_CHANGE] ADD  CONSTRAINT [DF_AC_UPD_DTIME]  DEFAULT (getdate()) FOR [UPD_DTIME]
GO