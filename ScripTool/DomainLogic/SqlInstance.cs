using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace ScripTool.DomainLogic
{
  public class SqlInstance
  {
    private SqlConnection _sqlConnection;

    public SqlConnection SqlConnection
    {
      get { return _sqlConnection; }      
    }
    private Server _server;

    public Server Server
    {
      get { return _server; }      
    }

    public SqlInstance(string connectionString)
    {      
      _sqlConnection = new SqlConnection(connectionString);
      _server = new Server(new Microsoft.SqlServer.Management.Common.ServerConnection(_sqlConnection));      
    }

    
  }
}
