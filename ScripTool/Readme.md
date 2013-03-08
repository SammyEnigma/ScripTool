This project is a commandline utility to facilitate generating scripts of SQL Server database 
objects (stored procedures, tables, views and functions). All objects (except tables) are 
scripted as "DROP-CREATE" scripts, meaning that if the object exists already, it will be 
dropped before the create script is run in order to prevent the need to manually drop the 
object or change the script to an ALTER statement.