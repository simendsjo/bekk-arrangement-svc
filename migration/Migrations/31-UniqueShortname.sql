USE [arrangement-db];

CREATE UNIQUE NONCLUSTERED INDEX UQ_Event_Shortname
    ON Events(Shortname)
    WHERE Shortname IS NOT NULL;