ALTER TABLE [Events]
DROP COLUMN OpenForRegistrationDate;

ALTER TABLE [Events]
DROP COLUMN OpenForRegistrationTime;

ALTER TABLE [Events]
ADD OpenForRegistrationTime bigint NOT NULL DEFAULT(1581077551324);