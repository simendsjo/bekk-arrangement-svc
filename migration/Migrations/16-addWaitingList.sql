USE [arrangement-db];

ALTER TABLE [Events]
ADD HasWaitingList BIT NOT NULL DEFAULT 0;