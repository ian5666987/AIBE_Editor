using Extension.String;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using logic = Aibe.Models.Tests.AibeSyntaxCheckerLogic;

namespace Aibe.Models.Docs {
  public class AibeTypingContext {
    public string GivenTableName { get; set; } //must be given outside
    public string WrittenTableName { get; private set; } //must be found from the text
    public string TableName { get { return WrittenTableName ?? GivenTableName; } }
    public string DirectTableContext { get { return RefWords == null || RefWords.Count <= 0 ? string.Empty : RefWords[0]; } } //can only be the first ref word
    //there must be exactly only the one ref word, so that this is the second
    public bool IsInDirectTableContext { get { return RefWords != null && (RefWords.Count == 1 || RefWords.Count == 2 || 
          (RefWords.Count > 0 && ParagraphContext != null && ParagraphContext == Aibe.DH.MICNForeignInfoColumns)); } }
    public bool IsInThisTableContext { get { return RefWords != null && RefWords.Count == 2 && (TypedChar == '=' || BreakChar == '='); } } //there must be exactly only the two ref words, so that this is the third
    public List<KeyValuePair<int, int>> Enclosures { get; private set; } = new List<KeyValuePair<int, int>>();
    public bool IsEnclosed { get; private set; } //to know if currently it is enclosed or in a value typing
    public int CurrentOpenEnclosureIndex { get; private set; } = -1;
    public List<KeyValuePair<int, int>> Parentheses { get; private set; } = new List<KeyValuePair<int, int>>();
    public bool IsParenthesed { get; private set; }
    public int CurrentOpenParenthesisIndex { get; private set; } = -1;
    public bool IsUserParenthesesContext { get; private set; }
    public string ParenthesesContext { get; private set; } = string.Empty;
    //to check if the item is in the Regex part
    public bool IsRegex { get { return ComponentRegexes.Count > 0 && ComponentRegexes.Any(x => x.Key < Position && x.Value >= Position); } }
    //to check if the item is in the Script part
    public bool IsScript { get { return ComponentScripts.Count > 0 && ComponentScripts.Any(x => x.Key < Position && x.Value >= Position); } }
    public bool IsBeginString { get; private set; } //used to indicate the very beginning position
    public bool IsBeginParagraph { get; private set; } //used to indicate position after ///---
    public bool IsBeginParagraphContent { get; private set; } //to indicate if it is in the beginning content
    public List<KeyValuePair<int, int>> ParagraphEnclosures { get; private set; }
    public List<KeyValuePair<int, int>> ParagraphParentheses { get; private set; }
    public List<KeyValuePair<int, int>> ComponentRegexes { get; private set; }
    public List<KeyValuePair<int, int>> ComponentScripts { get; private set; }
    public int ParagraphIndex { get; private set; } //to get the paragraph position index
    public int NextParagraphIndex { get; private set; } //to get the paragraph position index
    public int ParagraphOffsetPosition { get; private set; } //to get the current offset according to the current paragraph
    public int ParagraphEndHeaderPosition { get; private set; } //tp get where the paragraph header ends
    public int ParagraphContentOffset { get; private set; } //to indicate where the content of the paragraph is
    public string ParagraphContext { get; private set; } //to indicate in which paragraph type or context it is currently in
    public string Paragraph { get; private set; } //to indicate the current paragraph
    public List<int> ParagraphComponentIndices { get; private set; } = new List<int>(); //to indicate the components of the paragraph
    public string ParagraphFirstWord { // [{word
      get {
        if (ParagraphContentOffset <= 0 || string.IsNullOrWhiteSpace(Paragraph) || string.IsNullOrWhiteSpace(ParagraphContext))
          return null;
        int offset = ParagraphContentOffset - ParagraphIndex;
        if (offset >= Paragraph.Length) //cannot take the first word
          return null;
        string content = Paragraph.Substring(offset);
        int firstNonWordIndexEnds = -1;
        int secondNonWordIndexStarts = -1;
        for (int i = 0; i < content.Length; ++i) {
          char ch = content[i];
          bool isNonWord = DefaultBreakChars.Any(x => x == ch) || char.IsWhiteSpace(ch);
          if (isNonWord) {//as long as it is non word character
            if(firstNonWordIndexEnds < 0) //if the first non index is not found, then just skips this character
              continue;
            else { //after the first non index found and this is non word, thus is where the secondNonWord index starts
              secondNonWordIndexStarts = i;
              break; //not needed to proceed anymore
            }
          }
          if (firstNonWordIndexEnds < 0) //if it is a word character, immediately we ends the first nonWordIndex
            firstNonWordIndexEnds = i;
        }
        if (firstNonWordIndexEnds < 0) { //all is nonWord!
          return null; //we then do not know what is the first word
        } else if (secondNonWordIndexStarts < 0) { //second non word not found, only depends on the first word
          return content.Substring(firstNonWordIndexEnds);
        } else { //second word found, takes the both
          return content.Substring(firstNonWordIndexEnds, secondNonWordIndexStarts - firstNonWordIndexEnds);
        }
      }
    }
    public int ComponentIndex { get; private set; }
    public int NextComponentIndex { get; private set; }
    public int EndComponentIndex { get; private set; }
    public string ComponentText { get; private set; }
    public int ComponentLength { get; private set; }
    public List<KeyValuePair<int, char>> ContextBreaks { get; private set; } = new List<KeyValuePair<int, char>>(); //to indicate all breaks in the current component, given the position
    public string SCRefTableName { get; private set; }
    public string TypedWord { get; private set; } //to indicate the currently typed word
    public string FullWord { get; private set; } //to indicate the currently typed full word
    public char? TypedChar { get { if (CanTakeTypedChar) return FullText[Position-1]; return null; } } //to indicate exactly the typed character
    public char? BreakChar { get; private set; } //to indicate the last break character of the item
    public int BreakPosition { get; private set; } = -1; //to indicate the break position in the paragraph, if any
    public char? BreakCharBefore { get; private set; } //to indicate the last break character of the item
    public int BreakPositionBefore { get; private set; } = -1; //to indicate the break position in the paragraph, if any
    public List<string> RefWords { get; private set; } = new List<string>(); //to collect ref:ref:ref:ref style
    public string FullText { get; private set; } //given
    public int Position { get; private set; } //to indicate the current position in the text
    public bool CanTakeTypedChar { get { return !string.IsNullOrEmpty(FullText) && Position - 1 >= 0; } }
    public List<int> ParIndices { get; private set; } = new List<int>(); //paragraph indices
    public List<SectionType> PossibleSectionTypes { get; private set; } = new List<SectionType>();
    public SectionType DirectTypedWordMatch { get { return GetDirectTypedWordMatch(); } }
    public bool IsUpdating { get; private set; }

    //privates to be initialized
    private List<char> startStringComponents = AibeDocument.StartString.Distinct().ToList();
    private List<char> nonLetterDigitWordCharsAllowed = new List<char> {
      '_', //this will insert more on initalization
    };

    //public readonly static
    //public readonly static List<string> RegexMetaItemColumnNames = new List<string> {
    //  "RegexCheckedColumns", "RegexCheckedColumnExamples",
    //};
    public static readonly List<char> DefaultBreakChars = new List<char> {
      ':', '|', ',', '#', ';', //The aposthrope and quotation marks should not be default break chars
      '[', ']', '{', '}',
      '(', ')', '<', '>',
      '=', '/' }; //+ and - also cannot be the default break chars
    //public static readonly List<string> TVRBreakColumns = new List<string> { "FilterDropDownLists", "CreateEditDropDownLists", "ListColumns" };
    //public static readonly List<string> VBarBreakColumns = new List<string> { "ColoringList", "TimeStampColumns", "HistoryTriggers",
    //  "ScriptConstructorColumns", "CustomDateTimeFormatColumns", "PreActionTriggers", "PostActionTriggers" };

    public AibeTypingContext() {
      nonLetterDigitWordCharsAllowed.AddRange(startStringComponents.ToArray());
    }

    public void UpdateContext (string text, int position) {
      if (IsUpdating)
        Thread.Sleep(10);
      IsUpdating = true;
      FullText = text;
      Position = position;
      ParIndices = text.IndicesOf(AibeDocument.StartString, allowEmpty: true);
      int index = text.IndexOf(AibeDocument.TableHeader);
      if (index >= 6) { //must be at least index of 6 to be sure there is a table header
        string par = getThisPar(index);
        WrittenTableName = string.IsNullOrWhiteSpace(par) ? null :
          par.Substring(AibeDocument.StartString.Length).TrimStart()
            .Substring(AibeDocument.TableHeader.Length).Trim();
        if (string.IsNullOrWhiteSpace(WrittenTableName))
          WrittenTableName = null; //must be set to null to allow Null-coalescing ?? to work on TableName
      } else
        WrittenTableName = null;
      Enclosures.Clear();
      IsEnclosed = false;
      CurrentOpenEnclosureIndex = -1;
      foreach (var parIndex in ParIndices) {
        string par = getThisPar(parIndex);
        var encs = getEnclosures(par, parIndex);
        Enclosures.AddRange(encs);
        IsEnclosed = getIsEnclosed(); //done after the enclosures are found
        CurrentOpenEnclosureIndex = getClosestEnclosureIndex(position);
      }
      Parentheses.Clear();
      IsParenthesed = false;
      IsUserParenthesesContext = false;
      ParenthesesContext = string.Empty;
      CurrentOpenParenthesisIndex = -1;
      foreach (var parIndex in ParIndices) {
        string par = getThisPar(parIndex);
        var parents = getParentheses(par, parIndex);
        Parentheses.AddRange(parents);
        IsParenthesed = getIsParenthesed(); //done after the Parentheses are found
        updateParenthesesContext(); //must be updated after the parentheses are known and after IsParenthesed is known
      }
      UpdatePosition(position);
      IsUpdating = false;
    }

    public void UpdatePosition (int position) {
      TypedWord = GetTypedWord();
      FullWord = GetFullWord(); //must be put after TypedWord is obtained
      UpdateParagraph(position);
      UpdateComponent();
      UpdateSymbols();
      PossibleSectionTypes = GetPossibleSectionTypes(); //must be the very last
    }

