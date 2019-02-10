using Aibe.Models.Tests;
using Extension.Models;
using Extension.String;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using logic = Aibe.Models.Tests.AibeSyntaxCheckerLogic;

namespace Aibe.Models.Docs {
  public class AibeParagraph : BaseInfo {
    public int StartIndex { get; private set; } //given outside
    public int ContentOffset { get; private set; }
    public int AbsContentOffset { get { return StartIndex + ContentOffset; } }
    public int Length { get; private set; }
    public string ColumnName { get; private set; }
    public string Content { get; private set; } //large portion to indicate its description
    public AibeDocument Parent { get; private set; }
    public List<AibeSection> Sections { get; private set; } = new List<AibeSection>();
    public string TableName { get; private set; } //If there is a table name, then it is a new table paragraph
    public bool IsNewTableParagraph { get { return !string.IsNullOrWhiteSpace(TableName); } }
    public object DbValue {
      get {
        if (string.IsNullOrWhiteSpace(ColumnName) || string.IsNullOrWhiteSpace(Content))
          return DBNull.Value;
        string contentTrimmed = Content.Trim();
        if (ColumnName.EqualsIgnoreCase(Aibe.DH.MICNItemsPerPage)) { //These two are the exception
          short num;
          bool result = short.TryParse(contentTrimmed, out num);
          if (result && num > 0)
            return num;
        } else if (ColumnName.EqualsIgnoreCase(Aibe.DH.MICNDisableFilter)) {
          if (contentTrimmed.EqualsIgnoreCase(Aibe.DH.True))
            return 1;
          else if (contentTrimmed.EqualsIgnoreCase(Aibe.DH.False))
            return 0;
        } else
          return contentTrimmed;
        return DBNull.Value;
      }
    }

    public object Value {
      get {
        object val = DbValue;
        if (val is DBNull)
          return null;
        if (ColumnName.EqualsIgnoreCase(Aibe.DH.MICNDisableFilter))
          return (int)val == 1; //if 1, returns true, else returns false, null would have been taken cared of above
        return val;
      }
    }

    public AibeParagraph(string desc, int startIndex, AibeDocument parent) : base(desc) {
      if (desc == null || startIndex < 0 || parent == null)
        return;
      StartIndex = startIndex;
      Parent = parent;
      Length = desc.Length;

      string usedDesc = desc.Substring(AibeDocument.StartString.Length);
      AibeSection startStringSection = new AibeSection(AibeDocument.StartString, startIndex, 0, this, SectionType.NewParagraph);
      Sections.Add(startStringSection);
      int currentIndex = AibeDocument.StartString.Length;

      if (usedDesc.TrimStart().StartsWith(AibeDocument.TableHeader)) { //table paragraph
        int wsIndex = 0;
        List<char> wsChars = new List<char>();
        while (wsIndex < usedDesc.Length) {
          char wsTestChar = usedDesc[wsIndex];
          if (char.IsWhiteSpace(wsTestChar)) {
            wsChars.Add(wsTestChar);
            wsIndex++;
          } else
            break;
        }
        if (usedDesc.Length <= AibeDocument.TableHeader.Length + wsIndex) //cannot proceed further
          return;
        string testTableName = usedDesc.Substring(AibeDocument.TableHeader.Length + wsIndex);
        string earlyWs = new string(wsChars.ToArray());

        AibeParagraphState state = new AibeParagraphState { CurrentOffset = currentIndex };
        processUntrimmedElement(earlyWs + AibeDocument.TableHeader, SectionType.NewTableNamePrefix, state);
        if (string.IsNullOrEmpty(testTableName))
          return;
        TableName = testTableName.Trim(); //take the test table name, trim it
        processUntrimmedElement(testTableName, SectionType.NewTableName, state);
        Sections.AddRange(state.Sections);
        return;
      }

      //Means, typical paragraph, with meta item column name
      AibeStreamProcessResult processResult = processUntrimmedBasicStream(usedDesc, SectionType.MetaItemColumnName, currentIndex);
      if (processResult.MainSection == null || !processResult.Sections.Any()) //invalid
        return;

      Sections.AddRange(processResult.Sections);
      ColumnName = processResult.MainSection.UntrimmedOriginalDesc; //untrimmed is used here... also to check against error. because the enclosing white spaces should have been removed if everything goes right...
      Content = desc.Substring(processResult.FinalIndex); //here still uses desc
      ContentOffset = processResult.FinalIndex;

      List<AibeSection> sections = GetSections(ColumnName, Content, ContentOffset);
      if (sections != null && sections.Count > 0) {
        Sections.AddRange(sections);
        AibeSection lastSection = sections[sections.Count - 1];
        int finalIndex = lastSection.StartIndex + lastSection.Length;
        if (finalIndex < StartIndex + Length) {
          string appendixString = desc.Substring(finalIndex - startIndex);
          AibeSection appendix = new AibeSection(appendixString, finalIndex, lastSection.ParagraphOffset + lastSection.Length, this,
            string.IsNullOrWhiteSpace(appendixString) ? SectionType.Empty : SectionType.Appendix);
          Sections.Add(appendix);
        }
      }

      IsValid = true; //can only be considered true if it can at least come to this part.
    }

    public void Minimize(int startIndex) {
      int index = startIndex;
      int offset = 0;
      var minSections = Sections.Where(x => x.SectType != SectionType.Empty).ToList();
      Sections.Clear();
      Content = string.Empty;
      StringBuilder sb = new StringBuilder();
      StringBuilder initSb = new StringBuilder();
      for(int i = 0; i < minSections.Count; ++i) {
        if (i == 2) { //after new table name or meta item column name, the only one to give space
          AibeSection spaceSection = new AibeSection(" ", index + offset, offset, this, SectionType.Empty);
          offset += spaceSection.Length;
          Sections.Add(spaceSection);
          ContentOffset = offset;
          initSb.Append(" ");
        }
        AibeSection section = minSections[i];
        AibeSection newSection = new AibeSection(section.OriginalDesc, index + offset, offset, this, section.SectType);
        offset += newSection.Length;
        Sections.Add(section);
        if (i >= 2)
          sb.Append(newSection.UntrimmedOriginalDesc);
        else
          initSb.Append(newSection.UntrimmedOriginalDesc);
      }
      AibeSection finalSection = new AibeSection("\n", index + offset, offset, this, SectionType.Empty); //to give the final enter
      offset += finalSection.Length;
      Sections.Add(finalSection);
      sb.Append("\n");

      UntrimmedOriginalDesc = initSb.ToString() + sb.ToString();
      OriginalDesc = UntrimmedOriginalDesc.Trim();
      Content = sb.ToString();
      StartIndex = startIndex;     
      Length = Sections.Sum(x => x.Length);
    }

    private bool processBasicStream(string desc, bool checkWs, int relativeIndex, ref bool isFinal, out List<char> accChars) {
      char ch = '\0';
      accChars = new List<char>();
      if (isFinal) //nothing else
        return false;

      isFinal = false;
      int initialRelativeIndex = relativeIndex;
      do {
        if (relativeIndex > initialRelativeIndex)
          accChars.Add(ch); //add previous character
        if (relativeIndex >= desc.Length) {
          isFinal = true;
          break;
        }
        ch = desc[relativeIndex];
        relativeIndex++;
      } while ((checkWs && char.IsWhiteSpace(ch)) || (!checkWs && !char.IsWhiteSpace(ch)));

      if (accChars.Count <= 0) //invalid
        return false;

      return true;
    }

    private AibeStreamProcessResult processSpacedUntrimmedBasicStream(string desc, SectionType sectType, int currentIndex, bool allowKeyword = false) {
      AibeStreamProcessResult processResult = new AibeStreamProcessResult { FinalIndex = currentIndex };
      List<string> parts = desc.GetTrimmedParts();
      if (parts.Count <= 0)
        return processResult;
      bool hasEarlyWs = string.IsNullOrWhiteSpace(parts[0]) && parts[0].Length > 0;
      string content = parts.Count > 1 && hasEarlyWs ? parts[1] : !hasEarlyWs && !string.IsNullOrWhiteSpace(parts[0]) ? parts[0] : string.Empty;
      string laterWs = parts.Count > 2 ? parts[2] : parts.Count > 1 && string.IsNullOrWhiteSpace(parts[1]) ? parts[1] : string.Empty;
      if (hasEarlyWs) {
        AibeSection sect = new AibeSection(parts[0], StartIndex + currentIndex, currentIndex, this, SectionType.Empty);
        processResult.Sections.Add(sect);
        currentIndex += parts[0].Length;
        processResult.FinalIndex = currentIndex;
      }
      if (!string.IsNullOrWhiteSpace(content)) {
        bool isKeyword = logic.Keywords.Any(x => x.EqualsIgnoreCase(content)); //Aibe.DH.Skip.EqualsIgnoreCase(subDesc);
        AibeSection mainSection = new AibeSection(content, StartIndex + currentIndex, currentIndex, this, allowKeyword && isKeyword ? SectionType.Keyword : sectType);
        processResult.Sections.Add(mainSection);
        currentIndex += content.Length;
        processResult.FinalIndex = currentIndex;
        processResult.MainSection = mainSection;
      }
      if (!string.IsNullOrEmpty(laterWs)) {
        AibeSection sect = new AibeSection(laterWs, StartIndex + currentIndex, currentIndex, this, SectionType.Empty);
        processResult.Sections.Add(sect);
        currentIndex += laterWs.Length;
        processResult.FinalIndex = currentIndex;
      }
      return processResult;
    }

