AIBE Editor a solution created by me to help to write AIBE scripts with minimal effort.

*) It provides full description of the scripts (what the scripts do exactly).
*) It provides word suggestions as you type, just like IntelliSense for Visual Studio or any other editor with similar functionality.
*) It colors the typed words according to its role/type in its context, greatly help the scripting processing.
*) It helps to compile the scripts for the entire database used and shows errors if there is any.
*) Flexible to use, can be used to change the entire scripts or only the selected parts.
*) More help available in the application itself
*) See my AIBE project repository (https://github.com/ian5666987/AIBE) for more information.

AIBE script example, for table called CFG_CUS_INF in the database (note: ///--- indicates the new line in AIBE script):

///---TABLE: CFG_CUS_INF
///---DisplayName Customer Basic Information
///---ItemsPerPage 25
///---OrderBy CustomerSiteId;CustomerId
///---ActionList Create=Manager,Supervisor;Edit=Manager,Supervisor;Delete=Manager,Supervisor;Details
///---DefaultActionList Create;Edit;Delete;Details
///---TableActionList ExportToCSV;ExportAllToCSV
///---DefaultTableActionList ExportToCSV;ExportAllToCSV
///---RegexCheckedColumns <reg>ContactList=[a-zA-Z]*=\+?\d{10,14}\|[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$;*</reg>
///---RegexCheckedColumnExamples <ex>ContactList=Jack=+6590019005|jack_tan@gmail.com</ex>
///---ColumnExclusionList Cid;ContactList
///---FilterExclusionList Cid;ContactList
///---DetailsExclusionList Cid
///---CreateEditExclusionList Cid
///---AccessExclusionList Anonymous
///---FilterDropDownLists CustomerId=[CFG_CUS_INF:CustomerId],{ASC};CustomerSiteId=[CFG_CUS_INF:CustomerSiteId],{ASC}
///---CreateEditDropDownLists CustomerSiteId=[CFG_CUS_LOC:CustomerSiteId],{ASC}
///---ListColumns ContactList|Name#10,Phone,Email=remarks;
///---ForeignInfoColumns CustomerSiteId=CFG_CUS_LOC:CustomerSiteId:Country,SiteDescription|Description of Site

Singapore, 10-Feb-2019
-Ian Kamajaya
