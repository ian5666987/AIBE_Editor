using Aibe.Models.DB;
using Aibe.Models.Docs;
using Aibe.Models.Tests;
using Extension.Controls;
using Extension.Database.SqlServer;
using Extension.Extractor;
using Extension.Models;
using Extension.String;
using Extension.Versioning;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using logic = Aibe.Models.Tests.AibeSyntaxCheckerLogic;

namespace AibeEditor {
  public partial class AibeEditorForm : Form {
    bool useDB = true;
    bool showNotUseDB = true;
    bool showFailedUpdate = true;
    bool showErrorUpdate = true;
    bool showEmptyUpdate = true;
    bool showSuccessfulUpdate = true;

    AibeTypingContext typingContext = new AibeTypingContext();
    bool autoWriting = false;
    SyntaxCheckerResult finalColumnCheckResult;
    AibeDocument doc;
    int tickValue = 0;
    int scriptTickValue = 0;
    const int columnRenderingTick = 5;
    const int scriptRenderingTick = 10;
    string line = "------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------";
    List<string> excludedColorPrefixes = new List<string> { "Active", "Button", "Control", "Gradient", "Highlight", "Inactive", "Info", "Menu", "Window",
      "AppWorkspace", "Desktop", "HotTrack", "GrayText", "ScrollBar", "Transparent" };

