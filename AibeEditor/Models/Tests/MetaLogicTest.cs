using System.Collections.Generic;
using System.Data;
using System.Linq;
using Extension.Database.SqlServer;
using Extension.String;

namespace Aibe.Models.Tests {
  public class MetaLogicTest {
    public static List<string> GetAllConfiguredTables() {
      List<object> items = SQLServerHandler.GetSingleColumn(Aibe.DH.DataDBConnectionString,
        Aibe.DH.MetaTableName, Aibe.DH.MICNTableName);
      return items
        .Where(x => x != null)
        .Select(x => x.ToString())
        .OrderBy(x => x.ToLower())
        .ToList();
    }

    public static string GetDescriptionFor(string tableName, string columnName) {
      List<object> items = SQLServerHandler.GetSingleColumnWhere(Aibe.DH.DataDBConnectionString, 
        Aibe.DH.MetaTableName, columnName, 
        string.Concat(Aibe.DH.MICNTableName, "=", tableName.AsSqlStringValue()));
      return items.Count > 0 ? items[0].ToString() : string.Empty;
    }

    public static List<string> GetAllMetaColumns() {
      return SQLServerHandler.GetColumns(Aibe.DH.DataDBConnectionString, Aibe.DH.MetaTableName)
        .Select(x => x.ColumnName)
        .ToList();
    }
  }
}