    private AibeStreamProcessResult processUntrimmedBasicStream(string desc, SectionType sectType, int currentIndex, bool allowKeyword = false) {
      AibeStreamProcessResult processResult = new AibeStreamProcessResult { FinalIndex = currentIndex };
      List<char> accChars = new List<char>();
      bool isFinal = false;
      string subDesc = string.Empty;
      int relativeIndex = 0;

      bool result = processBasicStream(desc, true, relativeIndex, ref isFinal, out accChars);
      if (!result && isFinal) //if not isFinal, means there is no character
        return processResult;

      if (accChars.Count > 0) {
        subDesc = new string(accChars.ToArray());
        AibeSection earlyWhiteSection = new AibeSection(subDesc, StartIndex + currentIndex, currentIndex, this, SectionType.Empty);
        processResult.Sections.Add(earlyWhiteSection);
        currentIndex += subDesc.Length;
        relativeIndex += subDesc.Length;
        processResult.FinalIndex = currentIndex;
      }

      result = processBasicStream(desc, false, relativeIndex, ref isFinal, out accChars);
      if (!result && isFinal)
        return processResult;

      if (accChars.Count > 0) {
        subDesc = new string(accChars.ToArray());
        bool isKeyword = logic.Keywords.Any(x => x.EqualsIgnoreCase(subDesc)); //Aibe.DH.Skip.EqualsIgnoreCase(subDesc);
        AibeSection mainSection = new AibeSection(subDesc, StartIndex + currentIndex, currentIndex, this, allowKeyword && isKeyword ? SectionType.Keyword : sectType);
        processResult.Sections.Add(mainSection);
        currentIndex += subDesc.Length;
        relativeIndex += subDesc.Length;
        processResult.FinalIndex = currentIndex;
        processResult.MainSection = mainSection;
      }

      result = processBasicStream(desc, true, relativeIndex, ref isFinal, out accChars);
      if (!result && isFinal)
        return processResult;

      if (accChars.Count > 0) {
        subDesc = new string(accChars.ToArray());
        AibeSection laterWhiteSection = new AibeSection(subDesc, StartIndex + currentIndex, currentIndex, this, SectionType.Empty);
        processResult.Sections.Add(laterWhiteSection);
        currentIndex += subDesc.Length;
        relativeIndex += subDesc.Length;
        processResult.FinalIndex = currentIndex;
      }

      return processResult;
    }

    private void processWs(string desc, AibeParagraphState state) {
      AibeSection ws = new AibeSection(desc, StartIndex + state.CurrentOffset, state.CurrentOffset, this, SectionType.Empty);
      state.Sections.Add(ws);
      state.CurrentOffset += desc.Length;
    }

    private void processSimpleElement(string desc, AibeParagraphState state, SectionType type) {
      AibeSection element = new AibeSection(desc, StartIndex + state.CurrentOffset, state.CurrentOffset, this, type);
      state.Sections.Add(element);
      state.CurrentOffset += desc.Length;
    }

    private void processSymbol(string desc, AibeParagraphState state) {
      AibeSection symbol = new AibeSection(desc, StartIndex + state.CurrentOffset, state.CurrentOffset, this, SectionType.Symbol);
      state.Sections.Add(symbol);
      state.CurrentOffset += desc.Length;
    }

    private void processSpacedUntrimmedElement(string desc, SectionType sectType, AibeParagraphState state, bool allowKeyword = false) {
      AibeStreamProcessResult result = processSpacedUntrimmedBasicStream(desc, sectType, state.CurrentOffset, allowKeyword);
      state.Sections.AddRange(result.Sections);
      state.CurrentOffset = result.FinalIndex;
    }

    private void processUntrimmedElement(string desc, SectionType sectType, AibeParagraphState state, bool allowKeyword = false) {
      AibeStreamProcessResult result = processUntrimmedBasicStream(desc, sectType, state.CurrentOffset, allowKeyword);
      state.Sections.AddRange(result.Sections);
      state.CurrentOffset = result.FinalIndex;
    }

    private int processClosure(string desc, AibeParagraphState state, char closure, int index) {
      List<char> accChars = new List<char>();
      bool isFound = false;
      for (int i = index; i < desc.Length; ++i) {
        char ch = desc[i];
        if (ch != closure)
          accChars.Add(ch);
        else {
          isFound = true; //left Closure Found
          break;
        }
      }

      if (!isFound) { //incorrect: consider all as unknown section
        processSimpleElement(desc, state, SectionType.Unknown);
        return -1;
      }

      if (accChars.Count > 0) //process white space before the closure
        processWs(new string(accChars.ToArray()), state);
      processSymbol(closure.ToString(), state);
      return accChars.Count + 1; //returns the number of items plus the symbol
    }

    //Closure is defined of the same depth...
    private string processMainItemClosure(string desc, AibeParagraphState state, char leftClosure, char rightClosure, int index) {
      List<char> accChars = new List<char>();
      bool isFound = false;
      int leftClosureFound = 0;
      for (int i = index; i < desc.Length; ++i) {
        char ch = desc[i];
        if (ch == leftClosure && ch != rightClosure) { //left closure and not the right closure
          leftClosureFound++; //adds the left closure
          accChars.Add(ch);
        } else if (ch != rightClosure) {
          accChars.Add(ch);
        } else if (leftClosureFound > 0) {
          --leftClosureFound;
          accChars.Add(ch);
        } else { //right closure found here          
          isFound = true; //right closure found
          break;
        }
      }

      if (!isFound) {
        processSimpleElement(desc.Substring(index), state, SectionType.Unknown);
        return null;
      }

      string mainItem = string.Empty;
      if (accChars.Count > 0) //process the content of the main item
        mainItem = new string(accChars.ToArray());

      return mainItem;
    }

    private int processTrailingWs(string desc, AibeParagraphState state, int index) {
      List<char> accChars = new List<char>();
      bool isFound = false;
      for (int i = index; i < desc.Length; ++i) {
        char ch = desc[i];
        if (char.IsWhiteSpace(ch))
          accChars.Add(ch);
        else {
          isFound = true; //Non white space found
          break;
        }
      }

      if (!isFound) { //incorrect: consider all as unknown section
        processSimpleElement(desc.Substring(index), state, SectionType.Unknown);
        return -1;
      }

      if (accChars.Count > 0) //process white space before the element
        processWs(new string(accChars.ToArray()), state);
      return accChars.Count; //returns the number of white space
    }

    private Tuple<int, string> processWsClosureTillMainItem(string desc, AibeParagraphState state, char leftClosure, char rightClosure) {
      //handle left closure
      int index = 0;
      int processResult = processClosure(desc, state, leftClosure, index);
      if (processResult < 0)
        return new Tuple<int, string>(index, null);
      index += processResult;
      if (index >= desc.Length) //finish, nothing else
        return new Tuple<int, string>(index, null);

      //handle trailing white space before item
      processResult = processTrailingWs(desc, state, index);
      if (processResult < 0)
        return new Tuple<int, string>(index, null);
      index += processResult;
      if (index >= desc.Length) //finish, nothing else
        return new Tuple<int, string>(index, null);

      //Process the main item + extra white space before the end closure
      string mainItem = processMainItemClosure(desc, state, leftClosure, rightClosure, index);
      if (mainItem == null)
        return new Tuple<int, string>(index, null);
      index += mainItem.Length;
      return new Tuple<int, string>(index, mainItem);
    }

    //Only used for user filter
    private void processUntrimmedEnclosedCollection(string desc, AibeParagraphState state, char leftClosure, char rightClosure,
      char mainSeparator, char sideSeparator, SectionType leftType, SectionType rightType) {
      //handle closure till main item
      Tuple<int, string> val = processWsClosureTillMainItem(desc, state, leftClosure, rightClosure);
      if (val.Item2 == null)
        return;
      int index = val.Item1;
      string mainItem = val.Item2;
      if (mainItem.Length > 0) {
        UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(mainItem, mainSeparator.ToString());
        processMultipleRightExpression(exp, sideSeparator, state, leftType, rightType);
      }
      processSymbol(rightClosure.ToString(), state);
      index++;
      if (index >= desc.Length)
        return;

      //Process final white space, if any
      processWs(desc.Substring(index), state);
    }

    //something like @SpPar41=Par21,@SpPar42=Par42,…
    private void processUntrimmedEnclosedCollectionPair(string desc, AibeParagraphState state, char leftClosure, char rightClosure,
      char mainSeparator, char sideSeparator, SectionType leftType, SectionType rightType) {
      //handle closure till main item
      Tuple<int, string> val = processWsClosureTillMainItem(desc, state, leftClosure, rightClosure);
      if (val.Item2 == null)
        return;
      int index = val.Item1;
      string mainItem = val.Item2;
      if (mainItem.Length > 0) {
        List<string> components = mainItem.ParseComponents(mainSeparator, true);
        int compNo = 0;
        foreach (var component in components) { //each component is like @SpPar41=Par21
          if (compNo > 0)
            processSymbol(mainSeparator.ToString(), state);
          UntrimmedSimpleExpression subExp = new UntrimmedSimpleExpression(component, sideSeparator.ToString());
          processExpression(subExp, state, leftType, rightType);
          ++compNo;
        }
      }
      processSymbol(rightClosure.ToString(), state);
      index++;
      if (index >= desc.Length)
        return;

      //Process final white space, if any
      processWs(desc.Substring(index), state);
    }

