CREATE USER [mid-AppModAssist] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [mid-AppModAssist];
ALTER ROLE db_datawriter ADD MEMBER [mid-AppModAssist];
