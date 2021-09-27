USE [arrangement-db];

ALTER TABLE [Events]
ADD 
    CloseRegistrationTime BIGINT NULL DEFAULT NULL;