    private void processUntrimmedEnclosedItem(string desc, AibeParagraphState state, char leftClosure, char rightClosure,
      SectionType type) {
      //handle closure till main item
      Tuple<int, string> val = processWsClosureTillMainItem(desc, state, leftClosure, rightClosure);
      if (val.Item2 == null)
        return;
      int index = val.Item1;
      string mainItem = val.Item2;
      if (mainItem.Length > 0)
        processUntrimmedElement(mainItem, type, state);
      processSymbol(rightClosure.ToString(), state);
      index++;
      if (index >= desc.Length)
        return;

      //Process final white space, if any
      processWs(desc.Substring(index), state);
    }

    private void processUntrimmedTVR(string desc, AibeParagraphState state) {
      List<string> components = desc.ParseComponents(':', true, '"'); //sacrificing AddWhereClause for static, that is, by using " instead of '
      int compNo = 0;
      foreach (var component in components) {
        if (compNo > 0)
          processSymbol(":", state);
        if (compNo == 0) //table name
          processUntrimmedElement(component, SectionType.TableName, state);
        if (compNo == 1) //column name
          processUntrimmedElement(component, SectionType.ColumnName, state);
        if (compNo == 2) {
          UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(component, "=");
          if (exp.HasKey)
            processSpacedUntrimmedElement(exp.LeftSide, SectionType.ColumnName, state, allowKeyword: true);
          if (exp.HasSign)
            processSymbol(exp.MiddleSign, state);
          if (exp.HasValue)
            processSpacedUntrimmedElement(exp.RightSide, SectionType.Value, state);
        }
        if (compNo == 3)
          processSpacedUntrimmedElement(component, SectionType.SQLScript, state);
        compNo++;
      }
    }

    private void processUntrimmedEnclosedTVR(string desc, AibeParagraphState state, char leftClosure, char rightClosure) {
      //handle closure till main item
      Tuple<int, string> val = processWsClosureTillMainItem(desc, state, leftClosure, rightClosure);
      if (val.Item2 == null)
        return;
      int index = val.Item1;
      string mainItem = val.Item2;
      if (mainItem.Length > 0)
        processUntrimmedTVR(mainItem, state);
      processSymbol(rightClosure.ToString(), state);
      index++;
      if (index >= desc.Length)
        return;

      //Process final white space, if any
      processWs(desc.Substring(index), state);
    }

    private void processColoringTVR(string desc, AibeParagraphState state) {
      List<string> components = desc.ParseComponents(':', true);
      int compNo = 0;
      foreach (var component in components) {
        if (string.IsNullOrEmpty(component))
          continue;
        if (compNo > 0)
          processSymbol(":", state);
        if (compNo == 0) //table name
          processUntrimmedElement(component, SectionType.TableName, state);
        if (compNo == 1) //column name
          processUntrimmedElement(component, SectionType.ColumnName, state);
        if (compNo == 2)
          processSpacedUntrimmedElement(component, SectionType.Value, state); //just process everything as value for now        
        ++compNo;
      }
    }

    private void processColoringComparisonExpression(string desc, AibeParagraphState state) {
      //handle trailing white space before item
      int index = 0;
      int processResult = processTrailingWs(desc, state, index);
      if (processResult < 0)
        return;
      index += processResult;
      if (index >= desc.Length) //finish, nothing else
        return;

      //Process the main item + extra white space before the end closure
      string remnant = desc.Substring(index).ToUpper();
      string usedDesc = desc.Substring(index);
      bool isAggr = Aibe.DH.AggregateNames.Any(x => remnant.StartsWith(x.ToUpper()));
      ColoringTableValueRefInfoTest refTable = new ColoringTableValueRefInfoTest(desc);
      if (isAggr) {
        string aggrDefault = Aibe.DH.AggregateNames.FirstOrDefault(x => remnant.StartsWith(x.ToUpper()));
        string aggr = desc.Substring(index, aggrDefault.Length);
        processSimpleElement(aggr, state, SectionType.AggregateName); //get the element
        index += aggr.Length;
        if (index >= desc.Length)
          return;
        string tvr = desc.Substring(index);

        //handle closure till main item
        Tuple<int, string> val = processWsClosureTillMainItem(tvr, state, '(', ')');
        if (val.Item2 == null)
          return;
        int tvrIndex = val.Item1;
        string mainItem = val.Item2;
        index += tvrIndex;

        if (mainItem.Length > 0)
          processColoringTVR(mainItem, state);
        processSymbol(")", state);
        index++;
        if (index >= desc.Length)
          return;

        //Process final white space, if any
        processWs(desc.Substring(index), state);
      } else if (refTable.IsValid) {
        processColoringTVR(usedDesc, state);
      } else if (usedDesc.Contains('+') || usedDesc.Contains('-')) {
        UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(usedDesc, new List<string> { "+", "-" });
        processExpression(exp, state, SectionType.Keyword, SectionType.Number);
      } else
        processSpacedUntrimmedElement(usedDesc, SectionType.Value, state); //Just process as value
    }

    private void processColoringCondition(string desc, AibeParagraphState state) {
      //handle closure till main item
      Tuple<int, string> val = processWsClosureTillMainItem(desc, state, '[', ']');
      if (val.Item2 == null)
        return;
      int index = val.Item1;
      string mainItem = val.Item2;
      if (mainItem.Length > 0) {
        List<string> components = mainItem.ParseComponents('|', true);
        if (components.Count != 4) {
          processSimpleElement(mainItem, state, SectionType.Unknown);
        } else {
          UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(components[0], ",");
          processExpression(exp, state, SectionType.ThisTableColumnName, SectionType.ThisTableColumnName);
          processSymbol("|", state);
          processUntrimmedElement(components[1], SectionType.ColoringComparisonCode, state);
          processSymbol("|", state);
          processColoringComparisonExpression(components[2], state);
          processSymbol("|", state);
          processUntrimmedElement(components[3], SectionType.Color, state);
        }
      }
      processSymbol("]", state);
      index++;
      if (index >= desc.Length)
        return;

      //Process final white space, if any
      processWs(desc.Substring(index), state);
    }

    private void processDropdownList(string desc, AibeParagraphState state) {
      List<string> components = desc.ParseComponentsWithEnclosurePairs(',', true,
        new List<KeyValuePair<char, char>> { new KeyValuePair<char, char>('[', ']') });
      int compNo = 0;
      foreach (var component in components) {
        if (compNo > 0)
          processSymbol(",", state);
        string testElement = component.Trim();
        bool isTVR = testElement.StartsWith("[");
        bool isDirective = testElement.StartsWith("{");
        if (isTVR) {
          processUntrimmedEnclosedTVR(component, state, '[', ']');
        } else if (isDirective) { //The directive
          processUntrimmedEnclosedItem(component, state, '{', '}', SectionType.OrderByDirective);
        } else //the normal item
          processSpacedUntrimmedElement(component, SectionType.Value, state, true);
        ++compNo;
      }
    }

    private void processExpression(UntrimmedSimpleExpression exp, AibeParagraphState state, SectionType leftType, SectionType rightType) {
      if (exp.HasKey) //left side
        processSpacedUntrimmedElement(exp.LeftSide, leftType, state);
      if (exp.HasSign) //has middle sign
        processSymbol(exp.MiddleSign, state);
      if (exp.HasValue) //right side
        processSpacedUntrimmedElement(exp.RightSide, rightType, state);
    }

    private void processCommonExpressions(string desc, AibeParagraphState state, SectionType leftType, SectionType rightType) {
      List<string> components = desc.ParseComponents(';', true);
      int compNo = 0;
      foreach (var component in components) {
        if (compNo > 0)
          processSymbol(";", state);
        UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(component, "=");
        processExpression(exp, state, leftType, rightType);
        ++compNo;
      }
    }

    private void processSimpleList(string desc, char separator, AibeParagraphState state, SectionType sectionType, bool allowKeyword = false, int maxIndexToAllowKeyword = -1) {
      List<string> components = desc.ParseComponents(separator, true);
      int compNo = 0;
      foreach (var component in components) {
        if (compNo > 0)
          processSymbol(separator.ToString(), state);
        processSpacedUntrimmedElement(component, sectionType, state, maxIndexToAllowKeyword >= compNo && allowKeyword);
        ++compNo;
      }
    }

    private void processCyclicList(string desc, char separator, AibeParagraphState state, List<SectionType> types, bool allowKeyword = false) {
      List<string> components = desc.ParseComponents(separator, true);
      int compNo = 0;
      foreach (var component in components) {
        if (compNo > 0)
          processSymbol(separator.ToString(), state);
        SectionType type = types == null || types.Count <= 0 ? SectionType.Value : types[compNo % types.Count];
        processSpacedUntrimmedElement(component, type, state, allowKeyword);
        ++compNo;
      }
    }

