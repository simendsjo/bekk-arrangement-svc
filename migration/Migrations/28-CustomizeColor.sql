USE [arrangement-db];

ALTER TABLE [Events]
ADD 
    CustomHexColor NCHAR(6) NULL DEFAULT NULL;