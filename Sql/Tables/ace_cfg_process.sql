
CREATE TABLE [dbo].[ACE_CFG_PROCESS]
(
  [PROCESS_ID]           [numeric]   (6, 0)    NOT NULL,
  [DESCRIPTION]          [varchar]   (2048)    NOT NULL,
  [PROCESS_NAME]         [varchar]   (64)      NOT NULL,
  [ACTIVE_IND]           [char]      (1)       NOT NULL,
  [ADD_DTIME]            [datetime]            NOT NULL,
  [UPD_DTIME]            [datetime]            NOT NULL,
CONSTRAINT [PK_ACE_CFG_BUCKET] PRIMARY KEY CLUSTERED 
(
	[PROCESS_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[ACE_CFG_PROCESS] ADD CONSTRAINT [DF_ACP_ACTIVE_IND]  DEFAULT ('Y') FOR [ACTIVE_IND]
GO

ALTER TABLE [dbo].[ACE_CFG_PROCESS] ADD  CONSTRAINT [DF_ACP_ADD_DTIME]  DEFAULT (getdate()) FOR [ADD_DTIME]
GO

ALTER TABLE [dbo].[ACE_CFG_PROCESS] ADD  CONSTRAINT [DF_ACP_UPD_DTIME]  DEFAULT (getdate()) FOR [UPD_DTIME]
GO