    private void processTwoLayeredSimpleList(string desc, char separator1, char separator2, AibeParagraphState state, SectionType sectionType, bool allowKeyword, int maxIndexToAllowKeyword) {
      List<string> components = desc.ParseComponents(separator1, true);
      int compNo = 0;
      foreach (var component in components) {
        if (compNo > 0)
          processSymbol(separator1.ToString(), state);
        processSimpleList(component, separator2, state, sectionType, allowKeyword, maxIndexToAllowKeyword);
        ++compNo;
      }
    }

    private void processTwoLayeredMultipleRightExpression(UntrimmedSimpleExpression exp, char rightSeparator1, char rightSeparator2, AibeParagraphState state, SectionType leftType,
      SectionType rightType, bool allowKeyword, int maxIndexToAllowKeyword) {
      if (exp.HasKey) //left side
        processUntrimmedElement(exp.LeftSide, leftType, state);
      if (exp.HasSign) //has middle sign
        processSymbol(exp.MiddleSign, state);
      if (exp.HasValue)
        processTwoLayeredSimpleList(exp.RightSide, rightSeparator1, rightSeparator2, state, rightType, allowKeyword, maxIndexToAllowKeyword);
    }

    private void processListSimpleListMain(string desc, char separator1, char separator2, AibeParagraphState state, List<SectionType> types,
      bool allowKeyword = false) {
      List<string> components = desc.ParseComponents(separator1, true);
      int compNo = 0;
      foreach (var component in components) {
        if (compNo > 0)
          processSymbol(separator1.ToString(), state);
        SectionType type = types != null && compNo < types.Count ? types[compNo] : SectionType.Value;
        List<string> subComponents = component.ParseComponents(separator2, true);
        int subCompNo = 0;
        foreach (var subComponent in subComponents) {
          if (subCompNo > 0)
            processSymbol(separator2.ToString(), state);
          processSpacedUntrimmedElement(subComponent, type, state, allowKeyword);
          ++subCompNo;
        }
        ++compNo;
      }
    }

    private void processListSimpleListSub(string desc, char separator1, char separator2, AibeParagraphState state, List<SectionType> types,
      bool allowKeyword = false) {
      List<string> components = desc.ParseComponents(separator1, true);
      int compNo = 0;
      foreach (var component in components) {
        if (compNo > 0)
          processSymbol(separator1.ToString(), state);
        List<string> subComponents = component.ParseComponents(separator2, true);
        int subCompNo = 0;
        foreach (var subComponent in subComponents) {
          if (subCompNo > 0)
            processSymbol(separator2.ToString(), state);
          SectionType type = types != null && subCompNo < types.Count ? types[subCompNo] : SectionType.Value;
          processSpacedUntrimmedElement(subComponent, type, state, allowKeyword);
          ++subCompNo;
        }
        ++compNo;
      }
    }

    //RowAction11|TimeShiftValue11|IsFixed,RowAction12|TimeShiftValue12
    private void processListListRightExpressionSub(UntrimmedSimpleExpression exp, char rightSeparator1, char rightSeparator2,
      AibeParagraphState state, SectionType leftType, List<SectionType> rightTypes, bool allowKeyword = false) {
      if (exp.HasKey) //left side
        processUntrimmedElement(exp.LeftSide, leftType, state);
      if (exp.HasSign) //has middle sign
        processSymbol(exp.MiddleSign, state);
      if (exp.HasValue)
        processListSimpleListSub(exp.RightSide, rightSeparator1, rightSeparator2, state, rightTypes, allowKeyword);
    }

    //IsDataDeleted,MustEditHaveChange|RowActionN1,RowActionN2,…=TC-SQLS-N
    private void processListListLeftExpressionMain(UntrimmedSimpleExpression exp, char leftSeparator1, char leftSeparator2,
      AibeParagraphState state, List<SectionType> leftTypes, SectionType rightType, bool allowKeyword = false) {
      if (exp.HasKey) //left side
        processListSimpleListMain(exp.LeftSide, leftSeparator1, leftSeparator2, state, leftTypes, allowKeyword);
      if (exp.HasSign) //has middle sign
        processSymbol(exp.MiddleSign, state);
      if (exp.HasValue)
        processSpacedUntrimmedElement(exp.RightSide, rightType, state);
    }

    //GroupByColumn31:AutoDirective31,GroupByColumn32:AutoDirective32,…
    //Column1|DisplayName1,Column2|DisplayName2,…
    private void processTwoLayeredDividedList(string desc, char separator1, char separator2, AibeParagraphState state, SectionType type1, SectionType type2) {
      List<string> components = desc.ParseComponents(separator1, true);
      int compNo = 0;
      foreach (var component in components) {
        if (compNo > 0)
          processSymbol(separator1.ToString(), state);
        UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(component, separator2.ToString());
        processExpression(exp, state, type1, type2);
        ++compNo;
      }
    }

    //Column1|DisplayName1|AssignToColumn1,Column2|DisplayName2|AssignToColumn2,…
    private void processTwoLayeredDividedContinuousList(string desc, char separator1, char separator2, AibeParagraphState state, List<SectionType> types) {
      List<string> components = desc.ParseComponents(separator1, true);
      int compNo = 0;
      foreach (var component in components) {
        if (compNo > 0)
          processSymbol(separator1.ToString(), state);
        UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(component, separator2.ToString());
        int subCompNo = 0;
        SectionType type = types[Math.Min(types.Count - 1, subCompNo)];
        while (exp.HasValue) {
          if (exp.HasKey) //left side
            processSpacedUntrimmedElement(exp.LeftSide, type, state);
          if (exp.HasSign) //has middle sign
            processSymbol(exp.MiddleSign, state);
          ++subCompNo;
          exp = new UntrimmedSimpleExpression(exp.RightSide, separator2.ToString());
          type = types[Math.Min(types.Count - 1, subCompNo)];
        }
        if (exp.HasKey) //left side
          processSpacedUntrimmedElement(exp.LeftSide, type, state);
        if (exp.HasSign) //has middle sign
          processSymbol(exp.MiddleSign, state);
        ++compNo;
      }
    }

    private void processDividedMultipleRightExpression(UntrimmedSimpleExpression exp, char rightSeparator1, char rightSeparator2, AibeParagraphState state,
      SectionType leftType, SectionType rightType1, SectionType rightType2) {
      if (exp.HasKey) //left side
        processUntrimmedElement(exp.LeftSide, leftType, state);
      if (exp.HasSign) //has middle sign
        processSymbol(exp.MiddleSign, state);
      if (exp.HasValue)
        processTwoLayeredDividedList(exp.RightSide, rightSeparator1, rightSeparator2, state, rightType1, rightType2);
    }

    private void processMultipleLeftExpression(UntrimmedSimpleExpression exp,
      char leftSeparator, AibeParagraphState state,
      SectionType mainType, SectionType subType) {
      if (exp.HasKey) //left side
        processSpacedUntrimmedElement(exp.LeftSide, mainType, state);
      if (exp.HasSign) //has middle sign
        processSymbol(exp.MiddleSign, state);
      if (exp.HasValue)
        processSimpleList(exp.RightSide, leftSeparator, state, subType);
    }

    private void processMultipleRightExpression(UntrimmedSimpleExpression exp, char rightSeparator, AibeParagraphState state, SectionType leftType, SectionType rightType) {
      if (exp.HasKey) //left side
        processUntrimmedElement(exp.LeftSide, leftType, state);
      if (exp.HasSign) //has middle sign
        processSymbol(exp.MiddleSign, state);
      if (exp.HasValue)
        processSimpleList(exp.RightSide, rightSeparator, state, rightType);
    }

    private void processCommonMultipleRightExpressions(string desc, AibeParagraphState state, SectionType leftType, SectionType rightType) {
      List<string> components = desc.ParseComponents(';', true);
      int compNo = 0;
      foreach (var component in components) {
        if (compNo > 0)
          processSymbol(";", state);
        UntrimmedSimpleExpression subExp = new UntrimmedSimpleExpression(component, "=");
        processMultipleRightExpression(subExp, ',', state, leftType, rightType);
        ++compNo;
      }
    }

    private void processListColumn(string desc, AibeParagraphState state) {
      UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(desc, "=");
      if (exp.HasKey) {
        UntrimmedSimpleExpression leftExp = new UntrimmedSimpleExpression(exp.LeftSide, "|");
        if (leftExp.HasKey)
          processUntrimmedElement(leftExp.LeftSide, SectionType.ThisTableColumnName, state);
        if (leftExp.HasSign)
          processSymbol(leftExp.MiddleSign, state);
        if (leftExp.HasValue) {
          List<string> components = leftExp.RightSide.ParseComponents(',', true);
          int compNo = 0;
          foreach(var component in components) {
            if (compNo > 0)
              processSymbol(",", state);
            UntrimmedSimpleExpression subExp = new UntrimmedSimpleExpression(component, "#");
            processExpression(subExp, state, SectionType.Text, SectionType.Number);
            ++compNo;
          }
        }
      }
      if (exp.HasSign)
        processSymbol(exp.MiddleSign, state);
      if (exp.HasValue) {
        UntrimmedSimpleExpression rightExp = new UntrimmedSimpleExpression(exp.RightSide, "|");
        if (rightExp.HasKey)
          processUntrimmedElement(rightExp.LeftSide, SectionType.ListType, state);
        if (rightExp.HasSign)
          processSymbol(rightExp.MiddleSign, state);
        if (rightExp.HasValue) {
          //handle trailing white space before item
          int index = 0;
          int processResult = processTrailingWs(rightExp.RightSide, state, index);
          if (processResult < 0)
            return;
          index += processResult;
          if (index >= rightExp.RightSide.Length) //finish, nothing else
            return;
          processUntrimmedTVR(rightExp.RightSide.Substring(index), state);
        }
      }
    }

