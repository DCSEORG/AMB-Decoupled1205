-- script.sql
-- Grant managed identity database access roles
-- This script is run by run-sql-dbrole.py

-- Drop and recreate the managed identity user with correct SID
IF EXISTS (SELECT * FROM sys.database_principals WHERE name = 'mid-AppModAssist-30-11-30')
BEGIN
    DROP USER [mid-AppModAssist-30-11-30];
END

CREATE USER [mid-AppModAssist-30-11-30] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [mid-AppModAssist-30-11-30];
ALTER ROLE db_datawriter ADD MEMBER [mid-AppModAssist-30-11-30];
GRANT EXECUTE TO [mid-AppModAssist-30-11-30];
