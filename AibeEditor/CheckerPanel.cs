using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Aibe.Models.DB;
using Aibe.Models.Tests;
using Extension.Controls;
using Extension.Models;
using Extension.String;

namespace AibeEditor {
  public partial class CheckerPanel : UserControl {
    public CheckerPanel() {
      InitializeComponent();
    }

    string columnName;
    public void SetTitle(string title) {
      labelTitle.Text = title.ToCamelBrokenString();
      columnName = title;
    }

    public string GetSyntaxText() {
      return richTextBoxSyntax.Text;
    }

    public void SetSyntaxText(string text) {
      richTextBoxSyntax.Text = text;
      GetResult();
    }

    private void printResult(SyntaxCheckerResult result) {
      string text = string.Empty;
      richTextBoxResult.AppendText(Environment.NewLine);
      richTextBoxResult.AppendText("Result: ");
      richTextBoxResult.AppendStyledTextWithColor(FontStyle.Bold,
        result.Result ? Color.DarkGreen : Color.DarkRed,
        result.Result.ToString());
      richTextBoxResult.AppendText(string.Concat(Environment.NewLine,
        "Desc: "));
      richTextBoxResult.AppendTextWithColor(Color.Brown, result.Description);
      richTextBoxResult.AppendText(string.Concat(Environment.NewLine,
        "Message: "));
      richTextBoxResult.AppendTextWithColor(Color.Teal,
        string.IsNullOrWhiteSpace(result.Message) ? string.Empty : result.Message);
      richTextBoxResult.AppendText(Environment.NewLine);
    }

    public void GetResult() {
      richTextBoxResult.Clear();
      BaseMetaItem item = new BaseMetaItem();
      BaseMetaItemTest tester = new BaseMetaItemTest(item);
      PropertyInfo prop = item.GetType().GetProperty(columnName);
      if (null != prop && prop.CanWrite)
        prop.SetValue(item, richTextBoxSyntax.Text, null);
      if (prop == null) {
        SyntaxCheckerResult result = new SyntaxCheckerResult {
          Result = false,
          Message = "Property is not found or unwritable",
        };
        printResult(result);
        return;
      }
      MethodInfo method = tester.GetType().GetMethod("Test" + columnName);
      if (null == method) {
        SyntaxCheckerResult result = new SyntaxCheckerResult {
          Result = false,
          Message = "Checking method not available",
        };
        printResult(result);
        return;
      }
      object objResult = method.Invoke(tester, new object[] { richTextBoxSyntax.Text });
      SyntaxCheckerResult finalResult = (SyntaxCheckerResult)objResult;
      printResult(finalResult);
    }

    private void richTextBoxSyntax_TextChanged(object sender, EventArgs e) {
      GetResult();
    }
  }
}