    private void processActionTriggerLeft(string desc, AibeParagraphState state) {
      UntrimmedSimpleExpression leftExp = new UntrimmedSimpleExpression(desc, "|");
      if (leftExp.HasKey)//TriggerName3
        processSpacedUntrimmedElement(leftExp.LeftSide, SectionType.TriggerName, state);
      if (leftExp.HasSign) // |
        processSymbol(leftExp.MiddleSign, state);
      if (leftExp.HasValue) { //RowAction31,RowAction32,…|MustEditHaveChange
        UntrimmedSimpleExpression leftRightExp = new UntrimmedSimpleExpression(leftExp.RightSide, "|");
        if (leftRightExp.HasKey) //RowAction31,RowAction32,…
          processSimpleList(leftRightExp.LeftSide, ',', state, SectionType.TriggerRowAction);
        if (leftRightExp.HasSign) // |
          processSymbol(leftRightExp.MiddleSign, state);
        if (leftRightExp.HasValue) //MustEditHaveChange
          processSpacedUntrimmedElement(leftRightExp.RightSide, SectionType.TrueFalse, state);
      }
    }

    //Based on the content and the column name, produces different sections
    public List<AibeSection> GetSections(string columnName, string content, int contentOffset) {
      if (string.IsNullOrWhiteSpace(content)) //has no content
        return null; //the only one to return null
      bool knownColumn = logic.MetaItemColumnNames.Any(x => x.EqualsIgnoreCase(columnName));
      if (!knownColumn) //column is known but there is no content, assuming one content, others
        return new List<AibeSection> { new AibeSection(content, AbsContentOffset, ContentOffset, this, SectionType.Other) };
      string metaColumnName = logic.MetaItemColumnNames.FirstOrDefault(x => x.EqualsIgnoreCase(columnName));
      MethodInfo methodInfo = typeof(AibeParagraph).GetMethod("Section" + metaColumnName);
      if (methodInfo == null) //unable to invoke the method, return normal return
        return new List<AibeSection> { new AibeSection(content, AbsContentOffset, ContentOffset, this, SectionType.Other) };
      object result = methodInfo.Invoke(this, new object[] { content }); //Invoking based on column name
      if (result != null && result is List<AibeSection>) //successful
        return (List<AibeSection>)result;
      return new List<AibeSection> { new AibeSection(content, AbsContentOffset, ContentOffset, this, SectionType.Other) };
    }

    #region sections
    public List<AibeSection> SectionDisplayName(string content) {
      return new List<AibeSection> { new AibeSection(content, AbsContentOffset, ContentOffset, this, SectionType.Text) };
    }
    public List<AibeSection> SectionTableSource(string content) {
      return new List<AibeSection> { new AibeSection(content, AbsContentOffset, ContentOffset, this, SectionType.TableSource) };
    }

    public List<AibeSection> SectionPrefilledColumns(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      processCommonExpressions(content, state, SectionType.ThisTableColumnName, SectionType.Value);
      return state.Sections;
    }

    public List<AibeSection> SectionItemsPerPage(string content) {
      return new List<AibeSection> { new AibeSection(content, AbsContentOffset, ContentOffset, this, SectionType.Number) };
    }

    public List<AibeSection> SectionOrderBy(string content) {
      UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(content, ":"); //testing it
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      if (!string.IsNullOrWhiteSpace(exp.RightSide) &&
        (exp.LeftSide.TrimStart() + ":").Equals(Aibe.DH.SQLScriptDirectivePrefix)) { //only trim the starting part since that is how it is parsed, also, it is equals not ignorecase
        processExpression(exp, state, SectionType.SQLScriptPrefix, SectionType.SQLScript);
        return state.Sections;
      }
      processCommonExpressions(content, state, SectionType.ThisTableColumnName, SectionType.OrderByDirective);
      return state.Sections;
    }

    public List<AibeSection> SectionActionList(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      processCommonMultipleRightExpressions(content, state, SectionType.RowAction, SectionType.Role);
      return state.Sections;
    }

    public List<AibeSection> SectionDefaultActionList(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      processSimpleList(content, ';', state, SectionType.RowAction);
      return state.Sections;
    }

    public List<AibeSection> SectionTableActionList(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      processCommonMultipleRightExpressions(content, state, SectionType.TableAction, SectionType.Role);
      return state.Sections;
    }

    public List<AibeSection> SectionDefaultTableActionList(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      processSimpleList(content, ';', state, SectionType.TableAction);
      return state.Sections;
    }

    public List<AibeSection> SectionTextFieldColumns(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      processCommonExpressions(content, state, SectionType.ThisTableColumnName, SectionType.Number);
      return state.Sections;
    }

    public List<AibeSection> SectionPictureColumns(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      List<string> components = content.ParseComponents(';', true);
      int compNo = 0;
      foreach (var component in components) {
        if (compNo > 0)
          processSymbol(";", state);
        UntrimmedSimpleExpression subExp = new UntrimmedSimpleExpression(component, "=");
        processTwoLayeredMultipleRightExpression(subExp, '|', ',', state, SectionType.ThisTableColumnName, SectionType.Number, allowKeyword: true, maxIndexToAllowKeyword: 0);
        ++compNo;
      }
      return state.Sections;
    }

    public List<AibeSection> SectionIndexShownPictureColumns(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      processSimpleList(content, ';', state, SectionType.ThisTableColumnName);
      return state.Sections;
    }

    public List<AibeSection> SectionRequiredColumns(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      processSimpleList(content, ';', state, SectionType.ThisTableColumnName);
      return state.Sections;
    }

    public List<AibeSection> SectionNumberLimitColumns(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      List<string> components = content.ParseComponents(';', true);
      int compNo = 0;
      foreach (var component in components) {
        if (compNo > 0)
          processSymbol(";", state);
        UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(component, "=");
        processDividedMultipleRightExpression(exp, '|', ':', state, SectionType.ThisTableColumnName, SectionType.MinMax, SectionType.Number);
        ++compNo;
      }
      return state.Sections;
    }

    public List<AibeSection> SectionRegexCheckedColumns(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      List<string> components = content.ParseStrictTagUntrimmed(Aibe.DH.RegexCheckedColumnTag);
      string startTag = "<" + Aibe.DH.RegexCheckedColumnTag + ">";
      string endTag = "</" + Aibe.DH.RegexCheckedColumnTag + ">";
      foreach (var component in components) {
        if (string.IsNullOrWhiteSpace(component)) { //white space components
          if (component != null && component.Length > 0)
            processWs(component, state);
          continue;
        }
        if (component == startTag || component == endTag) { //strict checking of tag component, not ignore case or trim
          processSimpleElement(component, state, SectionType.Tag);
          continue;
        }
        UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(component, "=");
        processExpression(exp, state, SectionType.ThisTableColumnName, SectionType.Regex);
      }
      return state.Sections;
    }

    public List<AibeSection> SectionRegexCheckedColumnExamples(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      List<string> components = content.ParseStrictTagUntrimmed(Aibe.DH.RegexCheckedColumnExampleTag);
      string startTag = "<" + Aibe.DH.RegexCheckedColumnExampleTag + ">";
      string endTag = "</" + Aibe.DH.RegexCheckedColumnExampleTag + ">";
      foreach (var component in components) {
        if (string.IsNullOrWhiteSpace(component)) { //white space components
          if (component != null && component.Length > 0)
            processWs(component, state);
          continue;
        }
        if (component == startTag || component == endTag) { //strict checking of tag component, not ignore case or trim
          processSimpleElement(component, state, SectionType.Tag);
          continue;
        }
        UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(component, "=");
        processExpression(exp, state, SectionType.ThisTableColumnName, SectionType.Text);
      }
      return state.Sections;
    }

    public List<AibeSection> SectionUserRelatedFilters(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(content, "=");
      if (exp.HasKey) {
        UntrimmedSimpleExpression leftExp = new UntrimmedSimpleExpression(exp.LeftSide, "|");
        if (leftExp.HasKey)
          processSimpleElement(leftExp.LeftSide, state, SectionType.ThisTableColumnName);
        if (leftExp.HasSign)
          processSymbol(leftExp.MiddleSign, state);
        if (leftExp.HasValue)
          processUntrimmedEnclosedCollection(leftExp.RightSide, state, '{', '}', ':', ',', SectionType.UserRelatedDirective, SectionType.Value);
      }

      if (exp.HasSign)
        processSymbol(exp.MiddleSign, state);

      if (exp.HasValue) {
        UntrimmedSimpleExpression rightExp = new UntrimmedSimpleExpression(exp.RightSide, "|");
        if (rightExp.HasKey)
          processSimpleElement(rightExp.LeftSide, state, SectionType.UserColumnName);
        if (rightExp.HasSign)
          processSymbol(rightExp.MiddleSign, state);
        if (rightExp.HasValue)
          processUntrimmedEnclosedCollection(rightExp.RightSide, state, '{', '}', ':', ',', SectionType.UserRelatedDirective, SectionType.Value);
      }

      return state.Sections;
    }

