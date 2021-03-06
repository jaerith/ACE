
CREATE TABLE [dbo].[ACE_CFG_BUCKET]
(
 [PROCESS_ID]           [numeric] (6, 0)    NOT NULL,
 [API_TYPE]             [varchar] (3)       NOT NULL,
 [BUCKET_NM]            [varchar] (48)      NOT NULL,
 [ATTR_NM]              [varchar] (48)      NOT NULL,
 [TARGET_TABLE_NM]      [varchar] (48)      NOT NULL,
 [ATTR_ORA_TYPE]        [varchar] (24)      NOT NULL,
 [ATTR_ORA_TYPE_LEN]    [numeric] (3, 0)    NOT NULL,
 [ATTR_IS_KEY]          [char]    (1)       NOT NULL,
 [ATTR_XPATH]           [varchar] (128)     NOT NULL,
 [ATTR_IS_XML_BODY]     [char]    (1)       NOT NULL,
 [ADD_DTIME]            [datetime]          NOT NULL,
 [UPD_DTIME]            [datetime]          NOT NULL,
CONSTRAINT [PK_ACE_CFG_BUCKET] PRIMARY KEY CLUSTERED 
(
	[PROCESS_ID] ASC,
	[API_TYPE] ASC,
	[BUCKET_NM] ASC,
	[ATTR_NM] ASC	
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[ACE_CFG_BUCKET] ADD CONSTRAINT [DF_ACB_ATTR_IS_KEY]  DEFAULT ('N') FOR [ATTR_IS_KEY]
GO

ALTER TABLE [dbo].[ACE_CFG_BUCKET] ADD CONSTRAINT [DF_ACB_ATTR_IS_XML_BODY]  DEFAULT ('N') FOR [ATTR_IS_XML_BODY]
GO

ALTER TABLE [dbo].[ACE_CFG_BUCKET] ADD  CONSTRAINT [DF_ACB_ADD_DTIME]  DEFAULT (getdate()) FOR [ADD_DTIME]
GO

ALTER TABLE [dbo].[ACE_CFG_BUCKET] ADD  CONSTRAINT [DF_ACB_UPD_DTIME]  DEFAULT (getdate()) FOR [UPD_DTIME]
GO