    public AibeEditorForm() {
      try {
        InitializeComponent();
        initSettings();
        listBoxColumns.Items.AddRange(logic.MetaItemColumnNames.ToArray());
        comboBoxTableNames.Items.AddRange(logic.ConfiguredTableNames.ToArray());
        if (comboBoxTableNames.Items.Count > 0)
          comboBoxTableNames.SelectedIndex = 0;
        updateDBResult();

        Text += " v" + Info.GetProductVersionFor(Assembly.GetExecutingAssembly()) +
          ", Aibe v" + Info.GetProductVersionFor(Assembly.GetAssembly(typeof(Aibe.Models.MetaInfo))) +
          " \u00A9 " + DateTime.Now.Year.ToString() + " (by Ian K)";
        listBoxSuggestion.Visible = false;
        labelTypes.Visible = false;
        KeyPreview = true; //otherwise cannot capture key down for the form
        KnownColor[] colors = (KnownColor[])Enum.GetValues(typeof(KnownColor));
        foreach (KnownColor knowColor in colors) {
          Color color = Color.FromKnownColor(knowColor);
          string colorString = color.ToString().Substring("Color [".Length); //to get rid of the enclosing "[ ]"
          if (excludedColorPrefixes.Any(x => colorString.StartsWith(x)))
            continue;
          logic.Colors.Add(colorString.Substring(0, colorString.Length - 1));
        }
        maxListBoxSuggestionHeight = listBoxSuggestion.Height;
        initialWidth = listBoxSuggestion.Width;
      } catch (Exception ex) {
        MessageBox.Show("Error! Please check your .config file and ensure all keys and values in appSettings are correct!" + 
          Environment.NewLine + Environment.NewLine + "Message: " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        Close();
      }
    }

    private void refreshDB() {
      logic.MinorUpdates();
      string selectedTableName = comboBoxTableNames.SelectedIndex >= 0 ?
        comboBoxTableNames.SelectedItem.ToString() : string.Empty;
      comboBoxTableNames.Items.Clear();
      comboBoxTableNames.Items.AddRange(logic.ConfiguredTableNames.ToArray());
      if (comboBoxTableNames.Items.Count > 0) {
        if(string.IsNullOrWhiteSpace(selectedTableName))
          comboBoxTableNames.SelectedIndex = 0;
        else
          comboBoxTableNames.SelectedItem = selectedTableName;
      }
      updateCurrentBoxes();
      updateTableResult();
      updateDBResult();
    }

    private void minorDBUpdates() {
      logic.MinorUpdates();
      updateCurrentBoxes();
      updateTableResult();
    }

    private void initSettings() {
      try {
        string notUseDB = ConfigurationManager.AppSettings["NotUseDB"];
        string failedUpdate = ConfigurationManager.AppSettings["FailedUpdate"];
        string errorUpdate = ConfigurationManager.AppSettings["ErrorUpdate"];
        string emptyUpdate = ConfigurationManager.AppSettings["EmptyUpdate"];
        string successfulUpdate = ConfigurationManager.AppSettings["SuccessfulUpdate"];
        notUseDBWarningToolStripMenuItem.Checked = !notUseDB.EqualsIgnoreCase(false.ToString());
        failedUpdateWarningToolStripMenuItem.Checked = !failedUpdate.EqualsIgnoreCase(false.ToString());
        errorUpdateWarningToolStripMenuItem.Checked = !errorUpdate.EqualsIgnoreCase(false.ToString());
        emptyUpdateWarningToolStripMenuItem.Checked = !emptyUpdate.EqualsIgnoreCase(false.ToString());
        successfulUpdateNotificationToolStripMenuItem.Checked = !successfulUpdate.EqualsIgnoreCase(false.ToString());
        showNotUseDB = notUseDBWarningToolStripMenuItem.Checked;
        showFailedUpdate = failedUpdateWarningToolStripMenuItem.Checked;
        showErrorUpdate = errorUpdateWarningToolStripMenuItem.Checked;
        showEmptyUpdate = emptyUpdateWarningToolStripMenuItem.Checked;
        showSuccessfulUpdate = successfulUpdateNotificationToolStripMenuItem.Checked;

        string isDebugString = ConfigurationManager.AppSettings["IsDebug"];
        bool isDebug = isDebugString.EqualsIgnoreCase(true.ToString());
        string isFullDebugString = ConfigurationManager.AppSettings["IsFullDebug"];
        bool isFullDebug = isFullDebugString.EqualsIgnoreCase(true.ToString());
        if (!isDebug)
          splitContainerScriptChecker.Panel2Collapsed = true;
        else if (isFullDebug)
          splitContainerScriptChecker.Panel1Collapsed = true;
      } catch {
        MessageBox.Show("Some Initialization Parameter(s) Are Missing", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
      }
    }

    private void listBoxColumns_SelectedIndexChanged(object sender, EventArgs e) {
      richTextBoxEditor.Enabled = true;
      updateCurrentBoxes();
    }

    private void descriptionNotFound() {
      richTextBoxChecker.Clear();
      richTextBoxChecker.AppendTextWithColor(Color.DarkRed, "[Description Not Found]");
    }

    private void getCheckResult(string tableName, string columnName) {
      richTextBoxChecker.Clear();
      string text = richTextBoxEditor.Text;
      if (useDB) {
        BaseErrorModel result = logic.Check(tableName, columnName, text);
        switch (result.Code) {
          case -2:
          case -3: richTextBoxChecker.AppendTextWithColor(Color.DarkRed, "[" + result.Message + "]"); break;
        }
        if (result.Code != 0) {
          if (string.IsNullOrWhiteSpace(text))
            descriptionNotFound();
          return;
        }
        finalColumnCheckResult = (SyntaxCheckerResult)result.ReturnObject;    
      } else { //only basic checker is available
        BaseErrorModel result = logic.BasicCheck(columnName, text);
        finalColumnCheckResult = (SyntaxCheckerResult)result.ReturnObject;
      }
      startPrintCheckerResult(richTextBoxChecker, finalColumnCheckResult, printLine: false, isBasic: !useDB);
      if (string.IsNullOrWhiteSpace(text))
        descriptionNotFound();
    }

    private void updateCurrentBoxes() {
      if (listBoxColumns.SelectedIndex < 0) //nothing is validly selected
        return;
      string columnName = logic.MetaItemColumnNames.FirstOrDefault(x => x == listBoxColumns.SelectedItem.ToString());
      string text = richTextBoxEditor.Text;
      if (string.IsNullOrWhiteSpace(columnName))
        return;
      labelTitle.Text = columnName.ToCamelBrokenString();
      if (checkBoxUseDataDBTable.Checked && comboBoxTableNames.SelectedIndex >= 0) { //get something from the database
        string tableName = comboBoxTableNames.SelectedItem.ToString();
        if (logic.MetaColumns.Any(x => x.EqualsIgnoreCase(columnName)))
          richTextBoxEditor.Text = MetaLogicTest.GetDescriptionFor(tableName, columnName);
      }
      if (!useDB && comboBoxTableNames.SelectedIndex >= 0) {
        string tableName = comboBoxTableNames.SelectedItem.ToString();
        getCheckResult(tableName, columnName);
      }
      if (string.IsNullOrWhiteSpace(richTextBoxEditor.Text))
        descriptionNotFound();
    }

    private void checkBoxUseDataDBTable_CheckedChanged(object sender, EventArgs e) {
      useDB = checkBoxUseDataDBTable.Checked;
      linkLabelRefreshDatabase.Enabled = useDB;
      comboBoxTableNames.Enabled = useDB;
      buttonSubmit.Enabled = useDB;
      buttonScriptDelete.Enabled = useDB;
      buttonScriptLoad.Enabled = useDB;
      buttonScriptSubmit.Enabled = useDB;
      logic.UseDataDB = useDB;
      updateGivenTableContext();

      if (!useDB && showNotUseDB)
        MessageBox.Show("When [Data DB Table] is not used, only basic (incomplete) checking will be available." + Environment.NewLine +
          "This mode is only meant for checking basic column syntax without table reference." + Environment.NewLine + 
          "If you do not mean to do that, please tick the [Use Data DB Table] check-box.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
      
      richTextBoxTableDescriptor.Clear();
      updateCurrentBoxes();
      if (useDB) {
        updateTableResult();
        updateDBResult();
      } else
        richTextBoxTableDescriptor.AppendTextWithColor(Color.DarkRed, "[Only available when using Data DB]");
    }

    private void comboBoxTableNames_SelectedIndexChanged(object sender, EventArgs e) {
      updateGivenTableContext();
      updateCurrentBoxes();
      updateTableResult();
    }

    private void updateTableResult() {
      richTextBoxTableDescriptor.Clear();
      string tableName = comboBoxTableNames.SelectedItem?.ToString();
      if (string.IsNullOrWhiteSpace(tableName)) {
        if (comboBoxTableNames.Items.Count > 0)
          comboBoxTableNames.SelectedIndex = 0;
        return;
      }
      MetaInfoTest info = logic.MetaInfoList.FirstOrDefault(x => x.TableName.EqualsIgnoreCase(tableName));
      if (info == null)
        return;
      startPrintCheckerResult(richTextBoxTableDescriptor, info.CheckerResult, printLine: true, isBasic: false);
    }

    private void updateDBResult() {
      LockWindowUpdate(this.Handle);
      richTextBoxDBReport.Clear();
      printSummary(richTextBoxDBReport);
      var tableNames = logic.ConfiguredTableNames;
      foreach(var tableName in tableNames) {
        MetaInfoTest info = logic.MetaInfoList.FirstOrDefault(x => x.TableName.EqualsIgnoreCase(tableName));
        if (info == null)
          continue;
        printErrorResult(info.TableName, richTextBoxDBReport, info.CheckerResult, 0);
      }
      LockWindowUpdate(IntPtr.Zero);
    }

    private void printSummary(RichTextBox rtb) {
      int tableNo = logic.MetaInfoList.Count;
      var tableWithErrors = logic.MetaInfoList.Where(x => x.CheckerResult.HasError()).ToList();
      rtb.AppendText(line);
      rtb.AppendText(Environment.NewLine);
      rtb.AppendStyledTextWithColor(FontStyle.Bold, Color.DarkGreen, "\u2713");
      rtb.AppendStyledTextWithColor(FontStyle.Bold, Color.DarkBlue, "REPORT SUMMARY");
      rtb.AppendText(Environment.NewLine);
      rtb.AppendText(line);
      rtb.AppendText(Environment.NewLine);
      rtb.AppendStyledText(FontStyle.Bold, "    Number of checked table(s): ");
      rtb.AppendStyledTextWithColor(FontStyle.Bold, Color.DarkGreen, tableNo.ToString());
      rtb.AppendText(Environment.NewLine);
      rtb.AppendStyledText(FontStyle.Bold, "    Number of table(s) with error(s): ");
      rtb.AppendStyledTextWithColor(FontStyle.Bold, tableWithErrors.Count > 0 ? Color.DarkRed : Color.DarkGreen, tableWithErrors.Count.ToString());
      rtb.AppendText(Environment.NewLine);
      if (tableWithErrors.Count > 0) {
        rtb.AppendStyledText(FontStyle.Bold, "    List of table(s) with error(s): ");
        rtb.AppendText(Environment.NewLine);
        foreach(var table in tableWithErrors) {
          rtb.AppendText("       \u27A1 ");
          rtb.AppendTextWithColor(Color.DarkRed, table.TableName + " [" + table.CheckerResult.NumberOfErrors().ToString() + "]");
          rtb.AppendText(Environment.NewLine);
        }
        int totalError = tableWithErrors.Sum(x => x.CheckerResult.NumberOfErrors());
        rtb.AppendStyledText(FontStyle.Bold, "    Total error(s): ");
        rtb.AppendTextWithColor(Color.DarkRed, totalError.ToString());
        rtb.AppendText(Environment.NewLine);
      }

      rtb.AppendStyledText(FontStyle.Bold, "    Status: ");
      rtb.AppendStyledTextWithColor(FontStyle.Bold, tableWithErrors.Count > 0 ? Color.DarkRed : Color.DarkGreen, 
        tableWithErrors.Count > 0 ? "NOT OK" : "OK");
      rtb.AppendText(Environment.NewLine);

      rtb.AppendText(line);
      rtb.AppendText(Environment.NewLine);
      rtb.AppendText(Environment.NewLine);
    }

    private void printErrorResult(string tableName, RichTextBox rtb, SyntaxCheckerResult result, int depth) {
      LockWindowUpdate(this.Handle);

      int errors = result.NumberOfErrors();
      bool hasError = errors > 0;

      if (depth == 0) {
        rtb.AppendText(line);
        rtb.AppendText(Environment.NewLine);

        rtb.AppendStyledTextWithColor(FontStyle.Bold, hasError ? Color.DarkRed : Color.DarkGreen, hasError ? "\u274C " : "\u2713");
        rtb.AppendStyledTextWithColor(FontStyle.Bold, Color.DarkBlue, tableName);
        rtb.AppendStyledTextWithColor(FontStyle.Bold, hasError ? Color.DarkRed : Color.DarkGreen,
          (hasError ? " Error(s): " + errors.ToString() : " OK") + Environment.NewLine);
        rtb.AppendText(line);
        rtb.AppendText(Environment.NewLine);
        if (!hasError)
          return; // do not continue further if this has no error
      } else if (hasError) {
        string space = new string(Enumerable.Repeat(' ', depth).ToArray());
        string spaces = space + space + space + space;
        rtb.AppendText(spaces);
        rtb.AppendStyledTextWithColor(FontStyle.Bold, Color.DarkRed, "\u274C ");
        rtb.AppendStyledText(FontStyle.Bold, result.DisplayText);
        if (!string.IsNullOrWhiteSpace(result.Description)) {
          rtb.AppendText(" Desc: ");
          rtb.AppendTextWithColor(Color.Brown, result.Description);
        }

        if (!string.IsNullOrWhiteSpace(result.Message)) {
          rtb.AppendText(" Message: ");
          rtb.AppendTextWithColor(result.Result ? Color.Teal : Color.Red,
            string.IsNullOrWhiteSpace(result.Message) ? string.Empty : result.Message);
        }
        rtb.AppendText(Environment.NewLine);
      } else //return immediately when there is no error
        return;

      if (result.SubResults.Count > 0)
        foreach (var subResult in result.SubResults)
          printErrorResult(tableName, rtb, subResult, depth + 1);

      LockWindowUpdate(IntPtr.Zero);
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr LockWindowUpdate(IntPtr Handle);
    private void startPrintCheckerResult(RichTextBox rtb, SyntaxCheckerResult result, bool printLine, bool isBasic) {
      LockWindowUpdate(this.Handle);
      rtb.Clear();
      printCheckerResult(rtb, result, 0, printLine, isBasic);
      LockWindowUpdate(IntPtr.Zero);
    }

    private void printCheckerResult(RichTextBox rtb, SyntaxCheckerResult result, int depth, bool printLine, bool isBasic) {
      string space = new string(Enumerable.Repeat(' ', depth).ToArray());
      string spaces = space + space + space + space;

      if (isBasic && depth == 0) {
        rtb.AppendText(spaces);
        rtb.AppendTextWithColor(Color.DarkRed, "[Data DB Unvailable, Basic Checker Applied]");
        rtb.AppendText(Environment.NewLine);
      }

      rtb.AppendText(spaces);
      rtb.AppendStyledTextWithColor(FontStyle.Bold,
        result.Result ? Color.DarkGreen : Color.DarkRed,
        result.Result ? "\u2713" : "\u274C ");
      rtb.AppendStyledText(FontStyle.Bold, result.DisplayText);

      if (!string.IsNullOrWhiteSpace(result.Description)) {
        rtb.AppendText(" Desc: ");
        rtb.AppendTextWithColor(Color.Brown, result.Description);
      }

      if (!string.IsNullOrWhiteSpace(result.Message)) {
        rtb.AppendText(" Message: ");
        rtb.AppendTextWithColor(result.Result ? Color.Teal : Color.Red,
          string.IsNullOrWhiteSpace(result.Message) ? string.Empty : result.Message);
      }
      rtb.AppendText(Environment.NewLine);
      if (depth == 0 && printLine) {
        rtb.AppendText(line);
        rtb.AppendText(Environment.NewLine);
      }
      if (result.SubResults.Count > 0)
        foreach (var subResult in result.SubResults) {
          printCheckerResult(rtb, subResult, depth + 1, printLine, isBasic);
          if (depth == 0 && printLine) {
            rtb.AppendText(line);
            rtb.AppendText(Environment.NewLine);
          }
        }
    }

    private void linkLabelRefreshDatabase_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
      refreshDB();
      MessageBox.Show("Database successfully refreshed", "Succeeded", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    bool recoloring = false;
    private void richTextBoxEditor_TextChanged(object sender, EventArgs e) {
      if (recoloring || listBoxColumns.SelectedIndex < 0 || comboBoxTableNames.SelectedIndex < 0) //nothing is validly selected
        return;
      timerRendering.Enabled = true;
      tickValue = 0;
    }

    private void buttonSubmit_Click(object sender, EventArgs e) {
      if (listBoxColumns.SelectedIndex < 0 || comboBoxTableNames.SelectedIndex < 0) //nothing is validly selected
        return;
      string text = richTextBoxEditor.Text;
      string tableName = comboBoxTableNames.SelectedItem.ToString();
      string columnName = logic.MetaItemColumnNames.FirstOrDefault(x => x == listBoxColumns.SelectedItem.ToString());
      BaseErrorModel errorCheckResult = logic.Check(tableName, columnName, text);
      if (errorCheckResult.Code != 0 && errorCheckResult.Code != -4) { //-4 means text not available, and that is ok...
        if (showFailedUpdate)
          MessageBox.Show(errorCheckResult.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
      }
      if (errorCheckResult.Code != -4 && showErrorUpdate) { //-4 error is not checked, because it can't be since there is no text, and it is always correct to put something as empty
        SyntaxCheckerResult checkerResult = (SyntaxCheckerResult)errorCheckResult.ReturnObject;
        if (checkerResult.HasError()) {
          DialogResult dialogResult = MessageBox.Show("The description you are about to submit contains some error(s)." +
            Environment.NewLine + "Do you still want to proceed?", "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
          if (dialogResult == DialogResult.Cancel)
            return;
        }
      } else if(string.IsNullOrWhiteSpace(text) && showEmptyUpdate) {
        DialogResult dialogResult = MessageBox.Show("The description you are about to submit is empty." +
          Environment.NewLine + "It should be empty only when you want to nullify a column value." +
          Environment.NewLine + "Do you still want to proceed?", "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
        if (dialogResult == DialogResult.Cancel)
          return;
      }
      BaseErrorModel updateResult = logic.UpdateMeta(tableName, columnName, text);
      string header = "Failed";
      MessageBoxIcon icon = MessageBoxIcon.Warning;
      switch (updateResult.Code) {
        case 0: header = "Succeeded"; icon = MessageBoxIcon.Information; break;
        case -3: header = "Exception Error"; icon = MessageBoxIcon.Error;
          updateResult.Message += Environment.NewLine + "Error: " + updateResult.Exception + Environment.NewLine + "Stack Trace: " + updateResult.StackTrace;
          break;
      };
      if (showSuccessfulUpdate)
        MessageBox.Show(updateResult.Message, header, MessageBoxButtons.OK, icon);
    }

    private void timerRendering_Tick(object sender, EventArgs e) {
      tickValue++;
      if (tickValue >= columnRenderingTick) {
        timerRendering.Enabled = false;
        if (listBoxColumns.SelectedIndex < 0 || comboBoxTableNames.SelectedIndex < 0) //nothing is validly selected
          return;
        string tableName = comboBoxTableNames.SelectedItem.ToString();
        string columnName = logic.MetaItemColumnNames.FirstOrDefault(x => x == listBoxColumns.SelectedItem.ToString());
        getCheckResult(tableName, columnName);
        //LockWindowUpdate(this.Handle);
        //richTextBoxEditor.ReadOnly = true;
        //recoloring = true;
        //richTextBoxEditor.ForeColor = RichTextBox.DefaultForeColor;
        //recoloring = false;
        //richTextBoxEditor.ReadOnly = false;
        //LockWindowUpdate(IntPtr.Zero);
        tickValue = 0;
      }
    }

    #region tool-strip menu item
    private void notUseDBWarningToolStripMenuItem_Click(object sender, EventArgs e) {
      showNotUseDB = notUseDBWarningToolStripMenuItem.Checked;
    }

    private void failedUpdateWarningToolStripMenuItem_Click(object sender, EventArgs e) {
      showFailedUpdate = failedUpdateWarningToolStripMenuItem.Checked;
    }

    private void errorUpdateWarningToolStripMenuItem_Click(object sender, EventArgs e) {
      showErrorUpdate = errorUpdateWarningToolStripMenuItem.Checked;
    }

    private void emptyUpdateWarningToolStripMenuItem_Click(object sender, EventArgs e) {
      showEmptyUpdate = emptyUpdateWarningToolStripMenuItem.Checked;      
    }

    private void successfulUpdateNotificationToolStripMenuItem_Click(object sender, EventArgs e) {
      showSuccessfulUpdate = successfulUpdateNotificationToolStripMenuItem.Checked;
    }
    #endregion tool-strip menu item

    #region script-checker part
    int lastKeyedPosition = -1;
    char lastKeyedChar = '\0';
    private const int oneRowHeight = 18;
    private const int xOffSet = -5;
    private string lastFullWord = null;
    private int lastParagraphPosition = -1;
    private List<SectionType> lastTypes = new List<SectionType>();
    private void richTextBoxScriptWriter_TextChanged(object sender, EventArgs e) {
      if (autoWriting)
        return;
      if (contextUpdatedInSelectionChanged)
        contextUpdatedInSelectionChanged = false;
      else //only need to update context here if not coming from the selection changed, so that no need to update context twice
        updateContext();
      updateListBoxSuggestionAndTypes();
      lastFullWord = typingContext.FullWord;
      lastParagraphPosition = typingContext.ParagraphIndex;
      lastTypes = typingContext.PossibleSectionTypes;
      scriptTickValue = 0;
      timerScriptRendering.Enabled = true;
    }

    private void updateContext() {
      int position = Math.Max(0, richTextBoxScriptWriter.SelectionStart);
      typingContext.UpdateContext(richTextBoxScriptWriter.Text, position);
    }

    private bool contextUpdatedInSelectionChanged = false;
    private void richTextBoxScriptWriter_SelectionChanged(object sender, EventArgs e) {
      if (autoWriting)
        return;
      lastKeyedPosition = richTextBoxScriptWriter.SelectionStart - 1;
      lastKeyedPosition = Math.Max(0, lastKeyedPosition);
      if (richTextBoxScriptWriter.Text.Length > 0)
        lastKeyedChar = richTextBoxScriptWriter.Text[lastKeyedPosition];
      scriptTickValue = 0;
      updateContext();
      contextUpdatedInSelectionChanged = true;
      updateLabelTypes();
      bool sameWordAndLocation = lastFullWord == typingContext.FullWord && lastParagraphPosition == typingContext.ParagraphIndex &&
        typingContext.PossibleSectionTypes.Except(lastTypes).Count() > 0;
      listBoxSuggestion.Visible = sameWordAndLocation;
      if (!sameWordAndLocation)
        lastFullWord = null;
    }

    private int maxListBoxSuggestionHeight = oneRowHeight; //updated at start
    private int initialWidth = 232; //updated at start
    private int maxListBoxSuggestionWidth = 1200;
    private const int listBoxSuggestionHeightOffset = -125;
    private const int listBoxSuggestionPositionYOffset = 23;
    private void updateListBoxSuggestionAndTypes() {
      if (richTextBoxScriptWriter.SelectionLength > 0) {
        listBoxSuggestion.Visible = false;
        labelTypes.Visible = false;
        return;
      }
      int length = richTextBoxScriptWriter.Text.Length;
      if (lastKeyedPosition < 0 || length <= 0) {
        listBoxSuggestion.Visible = false;
        return;
      }
      int textPosition = typingContext.Position - typingContext.TypedWord.Length;
      if (textPosition < 0)
        return;
      showSuggestionsAndTypes();
      showDebugListBox();
      Point rtb = richTextBoxScriptWriter.FindForm().PointToClient(
        richTextBoxScriptWriter.Parent.PointToScreen(richTextBoxScriptWriter.Location));
      Point position = richTextBoxScriptWriter.GetPositionFromCharIndex(textPosition >= length ? length - 1 : textPosition);
      int widestText = getListBoxTextWidestWidth(listBoxSuggestion);
      int x = Math.Max(0, rtb.X + position.X + xOffSet); //minimum position is 0
      listBoxSuggestion.Width = Math.Min(maxListBoxSuggestionWidth, Math.Max(widestText, initialWidth));
      x = Math.Min(x, richTextBoxScriptWriter.Width - Math.Max(listBoxSuggestion.Width, labelTypes.Width)); //maximum position so that it won't exceed the screen
      x = Math.Max(0, x); //ensure the second time that minimum position of 0 is achieved
      int y = Math.Max(oneRowHeight, position.Y + oneRowHeight);
      labelTypes.Location = new Point(x, y);
      int suggestedHeight = listBoxSuggestion.ItemHeight * (listBoxSuggestion.Items.Count + 1);
      listBoxSuggestion.Location = new Point(x, y + listBoxSuggestionPositionYOffset);
      listBoxSuggestion.Height = Math.Max(oneRowHeight, Math.Min(suggestedHeight, //at least having one row height
        Math.Min(rtb.Y + splitContainerScriptWriter.Height - y + listBoxSuggestionHeightOffset,
        maxListBoxSuggestionHeight))); //and at most is what is initially defined
      listBoxSuggestion.BringToFront();
      listBoxSuggestion.Visible = listBoxSuggestion.Items.Count > 0;
      labelTypes.BringToFront();
      labelTypes.Visible = typingContext.PossibleSectionTypes.Count > 0;
    }

    private int getListBoxTextWidestWidth(ListBox listBox) {
      int textWidth = -1;
      foreach (var item in listBox.Items) {
        Size size = TextRenderer.MeasureText(item.ToString(), listBox.Font);
        if (textWidth < size.Width)
          textWidth = size.Width;
      }
      return textWidth;
    }

    private void updateLabelTypes() {
      int length = richTextBoxScriptWriter.Text.Length;
      if (lastKeyedPosition < 0 || length <= 0)
        return;
      int textPosition = typingContext.Position - typingContext.TypedWord.Length;
      if (textPosition < 0)
        return;
      labelTypes.Text = //"Suggestion(s): " + 
        string.Join(", ", typingContext.PossibleSectionTypes
        .Select(t => t.ToString().ToCamelBrokenString()).ToArray());
      Point rtb = richTextBoxScriptWriter.FindForm().PointToClient(
        richTextBoxScriptWriter.Parent.PointToScreen(richTextBoxScriptWriter.Location));
      Point position = richTextBoxScriptWriter.GetPositionFromCharIndex(textPosition >= length ? length - 1 : textPosition);
      int x = Math.Max(0, rtb.X + position.X + xOffSet); //minimum position is 0
      x = Math.Min(x, richTextBoxScriptWriter.Width - Math.Max(listBoxSuggestion.Width, labelTypes.Width)); //maximum position so that it won't exceed the screen
      int y = Math.Max(oneRowHeight, position.Y + oneRowHeight);
      labelTypes.BringToFront();
      labelTypes.Visible = typingContext.PossibleSectionTypes.Count > 0;
      labelTypes.Location = new Point(x, y);
    }

    private string getIndexValuePairs<T>(List<KeyValuePair<int, T>> encs) {
      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < encs.Count; ++i) {
        if (i > 0)
          sb.Append(", ");
        KeyValuePair<int, T> enc = encs[i];
        sb.Append("[" + enc.Key + ", " + enc.Value.ToString() + "]");
      }
      return sb.ToString();
    }

    private void showSuggestionsAndTypes() {
      listBoxSuggestion.Items.Clear();
      var suggestions = typingContext.GetSuggestions();
      int mostMatched = typingContext.GetMostSuggestedIndex(suggestions);
      listBoxSuggestion.Items.AddRange(suggestions.ToArray());
      if (mostMatched >= 0)
        listBoxSuggestion.SelectedIndex = mostMatched;
      labelTypes.Text = //"Suggestion(s): " + 
        string.Join(", ", typingContext.PossibleSectionTypes
        .Select(x => x.ToString().ToCamelBrokenString()).ToArray());
    }

    private void showDebugListBox() {
      listBoxDebug.Items.Clear();
      if (!string.IsNullOrWhiteSpace(typingContext.TypedWord))
        listBoxDebug.Items.Add("[Word] " + typingContext.TypedWord + " [of " + typingContext.FullWord + "]");
      if (typingContext.TypedChar != null)
        listBoxDebug.Items.Add("[Char] " + typingContext.TypedChar.Value);
      if (!string.IsNullOrWhiteSpace(typingContext.ComponentText))
        listBoxDebug.Items.Add("[Comp-" + typingContext.ComponentIndex + "-" + typingContext.NextComponentIndex + "-" +
          typingContext.ComponentLength + "] " + typingContext.ComponentText);
      if (typingContext.ComponentRegexes.Count > 0)
        listBoxDebug.Items.Add("[Regexes] " + getIndexValuePairs(typingContext.ComponentRegexes));
      listBoxDebug.Items.Add("[Is Regex] " + typingContext.IsRegex);
      if (typingContext.ComponentScripts.Count > 0)
        listBoxDebug.Items.Add("[Scripts] " + getIndexValuePairs(typingContext.ComponentScripts));
      listBoxDebug.Items.Add("[Is Script] " + typingContext.IsScript);
      listBoxDebug.Items.Add("[Begin] " + typingContext.IsBeginString + "-" + 
        typingContext.IsBeginParagraph + "-" + typingContext.IsBeginParagraphContent);
      listBoxDebug.Items.Add("[Position] " + typingContext.Position);
      listBoxDebug.Items.Add("[Match] " + typingContext.DirectTypedWordMatch.ToString());
      listBoxDebug.Items.Add("[Types] " + string.Join(", ", typingContext.PossibleSectionTypes.
        Select(x => x.ToString()).ToArray()));
      if (!string.IsNullOrWhiteSpace(typingContext.DirectTableContext))
        listBoxDebug.Items.Add("[DTContext] " + typingContext.DirectTableContext);
      if (!string.IsNullOrWhiteSpace(typingContext.ParagraphFirstWord))
        listBoxDebug.Items.Add("[First] " + typingContext.ParagraphFirstWord);
      if (typingContext.ContextBreaks.Count > 0)
        listBoxDebug.Items.Add("[Breaks] " + getIndexValuePairs(typingContext.ContextBreaks));
      if (typingContext.BreakChar != null)
        listBoxDebug.Items.Add("[Break Char-" + typingContext.BreakPosition + "] " + typingContext.BreakChar.Value);
      if (typingContext.RefWords.Count > 0)
        listBoxDebug.Items.Add("[Ref] " + string.Join(":", typingContext.RefWords));
      if (!string.IsNullOrWhiteSpace(typingContext.TableName))
        listBoxDebug.Items.Add("[Table] " + (string.IsNullOrWhiteSpace(typingContext.WrittenTableName) ? "[G]" : "[W]") + 
          " " + typingContext.TableName);
      listBoxDebug.Items.Add("[Is Enclosed] " + typingContext.IsEnclosed.ToString());
      if (typingContext.ParagraphEnclosures.Count > 0)
        listBoxDebug.Items.Add("[Par Enclosures] " + getIndexValuePairs(typingContext.ParagraphEnclosures));
      if (typingContext.Enclosures.Count > 0)
        listBoxDebug.Items.Add("[Enclosures] " + getIndexValuePairs(typingContext.Enclosures));
      if (typingContext.ParagraphParentheses.Count > 0)
        listBoxDebug.Items.Add("[Par Parentheses] " + getIndexValuePairs(typingContext.ParagraphParentheses));
      if (typingContext.Parentheses.Count > 0)
        listBoxDebug.Items.Add("[Parentheses] " + getIndexValuePairs(typingContext.Parentheses));
      listBoxDebug.Items.Add("[Is Parenthesed] " + typingContext.IsParenthesed.ToString());
      if (!string.IsNullOrWhiteSpace(typingContext.ParenthesesContext))
        listBoxDebug.Items.Add("[Parentheses Context] " + (typingContext.IsUserParenthesesContext ? "[U]" : "[D]") +
          " " + typingContext.ParenthesesContext);
      if (!string.IsNullOrWhiteSpace(typingContext.ParagraphContext))
        listBoxDebug.Items.Add("[Par Context] " + typingContext.ParagraphContext);
      if (typingContext.ParagraphComponentIndices.Count > 0)
        listBoxDebug.Items.Add("[Par Components] " + string.Join(", ", typingContext.ParagraphComponentIndices));
      if (typingContext.ParIndices.Count > 0)
        listBoxDebug.Items.Add("[Par Indices] " + string.Join(", ", typingContext.ParIndices));
      if (!string.IsNullOrWhiteSpace(typingContext.Paragraph))
        listBoxDebug.Items.Add("[Par-" + typingContext.ParagraphIndex + "-" + typingContext.NextParagraphIndex + "-" +
          typingContext.Paragraph.Length + "] " + typingContext.Paragraph);
    }

    //to capture key press before text change and before key down (may not be necessary)
    private void richTextBoxScriptWriter_KeyPress(object sender, KeyPressEventArgs e) {
      lastKeyedPosition = richTextBoxScriptWriter.SelectionStart;
      lastKeyedChar = e.KeyChar;
      scriptTickValue = 0;
    }

    private void changeSelectedWord(string text) {
      autoWriting = true;
      int typedWordLength = typingContext.TypedWord == AibeDocument.StartString ? 0 : typingContext.TypedWord.Length;
      int fullWordLength = typingContext.FullWord.Length - (typingContext.TypedWord == AibeDocument.StartString ? 
        AibeDocument.StartString.Length : 0);
      int selectStart = richTextBoxScriptWriter.SelectionStart;
      richTextBoxScriptWriter.SelectionStart = selectStart - typedWordLength;
      richTextBoxScriptWriter.SelectionLength = fullWordLength;
      richTextBoxScriptWriter.SelectedText = text;
      timerScriptRendering.Enabled = true;
      autoWriting = false;
    }

    private void applySuggestion() {
      string text = listBoxSuggestion.SelectedItem?.ToString();
      if (string.IsNullOrWhiteSpace(text))
        return;
      changeSelectedWord(text);
      richTextBoxScriptWriter.Focus();
      labelTypes.Visible = false;
      listBoxSuggestion.Visible = false;
    }

    private void AibeEditorForm_KeyDown(object sender, KeyEventArgs e) {
      scriptTickValue = 0;
      if (!listBoxSuggestion.Visible)
        return;
      switch (e.KeyData) {
        case Keys.Down:
          if (richTextBoxScriptWriter.Focused) {
            if (listBoxSuggestion.Items.Count > 0 && listBoxSuggestion.SelectedIndex < 0) {
              listBoxSuggestion.Focus();
              listBoxSuggestion.SelectedIndex = 0;
              e.SuppressKeyPress = true;
            } else if (listBoxSuggestion.SelectedIndex != listBoxSuggestion.Items.Count - 1) {
              listBoxSuggestion.Focus();
              listBoxSuggestion.SelectedIndex += 1;
              e.SuppressKeyPress = true;
            } //don't suppress the key if not necessary
          }
          break;
        case Keys.Enter:
          if (listBoxSuggestion.SelectedIndex >= 0) {
            e.SuppressKeyPress = true;
            applySuggestion();
          }
          break;
        case Keys.Up:
          if (listBoxSuggestion.Focused && listBoxSuggestion.SelectedIndex == 0) {
            e.SuppressKeyPress = true;
            richTextBoxScriptWriter.Focus(); //When losing focus, take the latest position
          }
          break;
      }
    }

    //So that tab would be intercepted.
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
      if (!listBoxSuggestion.Visible)
        base.ProcessCmdKey(ref msg, keyData);
      if (keyData == Keys.Tab) {
        if (listBoxSuggestion.SelectedIndex >= 0) {
          applySuggestion();
          return true;
        }
      }
      return base.ProcessCmdKey(ref msg, keyData);
    }

    private void listBoxSuggestion_MouseDoubleClick(object sender, MouseEventArgs e) {
      if (listBoxSuggestion.SelectedIndex < 0)
        return;
      applySuggestion();
    }

    List<Keys> pressedButtonDatas = new List<Keys> { Keys.Left, Keys.Right, Keys.Delete, Keys.Back };
    private void listBoxSuggestion_KeyDown(object sender, KeyEventArgs e) {
      if (!pressedButtonDatas.Contains(e.KeyData))
        return;
      if (e.KeyData == Keys.Left && richTextBoxScriptWriter.SelectionStart > 0)
        richTextBoxScriptWriter.SelectionStart -= 1;
      if (e.KeyData == Keys.Right && richTextBoxScriptWriter.SelectionStart < richTextBoxScriptWriter.Text.Length - 1)
        richTextBoxScriptWriter.SelectionStart += 1;
      if (e.KeyData == Keys.Back && richTextBoxScriptWriter.SelectionStart > 0) {
        richTextBoxScriptWriter.SelectionStart -= 1;
        richTextBoxScriptWriter.SelectionLength = 1;
        richTextBoxScriptWriter.SelectedText = string.Empty;
      }
      if (e.KeyData == Keys.Delete && richTextBoxScriptWriter.SelectionStart < richTextBoxScriptWriter.Text.Length - 1) {
        richTextBoxScriptWriter.SelectionLength = 1;
        richTextBoxScriptWriter.SelectedText = string.Empty;
      }
      richTextBoxScriptWriter.Focus();
    }

    //To capture Ctrl + F key
    private void richTextBoxScriptWriter_KeyDown(object sender, KeyEventArgs e) {
      if (!richTextBoxScriptWriter.Focused)
        return;
      if (e.Control) {
        if (e.Shift) {
          switch (e.KeyCode) {
            case Keys.D1: templateScript(isNew: true, isTypical: false); break;
            case Keys.D2: templateScript(isNew: false, isTypical: false); break;
            case Keys.D3: templateScript(isNew: true, isTypical: true); break;
            case Keys.D4: templateScript(isNew: false, isTypical: true); break;
            case Keys.S: buttonScriptSubmit_Click(null, null); break;
            case Keys.D: buttonScriptDelete_Click(null, null); break;
            case Keys.Z: buttonScriptLoad_Click(null, null); break;
            case Keys.C: buttonScriptCompile_Click(null, null); break;
            case Keys.P: richTextBoxScriptWriter.SelectedText = AibeDocument.StartString; break;
            case Keys.T: richTextBoxScriptWriter.SelectedText = AibeDocument.TableHeader; break;
          }
        } else {
          switch (e.KeyCode) {
            case Keys.F:
              scriptTickValue = 0;
              minimizeScript();
              timerScriptRendering.Enabled = true;
              break;
            case Keys.Space:
              updateContext();
              updateListBoxSuggestionAndTypes();
              if (listBoxSuggestion.Items.Count == 1) //if it is exactly one suggestion, apply the suggestion immediately
                applySuggestion();
              e.SuppressKeyPress = true;
              break;
          }
        }
      }
    }

    private void updateGivenTableContext() {
      typingContext.GivenTableName = useDB && comboBoxTableNames.SelectedIndex >= 0 ? comboBoxTableNames.SelectedItem.ToString() : string.Empty;
    }

    private void timerScriptRendering_Tick(object sender, EventArgs e) {
      scriptTickValue++;
      if (scriptTickValue >= scriptRenderingTick) {
        timerScriptRendering.Enabled = false;
        updateScriptCheckerResult();
        scriptTickValue = 0;
      }
    }

    private void initScriptWithDefaultColor(string text) {
      richTextBoxScriptWriter.Text = text;
      richTextBoxScriptWriter.SelectionStart = 0;
      richTextBoxScriptWriter.SelectionLength = text.Length;
      richTextBoxScriptWriter.SelectionColor = RichTextBox.DefaultForeColor;
      richTextBoxScriptWriter.SelectionStart = Math.Max(0, text.Length - 1);
      richTextBoxScriptWriter.SelectionLength = 0;
    }

    //learn to update without blinking and scrolling in
    //https://stackoverflow.com/questions/626988/prevent-autoscrolling-in-richtextbox
    private void coloringScript() {
      if (doc == null)
        return;
      List<AibeColoringSet> colorSets = logic.GetColoringSets(doc, doc.GivenTableName);
      int originalSelectionStart = richTextBoxScriptWriter.SelectionStart;
      int originalSelectionLength = richTextBoxScriptWriter.SelectionLength;
      Color originalColor = RichTextBox.DefaultForeColor;
      LockWindowUpdate(this.Handle);
      bool suggestionShown = listBoxSuggestion.Visible;
      bool typesShown = labelTypes.Visible;
      bool rtbFocused = richTextBoxScriptWriter.Focused;
      bool lbFocused = listBoxSuggestion.Focused;
      labelTableName.Focus(); //just get this focus somewhere else so that it won't be scrolling
      foreach (var colorSet in colorSets) {
        richTextBoxScriptWriter.SelectionStart = colorSet.StartPosition;
        richTextBoxScriptWriter.SelectionLength = colorSet.Length;
        richTextBoxScriptWriter.SelectionColor = colorSet.UsedColor;
        //colorSet.Text = richTextBoxScriptWriter.SelectedText; //just for debugging
      }
      richTextBoxScriptWriter.SelectionStart = originalSelectionStart;
      richTextBoxScriptWriter.SelectionLength = originalSelectionLength;
      richTextBoxScriptWriter.SelectionColor = originalColor;
      if (suggestionShown)
        listBoxSuggestion.Visible = true;
      if (typesShown)
        labelTypes.Visible = true;
      LockWindowUpdate(IntPtr.Zero);
      if (rtbFocused) //must be put outside of the windows lock to really make it work
        richTextBoxScriptWriter.Focus();
      if (lbFocused) //must be put outside of the windows lock to really make it work
        listBoxSuggestion.Focus();
    }

    private void minimizeScript() {
      if (doc == null)
        return;
      doc.Minimize();
      LockWindowUpdate(this.Handle);
      bool focused = richTextBoxScriptWriter.Focused;
      labelTableName.Focus();
      autoWriting = true;
      initScriptWithDefaultColor(doc.UntrimmedOriginalDesc);
      coloringScript();
      autoWriting = false;
      LockWindowUpdate(IntPtr.Zero);
    }

    private void templateScript(bool isNew, bool isTypical) {
      string text = AibeDocument.CreateTemplateText(isNew, isTypical);
      if (comboBoxTableNames.SelectedIndex < 0) //nothing is validly selected
        return;
      string tableName = comboBoxTableNames.SelectedItem.ToString();
      if (string.IsNullOrWhiteSpace(tableName))
        return;
      doc = new AibeDocument(text, tableName);
      LockWindowUpdate(this.Handle);
      bool focused = richTextBoxScriptWriter.Focused;
      labelTableName.Focus();
      autoWriting = true;
      initScriptWithDefaultColor(text);
      coloringScript();
      richTextBoxScriptWriter.SelectionStart = 0;
      autoWriting = false;
      LockWindowUpdate(IntPtr.Zero);
    }

    private void updateScriptCheckerResult() {
      autoWriting = true;
      string tableName = useDB && comboBoxTableNames.SelectedIndex >= 0 ? comboBoxTableNames.SelectedItem.ToString() : null;
      doc = new AibeDocument(richTextBoxScriptWriter.Text, tableName);
      coloringScript();
      autoWriting = false;
    }

    private int compile() {
      BaseMetaItem item = doc.CreateBaseMeta();
      MetaInfoTest test = new MetaInfoTest(item);
      int num = test.CheckerResult.NumberOfErrors();
      labelScriptResult.Text = num > 0 ? num + " Error(s)" : "Succeeded";
      labelScriptResult.ForeColor = num > 0 ? Color.Red : Color.DarkGreen;
      startPrintCheckerResult(richTextBoxScriptChecker, test.CheckerResult, printLine: true, isBasic: !logic.UseDataDB);
      return num;
    }

    private void buttonScriptSubmit_Click(object sender, EventArgs e) {
      if (doc == null) {
        MessageBox.Show("Document not available", "Not Available", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }
      if (string.IsNullOrWhiteSpace(doc.TableName)) {
        MessageBox.Show("Table not available", "Not Available", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }
      string tableName = doc.TableName;
      bool exist = false;
      if (!string.IsNullOrWhiteSpace(tableName)) //if the table is written
        exist = logic.MetaInfoList.Any(x => x.TableName.Equals(tableName)); //check if the table exists

      int num = compile();
      if (num > 0) {
        DialogResult dialogResult = MessageBox.Show("The compilation shows some error(s)." + 
          Environment.NewLine + "Do you want to continue submitting your script?",
          "Compilation Error Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (dialogResult == DialogResult.No)
          return;
      } else {
        DialogResult dialogResult = MessageBox.Show("The compilation shows no error." +
          Environment.NewLine + "Do you want to submit your script?",
          "Submission Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (dialogResult == DialogResult.No)
          return;
      }

      BaseScriptModel script = doc.ToSQLScript(!exist);
      if (script == null) {
        MessageBox.Show("Failed to generate SQL script", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
      }
      List<int> results = SQLServerHandler.ExecuteBaseScripts(Aibe.DH.DataDBConnectionString, new List<BaseScriptModel> { script });
      bool result = results.All(x => x > 0);
      if (result) {
        if (exist) //old item 
          minorDBUpdates();
        else //new item
          refreshDB();
        MessageBox.Show("Successfully " + (exist ? "updated" : "created") +
          " the table [" + doc.TableName + "] description from [" + Aibe.DH.MetaTableName + "]", 
          "Succeeded", MessageBoxButtons.OK, MessageBoxIcon.Information);
      } else {
        refreshDB(); //refresh the DB anyway!
        MessageBox.Show("Failed to " + (exist ? "update" : "create") +
          " the table [" + doc.TableName + "] description from [" + Aibe.DH.MetaTableName + "]",
          "Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private void buttonScriptDelete_Click(object sender, EventArgs e) {
      if (comboBoxTableNames.SelectedIndex < 0) //nothing is validly selected
        return;
      string tableName = comboBoxTableNames.SelectedItem.ToString();
      if (string.IsNullOrWhiteSpace(tableName)) {
        MessageBox.Show("Table name not found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
      }
      DialogResult result = MessageBox.Show("Do you want to delete the table [" + tableName + 
        "] description from [" + Aibe.DH.MetaTableName + "]", 
        "Delete Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
      if (result == DialogResult.No)
        return;
      int val = SQLServerHandler.DeleteFromTableWhere(Aibe.DH.DataDBConnectionString, Aibe.DH.MetaTableName, 
        string.Concat(Aibe.DH.MetaTableNameColumnName, "=", tableName.AsSqlStringValue()));
      if (val > 0) {
        refreshDB();
        MessageBox.Show("Successfully deleted" +
          " the table [" + tableName + "] description from [" + Aibe.DH.MetaTableName + "]",
          "Succeeded", MessageBoxButtons.OK, MessageBoxIcon.Information);
      } else {
        MessageBox.Show("No such record found", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
      }
    }

    private void buttonScriptLoad_Click(object sender, EventArgs e) {
      if (comboBoxTableNames.SelectedIndex < 0) //nothing is validly selected
        return;
      string tableName = comboBoxTableNames.SelectedItem.ToString();
      if (string.IsNullOrWhiteSpace(tableName)) {
        MessageBox.Show("Table name not found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
      }
      DataTable table = SQLServerHandler.GetFullDataTableWhere(Aibe.DH.DataDBConnectionString, Aibe.DH.MetaTableName, 
        string.Concat(Aibe.DH.MetaTableNameColumnName, "=", tableName.AsSqlStringValue()));
      if (table == null) {
        MessageBox.Show("Data table cannot be loaded", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
      }
      if (!string.IsNullOrWhiteSpace(richTextBoxScriptWriter.Text)) {
        DialogResult result = MessageBox.Show("The script box contains some scripts which will be replaced by the loaded scripts for table [" +
          tableName + "]." + Environment.NewLine + "Do you want to continue loading the script?",
          "Load Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result == DialogResult.No)
          return;
      }
      BaseMetaItem item = BaseMetaItem.ExtractMetaFromDataTable(table);
      doc = new AibeDocument(null, tableName);
      doc.Init(item);
      richTextBoxScriptWriter.Clear();
      richTextBoxScriptWriter.SelectionStart = 0;
      richTextBoxScriptWriter.ForeColor = RichTextBox.DefaultForeColor;
      richTextBoxScriptWriter.Text = doc.UntrimmedOriginalDesc;
    }

    private void buttonScriptCompile_Click(object sender, EventArgs e) {
      if (doc == null)
        return;
      int num = compile();
      MessageBox.Show("Compilation Completed.\n" + (num > 0 ? "Number of Error(s) Found: " + num : "No Error Found!"), 
        num > 0 ? "Failed" : "Succeeded", MessageBoxButtons.OK, num > 0 ? MessageBoxIcon.Error : MessageBoxIcon.Information);
    }
    #endregion script-checker part

    private void buttonUpdateReport_Click(object sender, EventArgs e) {
      updateDBResult();
      MessageBox.Show("Database Report Has Been Successfully Updated!", "Succeeded", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
  }
}

//private int getCurrentText() {
//  int length = richTextBoxScriptWriter.Text.Length;
//  int lastBeforeBreak = lastKeyedPosition;
//  //bool firstCharHasBeenChecked = false;
//  while (lastBeforeBreak > 0) {
//    if (lastBeforeBreak >= length) {
//      --lastBeforeBreak;
//      continue;
//    }
//    char ch = richTextBoxScriptWriter.Text[lastBeforeBreak];
//    //if (!char.IsLetterOrDigit(ch)) {
//    if (char.IsWhiteSpace(ch) || AibeTypingContext.DefaultBreakChars.Contains(ch)) { //changed to white space check to break, otherwise don't
//      //if (!firstCharHasBeenChecked) {
//      //  listBoxSuggestion.Visible = false;
//      //  return -1;
//      //}
//      ++lastBeforeBreak;
//      break;
//    }
//    //firstCharHasBeenChecked = true;
//    --lastBeforeBreak;
//  }
//  return lastBeforeBreak;
//}

//if (string.IsNullOrWhiteSpace(text))
//  richTextBoxChecker.AppendTextWithColor(Color.DarkRed, "[Description Not Found]");

//private void descriptionNotFound() {
//  richTextBoxEditor.Clear();
//  richTextBoxChecker.Clear();
//  richTextBoxChecker.AppendTextWithColor(Color.DarkRed, "[Description Not Found]");
//}

//listBoxColumns.Items.AddRange(logic.Options.Keys.ToArray());
//List<string> parts = logic.Options[listBoxColumns.SelectedItem.ToString()];
//List<string> texts = new List<string>();
//for (int i = flowLayoutPanelContent.Controls.Count - 1; i >= 0; --i) {
//  if (flowLayoutPanelContent.Controls[i] is CheckerPanel) {
//    CheckerPanel checkerPanel = (CheckerPanel)flowLayoutPanelContent.Controls[i];
//    texts.Add(checkerPanel.GetSyntaxText());
//  }
//  flowLayoutPanelContent.Controls.RemoveAt(i);
//}
//texts.Reverse();

//int index = 0;
//foreach (var part in parts) {
//  CheckerPanel panel = new CheckerPanel();
//  panel.SetTitle(part);
//  panel.Width = splitContainerContent.Panel2.Width - ContentLeftMargin;
//  if (checkBoxUseDataDBTable.Checked && comboBoxTableNames.SelectedIndex >= 0) { //get something from the database
//    string tableName = comboBoxTableNames.SelectedItem.ToString();
//    if (logic.MetaColumns.Any(x => x.EqualsIgnoreCase(part)))
//      panel.SetSyntaxText(MetaLogicTest.GetDescriptionFor(tableName, part));
//  } else if (texts.Count > index && !string.IsNullOrWhiteSpace(texts[index])) //get the last inputed text
//    panel.SetSyntaxText(texts[index]);
//  flowLayoutPanelContent.Controls.Add(panel);
//  ++index;
//}

//BaseMetaItem item = new BaseMetaItem();
//BaseMetaItemTest tester = new BaseMetaItemTest(item);
//MethodInfo method = tester.GetType().GetMethod("Test" + columnName);
//if (null == method) {
//  SyntaxCheckerResult result = new SyntaxCheckerResult {
//    Result = false,
//    Message = "Checking Method Not Available",
//  };
//  printCheckerResult(richTextBoxChecker, result, 0, false, true);
//  return;
//}
//object objResult = method.Invoke(tester, new object[] { text });
//(SyntaxCheckerResult)objResult;