    public void UpdateParagraph(int position) {
      ParagraphIndex = getClosestParIndex(position);
      NextParagraphIndex = getNextParIndex(position);
      Paragraph = getThisPar(position); //after the two indices
      ParagraphContext = getParType(); //after the two indices
      ParagraphOffsetPosition = Position - ParagraphIndex;
      ParagraphEnclosures = getThisParEnclosuresOnly(); //after ParIndices (very early done), Enclosures (very early done), and Paragraph
      ParagraphParentheses = getThisParParenthesesOnly(); //after ParIndices (very early done), Parentheses (very early done), and Paragraph
      ParagraphComponentIndices.Clear();
      ContextBreaks.Clear();
      ParagraphContentOffset = -1;
      ParagraphEndHeaderPosition = -1;
      IsBeginString = false;
      IsBeginParagraph = false;
      IsBeginParagraphContent = false;
      SCRefTableName = null;
      if (string.IsNullOrWhiteSpace(Paragraph) || //empty paragraph must necessarily be begin string
        ParagraphOffsetPosition < 6) { //Paragraph.Length < 6 not needed to be checked since it is checked together with offset < 6. Short paragraph must necessarily begin string
        IsBeginString = true;
        return; //cannot continue for such paragraph
      }

      string untrimmedCurrentText = Paragraph.Substring(0, ParagraphOffsetPosition); //Get untrimmed current text
      int index = untrimmedCurrentText.IndexOf(AibeDocument.StartString); //must be at least 6 to check here
      if (index < 0) { //if the start string does not exist, not found under then currently texted position, then assume begin string
        IsBeginString = true;
        return; //cannot continue for such paragraph
      }

      //index is found, continue the evaluation
      if (untrimmedCurrentText.Length == AibeDocument.StartString.Length || //if it has identical length
        untrimmedCurrentText.Trim() == AibeDocument.StartString) { //or trimmed item is the startString
        IsBeginParagraph = true; //definitely a begin paragraph
        return;
      }

      string currentText = untrimmedCurrentText.Substring(index + AibeDocument.StartString.Length).TrimStart(); //this is the paragraph where it started after ///---
      if (string.IsNullOrWhiteSpace(currentText) || !currentText.Any(x => char.IsWhiteSpace(x))) {
        IsBeginParagraph = true;
        return;
      }

      if (string.IsNullOrWhiteSpace(ParagraphContext)) //must have paragraph context before it can progress
        return;

      if (ParagraphContext == Aibe.DH.MICNScriptConstructorColumns) {
        string scPrefix = Aibe.DH.ScRefTableNameAttribute + ":";
        index = currentText.IndexOf(scPrefix);
        if (index >= 0 && currentText.Length > scPrefix.Length) {
          string scRefTableNameText = currentText.Substring(scPrefix.Length).TrimStart();
          int lastIndex = scRefTableNameText.IndexOfAny(DefaultBreakChars.ToArray());
          SCRefTableName = lastIndex >= 0 ? scRefTableNameText.Substring(0, lastIndex) : scRefTableNameText;
        }
      }

      index = Paragraph.IndexOf(ParagraphContext) + ParagraphContext.Length;
      if (index >= Paragraph.Length)
        return;

      string untrimmedContent = Paragraph.Substring(index);
      string content = untrimmedContent.TrimStart();
      int diff = untrimmedContent.Length - content.Length;
      ParagraphEndHeaderPosition = ParagraphIndex + index;
      ParagraphContentOffset = ParagraphIndex + index + diff;

      bool regexPar = Aibe.DH.MICNGroupTagged.Any(x => ParagraphContext.Equals(x));
      if (regexPar) { //special way to find the items
        string tag = ParagraphContext.Equals(Aibe.DH.MICNRegexCheckedColumns) ?
          Aibe.DH.RegexCheckedColumnTag : Aibe.DH.RegexCheckedColumnExampleTag;
        List<string> components = content.ParseStrictTagUntrimmed(tag); //returns tag, empty, or the whole content
        string startTag = "<" + tag + ">";
        string endTag = "</" + tag + ">";
        int compIndex = 0;
        foreach (var component in components) {
          if (component == startTag) //means new component
            ParagraphComponentIndices.Add(compIndex + ParagraphIndex); //add the current compIndex
          compIndex += component.Length;
        }
      } else //normal, all other paragraphs are obtained this way
        ParagraphComponentIndices = content.ComponentIndices(';', true)
        .Select(x => x + ParagraphContentOffset).ToList();

      List<char> breaks = ContextBreakSectionDicts.ContainsKey(ParagraphContext) ?
        ContextBreakSectionDicts[ParagraphContext].Select(x => x.BreakChar).ToList() :
        new List<char>();

      if (DefaultBreakChars.Count > 0) {
        //bool hasSegment = !nonSegmentedMetaItemColumnName.Any(x => x.Equals(ParagraphContext));
        //if (!regexPar && hasSegment && !breaks.Contains(';'))
        //  breaks.Add(';'); //do not insert this if the meta item column does not have segment
        ContextBreaks = content.GetBreaks(breaks, ParagraphContentOffset);
      }

      if (ParagraphEndHeaderPosition < 0)
        return;

      int lengthDiff = Position - ParagraphEndHeaderPosition; //check the different in length between the paragraph end header and the current position

      if (lengthDiff <= 0) //if there is no difference, then it cannot be
        return;

      string testText = string.Empty;
      if (FullText.Length <= ParagraphEndHeaderPosition + lengthDiff) { //exceeding the position, 
        testText = FullText.Substring(ParagraphEndHeaderPosition);
        IsBeginParagraphContent = string.IsNullOrWhiteSpace(testText);
        return;
      }
      testText = FullText.Substring(ParagraphEndHeaderPosition, lengthDiff);
      IsBeginParagraphContent = string.IsNullOrWhiteSpace(testText) || //Similar to begin pargraph, but... 
        (!testText.TrimStart().Any(x => char.IsWhiteSpace(x)) &&  //it has additional check on symbol break, 
        !testText.Any(x => DefaultBreakChars.Contains(x))); //thus " text" is beginning but " text;" is not beginning
    }

    public void UpdateComponent() {
      ComponentIndex = -1;
      NextComponentIndex = -1;
      EndComponentIndex = -1;
      ComponentText = string.Empty;
      ComponentRegexes = new List<KeyValuePair<int, int>>();
      ComponentScripts = new List<KeyValuePair<int, int>>();
      ComponentLength = 0;
      if (!ParagraphComponentIndices.Any())
        return;
      var components = ParagraphComponentIndices.Where(x => x <= Position);
      if (!components.Any()) //cannot progress
        return;
      ComponentIndex = ParagraphComponentIndices.Where(x => x <= Position).Max(); //must necessarily exist
      bool hasNextComponentIndex = ParagraphComponentIndices.Any(x => x > Position);
      if (hasNextComponentIndex)
        NextComponentIndex = ParagraphComponentIndices.Where(x => x > Position).Min(); //minimum among things greater
      EndComponentIndex = hasNextComponentIndex ? NextComponentIndex : NextParagraphIndex >= 0 ? NextParagraphIndex : -1;
      ComponentLength = EndComponentIndex > 0 ? EndComponentIndex - ComponentIndex : FullText.Length - ComponentIndex; //aa67, index = 1, length = 4
      if (ComponentLength <= 0) //cannot be helped at all
        return;
      ComponentText = FullText.Substring(ComponentIndex, ComponentLength);
      ComponentRegexes = getThisComponentRegexes(); //must be after ParagraphContext, ComponentIndices, and ComponentIndex-NextComponentIndex
      ComponentScripts = getThisComponentScripts(); //must be after ParagraphContext, ComponentIndices, and ComponentIndex-NextComponentIndex
    }

    public void UpdateSymbols() {
      BreakChar = null;
      BreakPosition = -1;
      BreakCharBefore = null;
      BreakPositionBefore = -1;
      RefWords.Clear();
      if (IsEnclosed)
        return; //if it is enclosed, it cannot be break char and cannot have reference words
      UpdateBreakCharAndRefs();
    }