    public List<AibeSection> SectionDisableFilter(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      processUntrimmedElement(content, SectionType.TrueFalse, state);
      return state.Sections;
    }

    public List<AibeSection> SectionForcedFilterColumns(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      processCommonMultipleRightExpressions(content, state, SectionType.ThisTableColumnName, SectionType.Role);
      return state.Sections;
    }

    public List<AibeSection> SectionColumnExclusionList(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      processCommonMultipleRightExpressions(content, state, SectionType.ThisTableColumnName, SectionType.Role);
      return state.Sections;
    }

    public List<AibeSection> SectionFilterExclusionList(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      processCommonMultipleRightExpressions(content, state, SectionType.ThisTableColumnName, SectionType.Role);
      return state.Sections;
    }

    public List<AibeSection> SectionDetailsExclusionList(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      processCommonMultipleRightExpressions(content, state, SectionType.ThisTableColumnName, SectionType.Role);
      return state.Sections;
    }

    public List<AibeSection> SectionCreateEditExclusionList(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      processCommonMultipleRightExpressions(content, state, SectionType.ThisTableColumnName, SectionType.Role);
      return state.Sections;
    }

    public List<AibeSection> SectionCsvExclusionList(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      processCommonMultipleRightExpressions(content, state, SectionType.ThisTableColumnName, SectionType.Role);
      return state.Sections;
    }

    public List<AibeSection> SectionAccessExclusionList(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      processSimpleList(content, ';', state, SectionType.Role);
      return state.Sections;
    }

    public List<AibeSection> SectionColoringList(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      List<string> components = content.ParseComponents(';', true);
      int compNo = 0;
      foreach (var component in components) {
        if (compNo > 0)
          processSymbol(";", state);
        processColoringCondition(component, state);
        ++compNo;
      }
      return state.Sections;
    }

    public List<AibeSection> SectionFilterDropDownLists(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      List<string> components = content.ParseComponentsWithEnclosurePairs(';', true, Aibe.DH.DefaultScriptEnclosurePairs);
      int compNo = 0;
      foreach (var component in components) {
        if (compNo > 0)
          processSymbol(";", state);
        UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(component, "=");
        if (exp.HasKey)
          processUntrimmedElement(exp.LeftSide, SectionType.ThisTableColumnName, state);
        if (exp.HasSign)
          processSymbol(exp.MiddleSign, state);
        if (exp.HasValue)
          processDropdownList(exp.RightSide, state);
        ++compNo;
      }
      return state.Sections;
    }

    public List<AibeSection> SectionCreateEditDropDownLists(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      List<string> components = content.ParseComponentsWithEnclosurePairs(';', true, Aibe.DH.DefaultScriptEnclosurePairs);
      int compNo = 0;
      foreach (var component in components) {
        if (compNo > 0)
          processSymbol(";", state);
        UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(component, "=");
        if (exp.HasKey)
          processUntrimmedElement(exp.LeftSide, SectionType.ThisTableColumnName, state);
        if (exp.HasSign)
          processSymbol(exp.MiddleSign, state);
        if (exp.HasValue)
          processDropdownList(exp.RightSide, state);
        ++compNo;
      }
      return state.Sections;
    }

    public List<AibeSection> SectionPrefixesOfColumns(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      processCommonExpressions(content, state, SectionType.ThisTableColumnName, SectionType.Text);
      return state.Sections;
    }

    public List<AibeSection> SectionPostfixesOfColumns(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      processCommonExpressions(content, state, SectionType.ThisTableColumnName, SectionType.Text);
      return state.Sections;
    }

    public List<AibeSection> SectionListColumns(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      List<string> components = content.ParseComponentsWithEnclosurePairs(';', true, Aibe.DH.DefaultScriptEnclosurePairs);
      int compNo = 0;
      foreach (var component in components) {
        if (compNo > 0)
          processSymbol(";", state);
        processListColumn(component, state);
        ++compNo;
      }
      return state.Sections;
    }

    public List<AibeSection> SectionTimeStampColumns(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      List<string> components = content.ParseComponents(';', true);
      int compNo = 0;
      foreach (var component in components) {
        if (compNo > 0)
          processSymbol(";", state);
        UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(component, "=");
        processListListRightExpressionSub(exp, ',', '|', state, SectionType.ThisTableColumnName,
          new List<SectionType> { SectionType.RowAction, SectionType.Value, SectionType.TrueFalse });
        ++compNo;
      }
      return state.Sections;
    }

    public List<AibeSection> SectionHistoryTable(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(content, "=");
      processListListRightExpressionSub(exp, ',', ':', state, SectionType.TableName,
        new List<SectionType> { SectionType.HistoryColumnName, SectionType.HistoryColumnName }, allowKeyword: true);
      return state.Sections;
    }

    public List<AibeSection> SectionHistoryTriggers(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      List<string> components = content.ParseComponents(';', true);
      int compNo = 0;
      foreach (var component in components) {
        if (compNo > 0)
          processSymbol(";", state);
        UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(component, "=");
        processListListLeftExpressionMain(exp, '|', ',', state,
          new List<SectionType> { SectionType.TrueFalse, SectionType.TriggerRowAction }, SectionType.SQLScript);
        ++compNo;
      }
      return state.Sections;
    }

    public List<AibeSection> SectionAutoGeneratedColumns(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      List<string> components = content.ParseComponents(';', true);
      int compNo = 0;
      foreach (var component in components) {
        if (compNo > 0)
          processSymbol(";", state);
        UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(component, "=");
        processDividedMultipleRightExpression(exp, ',', ':', state, SectionType.ThisTableColumnName,
          SectionType.TableName, SectionType.ColumnName);
        ++compNo;
      }
      return state.Sections;
    }

    public List<AibeSection> SectionColumnSequence(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      processSimpleList(content, ';', state, SectionType.ThisTableColumnName);
      return state.Sections;
    }

    public List<AibeSection> SectionColumnAliases(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      processCommonExpressions(content, state, SectionType.ThisTableColumnName, SectionType.Text);
      return state.Sections;
    }

    public List<AibeSection> SectionEditShowOnlyColumns(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      processSimpleList(content, ';', state, SectionType.ThisTableColumnName);
      return state.Sections;
    }

    public List<AibeSection> SectionScriptConstructorColumns(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      List<string> components = content.ParseComponents(';', true);
      int compNo = 0;
      foreach (var component in components) {
        if (compNo > 0)
          processSymbol(";", state);
        UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(component, "=");
        if (exp.HasKey) {
          UntrimmedSimpleExpression leftExp = new UntrimmedSimpleExpression(exp.LeftSide, "|");
          processDividedMultipleRightExpression(leftExp, '|', ':', state, SectionType.ThisTableColumnName,
            SectionType.ScriptConstructorAttribute, SectionType.Value);
        }
        if (exp.HasSign)
          processSymbol(exp.MiddleSign, state);
        if (exp.HasValue)
          processSpacedUntrimmedElement(exp.RightSide, SectionType.SQLScript, state);
        ++compNo;
      }
      return state.Sections;
    }


    public List<AibeSection> SectionScriptColumns(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      processCommonExpressions(content, state, SectionType.ThisTableColumnName, SectionType.SQLScript);
      return state.Sections;
    }

    public List<AibeSection> SectionCustomDateTimeFormatColumns(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      List<string> components = content.ParseComponents(';', true);
      int compNo = 0;
      foreach (var component in components) {
        if (compNo > 0)
          processSymbol(";", state);
        UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(component, "=");
        if (exp.HasKey)
          processUntrimmedElement(exp.LeftSide, SectionType.ThisTableColumnName, state);
        if (exp.HasSign)
          processSymbol(exp.MiddleSign, state);
        if (exp.HasValue)
          processCyclicList(exp.RightSide, '|', state,
            new List<SectionType> { SectionType.Page, SectionType.DateTimeFormat });
        ++compNo;
      }
      return state.Sections;
    }

    public List<AibeSection> SectionEmailMakerTriggers(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      List<string> components = content.ParseComponents(';', true);
      int compNo = 0;
      foreach (var component in components) {
        if (compNo > 0)
          processSymbol(";", state);
        UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(component, "=");
        if (exp.HasKey) {
          UntrimmedSimpleExpression leftExp = new UntrimmedSimpleExpression(exp.LeftSide, "|");
          processMultipleLeftExpression(leftExp, ',', state, SectionType.TriggerName, SectionType.TriggerRowAction);
        }
        if (exp.HasSign)
          processSymbol(exp.MiddleSign, state);
        if (exp.HasValue)
          processSpacedUntrimmedElement(exp.RightSide, SectionType.SQLScript, state);
        ++compNo;
      }
      return state.Sections;
    }

