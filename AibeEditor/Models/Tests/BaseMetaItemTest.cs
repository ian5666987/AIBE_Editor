using Aibe.Models.Core;
using Aibe.Models.DB;
using Extension.Models;
using Extension.String;
using System.Collections.Generic;
using System.Linq;

namespace Aibe.Models.Tests {
  public class BaseMetaItemTest {
    private BaseMetaItem item { get; set; }
    public BaseMetaItemTest(BaseMetaItem item) {
      this.item = item;
    }

    public SyntaxCheckerResult TestTableName(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Table Name",
        Description = desc,
        Result = !string.IsNullOrWhiteSpace(desc),       
      };
      finalResult.Message = (finalResult.Result ? "Valid" : "Invalid") + " Table Name";
      return finalResult;
    }

    public SyntaxCheckerResult TestDisplayName(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Display Name",
        Description = desc,
        Result = true,
        Message = "Basic Checker: Syntax Validity Only"
      };
      return finalResult;
    }

    public SyntaxCheckerResult TestTableSource(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Table Source",
        Description = desc,
        Result = true,
        Message = "Basic Checker: Syntax Validity Only"
      };
      return finalResult;
    }

    public SyntaxCheckerResult TestPrefilledColumns(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Prefilled Columns",
        Description = desc,
      };
      var items = desc.ParseComponents()
          .Select(x => new PrefilledColumnInfoTest(x)).ToList();
      foreach(var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestItemsPerPage(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Items Per Page",
        Description = desc,
      };
      int intResult = 0;
      bool result = int.TryParse(desc, out intResult);
      finalResult.Result = result;
      finalResult.Message = finalResult.Result ? "Valid Items Per Page" : 
        "Invalid Items Per Page. Description Value Must Be A Positive Integer Between 1 To " + short.MaxValue;
      return finalResult;
    }

    public SyntaxCheckerResult TestOrderBy(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Order By",
        Description = desc,
      };
      List<OrderByInfoTest> items = new List<OrderByInfoTest>();
      if (desc.Trim().StartsWith(Aibe.DH.SQLScriptDirectivePrefix)) { //special order by using SQL script directive
        OrderByInfoTest obInfo = new OrderByInfoTest(desc, true);
        items = obInfo.IsValid ? new List<OrderByInfoTest>() { obInfo } : new List<OrderByInfoTest>();
      } else //normal order-bys
        items = desc.GetTrimmedNonEmptyParts(';')
          .Select(x => new OrderByInfoTest(x, false)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestActionList(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Action List",
        Description = desc,
      };
      var items = desc.GetTrimmedNonEmptyParts(';')
        .Select(x => new ActionInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestDefaultActionList(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Default Action List",
        Description = desc,
      };
      finalResult.SubResults.AddRange(getResultsForString(desc));
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestTableActionList(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Table Action List",
        Description = desc,
      };
      var items = desc.GetTrimmedNonEmptyParts(';')
        .Select(x => new ActionInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestDefaultTableActionList(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Default Table Action List",
        Description = desc,
      };
      finalResult.SubResults.AddRange(getResultsForString(desc));
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestTextFieldColumns(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Text Field Columns",
        Description = desc,
      };
      var items = desc.GetTrimmedNonEmptyParts(';')
          .Select(x => new TextFieldColumnInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestPictureColumns(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Picture Columns",
        Description = desc,
      };
      var items = desc.GetTrimmedNonEmptyParts(';')
          .Select(x => new PictureColumnInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestIndexShownPictureColumns(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Index-Shown Picture Columns",
        Description = desc,
      };
      finalResult.SubResults.AddRange(getResultsForString(desc));
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestRequiredColumns(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Required Columns",
        Description = desc,
      };
      finalResult.SubResults.AddRange(getResultsForString(desc));
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestNumberLimitColumns(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Number Limit Columns",
        Description = desc,
      };
      var items = desc.GetTrimmedNonEmptyParts(';')
          .Select(x => new NumberLimitColumnInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestRegexCheckedColumns(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Regex-Checked Columns",
        Description = desc,
      };
      if (string.IsNullOrWhiteSpace(desc))
        return finalResult;
      var items = desc.GetXMLTaggedInnerStrings(Aibe.DH.RegexCheckedColumnTag)
        .Select(x => new RegexCheckedColumnInfoTest(x)).ToList();
      if (items == null || items.Count <= 0) {
        finalResult.Message = "Invalid syntax";
        return finalResult;
      }
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestRegexCheckedColumnExamples(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Regex-Checked Column Examples",
        Description = desc,
      };
      if (string.IsNullOrWhiteSpace(desc))
        return finalResult;
      var items = desc.GetXMLTaggedInnerStrings(Aibe.DH.RegexCheckedColumnExampleTag)
        .Select(x => new RegexCheckedColumnExampleInfoTest(x)).ToList();
      if (items == null || items.Count <= 0) {
        finalResult.Message = "Invalid syntax";
        return finalResult;
      }
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestUserRelatedFilters(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "User-Related Filters",
        Description = desc,
      };
      var items = desc.GetTrimmedNonEmptyParts(';')
        .Select(x => new UserRelatedFilterInfoTest(x)).ToList(); //exclude non successful parsing result
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestDisableFilter(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Disable Filter",
        Description = desc,
      };
      bool hasValidItem = true.ToString().EqualsIgnoreCase(desc) || false.ToString().EqualsIgnoreCase(desc);
      finalResult.Result = string.IsNullOrWhiteSpace(desc) || hasValidItem;
      finalResult.Message = finalResult.Result ? "Valid Description Value [" + desc + "]" :
        "Invalid Description Value [" + desc + "]. Valid Description Values: " + 
        string.Join(", ", Aibe.DH.TrueFalse);
      return finalResult;
    }

    public SyntaxCheckerResult TestForcedFilterColumns(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Forced Filter Columns",
        Description = desc,
      };
      var items = desc.GetTrimmedNonEmptyParts(';')
        .Select(x => new InclusionInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestColumnExclusionList(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Column Exclusion List",
        Description = desc,
      };
      var items = desc.GetTrimmedNonEmptyParts(';')
        .Select(x => new ExclusionInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestFilterExclusionList(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Filter Exclusion List",
        Description = desc,
      };
      var items = desc.GetTrimmedNonEmptyParts(';')
        .Select(x => new ExclusionInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestDetailsExclusionList(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Details Exclusion List",
        Description = desc,
      };
      var items = desc.GetTrimmedNonEmptyParts(';')
        .Select(x => new ExclusionInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestCreateEditExclusionList(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Create-Edit Exclusion List",
        Description = desc,
      };
      var items = desc.GetTrimmedNonEmptyParts(';')
        .Select(x => new ExclusionInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestCsvExclusionList(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Csv Exclusion List",
        Description = desc,
      };
      var items = desc.GetTrimmedNonEmptyParts(';')
        .Select(x => new ExclusionInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestAccessExclusionList(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Access Exclusion List",
        Description = desc,
      };
      finalResult.SubResults.AddRange(getResultsForString(desc));
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestColoringList(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Coloring List",
        Description = desc,
      };
      var items = desc.GetTrimmedNonEmptyParts(';')
        .Select(x => new ColoringInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestFilterDropDownLists(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Filter Dropdown List",
        Description = desc,
      };
      var items = desc.GetTrimmedNonEmptyParts(';')
        .Select(x => new DropDownInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestCreateEditDropDownLists(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Create-Edit Dropdown List",
        Description = desc,
      };
      var items = desc.GetTrimmedNonEmptyParts(';')
        .Select(x => new DropDownInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestPrefixesOfColumns(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Prefixes Of Columns",
        Description = desc,
      };
      var items = desc.GetTrimmedNonEmptyParts(';')
        .Select(x => new AffixColumnInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestPostfixesOfColumns(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Postfixes Of Columns",
        Description = desc,
      };
      var items = desc.GetTrimmedNonEmptyParts(';')
        .Select(x => new AffixColumnInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestListColumns(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "List Columns",
        Description = desc,
      };
      var items = desc.GetTrimmedNonEmptyParts(';')
        .Select(x => new ListColumnInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestTimeStampColumns(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Timestamp Columns",
        Description = desc,
      };
      var items = desc.GetTrimmedNonEmptyParts(';')
        .Select(x => new TimeStampColumnInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestHistoryTable(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "History Table",
        Description = desc,
      };
      List<SyntaxCheckerResult> results = new List<SyntaxCheckerResult>();
      if (string.IsNullOrWhiteSpace(desc)) {
        finalResult.Message = "Empty Text";
        return finalResult;
      }
      HistoryInfo testHistoryTable = new HistoryInfo(desc);
      SyntaxCheckerResult result = new SyntaxCheckerResult();
      result.Result = testHistoryTable != null && testHistoryTable.IsValid;
      result.Description = desc;
      results.Add(result);
      finalResult.SubResults.AddRange(results);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestHistoryTriggers(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "History Triggers",
        Description = desc,
      };
      var items = desc.ParseComponents(';')
        .Select(x => new HistoryTriggerInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestAutoGeneratedColumns(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Auto-Generated Columns",
        Description = desc,
      };
      var items = desc.GetTrimmedNonEmptyParts(';')
        .Select(x => new AutoGeneratedColumnInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestColumnSequence(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Column Sequence",
        Description = desc,
      };
      finalResult.SubResults.AddRange(getResultsForString(desc));
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestColumnAliases(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Column Aliases",
        Description = desc,
      };
      var items = desc.GetTrimmedNonEmptyParts(';')
        .Select(x => new CommonBaseInfo(x)).ToList();
      finalResult.SubResults.AddRange(getBaseResults(items.Select(x => (BaseInfo)x).ToList()));
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestEditShowOnlyColumns(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Edit Show-Only Columns",
        Description = desc,
      };
      finalResult.SubResults.AddRange(getResultsForString(desc));
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestScriptConstructorColumns(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Script Constructor Columns",
        Description = desc,
      };
      var items = desc.GetTrimmedNonEmptyParts(';')
        .Select(x => new CommonBaseInfo(x)).ToList();
      List<SyntaxCheckerResult> results = new List<SyntaxCheckerResult>();
      foreach (var item in items) {
        SyntaxCheckerResult result = new SyntaxCheckerResult();
        result.Result = item.IsValid && item.HasRightSide;
        var parts = item.Name.GetTrimmedNonEmptyParts('|');
        if (string.IsNullOrWhiteSpace(parts[0])) //the actual "Name" is only the first part
          result.Result = false;
        result.Description = item.UntrimmedOriginalDesc;
        results.Add(result);
      }
      finalResult.SubResults.AddRange(results);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestScriptColumns(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Script Columns",
        Description = desc,
      };
      var items = desc.GetTrimmedNonEmptyParts(';')
        .Select(x => new CommonBaseInfo(x)).ToList();
      List<SyntaxCheckerResult> results = new List<SyntaxCheckerResult>();
      foreach (var item in items) {
        SyntaxCheckerResult result = new SyntaxCheckerResult();
        result.Result = item.IsValid && item.HasRightSide;
        result.Description = item.UntrimmedOriginalDesc;
        results.Add(result);
      }
      finalResult.SubResults.AddRange(results);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestCustomDateTimeFormatColumns(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Custom Date-Time Format Columns",
        Description = desc,
      };
      var items = desc.GetTrimmedNonEmptyParts(';')
        .Select(x => new CustomDateTimeFormatInfoTest(x)).ToList(); //only accept custom date time formats for date time columns for obvious reason
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestEmailMakerTriggers(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Email Maker Triggers",
        Description = desc,
      };
      var items = desc.ParseComponents(';')
        .Select(x => new EmailMakerTriggerInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestEmailMakers(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Email Makers",
        Description = desc,
      };
      var items = desc.GetTrimmedNonEmptyParts(';')
        .Select(x => new EmailMakerInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestNonPictureAttachmentColumns(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Non-Picture Attachment Columns",
        Description = desc,
      };
      var items = desc.GetTrimmedNonEmptyParts(';')
        .Select(x => new AttachmentInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestDownloadColumns(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Download Columns",
        Description = desc,
      };
      finalResult.SubResults.AddRange(getResultsForString(desc));
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestPreActionTriggers(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Pre-Action Triggers",
        Description = desc,
      };
      var items = desc.ParseComponents(';')
        .Select(x => new ActionTriggerInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestPreActionProcedures(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Pre-Action Procedures",
        Description = desc,
      };
      var items = desc.ParseComponents(';')
        .Select(x => new ActionProcedureInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestPostActionTriggers(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Post-Action Triggers",
        Description = desc,
      };
      var items = desc.ParseComponents(';')
        .Select(x => new ActionTriggerInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestPostActionProcedures(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Post-Action Procedures",
        Description = desc,
      };
      var items = desc.ParseComponents(';')
        .Select(x => new ActionProcedureInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    public SyntaxCheckerResult TestTableType(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Table Type",
        Description = desc,
        Result = !string.IsNullOrWhiteSpace(desc) && Aibe.DH.TableTypes.Any(x => x.EqualsIgnoreCaseTrim(desc)),
      };
      finalResult.Message = (finalResult.Result ? "Valid" : "Invalid") + " Table Type";
      return finalResult;
    }

    public SyntaxCheckerResult TestAggregationStatement(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Aggregation Statement",
        Description = desc,
      };
      var item = new AggregationStatementInfoTest(desc);
      finalResult.Result = item.IsValid;
      finalResult.SubResults.Add(item.CheckerResult);
      return finalResult;
    }

    public SyntaxCheckerResult TestForeignInfoColumns(string desc) {
      SyntaxCheckerResult finalResult = new SyntaxCheckerResult {
        DisplayText = "Foreign Info Columns",
        Description = desc,
      };
      var items = desc.ParseComponents(';')
        .Select(x => new ForeignInfoColumnInfoTest(x)).ToList();
      foreach (var item in items)
        finalResult.SubResults.Add(item.CheckerResult);
      finalResult.Result = finalResult.GetDirectSubResults();
      return finalResult;
    }

    private List<SyntaxCheckerResult> getResultsForString(string desc) {
      List<SyntaxCheckerResult> results = new List<SyntaxCheckerResult>();
      var items = desc.GetTrimmedNonEmptyParts(';').ToList();
      foreach (var item in items) {
        SyntaxCheckerResult result = new SyntaxCheckerResult();
        result.Result = true;
        result.Description = item;
        results.Add(result);
      }
      return results;
    }

    private List<SyntaxCheckerResult> getBaseResults(List<BaseInfo> items) {
      List<SyntaxCheckerResult> results = new List<SyntaxCheckerResult>();
      foreach (var item in items) {
        SyntaxCheckerResult result = new SyntaxCheckerResult();
        result.Result = item.IsValid;
        result.Description = item.UntrimmedOriginalDesc;
        results.Add(result);
      }
      return results;
    }
  }
}