    public List<SectionType> GetPossibleSectionTypes() {
      List<SectionType> sectionTypes = new List<SectionType>();
      if (IsEnclosed) //enclosed items cannot be checked, immediately returns as value
        return new List<SectionType>() { SectionType.Text };
      if (IsBeginString)
        return new List<SectionType>() { SectionType.NewParagraph };
      if (IsBeginParagraph) { //return the table name prefix and the meta item column directly
        sectionTypes.Add(SectionType.NewTableNamePrefix); //there is no paragraph context, it cannot be known what it is, just assume there isn't any yet
        sectionTypes.Add(SectionType.MetaItemColumnName);
        return sectionTypes;
      }
      bool hasSets = ContextBreakSectionDicts.ContainsKey(ParagraphContext);
      if (IsBeginParagraphContent) { //context will be there
        if (hasSets) {
          var sectDict = ContextBreakSectionDicts[ParagraphContext];
          if (sectDict != null && sectDict.Count > 0)
            return sectDict[0].SectionTypes; //if found, should always returns the first one, that is, the default section type
        }
        return new List<SectionType>() { }; //else, just returns the empty section types
      }

      if (IsRegex) //returns regex whenever it is found
        return new List<SectionType>() { SectionType.Regex };
      if (IsScript) //returns script whenever it is found
        return new List<SectionType>() { SectionType.SQLScript };

      if (IsParenthesed) { //inside the parentheses
        if (ParagraphContext == Aibe.DH.MICNPreActionProcedures || ParagraphContext == Aibe.DH.MICNPostActionProcedures) {
          if (BreakChar == '=')
            return new List<SectionType> { SectionType.Parameter, SectionType.Value };
          if (BreakChar == ',' || BreakChar == '(')
            return new List<SectionType> { IsUserParenthesesContext ?
              SectionType.UserStoredProcedureParameter : SectionType.DataStoredProcedureParameter };
        }
      }

      bool hasJustTypedBreak = TypedChar != null && DefaultBreakChars.Contains(TypedChar.Value);
      char? usedChar = hasJustTypedBreak ? TypedChar : BreakChar;
      int usedPosition = hasJustTypedBreak ? Position : BreakPosition;
      if (usedChar == '/' &&
        ((hasJustTypedBreak && usedPosition > 1 && FullText[usedPosition - 2] == '/') ||
        (!hasJustTypedBreak && usedPosition > 0 && FullText[usedPosition - 1] == '/'))) //always return the new paragraph is two consecutive / are typed.
        return new List<SectionType>() { SectionType.NewParagraph };

      if (!hasSets || !usedChar.HasValue) //does not have the set, just return value or there isn't any break char to be checked for
        return new List<SectionType>() { };
      var sets = ContextBreakSectionDicts.FirstOrDefault(x => x.Key.EqualsIgnoreCase(ParagraphContext)).Value;
      AibeBreakSet breakSet = sets.FirstOrDefault(x => x.BreakChar == usedChar.Value);
      if (breakSet == null) //matching break char is not found, assume value
        return new List<SectionType>() { };
      sectionTypes = breakSet.SectionTypes != null && breakSet.SectionTypes.Count > 0 ?
        breakSet.SectionTypes : new List<SectionType>() { };
      List<SectionType> exclusions = null;
      if (Aibe.DH.MICNGroupTagged.Contains(ParagraphContext)) { //regex context
        if (BreakChar == '>')
          exclusions = BreakCharBefore == '/' ? new List<SectionType> { SectionType.ThisTableColumnName } : //remove this table column name possibilities when it is the end of the regex
            new List<SectionType> { SectionType.Symbol };  //inside the regex, then removes the symbol possibilities, though it is syntatically possibly to do <reg></reg>
      } else if (RefWords != null) { //elimination of some possibilities
        int count = RefWords.Count;
        if (ParagraphContext == Aibe.DH.MICNCustomDateTimeFormatColumns)
          if (count % 2 == 1)
            exclusions = new List<SectionType> { SectionType.RowAction, SectionType.Page };
          else
            exclusions = new List<SectionType> { SectionType.DateTimeFormat };
        switch (count) {
          case 0:
            if (Aibe.DH.MICNGroupTVR.Contains(ParagraphContext) && BreakChar == '=')
              exclusions = BreakCharBefore == ':' ? new List<SectionType> { SectionType.ListType } :
                new List<SectionType> { SectionType.ThisTableColumnName, SectionType.StaticValue };
            else if (ParagraphContext == Aibe.DH.MICNListColumns && BreakChar == '|')
              exclusions = new List<SectionType> { BreakCharBefore == '=' ? SectionType.Text : SectionType.TableName };
            break;
          case 1:
            if (ParagraphContext == Aibe.DH.MICNColoringList) {
              if (BreakChar == '|')
                exclusions = new List<SectionType> { SectionType.AggregateName, SectionType.TableName, SectionType.Value, SectionType.Color, SectionType.Now };
              else if (BreakChar == ':')
                exclusions = new List<SectionType> { SectionType.Self, SectionType.Value, };
            } else if (Aibe.DH.MICNGroupTVR.Contains(ParagraphContext)) {
              if (BreakChar == ':')
                exclusions = new List<SectionType> { SectionType.Skip, SectionType.ThisTableColumnName, SectionType.Value, SectionType.SQLScript };
              else if (ParagraphContext == Aibe.DH.MICNListColumns && BreakChar == '|')
                exclusions = new List<SectionType> { BreakCharBefore == '=' ? SectionType.Text : SectionType.TableName };
            } else if (ParagraphContext == Aibe.DH.MICNTimeStampColumns && BreakChar == '|')
              exclusions = new List<SectionType> { SectionType.TrueFalse };
            else if (ParagraphContext == Aibe.DH.MICNHistoryTriggers) //no matter what is the previous break char, just removes the true false as long as we are in the second place
              exclusions = new List<SectionType> { SectionType.TrueFalse };
            else if (ParagraphContext == Aibe.DH.MICNPreActionTriggers || ParagraphContext == Aibe.DH.MICNPostActionTriggers)
              exclusions = new List<SectionType> { BreakCharBefore == ',' ? SectionType.TriggerRowAction : SectionType.TrueFalse };
            else if (ParagraphContext == Aibe.DH.MICNScriptConstructorColumns && BreakChar == ':')
              exclusions = new List<SectionType> { DirectTableContext == Aibe.DH.ScRefTableNameAttribute ? SectionType.ScRefColumnName : SectionType.TableName };
            break;
          case 2:
            if (ParagraphContext == Aibe.DH.MICNColoringList) {
              if (BreakChar == '|')
                exclusions = new List<SectionType> { SectionType.ColoringComparisonCode, SectionType.Color, };
              else if (BreakChar == ':') //different case now
                exclusions = new List<SectionType> { SectionType.ColumnName, };
            } else if (ParagraphContext == Aibe.DH.MICNTimeStampColumns && BreakChar == '|')
              exclusions = new List<SectionType> { SectionType.Value };
            else if (Aibe.DH.MICNGroupTVR.Contains(ParagraphContext)) {
              if (BreakChar == ':')
                exclusions = new List<SectionType> { SectionType.ThisTableColumnName, SectionType.SQLScript };
              else if (ParagraphContext == Aibe.DH.MICNPreActionTriggers || ParagraphContext == Aibe.DH.MICNPostActionTriggers)
                exclusions = new List<SectionType> { SectionType.TriggerRowAction };
            } else if (ParagraphContext == Aibe.DH.MICNPreActionTriggers || ParagraphContext == Aibe.DH.MICNPostActionTriggers)
              exclusions = new List<SectionType> { SectionType.TriggerRowAction };
            break;
          case 3:
            if (ParagraphContext == Aibe.DH.MICNColoringList && BreakChar == '|') {
              exclusions = new List<SectionType> { SectionType.ColoringComparisonCode, SectionType.AggregateName, SectionType.TableName, SectionType.Value, SectionType.Now };
            } else if (Aibe.DH.MICNGroupTVR.Contains(ParagraphContext))
              exclusions = new List<SectionType> { SectionType.ColumnName, SectionType.ThisTableColumnName, SectionType.Skip, SectionType.Value };
            break;
        }

        if (ParagraphContext == Aibe.DH.MICNForeignInfoColumns && BreakChar == '|')          
          exclusions = new List<SectionType> { BreakCharBefore == ',' ? SectionType.ThisTableColumnName : SectionType.Text };

        if (ParagraphContext == Aibe.DH.MICNHistoryTriggers && BreakChar == ',') {
          bool rightSide = BreakCharBefore == ',' || BreakCharBefore == '|';
          if (exclusions == null)
            exclusions = new List<SectionType> { rightSide ? SectionType.TrueFalse : SectionType.TriggerRowAction };
          else
            exclusions.Add(rightSide ? SectionType.TrueFalse : SectionType.TriggerRowAction);
        }
      }
      if (exclusions != null)
        sectionTypes = sectionTypes.Except(exclusions).ToList();
      return sectionTypes;
    }

    public SectionType GetDirectTypedWordMatch(string input = null) { //to get the direct match of the typed word to the section type
      string word = input;
      if (string.IsNullOrWhiteSpace(input)) {
        if (string.IsNullOrWhiteSpace(TypedWord))
          return SectionType.Unknown;
        if (IsEnclosed) //enclosed cannot be checked
          return SectionType.Value;
        word = TypedWord;
      }
      //Get the section type based on the word alone
      if (logic.ConfiguredTableNames.Any(x => x == word))
        return SectionType.TableName;
      if (logic.UserColumnNames.Any(x => x == word))
        return SectionType.UserColumnName;
      if (logic.MetaItemColumnNames.Any(x => x == word))
        return SectionType.MetaItemColumnName;
      if (Aibe.DH.DefaultRowActions.Any(x => x.EqualsIgnoreCase(word)))
        return SectionType.RowAction;
      if (Aibe.DH.DefaultGroupByRowActions.Any(x => x.EqualsIgnoreCase(word)))
        return SectionType.RowAction;
      if (Aibe.DH.BaseTriggerRowActions.Any(x => x.EqualsIgnoreCase(word)))
        return SectionType.TriggerRowAction;
      if (Aibe.DH.DefaultTableActions.Any(x => x.EqualsIgnoreCase(word)))
        return SectionType.TableAction;
      if (logic.CompleteRoles.Any(x => x == word))
        return SectionType.Role;
      if (Aibe.DH.AcceptablePageNames.Any(x => x.EqualsIgnoreCase(word)))
        return SectionType.Page;
      if (Aibe.DH.ValidOrderDirections.Any(x => x.EqualsIgnoreCase(word)))
        return SectionType.OrderByDirective;
      if (Aibe.DH.UserRelatedDirectives.Any(x => x.EqualsIgnoreCase(word)))
        return SectionType.UserRelatedDirective;
      if (logic.Tags.Any(x => x == word))
        return SectionType.Tag;
      if (Aibe.DH.ValidComparatorSigns.Any(x => x.EqualsIgnoreCase(word)))
        return SectionType.ColoringComparisonCode;
      if (Aibe.DH.AggregateNames.Any(x => x.EqualsIgnoreCase(word)))
        return SectionType.AggregateName;
      if (logic.Colors.Any(x => x == word))
        return SectionType.Color;
      if (Aibe.DH.ScAttributes.Any(x => x.EqualsIgnoreCase(word)))
        return SectionType.ScriptConstructorAttribute;
      if (Aibe.DH.TrueFalse.Any(x => x.EqualsIgnoreCase(word)))
        return SectionType.TrueFalse;
      if (logic.TemplateNames.Any(x => x == word))
        return SectionType.TemplateName;
      if (Aibe.DH.TableTypes.Any(x => x.EqualsIgnoreCase(word)))
        return SectionType.TableType;
      if (logic.TableSources.Any(x => x.EqualsIgnoreCase(word)))
        return SectionType.TableSource;
      if (Aibe.DH.Skip.EqualsIgnoreCase(word))
        return SectionType.Skip;
      if (Aibe.DH.Self.EqualsIgnoreCase(word))
        return SectionType.Self;
      if (Aibe.DH.Now.EqualsIgnoreCase(word))
        return SectionType.Now;
      if (Aibe.DH.Cid.EqualsIgnoreCase(word))
        return SectionType.Cid;
      if (Aibe.DH.AutoGeneratedHRTSWord == word)
        return SectionType.AutoGenerated;
      if (Aibe.DH.SQLScriptDirectivePrefix == word)
        return SectionType.SQLScriptPrefix;
      if (Aibe.DH.UserPrefix == word)
        return SectionType.UserDBReferencePrefix;
      if (AibeDocument.TableHeader == word)
        return SectionType.NewTableNamePrefix;
      if (AibeDocument.StartString == word)
        return SectionType.NewParagraph;
      if (logic.Keywords.Any(x => x == word)) //must be after some more specific keywords
        return SectionType.Keyword;
      if (Aibe.DH.MinMax.Any(x => x.EqualsIgnoreCase(word)))
        return SectionType.MinMax;
      if (Aibe.DH.GroupByAutoDirectives.Any(x => x.EqualsIgnoreCase(word)))
        return SectionType.GroupByAutoDirective;
      if (logic.DataTableColumnNames.Any(x => x.EqualsIgnoreCase(word)))
        return SectionType.ColumnName;
      if (logic.UserTableColumnNames.Any(x => x.EqualsIgnoreCase(word)))
        return SectionType.UserColumnName;
      if (logic.DataProcedureParameterNames.Any(x => x.EqualsIgnoreCase(word)))
        return SectionType.DataStoredProcedureParameter;
      if (logic.UserProcedureParameterNames.Any(x => x.EqualsIgnoreCase(word)))
        return SectionType.UserStoredProcedureParameter;
      return SectionType.Unknown;
    }

