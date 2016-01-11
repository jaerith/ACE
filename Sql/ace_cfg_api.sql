ALTER TABLE ACE_CFG_API
 DROP PRIMARY KEY CASCADE;
DROP TABLE ACE_CFG_API CASCADE CONSTRAINTS;

CREATE TABLE ACE_CFG_API
(
  PROCESS_ID              NUMBER(6)             NOT NULL,
  API_TYPE                VARCHAR2(3 BYTE)      NOT NULL,
  BASE_URL                VARCHAR2(1024 BYTE)   NOT NULL,
  BUCKET_LIST             VARCHAR2(256 BYTE)    NOT NULL,
  SINCE_URL_ARG_NM        VARCHAR2(24 BYTE),
  ANCHOR_IND_TAG_NM       VARCHAR2(48 BYTE),
  ANCHOR_VAL_TAG_NM       VARCHAR2(1024 BYTE),
  ANCHOR_FILTER_ARGS      VARCHAR2(256 BYTE),
  REQUEST_FILTER_ARGS     VARCHAR2(256 BYTE),
  XPATH_RESP_FILTER       VARCHAR2(128 BYTE),
  TARGET_CHLD_TAG         VARCHAR2(48 BYTE),
  TARGET_CHLD_KEY_TAG     VARCHAR2(48 BYTE),
  PULLS_SINGLE_ITEM_FLAG  CHAR(1 BYTE)          DEFAULT 'N',
  TARGET_KEY_LIST         VARCHAR2(4000 BYTE),
  ADD_DTIME               DATE                  DEFAULT SYSDATE,
  UPD_DTIME               DATE                  DEFAULT SYSDATE
);


CREATE UNIQUE INDEX ACE_CFG_API_PK ON ACE_CFG_API
(PROCESS_ID, API_TYPE)
LOGGING
TABLESPACE INDEX
PCTFREE    10
INITRANS   2
MAXTRANS   255;

DROP PUBLIC SYNONYM ACE_CFG_API;

CREATE PUBLIC SYNONYM ACE_CFG_API FOR ACE_CFG_API;

ALTER TABLE ACE_CFG_API ADD (
  PRIMARY KEY
 (PROCESS_ID, API_TYPE));