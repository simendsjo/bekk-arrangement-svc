USE [arrangement-db];

ALTER TABLE [Events]
ADD HasWaitlist BIT NOT NULL DEFAULT 0;