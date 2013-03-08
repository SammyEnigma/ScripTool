using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScripTool.DomainLogic;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using System.IO;
using System.Xml.Linq;
using System.Collections.Specialized;

namespace ScripTool.Helpers
{
  public static class ExtensionMethods
  {
    public static void Script(this SqlInstance sqlInstance, string outputpath, Action<string> logAction)
    {
      var scripter = new Scripter(sqlInstance.Server);

      var database = sqlInstance.Server.Databases[sqlInstance.SqlConnection.Database];

      //add a database level folder 
      var dbDirectory = Directory.CreateDirectory(Path.Combine(outputpath, database.Name));
      logAction(string.Format("Created folder for database : {0}", dbDirectory.FullName));

      if (database.Tables.Count > 0)
      {
        ScriptTables(logAction, scripter, database, dbDirectory);
      }

      if (database.StoredProcedures.Count > 0)
      {
        ScriptStoredProcedures(logAction, scripter, database, dbDirectory);
      }

      if (database.Views.Count > 0)
      {
        ScriptViews(logAction, scripter, database, dbDirectory);
      }

      if (database.UserDefinedFunctions.Count > 0)
      {
        ScriptUserDefinedFunctions(logAction, scripter, database, dbDirectory);
      }

    }

    private static string GetDropString(string objectType, string objectName, string objectSchema)
    {
      if (objectType.Equals("table", StringComparison.OrdinalIgnoreCase))
        return string.Format(@"IF OBJECT_ID('{0}.{1}', 'U') IS NOT NULL
                            DROP {2} {0}.{1}
                            GO", objectSchema, objectName, objectType);

      if (objectType.Equals("function", StringComparison.OrdinalIgnoreCase))
        return string.Format(@"IF OBJECT_ID('{0}.{1}', 'FN') IS NOT NULL
                            DROP {2} {0}.{1}
                            GO", objectSchema, objectName, objectType);

      if (objectType.Equals("view", StringComparison.OrdinalIgnoreCase))
        return string.Format(@"IF OBJECT_ID('{0}.{1}', 'V') IS NOT NULL
                            DROP {2} {0}.{1}
                            GO", objectSchema, objectName, objectType);

      if (objectType.Equals("procedure", StringComparison.OrdinalIgnoreCase))
        return string.Format(@"IF OBJECT_ID('{0}.{1}', 'P') IS NOT NULL
                            DROP {2} {0}.{1}
                            GO", objectSchema, objectName, objectType);

      else throw new Exception("Unexpected object type");
    }

    private static void ScriptUserDefinedFunctions(Action<string> logAction, Scripter scripter, Database database, DirectoryInfo dbDirectory)
    {
      //create object level folder for tables
      var objectTypeDirectory = CreateObjectTypeFolder(logAction, dbDirectory, "Functions");

      scripter.Options.ScriptDrops = false;
      scripter.Options.DriAllConstraints = false;
      scripter.Options.IncludeIfNotExists = false;
      scripter.Options.IncludeDatabaseContext = false;


      foreach (UserDefinedFunction dbObject in database.UserDefinedFunctions)
      {
        //do not script system objects
        if (dbObject.IsSystemObject)
        {
          continue;
        }

        var objectScriptLineCollection = scripter.Script(new Urn[] { dbObject.Urn });

        WriteScriptLineCollectionToFile(objectTypeDirectory, "FUNCTION", dbObject.Schema, dbObject.Name, objectScriptLineCollection, database.Name);

        logAction(string.Format("Scripted user defined function : {0}.{1}", dbObject.Schema, dbObject.Name));
      }
    }

    private static void ScriptStoredProcedures(Action<string> logAction, Scripter scripter, Database database, DirectoryInfo dbDirectory)
    {
      //create object level folder for tables
      var objectTypeDirectory = CreateObjectTypeFolder(logAction, dbDirectory, "StoredProcedures");

      scripter.Options.ScriptDrops = false;
      scripter.Options.DriAllConstraints = false;
      scripter.Options.IncludeIfNotExists = false;
      scripter.Options.IncludeDatabaseContext = false;

      foreach (StoredProcedure dbObject in database.StoredProcedures)
      {
        //do not script system objects
        if (dbObject.IsSystemObject)
        {
          continue;
        }

        var objectScriptLineCollection = scripter.Script(new Urn[] { dbObject.Urn });

        WriteScriptLineCollectionToFile(objectTypeDirectory, "PROCEDURE", dbObject.Schema, dbObject.Name, objectScriptLineCollection, database.Name);

        logAction(string.Format("Scripted stored procedure : {0}.{1}", dbObject.Schema, dbObject.Name));
      }
    }

    private static void ScriptTables(Action<string> logAction, Scripter scripter, Database database, DirectoryInfo dbDirectory)
    {
      //create object level folder for tables
      var objectTypeDirectory = CreateObjectTypeFolder(logAction, dbDirectory, "Tables");

      scripter.Options.ScriptDrops = false;
      scripter.Options.DriAllConstraints = false;
      scripter.Options.IncludeIfNotExists = false;
      scripter.Options.IncludeDatabaseContext = false;

      foreach (Table table in database.Tables)
      {
        //do not script system objects
        if (table.IsSystemObject)
        {
          continue;
        }

        var objectScriptLineCollection = scripter.Script(new Urn[] { table.Urn });

        WriteScriptLineCollectionToFile(objectTypeDirectory, "TABLE", table.Schema, table.Name, objectScriptLineCollection, database.Name);

        logAction(string.Format("Scripted table : {0}.{1}", table.Schema, table.Name));
      }
    }

    private static void ScriptViews(Action<string> logAction, Scripter scripter, Database database, DirectoryInfo dbDirectory)
    {
      //create object level folder for tables
      var objectTypeDirectory = CreateObjectTypeFolder(logAction, dbDirectory, "Views");

      scripter.Options.ScriptDrops = false;
      scripter.Options.DriAllConstraints = false;
      scripter.Options.IncludeIfNotExists = false;
      scripter.Options.IncludeDatabaseContext = false;
      scripter.Options.IncludeDatabaseContext = false;

      foreach (View dbObject in database.Views)
      {
        //do not script system objects
        if (dbObject.IsSystemObject)
        {
          continue;
        }

        var objectScriptLineCollection = scripter.Script(new Urn[] { dbObject.Urn });

        WriteScriptLineCollectionToFile(objectTypeDirectory, "VIEW", dbObject.Schema, dbObject.Name, objectScriptLineCollection, database.Name);

        logAction(string.Format("Scripted view : {0}.{1}", dbObject.Schema, dbObject.Name));
      }
    }

    private static void WriteScriptLineCollectionToFile(DirectoryInfo objectTypeDirectory, string objectType, string objectSchema, string objectName, StringCollection objectScriptLineCollection, string databasename)
    {
      using (var stream = File.Create(Path.Combine(objectTypeDirectory.FullName, string.Format("{0}.{1}.sql", objectSchema, objectName))))
      {
        using (var sw = new StreamWriter(stream))
        {
          //set database context
          sw.WriteLine(string.Format(@"USE {0}{1}GO", databasename, Environment.NewLine));

          if (!objectType.Equals("table", StringComparison.OrdinalIgnoreCase))
          {
            sw.WriteLine(GetDropString(objectType, objectName, objectSchema));
          }

          bool passedCreate = false;

          foreach (string scriptline in objectScriptLineCollection)
          {
            if (passedCreate &&
                (scriptline.Contains("CREATE PROCEDURE")
                  || scriptline.Contains("CREATE TABLE")
                  || scriptline.Contains("CREATE VIEW")
                  || scriptline.Contains("CREATE FUNCTION")))
            {
              passedCreate = true;
            }

            //do not save these lines as this causes the script to fail, dbobject must be first in batch
            if (!passedCreate && 
                (scriptline.Equals("SET ANSI_NULLS ON", StringComparison.OrdinalIgnoreCase)
                  || scriptline.Equals("SET QUOTED_IDENTIFIER ON", StringComparison.OrdinalIgnoreCase)))
            {
              continue;
            }

            sw.WriteLine(scriptline);
          }
        }
      }
    }

    private static DirectoryInfo CreateObjectTypeFolder(Action<string> logAction, DirectoryInfo dbDirectory, string folderName)
    {
      var tablesDirectory = Directory.CreateDirectory(Path.Combine(dbDirectory.FullName, folderName));
      logAction(string.Format("Created object type folder : {0}", tablesDirectory.FullName));
      return tablesDirectory;
    }
  }
}