    private static List<SectionType> specialColumnSectionTypes = new List<SectionType> {
      SectionType.HistoryColumnName, SectionType.ScRefColumnName, SectionType.ThisTableColumnName, SectionType.ColumnName,
    };
    private static List<SectionType> specialProcedureParameterSectionTypes = new List<SectionType> {
      SectionType.DataStoredProcedureParameter, SectionType.UserStoredProcedureParameter,
    };
    public static List<string> GetSelectedSuggestionsBasedOnSections(List<SectionType> sectionTypes, 
      string refTable, string spName, bool getAllPossibilities = false) {
      List<string> suggestions = new List<string>();
      foreach (var sectionType in sectionTypes) {
        if (specialColumnSectionTypes.Contains(sectionType)) { //special column name
          if (getAllPossibilities) {
            foreach (var table in logic.DataDBTableColumns.Keys) {
              var sugs = GetSuggestionsBasedOnSections(sectionType, table);
              if (sugs != null && sugs.Count > 0)
                suggestions.AddRange(sugs);
            }
            foreach (var table in logic.UserDBTableColumns.Keys) {
              var sugs = GetSuggestionsBasedOnSections(sectionType, table);
              if (sugs != null && sugs.Count > 0)
                suggestions.AddRange(sugs);
            }
          } else {
            var sugs = GetSuggestionsBasedOnSections(sectionType, refTable);
            if (sugs != null && sugs.Count > 0)
              suggestions.AddRange(sugs);
          }
          continue;
        }

        if (specialProcedureParameterSectionTypes.Contains(sectionType)) { //special procedure parameter name
          if (getAllPossibilities) {
            foreach(var procedure in logic.DataDBProcedureParameterNames.Keys) {
              var sugs = GetSuggestionsBasedOnSections(sectionType, procedure);
              if (sugs != null && sugs.Count > 0)
                suggestions.AddRange(sugs);
            }
            foreach (var procedure in logic.UserDBProcedureParameterNames.Keys) {
              var sugs = GetSuggestionsBasedOnSections(sectionType, procedure);
              if (sugs != null && sugs.Count > 0)
                suggestions.AddRange(sugs);
            }
          } else {
            var sugs = GetSuggestionsBasedOnSections(sectionType, spName);
            if (sugs != null && sugs.Count > 0)
              suggestions.AddRange(sugs);
          }
        }

        var secSugs = GetSuggestionsBasedOnSections(sectionType, null);
        if (secSugs != null && secSugs.Count > 0)
          suggestions.AddRange(secSugs);
      }
      suggestions = suggestions.Distinct().ToList(); //only gets distinct items
      return suggestions;
    }

    public static List<string> GetCompleteSuggestions() {
      List<SectionType> sectionTypes = Enum.GetValues(typeof(SectionType)).Cast<SectionType>().ToList();
      return GetSelectedSuggestionsBasedOnSections(sectionTypes, null, null, getAllPossibilities: true);
    }

    public static List<string> GetSuggestionsBasedOnSections(SectionType sectionType, string currentContext = null) {
      List<string> items = new List<string>();
      switch (sectionType) {
        case SectionType.TableName: return logic.ConfiguredTableNames;
        case SectionType.ColumnName:
        case SectionType.HistoryColumnName: //if it can be obtained, then so be it...
        case SectionType.ThisTableColumnName: //not different from column name here, but this helps to indicate it in PargraphSection and TypingContext
        case SectionType.ScRefColumnName:
          if (string.IsNullOrEmpty(currentContext))
            return null;
          if (logic.DataDBTableColumns.Keys.Any(x => x.EqualsIgnoreCase(currentContext)))
            return logic.DataDBTableColumns.FirstOrDefault(x => x.Key.EqualsIgnoreCase(currentContext)).Value;
          if (logic.UserDBTableColumns.Keys.Any(x => x.EqualsIgnoreCase(currentContext)))
            return logic.UserDBTableColumns.FirstOrDefault(x => x.Key.EqualsIgnoreCase(currentContext)).Value;
          break;
        case SectionType.DataStoredProcedureParameter:
          if (string.IsNullOrEmpty(currentContext))
            return null;
          if (logic.DataDBProcedureParameterNames.Keys.Any(x => x.EqualsIgnoreCase(currentContext)))
            return logic.DataDBProcedureParameterNames.FirstOrDefault(x => x.Key.EqualsIgnoreCase(currentContext)).Value;
          break;
        case SectionType.UserStoredProcedureParameter:
          if (string.IsNullOrEmpty(currentContext))
            return null;
          if (logic.UserDBProcedureParameterNames.Keys.Any(x => x.EqualsIgnoreCase(currentContext)))
            return logic.UserDBProcedureParameterNames.FirstOrDefault(x => x.Key.EqualsIgnoreCase(currentContext)).Value;          
          break;
        case SectionType.UserColumnName: return logic.UserColumnNames;
        case SectionType.MetaItemColumnName: return logic.MetaItemColumnNames;
        case SectionType.RowAction:
          List<string> rowActions = Aibe.DH.DefaultRowActions.Union(Aibe.DH.DefaultGroupByRowActions).ToList();
          return rowActions;
        case SectionType.TriggerRowAction: return Aibe.DH.BaseTriggerRowActions;
        case SectionType.TableAction: return Aibe.DH.DefaultTableActions;
        case SectionType.Role: return logic.CompleteRoles;
        case SectionType.Page: return Aibe.DH.AcceptablePageNames;
        case SectionType.OrderByDirective: return Aibe.DH.ValidOrderDirections;
        case SectionType.UserRelatedDirective: return Aibe.DH.UserRelatedDirectives;
        case SectionType.Keyword: return logic.Keywords;
        case SectionType.Tag: return logic.Tags;
        case SectionType.ColoringComparisonCode: return Aibe.DH.ValidComparatorSigns;
        case SectionType.AggregateName: return Aibe.DH.AggregateNames;
        case SectionType.Color: return logic.Colors;
        case SectionType.ScriptConstructorAttribute: return Aibe.DH.ScAttributes;
        case SectionType.TrueFalse: return Aibe.DH.TrueFalse;
        case SectionType.TemplateName: return logic.TemplateNames;
        case SectionType.TableType: return Aibe.DH.TableTypes;
        case SectionType.TableSource: return logic.TableSources;
        case SectionType.Skip: return new List<string> { Aibe.DH.Skip };
        case SectionType.Self: return new List<string> { Aibe.DH.Self };
        case SectionType.Now: return new List<string> { Aibe.DH.Now };
        case SectionType.Cid: return new List<string> { Aibe.DH.Cid };
        case SectionType.AutoGenerated: return new List<string> { Aibe.DH.AutoGeneratedHRTSWord };
        case SectionType.SQLScriptPrefix: return new List<string> { Aibe.DH.SQLScriptDirectivePrefix };
        case SectionType.UserDBReferencePrefix: return new List<string> { Aibe.DH.UserPrefix };
        case SectionType.DataStoredProcedure:
          items = logic.DataDBProcedureParameterNames.Keys.ToList();
          items.AddRange(logic.DataDBProcedureSignatures.Values);
          return items;
        case SectionType.UserStoredProcedure:
          items = logic.UserDBProcedureParameterNames.Keys.ToList();
          items.AddRange(logic.UserDBProcedureSignatures.Values);
          return items;
        case SectionType.NewTableNamePrefix: return new List<string> { AibeDocument.TableHeader };
        case SectionType.NewParagraph: return new List<string> { AibeDocument.StartString };
        case SectionType.MinMax: return Aibe.DH.MinMax;
        case SectionType.GroupByAutoDirective: return Aibe.DH.GroupByAutoDirectives;
      }
      return null;
    }

    public int GetMostSuggestedIndex(List<string> suggestions, string word = null) {
      string usedWord = word ?? TypedWord;
      if (usedWord == null)
        return -1;
      return usedWord.MostMatchedIndex(suggestions);
    }