    public List<AibeSection> SectionEmailMakers(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      processCommonExpressions(content, state, SectionType.TriggerName, SectionType.TemplateName);
      return state.Sections;
    }

    public List<AibeSection> SectionNonPictureAttachmentColumns(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      processCommonMultipleRightExpressions(content, state, SectionType.ThisTableColumnName, SectionType.FileFormat);
      return state.Sections;
    }

    public List<AibeSection> SectionDownloadColumns(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      processSimpleList(content, ';', state, SectionType.ThisTableColumnName);
      return state.Sections;
    }

    public List<AibeSection> SectionPreActionTriggers(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      List<string> components = content.ParseComponents(';', true);
      int compNo = 0;
      foreach (var component in components) {//TriggerName3|RowAction31,RowAction32,…|MustEditHaveChange=TC-SQLS-3
        if (compNo > 0)
          processSymbol(";", state);
        UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(component, "=");
        if (exp.HasKey) //TriggerName3|RowAction31,RowAction32,…|MustEditHaveChange
          processActionTriggerLeft(exp.LeftSide, state);
        if (exp.HasSign) // =
          processSymbol(exp.MiddleSign, state);
        if (exp.HasValue) //TC-SQLS-3
          processSpacedUntrimmedElement(exp.RightSide, SectionType.SQLScript, state);        
        ++compNo;
      }
      return state.Sections;
    }

    public List<AibeSection> SectionPreActionProcedures(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      List<string> components = content.ParseComponents(';', true);
      int compNo = 0;
      bool isUser = false;
      foreach (var component in components) {
        if (compNo > 0)
          processSymbol(";", state);
        UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(component, "="); //TriggerName4=USER:StoredProcedure4(@SpPar41=Par21,@SpPar42=Par42,…);
        if (exp.HasKey) //TriggerName4
          processSpacedUntrimmedElement(exp.LeftSide, SectionType.TriggerName, state);
        if (exp.HasSign) //=
          processSymbol(exp.MiddleSign, state);
        if (exp.HasValue) { //USER:StoredProcedure4(@SpPar41=Par21,@SpPar42=Par42,…);
          UntrimmedSimpleExpression rightExp = new UntrimmedSimpleExpression(exp.RightSide, "(");
          if (rightExp.HasKey) { //process USER:StoredProcedure4
            UntrimmedSimpleExpression spNameExp = new UntrimmedSimpleExpression(rightExp.LeftSide, ":");
            if (spNameExp.HasKey)
              processSpacedUntrimmedElement(spNameExp.LeftSide, spNameExp.HasValue ?
                SectionType.UserDBReferencePrefix : SectionType.DataStoredProcedure, state); //distinguish between StoredProcedure4 and USER:StoredProcedure4
            if (spNameExp.HasSign)
              processSymbol(":", state);
            if (spNameExp.HasValue) {
              isUser = true;
              processSpacedUntrimmedElement(spNameExp.RightSide, SectionType.UserStoredProcedure, state);
            }
          }
          if (rightExp.HasSign)
            if (rightExp.HasValue) //has the value (@SpPar41=Par21,@SpPar42=Par42,…)
              processUntrimmedEnclosedCollectionPair("(" + rightExp.RightSide, state, '(', ')', ',', '=', 
                isUser ? SectionType.UserStoredProcedureParameter : SectionType.DataStoredProcedureParameter, SectionType.Parameter);
            else //only has sign, but does not have value
              processSymbol("(", state);
        }
        ++compNo;
      }
      return state.Sections;
    }

    public List<AibeSection> SectionPostActionTriggers(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      List<string> components = content.ParseComponents(';', true);
      int compNo = 0;
      foreach (var component in components) {//TriggerName3|RowAction31,RowAction32,…|MustEditHaveChange=TC-SQLS-3
        if (compNo > 0)
          processSymbol(";", state);
        UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(component, "=");
        if (exp.HasKey) //TriggerName3|RowAction31,RowAction32,…|MustEditHaveChange
          processActionTriggerLeft(exp.LeftSide, state);
        if (exp.HasSign) // =
          processSymbol(exp.MiddleSign, state);
        if (exp.HasValue) //TC-SQLS-3
          processSpacedUntrimmedElement(exp.RightSide, SectionType.SQLScript, state);
        ++compNo;
      }
      return state.Sections;
    }

    public List<AibeSection> SectionPostActionProcedures(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      List<string> components = content.ParseComponents(';', true);
      int compNo = 0;
      bool isUser = false;
      foreach (var component in components) {
        if (compNo > 0)
          processSymbol(";", state);
        UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(component, "="); //TriggerName4=USER:StoredProcedure4(@SpPar41=Par21,@SpPar42=Par42,…);
        if (exp.HasKey) //TriggerName4
          processSpacedUntrimmedElement(exp.LeftSide, SectionType.TriggerName, state);
        if (exp.HasSign) //=
          processSymbol(exp.MiddleSign, state);
        if (exp.HasValue) { //USER:StoredProcedure4(@SpPar41=Par21,@SpPar42=Par42,…);
          UntrimmedSimpleExpression rightExp = new UntrimmedSimpleExpression(exp.RightSide, "(");
          if (rightExp.HasKey) { //process USER:StoredProcedure4
            UntrimmedSimpleExpression spNameExp = new UntrimmedSimpleExpression(rightExp.LeftSide, ":");
            if (spNameExp.HasKey)
              processSpacedUntrimmedElement(spNameExp.LeftSide, spNameExp.HasValue ?
                SectionType.UserDBReferencePrefix : SectionType.DataStoredProcedure, state); //distinguish between StoredProcedure4 and USER:StoredProcedure4
            if (spNameExp.HasSign)
              processSymbol(":", state);
            if (spNameExp.HasValue) {
              isUser = true;
              processSpacedUntrimmedElement(spNameExp.RightSide, SectionType.UserStoredProcedure, state);
            }
          }
          if (rightExp.HasSign)
            if (rightExp.HasValue) //has the value (@SpPar41=Par21,@SpPar42=Par42,…)
              processUntrimmedEnclosedCollectionPair("(" + rightExp.RightSide, state, '(', ')', ',', '=', 
                isUser ? SectionType.UserStoredProcedureParameter : SectionType.DataStoredProcedureParameter, SectionType.Parameter);
            else //only has sign, but does not have value
              processSymbol("(", state);
        }
        ++compNo;
      }
      return state.Sections;
    }

    public List<AibeSection> SectionTableType(string content) {
      return new List<AibeSection> { new AibeSection(content, AbsContentOffset, ContentOffset, this, SectionType.TableType) };
    }

    public List<AibeSection> SectionAggregationStatement(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(content, "=");
      if (exp.HasKey) //GroupByColumn31:AutoDirective31,GroupByColumn32:AutoDirective32,…
        processTwoLayeredDividedList(exp.LeftSide, ',', ':', state, SectionType.ThisTableColumnName, SectionType.GroupByAutoDirective);
      if (exp.HasSign)
        processSymbol(exp.MiddleSign, state);
      if (exp.HasValue) //AQ-SQLS
        processSpacedUntrimmedElement(exp.RightSide, SectionType.SQLAggregationScript, state);      
      return state.Sections;
    }

    public List<AibeSection> SectionForeignInfoColumns(string content) {
      AibeParagraphState state = new AibeParagraphState { CurrentOffset = ContentOffset };
      List<string> components = content.ParseComponents(';', true);
      int compNo = 0;

      foreach (var component in components) {
        if (compNo > 0)
          processSymbol(";", state);
        UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(component, "=");
        if (exp.HasKey)
          processUntrimmedElement(exp.LeftSide, SectionType.ThisTableColumnName, state);
        if (exp.HasSign)
          processSymbol("=", state);
        if (exp.HasValue) {
          UntrimmedSimpleExpression rightExp = new UntrimmedSimpleExpression(exp.RightSide, ":");
          if (rightExp.HasKey)
            processUntrimmedElement(rightExp.LeftSide, SectionType.TableSource, state);
          if (rightExp.HasSign)
            processSymbol(":", state);
          if (rightExp.HasValue) {
            UntrimmedSimpleExpression columnExp = new UntrimmedSimpleExpression(rightExp.RightSide, ":");
            if (columnExp.HasKey)
              processUntrimmedElement(columnExp.LeftSide, SectionType.ColumnName, state);
            if (columnExp.HasSign)
              processSymbol(":", state);
            if (columnExp.HasValue) { //a list ColumnName1|DisplayName1|AssignToColumn1,ColumnName2|DisplayName2|AssignToColumn2,...,ColumnNameN|DisplayNameN|AssignToColumnN
              processTwoLayeredDividedContinuousList(columnExp.RightSide, ',', '|', state, new List<SectionType> {
                SectionType.ColumnName, SectionType.Text, SectionType.ThisTableColumnName });
              //processTwoLayeredDividedList(columnExp.RightSide, ',', '|', state, SectionType.ColumnName, SectionType.Text);
            }
          }
        }
        ++compNo;
      }

      return state.Sections;
    }

    public override string ToString() {
      return ColumnName + "-" + StartIndex + "-" + 
        Length + ": [" + Content?.ToString() + "]";
    }
    #endregion sections
  }
}

