USE [arrangement-db];

UPDATE
    [Events]
SET
    [Events].Shortname = OldShortname.Shortname
    FROM
    [Events] E
        INNER JOIN
    [Shortnames] OldShortname
ON
    OldShortname.EventId = E.Id;

DROP TABLE [Shortnames];