    public List<string> GetSuggestions() {
      List<string> suggestions = new List<string>();
      bool hasDirectTableContext = !string.IsNullOrWhiteSpace(DirectTableContext); //check first if it has direct table context
      string refTable = hasDirectTableContext && IsInDirectTableContext ? DirectTableContext : IsInThisTableContext ? TableName : null;
      foreach (var sect in PossibleSectionTypes) {
        List<string> sugs;
        switch (sect) {
          case SectionType.HistoryColumnName: sugs = GetSuggestionsBasedOnSections(sect, ParagraphFirstWord); break; //special one
          case SectionType.ThisTableColumnName: sugs = GetSuggestionsBasedOnSections(sect, TableName); break;
          case SectionType.ColumnName: sugs = GetSuggestionsBasedOnSections(sect, refTable); break; //special one
          case SectionType.ScRefColumnName: sugs = GetSuggestionsBasedOnSections(sect, SCRefTableName); break; //special one
          case SectionType.DataStoredProcedureParameter: //special one
          case SectionType.UserStoredProcedureParameter: //special one
            sugs = GetSuggestionsBasedOnSections(sect, ParenthesesContext); break; 
          default: sugs = GetSuggestionsBasedOnSections(sect, null); break;
        }
        if (sugs != null)
          suggestions.AddRange(sugs);
      }
      suggestions = suggestions.Distinct().OrderBy(x => x.ToLower())
        .Where(x => x.PartiallyMatchWith(
          TypedWord == AibeDocument.StartString ? string.Empty : TypedWord, TypedWord == AibeDocument.StartString)).ToList();
      return suggestions;
    }

    private List<KeyValuePair<int, int>> getEnclosures(string par, int parIndex) {
      List<KeyValuePair<int, int>> enclosures = new List<KeyValuePair<int, int>>();
      List<char> symbols = new List<char> { '\'', '"' };
      char lastEnclosureSymbol = '\0';
      int openIndex = -1;
      for (int i = 0; i < par.Length; ++i) {
        char ch = par[i];
        bool isEnclosure = symbols.Any(x => x.Equals(ch));
        if (lastEnclosureSymbol != '\0') { //in the enclosure
          if (isEnclosure && ch == lastEnclosureSymbol) { //close enclosure //if the enclosure is the same as the last enclosure index, then enter the enclosure
            lastEnclosureSymbol = '\0';
            enclosures.Add(new KeyValuePair<int, int>(openIndex + parIndex, i + parIndex));
          } //else do nothing
        } else if (isEnclosure) { //outside of the enclosure
          lastEnclosureSymbol = ch; //take the current character as the last enclosure
          openIndex = i;
        } //nothing to be done, just continue if it is not an open enclosure and is not an enclosure
      }
      if (lastEnclosureSymbol != '\0') //there is an enclosed enclosure, considered as (incomplete) open enclosure
        enclosures.Add(new KeyValuePair<int, int>(openIndex + parIndex, -1));      
      return enclosures;
    }
    
    private bool getIsInBetween(List<KeyValuePair<int,int>> pairs) {
      bool pairsExists = pairs.Any(x => x.Key < Position && x.Value >= Position);
      if (pairsExists)
        return true;
      if (!pairs.Any())
        return false;
      bool openIB = false;
      int thisParIndex = getClosestParIndex(Position); //for example, 50
      int nextParIndex = getNextParIndex(Position); //for example, 100 or -1
      int lastIBStart = pairs[pairs.Count - 1].Key;
      if (thisParIndex < 0) //the paragraph is not found, something suspicious is smelled
        return false;
      if (nextParIndex > 0) { //next paragraph is found
        openIB = pairs.Any(x => x.Key < Position && x.Value > Position && x.Key > thisParIndex && x.Value < nextParIndex);
      } else { //last paragraph
        openIB = pairs.Any(x => x.Key < Position && x.Value == -1 && x.Key > thisParIndex); //the key must be greater than this paragraph index
      }
      return pairsExists || openIB;
    }

    private bool getIsEnclosed() {
      return getIsInBetween(Enclosures);
    }

    private List<KeyValuePair<int, int>> getParentheses(string par, int parIndex) {
      List<KeyValuePair<int, int>> parentheses = new List<KeyValuePair<int, int>>();
      char openSymbol = '(', closeSymbol = ')';
      List<int> openIndices = new List<int>(); //to store the open indices
      int depth = 0;
      for (int i = 0; i < par.Length; ++i) {
        char ch = par[i];
        bool isOpen = openSymbol == ch;
        bool isClose = closeSymbol == ch;
        if (isOpen) { //open symbol found
          ++depth; //increase the depth
          openIndices.Add(i); //put open index here
        } else if (isClose && depth > 0) { //find isClose in the depth
          --depth; //decrease the depth
          int openIndex = openIndices[depth];
          parentheses.Add(new KeyValuePair<int, int>(openIndex + parIndex, i + parIndex));
          openIndices.RemoveAt(depth);
        }
      }
      if (openIndices.Any()) //there is a left-over open symbol
        foreach(var openIndex in openIndices)
          parentheses.Add(new KeyValuePair<int, int>(openIndex + parIndex, -1));
      return parentheses;
    }

    private bool getIsParenthesed() {
      return getIsInBetween(Parentheses);
    }

    private void updateParenthesesContext() {
      if (!IsParenthesed)
        return;
      int position = Position;
      CurrentOpenParenthesisIndex = getClosestParenthesesIndex(position); //get the parenthesis index
      if (CurrentOpenParenthesisIndex <= 0) //if the index is in the 0 then just return, because we cannot go back
        return;
      int contextStartIndex = CurrentOpenParenthesisIndex - 1;
      char ch = FullText[contextStartIndex];
      List<char> obtainedChars = new List<char>();
      while (!DefaultBreakChars.Contains(ch) && contextStartIndex >= 0) { //not break char and is valid for contextStartIndex
        obtainedChars.Insert(0, ch); //insert it
        --contextStartIndex; //go back one step
        ch = contextStartIndex >= 0 ? FullText[contextStartIndex] : '\0'; //get next char, if available
      }
      ParenthesesContext = new string(obtainedChars.ToArray()).Trim();
      if (contextStartIndex < 0)
        return;
      int userRefKeywordTestIndex = contextStartIndex;
      ch = FullText[userRefKeywordTestIndex];
      while(char.IsWhiteSpace(ch) && userRefKeywordTestIndex >= 0) {
        --userRefKeywordTestIndex;
        ch = userRefKeywordTestIndex >= 0 ? FullText[userRefKeywordTestIndex] : '\0';
      }
      if (userRefKeywordTestIndex < Aibe.DH.UserPrefix.Length && //minimum of Aibe.DH.UserPrefix.Length
        FullText.Length > userRefKeywordTestIndex + Aibe.DH.UserPrefix.Length) //the Full Length must also be sufficient
        return;
      string userPrefixTest = FullText.Substring(userRefKeywordTestIndex, Aibe.DH.UserPrefix.Length);
      IsUserParenthesesContext = userPrefixTest.Equals(Aibe.DH.UserPrefix); //only correct at this point      
    }

    //Next par exists
    //ParIndex 213, NextParIndex 266, Enclosures [600, -1] 
    // parIndex < 600 1 
    // nextParIndex > 600
    // nextParIndex > -1 1 //and this is a false result! because this is not supposed to be in the enclosure
    //ParIndex 513, NextParIndex 766, Enclosures [600, -1] 
    // parIndex < 600 1 
    // nextParIndex > -1 1
    //next paragraph does not exist?
    //ParIndex 513, NextParIndex -1, Enclosures [600, -1] 
    //key > parIndex
    private List<KeyValuePair<int, int>> getThisParEnclosuresOnly() {
      if (NextParagraphIndex > 0) //next par index exist, then it can be compared
        return Enclosures.Where(x => x.Key > ParagraphIndex && x.Key <= NextParagraphIndex).ToList();
      return Enclosures.Where(x => x.Key > ParagraphIndex).ToList();
    }

    private List<KeyValuePair<int, int>> getThisParParenthesesOnly() {
      if (NextParagraphIndex > 0) //next par index exist, then it can be compared
        return Parentheses.Where(x => x.Key > ParagraphIndex && x.Key <= NextParagraphIndex).ToList();
      return Parentheses.Where(x => x.Key > ParagraphIndex).ToList();
    }

    //return getThisParEnclosuresOnly(parIndex, nextParIndex);
    //private List<KeyValuePair<int, int>> getThisParEnclosuresOnly(int parIndex, int nextParIndex) { //assuming
    //  List<KeyValuePair<int, int>> enclosures = new List<KeyValuePair<int, int>>();
    //  if (nextParIndex > 0) //next par index exist, then it can be compared
    //    return Enclosures.Where(x => x.Key > parIndex && x.Key <= nextParIndex).ToList();
    //  return Enclosures.Where(x => x.Key > parIndex).ToList();
    //}

    private List<KeyValuePair<int, int>> getThisComponentRegexes() {
      if (ParagraphContext != Aibe.DH.MICNRegexCheckedColumns || ComponentLength <= 0)
        return new List<KeyValuePair<int, int>>();
      int index = ComponentText.IndexOf('=');
      int regEndComponentIndexOffset = ComponentText.IndexOf("</" + Aibe.DH.RegexCheckedColumnTag + ">");
      if (index < 0 || ComponentLength <= index + 1) //at the very least, the length must be more than the index + 1
        return new List<KeyValuePair<int, int>>();      
      return new List<KeyValuePair<int, int>> { new KeyValuePair<int, int>(index + 1 + ComponentIndex, 
        regEndComponentIndexOffset < 0 ? EndComponentIndex : (regEndComponentIndexOffset + ComponentIndex)) }; //from index + 1 till the end
    }

