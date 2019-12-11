USE [arrangement-db];

ALTER TABLE [Participants]
ADD 
    RegistrationTime BIGINT NOT NULL DEFAULT DATEDIFF(SECOND,'1970-01-01', GETUTCDATE());