////handle left closure
//processResult = processClosure(desc, state, '(', index);
//if (processResult < 0)
//  return;
//index += processResult;
//if (index >= desc.Length) //finish, nothing else
//  return;

////handle trailing white space before item
//processResult = processTrailingWhiteSpaces(desc, state, index);
//if (processResult < 0)
//  return;
//index += processResult;
//if (index >= desc.Length) //finish, nothing else
//  return;

////Process the main item + extra white space before the end closure
//string mainItem = processMainItemClosure(desc, state, ')', index);
//if (mainItem == null)
//  return;
//index += mainItem.Length;

//List<char> accChars = new List<char>();
//bool isFinal = false;
//string subDesc = string.Empty;
//int currentIndex = 0;

//bool result = processBasicStream(desc, true, currentIndex, ref isFinal, out accChars);
//if (!result)
//  return;

//subDesc = new string(accChars.ToArray());
//AibeSection earlyWhiteSpaceSection = new AibeSection(subDesc, StartIndex, 0, this, SectionType.Empty);
//Sections.Add(earlyWhiteSpaceSection);
//currentIndex += subDesc.Length;

//result = processBasicStream(desc, false, currentIndex, ref isFinal, out accChars);
//if (!result)
//  return;

//ColumnName = new string(accChars.ToArray());
//AibeSection metaColumnNameSection = new AibeSection(ColumnName, StartIndex + currentIndex, currentIndex, this, SectionType.MetaItemColumnName);
//Sections.Add(metaColumnNameSection);
//currentIndex += ColumnName.Length;

//result = processBasicStream(desc, true, currentIndex, ref isFinal, out accChars);
//if (!result)
//  return;

//subDesc = new string(accChars.ToArray());
//AibeSection laterWhiteSection = new AibeSection(subDesc, StartIndex + currentIndex, currentIndex, this, SectionType.Empty);
//Sections.Add(laterWhiteSection);
//currentIndex += subDesc.Length;

//private bool processBasicStream(string desc, bool checkOk, char chRef, int currentIndex, ref bool isFinal, out List<char> accChars) {
//  char ch = '\0';
//  accChars = new List<char>();
//  if (isFinal) //nothing else
//    return false;

//  isFinal = false;
//  int initialCurrentIndex = currentIndex;
//  do {
//    if (currentIndex > initialCurrentIndex)
//      accChars.Add(ch); //add previous character
//    if (currentIndex >= desc.Length) {
//      isFinal = true;
//      break;
//    }
//    ch = desc[currentIndex];
//    currentIndex++;
//  } while ((checkOk && ch == chRef) || (!checkOk && ch != chRef));

//  if (accChars.Count <= 0) //invalid
//    return false;

//  return true;
//}


//List<char> accChars = new List<char>();
//bool isFound = false;
//foreach(var ch in desc) {
//  if (ch != leftClosure)
//    accChars.Add(ch);
//  else {
//    isFound = true; //left Closure Found
//    break;
//  }
//}

//if (!isFound) { //incorrect: consider all as unknown section
//  processSimpleElement(desc, state, SectionType.Unknown);
//  return;
//}

//int index = accChars.Count;
//if (accChars.Count > 0) { //process white space before the left closure
//  processWhiteSpaces(new string(accChars.ToArray()), state);
//  accChars.Clear();
//}
//processSymbol(leftClosure.ToString(), state);
//index++;
//isFound = false;
//if (index >= desc.Length) //finish, nothing else
//  return;

//handle empty string before anything else that is not empty
//for (int i = index; i < desc.Length; ++i) {
//  char ch = desc[i];
//  if (char.IsWhiteSpace(desc[i]))
//    accChars.Add(ch);
//  else {
//    isFound = true;
//    break;
//  }
//}

//if (!isFound) { //incorrect, consider the rests as unknown section
//  processSimpleElement(desc.Substring(index), state, SectionType.Unknown);
//  return;
//}
//index += accChars.Count;
//if (accChars.Count > 0) { //process white space after the left closure
//  processWhiteSpaces(new string(accChars.ToArray()), state);
//  accChars.Clear();
//}
//isFound = false;
//if (index >= desc.Length)
//  return;
//for (int i = index; i < desc.Length; ++i) {
//  char ch = desc[i];
//  if (ch != rightClosure)
//    accChars.Add(ch);
//  else {
//    isFound = true; //right closure found
//    break;
//  }
//}

//if (!isFound) {
//  processSimpleElement(desc.Substring(index), state, SectionType.Unknown);
//  return;
//}
//index += accChars.Count;
//if (accChars.Count > 0) { //process the content of the main item
//  string mainItem = new string(accChars.ToArray());
//  UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(mainItem, mainSeparator.ToString());
//  processMultipleRightExpression(exp, sideSeparator, state, leftType, rightType);
//  accChars.Clear();
//}
//processSymbol(rightClosure.ToString(), state);
//index++;
//isFound = false;
//if (index >= desc.Length)
//  return;

////Process final white space, if any
//processWhiteSpaces(desc.Substring(index), state);


//List<char> accChars = new List<char>();
//bool isFound = false;
//foreach (var ch in desc) {
//  if (ch != '[')
//    accChars.Add(ch);
//  else {
//    isFound = true; //left Closure Found
//    break;
//  }
//}

//if (!isFound) { //incorrect: consider all as unknown section
//  processSimpleElement(desc, state, SectionType.Unknown);
//  return;
//}

//int index = accChars.Count;
//if (accChars.Count > 0) { //process white space before the left closure
//  processWhiteSpaces(new string(accChars.ToArray()), state);
//  accChars.Clear();
//}
//processSymbol("[", state);
//index++;
//isFound = false;
//if (index >= desc.Length) //finish, nothing else
//  return;

////handle empty string before anything else that is not empty
//for (int i = index; i < desc.Length; ++i) {
//  char ch = desc[i];
//  if (char.IsWhiteSpace(desc[i]))
//    accChars.Add(ch);
//  else {
//    isFound = true;
//    break;
//  }
//}

//if (!isFound) { //incorrect, consider the rests as unknown section
//  processSimpleElement(desc.Substring(index), state, SectionType.Unknown);
//  return;
//}
//index += accChars.Count;
//if (accChars.Count > 0) { //process white space after the left closure
//  processWhiteSpaces(new string(accChars.ToArray()), state);
//  accChars.Clear();
//}
//isFound = false;
//if (index >= desc.Length)
//  return;

////Process the main item + extra white space before the end closure
//for (int i = index; i < desc.Length; ++i) {
//  char ch = desc[i];
//  if (ch != ']')
//    accChars.Add(ch);
//  else {
//    isFound = true; //right closure found
//    break;
//  }
//}

//if (!isFound) {
//  processSimpleElement(desc.Substring(index), state, SectionType.Unknown);
//  return;
//}
//index += accChars.Count;
//if (accChars.Count > 0) { //process the content of the main item
//  string mainItem = new string(accChars.ToArray());
//  List<string> components = mainItem.ParseComponents('|', true);
//  if (components.Count != 4) {
//    processSimpleElement(mainItem, state, SectionType.Unknown);
//  } else {
//    processUntrimmedElement(components[0], SectionType.ColumnName, state);
//    processSymbol("|", state);
//    processUntrimmedElement(components[1], SectionType.ColoringComparisonCode, state);
//    processSymbol("|", state);
//    processColoringComparisonExpression(components[2], state);
//    processSymbol("|", state);
//    processUntrimmedElement(components[3], SectionType.Color, state);
//  }
//  //must consists of four components
//  accChars.Clear();
//}
//processSymbol("]", state);
//isFound = false;
//if (index >= desc.Length)
//  return;

////Process final white space, if any
//processWhiteSpaces(desc.Substring(index), state);

//int index = 0;
//int processResult = processClosure(desc, state, leftClosure, index);
//if (processResult < 0)
//  return;
//index += processResult;
//if (index >= desc.Length) //finish, nothing else
//  return;

////handle trailing white space before item
//processResult = processTrailingWhiteSpaces(desc, state, index);
//if (processResult < 0)
//  return;
//index += processResult;
//if (index >= desc.Length) //finish, nothing else
//  return;

////Process the main item + extra white space before the end closure
//string mainItem = processMainItemClosure(desc, state, rightClosure, index);
//if (mainItem == null)
//  return;
//index += mainItem.Length;


////handle left closure
//int index = 0;
//int processResult = processClosure(desc, state, '[', index);
//if (processResult < 0)
//  return;
//index += processResult;
//if (index >= desc.Length) //finish, nothing else
//  return;

////handle trailing white space before item
//processResult = processTrailingWhiteSpaces(desc, state, index);
//if (processResult < 0)
//  return;
//index += processResult;
//if (index >= desc.Length) //finish, nothing else
//  return;

////Process the main item + extra white space before the end closure
//string mainItem = processMainItemClosure(desc, state, ']', index);
//if (mainItem == null)
//  return;
//index += mainItem.Length;

//if (component.Contains('+')) {
//  UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(component, "+");
//  processExpression(exp, state, SectionType.Value, rightType);
//} else if (component.Contains('-')) {
//  UntrimmedSimpleExpression exp = new UntrimmedSimpleExpression(component, "+");
//} else {
//}
