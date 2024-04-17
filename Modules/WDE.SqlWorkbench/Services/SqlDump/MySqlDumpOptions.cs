using System.ComponentModel;

namespace WDE.SqlWorkbench.Services.SqlDump;

internal struct MySqlDumpOptions
{
    [Argument("add-drop-database")]
    [Description("Add DROP DATABASE statement before each CREATE DATABASE statement")]
    public bool AddDropDatabase = true;

    [Argument("add-drop-table")]
    [Description("Add DROP TABLE statement before each CREATE TABLE statement")]
    public bool AddDropTable = true;

    [Argument("add-drop-trigger")]
    [Description("Add DROP TRIGGER statement before each CREATE TRIGGER statement")]
    public bool AddDropTrigger = true;

    [Argument("comments")]
    [Description("Add comments to dump file")]
    public bool Comments = true;

    [Argument("no-create-db")]
    [Description("Do not write CREATE DATABASE statements")]
    public bool NoCreateDb = false;

    [Argument("no-create-info")]
    [Description("Do not write CREATE TABLE statements that re-create each dumped table")]
    public bool NoCreateInfo = false;

    [Argument("no-data")]
    [Description("Do not dump table contents")]
    public bool NoData = false;

    [Argument("triggers")]
    [Description("Dump triggers for each dumped table")]
    public bool Triggers = true;

    [Argument("add-locks")]
    [Description("Surround each table dump with LOCK TABLES and UNLOCK TABLES statements")]
    public bool AddLocks = true;

    [Argument("allow-keywords")]
    [Description("Allow creation of column names that are keywords")]
    public bool AllowKeywords = false;

    [Argument("compact")]
    [Description("Produce more compact output")]
    public bool Compact = false;

    // [Argument("compatible")]
    // [Description("Produce output that is more compatible with other database systems or with older MySQL servers")]
    // public bool Compatible;

    [Argument("complete-insert")]
    [Description("Use complete INSERT statements that include column names")]
    public bool CompleteInsert = true;

    [Argument("create-options")]
    [Description("Include all MySQL-specific table options in CREATE TABLE statements")]
    public bool CreateOptions = true;

    [Argument("disable-keys")]
    [Description("For each table, surround INSERT statements with statements to disable and enable keys")]
    public bool DisableKeys = true;

    [Argument("dump-date")]
    [Description("Include dump date as \"Dump completed on\" comment if --comments is given")]
    public bool DumpDate = true;

    [Argument("events")]
    [Description("Dump events from dumped databases")]
    public bool Events = false;

    [Argument("extended-insert")]
    [Description("Use multiple-row INSERT syntax")]
    public bool ExtendedInsert = true;

    [Argument("force")]
    [Description("Continue even if an SQL error occurs during a table dump")]
    public bool Force = false;

    [Argument("hex-blob")]
    [Description("Dump binary columns using hexadecimal notation")]
    public bool HexBlob = true;

    [Argument("insert-ignore")]
    [Description("Write INSERT IGNORE rather than INSERT statements")]
    public bool InsertIgnore = false;

    [Argument("lock-all-tables")]
    [Description("Lock all tables across all databases")]
    public bool LockAllTables = false;

    [Argument("lock-tables")]
    [Description("Lock all tables before dumping them")]
    public bool LockTables = true;

    [Argument("no-autocommit")]
    [Description("Enclose the INSERT statements for each dumped table within SET autocommit = 0 and COMMIT statements")]
    public bool NoAutocommit = false;

    [Argument("no-tablespaces")]
    [Description("Do not write any CREATE LOGFILE GROUP or CREATE TABLESPACE statements in output")]
    public bool NoTablespaces = false;

    [Argument("order-by-primary")]
    [Description("Dump each table's rows sorted by its primary key, or by its first unique index")]
    public bool OrderByPrimary = false;

    [Argument("quote-names")]
    [Description("Quote identifiers within backtick characters")]
    public bool QuoteNames = true;

    [Argument("quick")]
    [Description("Retrieve rows for a table from the server a row at a time")]
    public bool Quick = true;

    [Argument("replace")]
    [Description("Write REPLACE statements rather than INSERT statements")]
    public bool Replace = false;

    [Argument("routines")]
    [Description("Dump stored routines (procedures and functions) from dumped databases")]
    public bool Routines = true;

    [Argument("set-charset")]
    [Description("Add SET NAMES default_character_set to output")]
    public bool SetCharset = true;

    [Argument("single-transaction")]
    [Description("Issue a BEGIN SQL statement before dumping data from server")]
    public bool SingleTransaction = false;

    // [Argument("tab")]
    // [Description("Produce tab-separated data files")]
    // public bool Tab;

    [Argument("tz-utc")]
    [Description("Add SET TIME_ZONE='+00:00' to dump file")]
    public bool TzUtc = false;

    [SkipWhenFalse]
    [Argument("xml")]
    [Description("Produce XML output")]
    public bool Xml = false;

    public MySqlDumpOptions()
    {
    }
}
