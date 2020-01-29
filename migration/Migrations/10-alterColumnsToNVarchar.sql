ALTER TABLE [Events]
ALTER COLUMN [Title] nvarchar(255) not null

ALTER TABLE [Events]
ALTER COLUMN [Description] nvarchar(MAX) null 

ALTER TABLE [Events]
ALTER COLUMN [Location] nvarchar(MAX) not null 