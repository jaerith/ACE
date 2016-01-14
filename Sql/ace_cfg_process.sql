ALTER TABLE ACE_CFG_PROCESS
 DROP PRIMARY KEY CASCADE;
DROP TABLE ACE_CFG_PROCESS CASCADE CONSTRAINTS;

CREATE TABLE ACE_CFG_PROCESS
(
  PROCESS_ID    NUMBER(6)                       NOT NULL,
  DESCRIPTION   VARCHAR2(2048 BYTE),
  ADD_DTIME     DATE                            DEFAULT SYSDATE,
  UPD_DTIME     DATE                            DEFAULT SYSDATE,
  PROCESS_NAME  VARCHAR2(64 BYTE),
  ACTIVE_IND    CHAR(1 BYTE)                    DEFAULT 'Y'
);

CREATE UNIQUE INDEX ACE_CFG_PROCESS_PK ON ACE_CFG_PROCESS
(PROCESS_ID)
LOGGING
NOPARALLEL;

DROP PUBLIC SYNONYM ACE_CFG_PROCESS;

CREATE PUBLIC SYNONYM ACE_CFG_PROCESS FOR ACE_CFG_PROCESS;

ALTER TABLE ACE_CFG_PROCESS ADD (
  PRIMARY KEY
 (PROCESS_ID));