    private List<KeyValuePair<int, int>> getThisComponentScripts() {
      List<KeyValuePair<int, int>> results = new List<KeyValuePair<int, int>>();
      if (!Aibe.DH.MICNGroupScripted.Contains(ParagraphContext))
        return results;
      if (ParagraphContext == Aibe.DH.MICNOrderBy) {
        int index = ComponentText.IndexOf(Aibe.DH.SQLScriptDirectivePrefix);
        if (index < 0 || ComponentLength <= index + Aibe.DH.SQLScriptDirectivePrefix.Length) //at the very least, the length must be more than the index + 1
          return results;
        results.Add(new KeyValuePair<int, int>(index + 1 + ComponentIndex, EndComponentIndex)); //from index + 1 till the end
        return results; 
      } else if (Aibe.DH.MICNGroupDropDownLists.Contains(ParagraphContext)) { //TVR, dropdown lists
        List<string> subComponents = ComponentText.ParseComponentsWithEnclosurePairs(',', true, //must allow the empty string here 
          new List<KeyValuePair<char, char>> { new KeyValuePair<char, char>('[', ']') });
        int accumulatedIndex = 0;
        for (int i = 0; i < subComponents.Count; ++i) { //process per sub-component
          string subComponent = subComponents[i];
          List<char> sequence = new List<char> { '[', ':', ':', ':' };
          List<int> indices = subComponent.IndexSequenceOf(sequence);
          if (indices[3] == -1) { //the right sequence not found
            accumulatedIndex += subComponent.Length;
            continue;
          }
          int enclosingCharIndex = subComponent.LastIndexOf(']'); //closing index here would be a bit tricky, we need to find the last enclosing char
          bool enclosingCharIsLastChar = subComponent.Length - 1 == enclosingCharIndex;
          bool isConfirmedEnclosingChar = enclosingCharIsLastChar || string.IsNullOrWhiteSpace(
            subComponent.Substring(enclosingCharIndex + 1));          
          int closingIndex = enclosingCharIndex > 0 && isConfirmedEnclosingChar ? 
            enclosingCharIndex : subComponent.Length; //the whole index is the closing index if the index does not exist
          int offset = ComponentIndex + accumulatedIndex + i;
          results.Add(new KeyValuePair<int, int>(indices[3] + offset, closingIndex + offset));
          accumulatedIndex += subComponent.Length;
          //take index of the last ':', but needs to add:
          //i -> the number of the symbols so far
          //accumulatedIndex -> the amount of the sub-component length
          //ComponentIndex -> to add the absolute position of the current component index
        }
      } else if (ParagraphContext == Aibe.DH.MICNListColumns) { //TVR, The list columns could be problematic
        List<char> sequence = new List<char> { '=', '|', ':', ':', ':' };
        List<int> indices = ComponentText.IndexSequenceOf(sequence);
        if (indices[4] == -1) //the right sequence not found
          new List<KeyValuePair<int, int>>();
        int closingIndex = ComponentText.Length;
        results.Add(new KeyValuePair<int, int>(indices[4], closingIndex));
      } else { //for the other components just return everything from = as the scripts
        int index = ComponentText.IndexOf('=');
        if (index < 0 || ComponentLength <= index + 1) //at the very least, the length must be more than the index + 1
          return results;
        results.Add(new KeyValuePair<int, int>(index + 1 + ComponentIndex, EndComponentIndex));
        return results; //from index + 1 till the end
      }
      return results;
    }

    private string getParType() {
      if (Paragraph.Length <= AibeDocument.StartString.Length)
        return string.Empty;
      string content = Paragraph.Substring(AibeDocument.StartString.Length).TrimStart();
      if (content.StartsWith(AibeDocument.TableHeader))
        return AibeDocument.TableHeader;
      int index = 0;
      while (index < content.Length && !char.IsWhiteSpace(content[index]))
        ++index;
      if (index < content.Length)
        return content.Substring(0, index);
      return content;
    }

    private string getThisPar(int pos) {
      int parIndex = getClosestParIndex(pos);
      int nextParIndex = getNextParIndex(pos);
      if (parIndex < 0)
        return string.Empty; //do not have paragraph
      string par = nextParIndex < 0 ? FullText.Substring(parIndex) : FullText.Substring(parIndex, nextParIndex - parIndex);
      if (!string.IsNullOrEmpty(par))
        return par;
      return string.Empty; //do not have paragraph
    }

    private int getClosestParIndex(int pos) {
      if (ParIndices.Count <= 0)
        return -1;
      var cands = ParIndices.Where(x => x <= pos); //lower or equal to the position, return the max
      if (cands.Any())
        return cands.Max();
      return -1; //cannot find closest par index
    }

    private int getNextParIndex (int pos) {
      if (ParIndices.Count <= 0)
        return -1;
      var cands = ParIndices.Where(x => x > pos); //greater positoin than the 
      if (cands.Any())
        return cands.Min();
      return -1; //cannot find next par index
    }

    private int getClosestEnclosureIndex(int pos) {
      if (Enclosures.Count <= 0)
        return -1;
      var cands = Enclosures.Where(x => x.Key <= pos);
      if (cands.Any())
        return cands.Max(x => x.Key);
      return -1; //cannot find closest enclosure index
    }

    private int getClosestParenthesesIndex(int pos) {
      if (Parentheses.Count <= 0)
        return -1;
      var cands = Parentheses.Where(x => x.Key <= pos);
      if (cands.Any())
        return cands.Max(x => x.Key);
      return -1;
    }

    private bool isCharWordCompound(char ch) {
      return char.IsWhiteSpace(ch) || char.IsLetterOrDigit(ch) || ch == '_';
    }

    public string GetWordBeforePosition(int initPos, out int earlyPos) {
      earlyPos = -1;
      int usedPos = initPos - 1;
      if (usedPos <= 0)
        return null;
      int pos = usedPos;
      int finalPos = initPos;
      while (pos > 0 && isCharWordCompound(FullText[pos]))
        --pos;      
      if (pos > 0) //adjust if the break cause is not the position
        ++pos; //do not include the word compound
      int length = finalPos - pos;
      earlyPos = pos;
      if (length <= 0)
        return string.Empty; //very important to distinguish between null and empty here!
      return FullText.Substring(pos, length);
    }

    public string GetTypedWord() {
      //If cannot get typed char, means there is no selected word, empty
      //If it is neither digit nor underscore, it is a new item, return empty too
      //bool canTakeTypedChar = CanTakeTypedChar;
      //char? typedChar = TypedChar;
      //int position = Position;
      //string fullText = FullText;
      if (!CanTakeTypedChar || (!char.IsLetterOrDigit(TypedChar.Value) && 
        !nonLetterDigitWordCharsAllowed.Contains(TypedChar.Value)))
        return string.Empty;
      int pos = Position - 1;
      int finalPos = Position;
      bool testForStartString = startStringComponents.Contains(TypedChar.Value);
      if (pos < 0)
        return string.Empty;
      while (pos >= 0 && //while pos is more than zero there must be at least one typed char to the left of it
        (testForStartString && startStringComponents.Contains(FullText[pos])) || //test for start string
        (!testForStartString && (char.IsLetterOrDigit(FullText[pos]) || FullText[pos] == '_'))) { //test for others
        --pos;
        if (pos < 0)
          break;
      }
      if (pos >= -1) //adjust if the break cause is not the position
        ++pos; //do not include the last char caused the break
      int length = finalPos - pos;
      if (length <= 0)
        return string.Empty;
      return FullText.Substring(pos, length).Trim(); //typed word can be trimmed, does not matter
    }

    public string GetFullWord() {      
      //bool canTakeTypedChar = CanTakeTypedChar;
      //char? typedChar = TypedChar;
      //int position = Position;
      //string fullText = FullText;
      //int fullTextLength = fullText.Length;
      //string typedWord = TypedWord;
      if (!CanTakeTypedChar || (!char.IsLetterOrDigit(TypedChar.Value) &&
        !nonLetterDigitWordCharsAllowed.Contains(TypedChar.Value)))
        return string.Empty;
      int pos = Position;
      int initialPosition = Position;
      if (pos >= FullText.Length)
        return TypedWord;
      bool testForStartString = startStringComponents.Contains(TypedChar.Value);

      while (pos < FullText.Length && //while pos is less than full text, there is a character to check on the right
        (testForStartString && startStringComponents.Contains(FullText[pos])) || //test for start string
        (!testForStartString && (char.IsLetterOrDigit(FullText[pos]) || FullText[pos] == '_'))) { //test for others
        ++pos;
        if (pos >= FullText.Length) //has to be put otherwise seems like the fullText[pos] in the while would be evaluated first
          break;
      }
      int length = pos - initialPosition;
      string rightWord = length <= 0 ? string.Empty : FullText.Substring(initialPosition, length).Trim();
      return TypedWord + rightWord;
    }

    private bool isBreakChar(char ch) {
      return DefaultBreakChars.Any(x => x == ch);
    }

    public void UpdateBreakCharAndRefs() {
      if (!ContextBreaks.Any())
        return;
      int breakCharPos = -1;
      char? ch = GetBreakChar(Position, out breakCharPos);
      BreakPosition = breakCharPos;
      BreakChar = ch;
      char? breakChar = ch;
      if (BreakChar == ':' || //to provide ref words
        (BreakChar == '|' && Aibe.DH.MICNGroupVBarBreak.Contains(ParagraphContext))) { //special treatment for '|' break char in the coloring list and for pre- and post- action triggers
        while (breakChar.HasValue && breakChar.Value == BreakChar.Value) {
          int earlyPos;
          string word = GetWordBeforePosition(breakCharPos, out earlyPos);
          if (word != null) //as long as the word is not null then insert it! this is the cure for skipped SKIP
            RefWords.Insert(0, word.Trim()); //ref words can be trimmed here, does not matter
          if (earlyPos >= 1)
            breakChar = GetBreakChar(earlyPos, out breakCharPos);
          else
            breakChar = null;
        }
      } else if ((BreakChar == ',' || BreakChar == '|') && ParagraphContext == Aibe.DH.MICNForeignInfoColumns) { //special case for ForeignInfoColumns "," can give direct table context, cannot be mixed with previous case because the previous case only checks one type of break char
        while(breakChar.HasValue && (breakChar.Value == ':' || breakChar.Value == ',' || breakChar == '|')) {
          int earlyPos;
          string word = GetWordBeforePosition(breakCharPos, out earlyPos);
          if (word != null) //as long as the word is not null then insert it! this is the cure for skipped SKIP
            RefWords.Insert(0, word.Trim()); //ref words can be trimmed here, does not matter
          if (earlyPos >= 1)
            breakChar = GetBreakChar(earlyPos, out breakCharPos);
          else
            breakChar = null;
        }
        processToObtainBreakCharBefore(); //need to add this even for the foreign info columns since v1.4.1.0
      } else if ((BreakChar == '=' && Aibe.DH.MICNGroupTVR.Contains(ParagraphContext)) //very special case for determining = inside or outside of TVR
        || (BreakChar == '|' && ParagraphContext == Aibe.DH.MICNListColumns) //to distinguish between left and right parts of the 'equation'
        || (BreakChar == ',' && ParagraphContext == Aibe.DH.MICNHistoryTriggers) //to detect if ',' is present, before that there must be either ',' or '|' to distinguish between row action or true/false
        || (BreakChar == '>' && Aibe.DH.MICNGroupTagged.Contains(ParagraphContext))) //case to distinguish end of regex or inside of regex
        processToObtainBreakCharBefore();

      //to detect if this is done on row action/true-false region
      //this is processed independently from initial BreakChar found ":" or "|", unlike other conditions to obtain BreakCharBefore
      if (BreakChar == '|' && Aibe.DH.MICNGroupActionTriggers.Contains(ParagraphContext))
        processToObtainBreakCharBefore();
    }

