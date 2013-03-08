using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NDesk.Options;
using ScripTool.DomainLogic;
using ScripTool.Helpers;

namespace ScripTool
{
  class Program
  {
    static void Main(string[] args)
    {
      string connectionString = null;
      string outputPath = null;

      OptionSet p = new OptionSet() {
            { "c|connectionString=",    v => connectionString = v },
            { "o|outputPath=",  v => outputPath = v }            
        };

      var extra = p.Parse(args);

      if (connectionString != null && outputPath != null)
      {
        var sql = new SqlInstance(connectionString);
        sql.Script(outputPath, s => Console.WriteLine(s));
      }
      else
      {
        throw new Exception("Invalid parameters, connectionstring and outputPath must be set.");
      }
    }
  }
}
