USE [arrangement-db];

ALTER TABLE [Participants]
ADD
    Name VARCHAR(255) NOT NULL DEFAULT '',
    Comment VARCHAR(MAX) NULL;
