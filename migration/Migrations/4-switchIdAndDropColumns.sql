USE [arrangement-db]

ALTER TABLE [Events]
DROP CONSTRAINT PK__Events__3214EC07B7D6C63A;
--PK name for Dev
ALTER TABLE [Events]
ADD CONSTRAINT PK__Events PRIMARY KEY (Guid);

ALTER TABLE [Events]
DROP COLUMN 
    FromDate,
    ToDate,
    Id;

-- Find PK name using this command:
-- SELECT * 
-- FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
-- WHERE CONSTRAINT_TYPE = 'PRIMARY KEY'