-----------------------------------------------------------------------
Aibe Editor v1.0.0.0 (Released on 30-Jan-2018 12:05 PM SGT, by Ian K)
-----------------------------------------------------------------------


-----------------------------------------------------------------------
Compatibility: 
-----------------------------------------------------------------------
 - .NET Framework 4 or above
 - Aibe v1.0.0.0 or above 
   Note: the editor itself is packaged with Aibe v1.1.0.0


-----------------------------------------------------------------------
Description
-----------------------------------------------------------------------
To write and to check Aibe syntax on a database


-----------------------------------------------------------------------
Features
-----------------------------------------------------------------------
1> Table Descriptor
   - Basic feature of Aibe Editor
   - To show the most complete explanation of Aibe syntax per table in the database
     Note: Table Descriptor shows the most accurate explanation of Aibe syntax per table. 
           See [Column Editor] [Limitation]
   - Limitation: Does not check ScriptConstructorColumns-ScriptColumns combined-column component.
     
2> Column Editor
   - To check and to edit individual column component of the Aibe Syntax in the chosen [Table Name]
   - Press [Submit] button to edit the column component individually
   - Limitation: Cannot show the checking results of ALL the combined-column components.
   - Unchecked combined-column components:
     -> ScriptConstructorColumns-ScriptColumns
     -> RegexCheckedColumns-RegexCheckedColumnExamples
     -> EmailMakerTriggers-EmailMakers
     -> PreActionTriggers-PreActionProcedures
     -> PostActionTriggers-PostActionProcedures

3> Database Report
   - To show the summary report of the compilation of all the Aibe Syntax in the database
   - To show the Aibe syntax error(s) for all tables in the database
   - Press [Update Report] to update the compilation results

4> Script Writer
   - Best feature of Aibe Editor
   - To write, to edit, to load, to delete, and to compile (check) Aibe syntax per table by scripting/as a script
   - Show suggestions and expected description/syntax types as you type
   - Press [Submit] button to write new element (Aibe description) into [Meta Item] table or to edit the old element from the [Meta Item] table.
     -> If [TABLE:] element is not found in the script, selected table in the [Table Name] will be updated
     -> If [TABLE:] element is found in the script, the written table name in the [TABLE:] element will be:
        (a) written if it does not yet exist in the [Meta Item] table or 
        (b) replaced if it already exists in the [Meta Item] table
   - Press [Delete] button to delete an element currently selected in the [Table Name] 
   - Press [Load] button to load the element currently selected in the [Table Name] as a script
   - Press [Compile] button to compile the written script, showing error(s) in compilation - if there is any
   - See [Help] tab for shortcuts in script typing

5> Others
   - Had the database changed outside of the editor, press [Refresh Database] to recompile the syntax checking of the Aibe editor
   - Untick [Use Data DB Table] to use minimum syntax checking feature of the Editor (not recommended)


-----------------------------------------------------------------------
In-Application Settings
-----------------------------------------------------------------------
1> Not Use DB Warning: Shows warning when [Use Data DB Table] is unticked. Recommendation: unticked.
2> Failed Update Warning: Shows warning when [Column Editor] [Submit] failed. Recommendation: ticked.
3> Error Update Warning: Shows warning when [Column Editor] has error when submitted. Recommendation: ticked.
4> Empty Update Warning: Shows warning when [Column Editor] is empty when submitted. Empty update is OK if the intention is to nullify the column value. Recommendation: unticked.
5> Successful Update Notification: Shows successful box when [Column Editor] [Submit] is successful. Recommendation: ticked.


-----------------------------------------------------------------------
Application Settings
-----------------------------------------------------------------------
1> Typical Aibe connection strings named "CoreDataModel" for [Data DB] connection and "DefaultConnection" for [User DB]
2> All settings in the [In-Application Settings]
3> UserTableName: the user table name in the [User DB]. Default value for Aiwe: AspNetUsers
4> RoleTableName: the role table name in the [User DB]. Default value for Aiwe: AspNetRoles
5> IsDebug: developer option for debugging. Recommendation: False
6> IsFullDebug: developer option for debugging. Recommendation: False