    private void processToObtainBreakCharBefore() {
      if (ContextBreaks.Count <= 1)
        return;
      for (int i = 1; i < ContextBreaks.Count; ++i) {
        KeyValuePair<int, char> kvp = ContextBreaks[i];
        if (kvp.Key > BreakPosition)
          break;
        if (kvp.Key == BreakPosition) {
          KeyValuePair<int, char> prevKvp = ContextBreaks[i - 1];
          BreakPositionBefore = prevKvp.Key;
          BreakCharBefore = prevKvp.Value;
          break;
        }
      }
    }

    public char? GetBreakChar(int pos, out int breakCharPos) {
      char? ch = null;
      breakCharPos = -1;
      if (!ContextBreaks.Any(x => x.Key < pos))
        return ch;
      var bc = ContextBreaks.Last(x => x.Key < pos);
      if (bc.Key <= Position) {
        breakCharPos = bc.Key;
        ch = bc.Value;
      }
      return ch;
    }

    public Dictionary<string, List<AibeBreakSet>> ContextBreakSectionDicts { get; private set; } = new Dictionary<string, List<AibeBreakSet>> {
      { AibeDocument.TableHeader, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.Text, SectionType.TableName } },
      } }, //nothing is expected from simple table header
      { Aibe.DH.MICNDisplayName, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.Text, } },
      } }, //nothing
      { Aibe.DH.MICNTableSource, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.TableName, } },
      } }, //nothing
      { Aibe.DH.MICNPrefilledColumns, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.Value, } },
      } },
      { Aibe.DH.MICNItemsPerPage, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.Number, } },
      } }, //nothing
      { Aibe.DH.MICNOrderBy, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, SectionType.SQLScriptPrefix, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.OrderByDirective, } },
        new AibeBreakSet { BreakChar = ':', SectionTypes = new List<SectionType> { SectionType.SQLScript, } },
      } },
      { Aibe.DH.MICNActionList, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.RowAction, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.RowAction, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.Role, } },
        new AibeBreakSet { BreakChar = ',', SectionTypes = new List<SectionType> { SectionType.Role, } },
      } },
      { Aibe.DH.MICNDefaultActionList, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.RowAction, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.RowAction, } },
      } }, //nothing
      { Aibe.DH.MICNTableActionList, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.TableAction, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.TableAction, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.Role, } },
        new AibeBreakSet { BreakChar = ',', SectionTypes = new List<SectionType> { SectionType.Role, } },
      } },
      { Aibe.DH.MICNDefaultTableActionList, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.TableAction, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.TableAction, } },
      } }, //nothing
      { Aibe.DH.MICNTextFieldColumns, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.Number, } },
      } },
      { Aibe.DH.MICNPictureColumns, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.Number, SectionType.Skip } },
        new AibeBreakSet { BreakChar = ',', SectionTypes = new List<SectionType> { SectionType.Number, } },
        new AibeBreakSet { BreakChar = '|', SectionTypes = new List<SectionType> { SectionType.Number, SectionType.Skip } },
      } },
      { Aibe.DH.MICNIndexShownPictureColumns, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
      } }, //nothing
      { Aibe.DH.MICNRequiredColumns, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
      } }, //nothing
      { Aibe.DH.MICNNumberLimitColumns, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.MinMax, } },
        new AibeBreakSet { BreakChar = ':', SectionTypes = new List<SectionType> { SectionType.Number, } },
        new AibeBreakSet { BreakChar = '|', SectionTypes = new List<SectionType> { SectionType.MinMax, } },
      } },
      { Aibe.DH.MICNRegexCheckedColumns, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.Symbol, } },
        new AibeBreakSet { BreakChar = '<', SectionTypes = new List<SectionType> { SectionType.Tag, } },
        new AibeBreakSet { BreakChar = '/', SectionTypes = new List<SectionType> { SectionType.Tag, } },
        new AibeBreakSet { BreakChar = '>', SectionTypes = new List<SectionType> { SectionType.Symbol, SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.Regex, } },
      } },
      { Aibe.DH.MICNRegexCheckedColumnExamples, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.Symbol, } },
        new AibeBreakSet { BreakChar = '<', SectionTypes = new List<SectionType> { SectionType.Tag, } },
        new AibeBreakSet { BreakChar = '/', SectionTypes = new List<SectionType> { SectionType.Tag, } },
        new AibeBreakSet { BreakChar = '>', SectionTypes = new List<SectionType> { SectionType.Symbol, SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.Text, } },
      } },
      { Aibe.DH.MICNUserRelatedFilters, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.UserColumnName, } },
        new AibeBreakSet { BreakChar = '|', SectionTypes = new List<SectionType> { SectionType.Symbol, } },
        new AibeBreakSet { BreakChar = '{', SectionTypes = new List<SectionType> { SectionType.UserRelatedDirective, } },
        new AibeBreakSet { BreakChar = '}', SectionTypes = new List<SectionType> { SectionType.Symbol, } },
        new AibeBreakSet { BreakChar = ':', SectionTypes = new List<SectionType> { SectionType.Value, } },
        new AibeBreakSet { BreakChar = ',', SectionTypes = new List<SectionType> { SectionType.Value, } },
      } },
      { Aibe.DH.MICNDisableFilter, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.TrueFalse, } },
      } }, //nothing
      { Aibe.DH.MICNForcedFilterColumns, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.Role, } },
        new AibeBreakSet { BreakChar = ',', SectionTypes = new List<SectionType> { SectionType.Role, } },
      } },
      { Aibe.DH.MICNColumnExclusionList, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.Role, } },
        new AibeBreakSet { BreakChar = ',', SectionTypes = new List<SectionType> { SectionType.Role, } },
      } },
      { Aibe.DH.MICNFilterExclusionList, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.Role, } },
        new AibeBreakSet { BreakChar = ',', SectionTypes = new List<SectionType> { SectionType.Role, } },
      } },
      { Aibe.DH.MICNDetailsExclusionList, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.Role, } },
        new AibeBreakSet { BreakChar = ',', SectionTypes = new List<SectionType> { SectionType.Role, } },
      } },
      { Aibe.DH.MICNCreateEditExclusionList, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.Role, } },
        new AibeBreakSet { BreakChar = ',', SectionTypes = new List<SectionType> { SectionType.Role, } },
      } },
      { Aibe.DH.MICNCsvExclusionList, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.Role, } },
        new AibeBreakSet { BreakChar = ',', SectionTypes = new List<SectionType> { SectionType.Role, } },
      } },
      { Aibe.DH.MICNAccessExclusionList, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.Role, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.Role, } },
      } }, //nothing
      { Aibe.DH.MICNColoringList, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.Symbol, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.Symbol, } },
        new AibeBreakSet { BreakChar = '[', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ',', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ']', SectionTypes = new List<SectionType> { SectionType.Symbol, } },
        new AibeBreakSet { BreakChar = '|', SectionTypes = new List<SectionType> {
          SectionType.ColoringComparisonCode, SectionType.AggregateName, SectionType.TableName, SectionType.Value, SectionType.Color, SectionType.Now
        } },
        new AibeBreakSet { BreakChar = '(', SectionTypes = new List<SectionType> { SectionType.TableName, } },
        new AibeBreakSet { BreakChar = ')', SectionTypes = new List<SectionType> { SectionType.Symbol, } },
        new AibeBreakSet { BreakChar = ':', SectionTypes = new List<SectionType> { SectionType.ColumnName, SectionType.Number, SectionType.Self } },
      } },
      { Aibe.DH.MICNFilterDropDownLists, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.Value, SectionType.Symbol, SectionType.ThisTableColumnName } },
        new AibeBreakSet { BreakChar = '[', SectionTypes = new List<SectionType> { SectionType.TableName, } },
        new AibeBreakSet { BreakChar = ']', SectionTypes = new List<SectionType> { SectionType.Symbol, } },
        new AibeBreakSet { BreakChar = '{', SectionTypes = new List<SectionType> { SectionType.OrderByDirective, } },
        new AibeBreakSet { BreakChar = '}', SectionTypes = new List<SectionType> { SectionType.Symbol, } },
        new AibeBreakSet { BreakChar = ':', SectionTypes = new List<SectionType> {
          SectionType.ColumnName, SectionType.Skip, SectionType.Value, SectionType.SQLScript,
        } },
        new AibeBreakSet { BreakChar = ',', SectionTypes = new List<SectionType> { SectionType.Value, SectionType.Symbol, } },
      } },
      { Aibe.DH.MICNCreateEditDropDownLists, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.Value, SectionType.Symbol, SectionType.ThisTableColumnName } },
        new AibeBreakSet { BreakChar = '[', SectionTypes = new List<SectionType> { SectionType.TableName, } },
        new AibeBreakSet { BreakChar = ']', SectionTypes = new List<SectionType> { SectionType.Symbol, } },
        new AibeBreakSet { BreakChar = '{', SectionTypes = new List<SectionType> { SectionType.OrderByDirective, } },
        new AibeBreakSet { BreakChar = '}', SectionTypes = new List<SectionType> { SectionType.Symbol, } },
        new AibeBreakSet { BreakChar = ':', SectionTypes = new List<SectionType> {
          SectionType.ColumnName, SectionType.Skip, SectionType.Value, SectionType.SQLScript,
        } },
        new AibeBreakSet { BreakChar = ',', SectionTypes = new List<SectionType> { SectionType.Value, SectionType.Symbol, } },
      } },
      { Aibe.DH.MICNPrefixesOfColumns, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.Text, } },
      } },
      { Aibe.DH.MICNPostfixesOfColumns, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.Text, } },
      } },
      { Aibe.DH.MICNListColumns, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.ListType, SectionType.ThisTableColumnName, SectionType.StaticValue } },
        new AibeBreakSet { BreakChar = '|', SectionTypes = new List<SectionType> { SectionType.Text, SectionType.TableName, } },
        new AibeBreakSet { BreakChar = ',', SectionTypes = new List<SectionType> { SectionType.Text, } },
        new AibeBreakSet { BreakChar = ':', SectionTypes = new List<SectionType> {
          SectionType.ColumnName, SectionType.Skip, SectionType.SQLScript
        } },
        new AibeBreakSet { BreakChar = '#', SectionTypes = new List<SectionType> { SectionType.Number, } },
      } },
      { Aibe.DH.MICNTimeStampColumns, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.RowAction, } },
        new AibeBreakSet { BreakChar = ',', SectionTypes = new List<SectionType> { SectionType.RowAction, } },
        new AibeBreakSet { BreakChar = '|', SectionTypes = new List<SectionType> { SectionType.Number, SectionType.TrueFalse, } },
      } },
      { Aibe.DH.MICNHistoryTable, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.TableName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.HistoryColumnName, } },
        new AibeBreakSet { BreakChar = ',', SectionTypes = new List<SectionType> { SectionType.HistoryColumnName, } },
        new AibeBreakSet { BreakChar = ':', SectionTypes = new List<SectionType> { SectionType.HistoryColumnName, SectionType.AutoGenerated, } },
      } },
      { Aibe.DH.MICNHistoryTriggers, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.TrueFalse, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.TrueFalse, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.SQLScript, } },
        new AibeBreakSet { BreakChar = ',', SectionTypes = new List<SectionType> { SectionType.TrueFalse, SectionType.TriggerRowAction, } },
        new AibeBreakSet { BreakChar = '|', SectionTypes = new List<SectionType> { SectionType.TriggerRowAction, } },
      } },
      { Aibe.DH.MICNAutoGeneratedColumns, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.TableName, } },
        new AibeBreakSet { BreakChar = ',', SectionTypes = new List<SectionType> { SectionType.TableName, } },
        new AibeBreakSet { BreakChar = ':', SectionTypes = new List<SectionType> { SectionType.ColumnName, } },
      } },
      { Aibe.DH.MICNColumnSequence, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
      } }, //nothing
      { Aibe.DH.MICNColumnAliases, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.Text, } },
      } },
      { Aibe.DH.MICNEditShowOnlyColumns, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
      } }, //nothing
      { Aibe.DH.MICNScriptConstructorColumns, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '#', SectionTypes = new List<SectionType> { SectionType.Number, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.SQLScript, } },
        new AibeBreakSet { BreakChar = '|', SectionTypes = new List<SectionType> { SectionType.ScriptConstructorAttribute, } },
        new AibeBreakSet { BreakChar = ',', SectionTypes = new List<SectionType> { SectionType.ScRefColumnName } }, //is the same as Text below, if the Text is changed, this must be changed too
        new AibeBreakSet { BreakChar = ':', SectionTypes = new List<SectionType> {
          SectionType.TableName, SectionType.ScRefColumnName
        } },
      } },
      { Aibe.DH.MICNScriptColumns, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.SQLScript, } },
      } },
      { Aibe.DH.MICNCustomDateTimeFormatColumns, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.Page, } },
        new AibeBreakSet { BreakChar = '|', SectionTypes = new List<SectionType> { SectionType.Page, SectionType.DateTimeFormat, } },
      } },
      { Aibe.DH.MICNEmailMakerTriggers, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.TriggerName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.TriggerName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.SQLScript, } },
        new AibeBreakSet { BreakChar = '|', SectionTypes = new List<SectionType> { SectionType.TriggerRowAction, } },
        new AibeBreakSet { BreakChar = ',', SectionTypes = new List<SectionType> { SectionType.TriggerRowAction, } },
      } },
      { Aibe.DH.MICNEmailMakers, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.TriggerName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.TriggerName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.TemplateName, } },
      } },
      { Aibe.DH.MICNNonPictureAttachmentColumns, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.FileFormat, } },
        new AibeBreakSet { BreakChar = ',', SectionTypes = new List<SectionType> { SectionType.FileFormat, } },
      } },
      { Aibe.DH.MICNDownloadColumns, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
      } }, //nothing
      { Aibe.DH.MICNPreActionTriggers, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.TriggerName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.TriggerName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.SQLScript, } },
        new AibeBreakSet { BreakChar = '|', SectionTypes = new List<SectionType> { SectionType.TriggerRowAction, SectionType.TrueFalse, } },
        new AibeBreakSet { BreakChar = ',', SectionTypes = new List<SectionType> { SectionType.TriggerRowAction, } },
      } },
      { Aibe.DH.MICNPreActionProcedures, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.TriggerName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.TriggerName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.DataStoredProcedure, SectionType.UserDBReferencePrefix, } },
        new AibeBreakSet { BreakChar = ':', SectionTypes = new List<SectionType> { SectionType.UserStoredProcedure, } },
        new AibeBreakSet { BreakChar = '(', SectionTypes = new List<SectionType> { SectionType.DataStoredProcedureParameter, SectionType.UserStoredProcedureParameter, } },
        new AibeBreakSet { BreakChar = ',', SectionTypes = new List<SectionType> { SectionType.DataStoredProcedureParameter, SectionType.UserStoredProcedureParameter, } },
        new AibeBreakSet { BreakChar = ')', SectionTypes = new List<SectionType> { SectionType.Symbol, } },
      } },
      { Aibe.DH.MICNPostActionTriggers, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.TriggerName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.TriggerName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.SQLScript, } },
        new AibeBreakSet { BreakChar = '|', SectionTypes = new List<SectionType> { SectionType.TriggerRowAction, SectionType.TrueFalse, } },
        new AibeBreakSet { BreakChar = ',', SectionTypes = new List<SectionType> { SectionType.TriggerRowAction, } },
      } },
      { Aibe.DH.MICNPostActionProcedures, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.TriggerName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.TriggerName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.DataStoredProcedure, SectionType.UserDBReferencePrefix, } },
        new AibeBreakSet { BreakChar = ':', SectionTypes = new List<SectionType> { SectionType.UserStoredProcedure, } },
        new AibeBreakSet { BreakChar = '(', SectionTypes = new List<SectionType> { SectionType.DataStoredProcedureParameter, SectionType.UserStoredProcedureParameter, } },
        new AibeBreakSet { BreakChar = ',', SectionTypes = new List<SectionType> { SectionType.DataStoredProcedureParameter, SectionType.UserStoredProcedureParameter, } },
        new AibeBreakSet { BreakChar = ')', SectionTypes = new List<SectionType> { SectionType.Symbol, } },
      } },
      { Aibe.DH.MICNTableType, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.TableType, } },
      } }, 
      { Aibe.DH.MICNAggregationStatement, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.SQLAggregationScript, } },
        new AibeBreakSet { BreakChar = ',', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ':', SectionTypes = new List<SectionType> { SectionType.GroupByAutoDirective, } },
      } },
      { Aibe.DH.MICNForeignInfoColumns, new List<AibeBreakSet>() {
        new AibeBreakSet { BreakChar = '\0', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = ';', SectionTypes = new List<SectionType> { SectionType.ThisTableColumnName, } },
        new AibeBreakSet { BreakChar = '=', SectionTypes = new List<SectionType> { SectionType.TableSource } },
        new AibeBreakSet { BreakChar = '|', SectionTypes = new List<SectionType> { SectionType.Text, SectionType.ThisTableColumnName } },
        new AibeBreakSet { BreakChar = ':', SectionTypes = new List<SectionType> { SectionType.ColumnName, } },
        new AibeBreakSet { BreakChar = ',', SectionTypes = new List<SectionType> { SectionType.ColumnName, } },
      } },
      //nothing
    };
  }

  public class AibeBreakSet {
    public char BreakChar { get; set; } = '\0';
    public List<SectionType> SectionTypes { get; set; } = new List<SectionType>();
  }
}

//if the currentText is empty, then it should 
//int supposedEndStartString = index + AibeDocument.StartString.Length; //where ///--- really should end
//if (ParagraphOffsetPosition < supposedEndStartString) { //where the current position is less than ///---, it must be something like ///-- or ///-
//  IsBeginString = true;
//  return;
//}

//that means the item is really found          
//if (supposedEndStartString >= Paragraph.Length) { //that means the item cannot be taken, but is found
//  IsBeginParagraph = true; //definitely true
//} else if (index == ParagraphOffsetPosition) { //not begin paragraph, but begin string
//  IsBeginString = true;
//  //IsBeginParagraph = true; //definitely true
//} else {

//Must check where we are now
//int lengthDiff = ParagraphOffsetPosition - AibeDocument.StartString.Length;
//if (lengthDiff > 0) {
//  string testText = Paragraph.Substring(index + AibeDocument.StartString.Length, lengthDiff);
//  //first, if all is white space
//  //second if the format is "  text", not "  text " //That is, if we trim start, it does not contain white space at all
//  IsBeginParagraph = string.IsNullOrWhiteSpace(testText) || !testText.TrimStart().Any(x => char.IsWhiteSpace(x));
//}
//} //despite paragraph length, this is not found, assuming begin String
