
CREATE TABLE [dbo].[ACE_CFG_API](
(
 [PROCESS_ID]             [numeric] (6, 0)       NOT NULL,
 [API_TYPE]               [varchar](3)           NOT NULL,
 [BASE_URL]               [varchar](1024)        NOT NULL,
 [BUCKET_LIST]            [varchar](256)         NOT NULL,
 [SINCE_URL_ARG_NM]       [varchar](32)          NOT NULL,
 [ANCHOR_IND_TAG_NM]      [varchar](64)          NOT NULL,
 [ANCHOR_VAL_TAG_NM]      [varchar](1024)        NOT NULL,
 [ANCHOR_FILTER_ARGS]     [varchar](256)         NOT NULL,
 [REQUEST_FILTER_ARGS]    [varchar](256)         NOT NULL,
 [XPATH_RESP_FILTER]      [varchar](128)         NOT NULL,
 [TARGET_CHLD_TAG]        [varchar](64)          NOT NULL,
 [TARGET_CHLD_KEY_TAG]    [varchar](64)          NOT NULL,
 [PULLS_SINGLE_ITEM_FLAG] [char](1)              NOT NULL,
 [TARGET_KEY_LIST]        [varchar](4000)        NOT NULL,
 [CONTENT_TYPE]           [varchar](8)           NOT NULL,
 [REQUEST_HDR_ARGS]       [varchar](2000)        NOT NULL,
 [IS_URL_SINCE_EPOCH]     [char](1)              NOT NULL,
 [ADD_DTIME]              [datetime]             NOT NULL,
 [UPD_DTIME]              [datetime]             NOT NULL,
CONSTRAINT [PK_ACE_CFG_API] PRIMARY KEY CLUSTERED 
(
	[PROCESS_ID] ASC,
	[API_TYPE] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[ACE_CFG_API] ADD CONSTRAINT [DF_ACA_PULLS_SINGLE_ITEM_FLAG]  DEFAULT ('N') FOR [PULLS_SINGLE_ITEM_FLAG]
GO

ALTER TABLE [dbo].[ACE_CFG_API] ADD CONSTRAINT [DF_ACA_IS_URL_SINCE_EPOCH]  DEFAULT ('Y') FOR [IS_URL_SINCE_EPOCH]
GO

ALTER TABLE [dbo].[ACE_CFG_API] ADD CONSTRAINT [DF_ACA_CONTENT_TYPE]  DEFAULT ('XML') FOR [CONTENT_TYPE]
GO

ALTER TABLE [dbo].[ACE_CFG_API] ADD  CONSTRAINT [DF_ACA_ADD_DTIME]  DEFAULT (getdate()) FOR [ADD_DTIME]
GO

ALTER TABLE [dbo].[ACE_CFG_API] ADD  CONSTRAINT [DF_ACA_UPD_DTIME]  DEFAULT (getdate()) FOR [UPD_DTIME]
GO