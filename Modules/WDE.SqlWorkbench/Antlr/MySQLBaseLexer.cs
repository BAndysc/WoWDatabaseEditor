using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Antlr4.Runtime;

namespace WDE.SqlWorkbench.Antlr;

// this is based on https://github.com/mysql/mysql-workbench/tree/8.0/library/parsers/mysql
// don't rename things here!
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public abstract class MySQLBaseLexer : Lexer
{
    public HashSet<string> Charsets { get; private set; } // Used to check repertoires.
    public bool inVersionComment { get; set; }

    private MySQLRecognizerCommon common; // Replacing multiple inheritance
    
    public long serverVersion => common.serverVersion;
    public SqlModeEnum sqlMode => common.sqlMode;
    public bool isSqlModeActive(SqlModeEnum mode) => common.isSqlModeActive(mode);
    public void sqlModeFromString(string modes) => common.sqlModeFromString(modes);
    public static SqlModeEnum NoMode => SqlModeEnum.NoMode;
    public static SqlModeEnum AnsiQuotes => SqlModeEnum.AnsiQuotes;
    public static SqlModeEnum HighNotPrecedence => SqlModeEnum.HighNotPrecedence;
    public static SqlModeEnum PipesAsConcat => SqlModeEnum.PipesAsConcat;
    public static SqlModeEnum IgnoreSpace => SqlModeEnum.IgnoreSpace;
    public static SqlModeEnum NoBackslashEscapes => SqlModeEnum.NoBackslashEscapes;

    public string getText() => Text;
    public void setText(string value) => Text = value;
    public void setType(int type) => Type = type;
    
    public MySQLBaseLexer(ICharStream input, TextWriter output, TextWriter errorOutput) : base(input, output, errorOutput)
    {
        Charsets = new HashSet<string>();
        common = new MySQLRecognizerCommon
        {
            serverVersion = 0,
            sqlMode = SqlModeEnum.NoMode // Assuming SQLMode is a nested enum or similar type in MySQLRecognizerCommon
        };
        inVersionComment = false;
    }

    public MySQLBaseLexer(ICharStream input) : this(input, Console.Out, Console.Error)
    {
    }

    public override void Reset()
    {
        inVersionComment = false;
        base.Reset(); // Calls the reset method of the base Lexer class
    }

    /**
     * Returns true if the given token is an identifier. This includes all those keywords that are
     * allowed as identifiers when unquoted (non-reserved keywords).
     */
    public bool IsIdentifier(int type)
    {
        if (type == Lexer.Eof)
            return false;

        if (type == MySQLLexer.IDENTIFIER || type == MySQLLexer.BACK_TICK_QUOTED_ID)
            return true;

        // Double quoted text represents identifiers only if the ANSI QUOTES sql mode is active.
        if ((common.sqlMode & SqlModeEnum.AnsiQuotes) != 0 &&
            type == MySQLLexer.DOUBLE_QUOTED_TEXT)
            return true;

        string symbol = Vocabulary.GetSymbolicName(type);
        if (!string.IsNullOrEmpty(symbol) && !MySQLSymbolInfo.IsReservedKeyword(symbol,
                MySQLSymbolInfo.NumberToVersion(common.serverVersion)))
            return true;

        return false;
    }


    public int KeywordFromText(string name)
    {
        // (My)SQL only uses ASCII chars for keywords so we can do a simple downcase here for comparison.
        string transformed = name.ToLowerInvariant();

        if (!MySQLSymbolInfo.IsKeyword(transformed,
                MySQLSymbolInfo.NumberToVersion(common.serverVersion)))
            return int.MaxValue - 1; // INVALID_INDEX alone can be interpreted as EOF.

        // Generate string -> enum value map, if not yet done.
        if (_symbols == null || _symbols.Count == 0)
        {
            _symbols = new Dictionary<string, int>();
            var vocabulary = (Vocabulary)Vocabulary;
            int max = vocabulary.getMaxTokenType();
            for (int i = 0; i <= max; i++)
            {
                string symbolicName = vocabulary.GetSymbolicName(i);
                if (!string.IsNullOrEmpty(symbolicName))
                {
                    _symbols[symbolicName] = i;
                }
            }
        }

        // Here we know for sure we got a keyword.
        if (_symbols.TryGetValue(transformed, out int tokenType))
            return tokenType;
        else
            return int.MaxValue - 1;
    }

    public MySQLQueryType DetermineQueryType()
    {
        IToken token = NextDefaultChannelToken();
        if (token.Type == Eof)
            return MySQLQueryType.QtUnknown;

        switch (token.Type)
        {
            case MySQLLexer.ALTER_SYMBOL:
                token = NextDefaultChannelToken();
                if (token.Type == Eof)
                    return MySQLQueryType.QtAmbiguous;

                switch (token.Type)
                {
                    case MySQLLexer.DATABASE_SYMBOL:
                        return MySQLQueryType.QtAlterDatabase;

                    case MySQLLexer.LOGFILE_SYMBOL:
                        return MySQLQueryType.QtAlterLogFileGroup;

                    case MySQLLexer.FUNCTION_SYMBOL:
                        return MySQLQueryType.QtAlterFunction;

                    case MySQLLexer.PROCEDURE_SYMBOL:
                        return MySQLQueryType.QtAlterProcedure;

                    case MySQLLexer.SERVER_SYMBOL:
                        return MySQLQueryType.QtAlterServer;

                    case MySQLLexer.TABLE_SYMBOL:
                    case MySQLLexer.ONLINE_SYMBOL: // Optional part of ALTER TABLE.
                    case MySQLLexer.OFFLINE_SYMBOL: // ditto
                    case MySQLLexer.IGNORE_SYMBOL:
                        return MySQLQueryType.QtAlterTable;

                    case MySQLLexer.TABLESPACE_SYMBOL:
                        return MySQLQueryType.QtAlterTableSpace;

                    case MySQLLexer.EVENT_SYMBOL:
                        return MySQLQueryType.QtAlterEvent;

                    case MySQLLexer.VIEW_SYMBOL:
                        return MySQLQueryType.QtAlterView;

                    case MySQLLexer.DEFINER_SYMBOL: // Can be both event or view.
                        if (!skipDefiner(ref token))
                            return MySQLQueryType.QtAmbiguous;

                        token = NextDefaultChannelToken();

                        switch (token.Type)
                        {
                            case MySQLLexer.EVENT_SYMBOL:
                                return MySQLQueryType.QtAlterEvent;

                            case MySQLLexer.SQL_SYMBOL:
                            case MySQLLexer.VIEW_SYMBOL:
                                return MySQLQueryType.QtAlterView;
                        }

                        break;

                    case MySQLLexer.ALGORITHM_SYMBOL: // Optional part of CREATE VIEW.
                        return MySQLQueryType.QtAlterView;

                    case MySQLLexer.USER_SYMBOL:
                        return MySQLQueryType.QtAlterUser;
                }

                break;

            case MySQLLexer.CREATE_SYMBOL:
            {
                token = NextDefaultChannelToken();
                if (token.Type == Eof)
                    return MySQLQueryType.QtAmbiguous;

                switch (token.Type)
                {
                    case MySQLLexer.TEMPORARY_SYMBOL: // Optional part of CREATE TABLE.
                    case MySQLLexer.TABLE_SYMBOL:
                        return MySQLQueryType.QtCreateTable;

                    case MySQLLexer.ONLINE_SYMBOL:
                    case MySQLLexer.OFFLINE_SYMBOL:
                    case MySQLLexer.INDEX_SYMBOL:
                    case MySQLLexer.UNIQUE_SYMBOL:
                    case MySQLLexer.FULLTEXT_SYMBOL:
                    case MySQLLexer.SPATIAL_SYMBOL:
                        return MySQLQueryType.QtCreateIndex;

                    case MySQLLexer.DATABASE_SYMBOL:
                        return MySQLQueryType.QtCreateDatabase;

                    case MySQLLexer.TRIGGER_SYMBOL:
                        return MySQLQueryType.QtCreateTrigger;

                    case MySQLLexer.DEFINER_SYMBOL: // Can be event, view, procedure, function, UDF, trigger.
                    {
                        if (!skipDefiner(ref token))
                            return MySQLQueryType.QtAmbiguous;

                        switch (token.Type)
                        {
                            case MySQLLexer.EVENT_SYMBOL:
                                return MySQLQueryType.QtCreateEvent;

                            case MySQLLexer.VIEW_SYMBOL:
                            case MySQLLexer.SQL_SYMBOL:
                                return MySQLQueryType.QtCreateView;

                            case MySQLLexer.PROCEDURE_SYMBOL:
                                return MySQLQueryType.QtCreateProcedure;

                            case MySQLLexer.FUNCTION_SYMBOL:
                            {
                                token = NextDefaultChannelToken();
                                if (token.Type == Eof)
                                    return MySQLQueryType.QtAmbiguous;

                                if (!IsIdentifier(token.Type))
                                    return MySQLQueryType.QtAmbiguous;

                                token = NextDefaultChannelToken();
                                if (token.Type == MySQLLexer.RETURNS_SYMBOL)
                                    return MySQLQueryType.QtCreateUdf;

                                return MySQLQueryType.QtCreateFunction;
                            }

                            case MySQLLexer.AGGREGATE_SYMBOL:
                                return MySQLQueryType.QtCreateUdf;

                            case MySQLLexer.TRIGGER_SYMBOL:
                                return MySQLQueryType.QtCreateTrigger;
                        }

                        break;
                    }

                    case MySQLLexer.VIEW_SYMBOL:
                    case MySQLLexer.OR_SYMBOL: // CREATE OR REPLACE ... VIEW
                    case MySQLLexer.ALGORITHM_SYMBOL: // CREATE ALGORITHM ... VIEW
                        return MySQLQueryType.QtCreateView;

                    case MySQLLexer.EVENT_SYMBOL:
                        return MySQLQueryType.QtCreateEvent;

                    case MySQLLexer.FUNCTION_SYMBOL:
                        return MySQLQueryType.QtCreateFunction;

                    case MySQLLexer.AGGREGATE_SYMBOL:
                        return MySQLQueryType.QtCreateUdf;

                    case MySQLLexer.PROCEDURE_SYMBOL:
                        return MySQLQueryType.QtCreateProcedure;

                    case MySQLLexer.LOGFILE_SYMBOL:
                        return MySQLQueryType.QtCreateLogFileGroup;

                    case MySQLLexer.SERVER_SYMBOL:
                        return MySQLQueryType.QtCreateServer;

                    case MySQLLexer.TABLESPACE_SYMBOL:
                        return MySQLQueryType.QtCreateTableSpace;

                    case MySQLLexer.USER_SYMBOL:
                        return MySQLQueryType.QtCreateUser;
                }

                break;
            }

            case MySQLLexer.DROP_SYMBOL:
            {
                token = NextDefaultChannelToken();
                if (token.Type == Eof)
                    return MySQLQueryType.QtAmbiguous;

                switch (token.Type)
                {
                    case MySQLLexer.DATABASE_SYMBOL:
                        return MySQLQueryType.QtDropDatabase;

                    case MySQLLexer.EVENT_SYMBOL:
                        return MySQLQueryType.QtDropEvent;

                    case MySQLLexer.PROCEDURE_SYMBOL:
                        return MySQLQueryType.QtDropProcedure;

                    case MySQLLexer.FUNCTION_SYMBOL:
                        return MySQLQueryType.QtDropFunction;

                    case MySQLLexer.ONLINE_SYMBOL:
                    case MySQLLexer.OFFLINE_SYMBOL:
                    case MySQLLexer.INDEX_SYMBOL:
                        return MySQLQueryType.QtDropIndex;

                    case MySQLLexer.LOGFILE_SYMBOL:
                        return MySQLQueryType.QtDropLogfileGroup;

                    case MySQLLexer.SERVER_SYMBOL:
                        return MySQLQueryType.QtDropServer;

                    case MySQLLexer.TEMPORARY_SYMBOL:
                    case MySQLLexer.TABLE_SYMBOL:
                    case MySQLLexer.TABLES_SYMBOL:
                        return MySQLQueryType.QtDropTable;

                    case MySQLLexer.TABLESPACE_SYMBOL:
                        return MySQLQueryType.QtDropTablespace;

                    case MySQLLexer.TRIGGER_SYMBOL:
                        return MySQLQueryType.QtDropTrigger;

                    case MySQLLexer.VIEW_SYMBOL:
                        return MySQLQueryType.QtDropView;

                    case MySQLLexer.PREPARE_SYMBOL:
                        return MySQLQueryType.QtDeallocate;

                    case MySQLLexer.USER_SYMBOL:
                        return MySQLQueryType.QtDropUser;
                }

                break;
            }

            case MySQLLexer.TRUNCATE_SYMBOL:
                return MySQLQueryType.QtTruncateTable;

            case MySQLLexer.CALL_SYMBOL:
                return MySQLQueryType.QtCall;

            case MySQLLexer.DELETE_SYMBOL:
                return MySQLQueryType.QtDelete;

            case MySQLLexer.DO_SYMBOL:
                return MySQLQueryType.QtDo;

            case MySQLLexer.HANDLER_SYMBOL:
                return MySQLQueryType.QtHandler;

            case MySQLLexer.INSERT_SYMBOL:
                return MySQLQueryType.QtInsert;

            case MySQLLexer.LOAD_SYMBOL:
            {
                token = NextDefaultChannelToken();
                if (token.Type == Eof)
                    return MySQLQueryType.QtAmbiguous;

                switch (token.Type)
                {
                    case MySQLLexer.DATA_SYMBOL:
                    {
                        token = NextDefaultChannelToken();
                        if (token.Type == Eof)
                            return MySQLQueryType.QtAmbiguous;

                        if (token.Type == MySQLLexer.FROM_SYMBOL)
                            return MySQLQueryType.QtLoadDataMaster;
                        return MySQLQueryType.QtLoadData;
                    }
                    case MySQLLexer.XML_SYMBOL:
                        return MySQLQueryType.QtLoadXML;

                    case MySQLLexer.TABLE_SYMBOL:
                        return MySQLQueryType.QtLoadTableMaster;

                    case MySQLLexer.INDEX_SYMBOL:
                        return MySQLQueryType.QtLoadIndex;
                }

                break;
            }

            case MySQLLexer.REPLACE_SYMBOL:
                return MySQLQueryType.QtReplace;

            case MySQLLexer.SELECT_SYMBOL:
                return MySQLQueryType.QtSelect;

            case MySQLLexer.TABLE_SYMBOL:
                return MySQLQueryType.QtTable;

            case MySQLLexer.VALUES_SYMBOL:
                return MySQLQueryType.QtValues;

            case MySQLLexer.UPDATE_SYMBOL:
                return MySQLQueryType.QtUpdate;

            case MySQLLexer.OPEN_PAR_SYMBOL: // Either (((select ..))) or (partition...)
            {
                while (token.Type == MySQLLexer.OPEN_PAR_SYMBOL)
                {
                    token = NextDefaultChannelToken();
                    if (token.Type == Eof)
                        return MySQLQueryType.QtAmbiguous;
                }

                if (token.Type == MySQLLexer.SELECT_SYMBOL)
                    return MySQLQueryType.QtSelect;
                return MySQLQueryType.QtPartition;
            }

            case MySQLLexer.PARTITION_SYMBOL:
            case MySQLLexer.PARTITIONS_SYMBOL:
                return MySQLQueryType.QtPartition;

            case MySQLLexer.START_SYMBOL:
            {
                token = NextDefaultChannelToken();
                if (token.Type == Eof)
                    return MySQLQueryType.QtAmbiguous;

                if (token.Type == MySQLLexer.TRANSACTION_SYMBOL)
                    return MySQLQueryType.QtStartTransaction;
                return MySQLQueryType.QtStartSlave;
            }

            case MySQLLexer.BEGIN_SYMBOL: // Begin directly at the start of the query must be a transaction start.
                return MySQLQueryType.QtBeginWork;

            case MySQLLexer.COMMIT_SYMBOL:
                return MySQLQueryType.QtCommit;

            case MySQLLexer.ROLLBACK_SYMBOL:
            {
                // We assume a transaction statement here unless we exactly know it's about a savepoint.
                token = NextDefaultChannelToken();
                if (token.Type == Eof)
                    return MySQLQueryType.QtRollbackWork;
                if (token.Type == MySQLLexer.WORK_SYMBOL)
                {
                    token = NextDefaultChannelToken();
                    if (token.Type == Eof)
                        return MySQLQueryType.QtRollbackWork;
                }

                if (token.Type == MySQLLexer.TO_SYMBOL)
                    return MySQLQueryType.QtRollbackSavepoint;
                return MySQLQueryType.QtRollbackWork;
            }

            case MySQLLexer.SET_SYMBOL:
            {
                token = NextDefaultChannelToken();
                if (token.Type == Eof)
                    return MySQLQueryType.QtSet;

                switch (token.Type)
                {
                    case MySQLLexer.PASSWORD_SYMBOL:
                        return MySQLQueryType.QtSetPassword;

                    case MySQLLexer.GLOBAL_SYMBOL:
                    case MySQLLexer.LOCAL_SYMBOL:
                    case MySQLLexer.SESSION_SYMBOL:
                        token = NextDefaultChannelToken();
                        if (token.Type == Eof)
                            return MySQLQueryType.QtSet;
                        break;

                    case MySQLLexer.IDENTIFIER:
                    {
                        if (token.Text.Equals("autocommit", StringComparison.OrdinalIgnoreCase))
                            return MySQLQueryType.QtSetAutoCommit;
                        break;
                    }
                }

                if (token.Type == MySQLLexer.TRANSACTION_SYMBOL)
                    return MySQLQueryType.QtSetTransaction;
                return MySQLQueryType.QtSet;
            }

            case MySQLLexer.SAVEPOINT_SYMBOL:
                return MySQLQueryType.QtSavepoint;

            case MySQLLexer.RELEASE_SYMBOL: // Release at the start of the query, obviously.
                return MySQLQueryType.QtReleaseSavepoint;

            case MySQLLexer.LOCK_SYMBOL:
                return MySQLQueryType.QtLock;

            case MySQLLexer.UNLOCK_SYMBOL:
                return MySQLQueryType.QtUnlock;

            case MySQLLexer.XA_SYMBOL:
                return MySQLQueryType.QtXA;

            case MySQLLexer.PURGE_SYMBOL:
                return MySQLQueryType.QtPurge;

            case MySQLLexer.CHANGE_SYMBOL:
                return MySQLQueryType.QtChangeMaster;

            case MySQLLexer.RESET_SYMBOL:
            {
                token = NextDefaultChannelToken();
                if (token.Type == Eof)
                    return MySQLQueryType.QtReset;

                switch (token.Type)
                {
                    case MySQLLexer.SERVER_SYMBOL:
                        return MySQLQueryType.QtResetMaster;
                    case MySQLLexer.SLAVE_SYMBOL:
                        return MySQLQueryType.QtResetSlave;
                    default:
                        return MySQLQueryType.QtReset;
                }
            }

            case MySQLLexer.STOP_SYMBOL:
                return MySQLQueryType.QtStopSlave;

            case MySQLLexer.PREPARE_SYMBOL:
                return MySQLQueryType.QtPrepare;

            case MySQLLexer.EXECUTE_SYMBOL:
                return MySQLQueryType.QtExecute;

            case MySQLLexer.DEALLOCATE_SYMBOL:
                return MySQLQueryType.QtDeallocate;

            case MySQLLexer.GRANT_SYMBOL:
            {
                token = NextDefaultChannelToken();
                if (token.Type == Eof)
                    return MySQLQueryType.QtAmbiguous;

                if (token.Type == MySQLLexer.PROXY_SYMBOL)
                    return MySQLQueryType.QtGrantProxy;
                return MySQLQueryType.QtGrant;
            }

            case MySQLLexer.RENAME_SYMBOL:
            {
                token = NextDefaultChannelToken();
                if (token.Type == Eof)
                    return MySQLQueryType.QtAmbiguous;

                if (token.Type == MySQLLexer.USER_SYMBOL)
                    return MySQLQueryType.QtRenameUser;
                return MySQLQueryType.QtRenameTable;
            }

            case MySQLLexer.REVOKE_SYMBOL:
            {
                token = NextDefaultChannelToken();
                if (token.Type == Eof)
                    return MySQLQueryType.QtAmbiguous;

                if (token.Type == MySQLLexer.PROXY_SYMBOL)
                    return MySQLQueryType.QtRevokeProxy;
                return MySQLQueryType.QtRevoke;
            }

            case MySQLLexer.ANALYZE_SYMBOL:
                return MySQLQueryType.QtAnalyzeTable;

            case MySQLLexer.CHECK_SYMBOL:
                return MySQLQueryType.QtCheckTable;

            case MySQLLexer.CHECKSUM_SYMBOL:
                return MySQLQueryType.QtChecksumTable;

            case MySQLLexer.OPTIMIZE_SYMBOL:
                return MySQLQueryType.QtOptimizeTable;

            case MySQLLexer.REPAIR_SYMBOL:
                return MySQLQueryType.QtRepairTable;

            case MySQLLexer.BACKUP_SYMBOL:
                return MySQLQueryType.QtBackUpTable;

            case MySQLLexer.RESTORE_SYMBOL:
                return MySQLQueryType.QtRestoreTable;

            case MySQLLexer.INSTALL_SYMBOL:
                return MySQLQueryType.QtInstallPlugin;

            case MySQLLexer.UNINSTALL_SYMBOL:
                return MySQLQueryType.QtUninstallPlugin;

            case MySQLLexer.SHOW_SYMBOL:
            {
                token = NextDefaultChannelToken();
                if (token.Type == Eof)
                    return MySQLQueryType.QtShow;

                if (token.Type == MySQLLexer.FULL_SYMBOL)
                {
                    // Not all SHOW cases allow an optional FULL keyword, but this is not about checking for
                    // a valid query but to find the most likely type.
                    token = NextDefaultChannelToken();
                    if (token.Type == Eof)
                        return MySQLQueryType.QtShow;
                }

                switch (token.Type)
                {
                    case MySQLLexer.GLOBAL_SYMBOL:
                    case MySQLLexer.LOCK_SYMBOL:
                    case MySQLLexer.SESSION_SYMBOL:
                    {
                        token = NextDefaultChannelToken();
                        if (token.Type == Eof)
                            return MySQLQueryType.QtShow;

                        if (token.Type == MySQLLexer.STATUS_SYMBOL)
                            return MySQLQueryType.QtShowStatus;
                        return MySQLQueryType.QtShowVariables;
                    }

                    case MySQLLexer.AUTHORS_SYMBOL:
                        return MySQLQueryType.QtShowAuthors;

                    case MySQLLexer.BINARY_SYMBOL:
                        return MySQLQueryType.QtShowBinaryLogs;

                    case MySQLLexer.BINLOG_SYMBOL:
                        return MySQLQueryType.QtShowBinlogEvents;

                    case MySQLLexer.RELAYLOG_SYMBOL:
                        return MySQLQueryType.QtShowRelaylogEvents;

                    case MySQLLexer.CHAR_SYMBOL:
                        return MySQLQueryType.QtShowCharset;

                    case MySQLLexer.COLLATION_SYMBOL:
                        return MySQLQueryType.QtShowCollation;

                    case MySQLLexer.COLUMNS_SYMBOL:
                        return MySQLQueryType.QtShowColumns;

                    case MySQLLexer.CONTRIBUTORS_SYMBOL:
                        return MySQLQueryType.QtShowContributors;

                    case MySQLLexer.COUNT_SYMBOL:
                    {
                        token = NextDefaultChannelToken();
                        if (token.Type != MySQLLexer.OPEN_PAR_SYMBOL)
                            return MySQLQueryType.QtShow;
                        token = NextDefaultChannelToken();
                        if (token.Type != MySQLLexer.MULT_OPERATOR)
                            return MySQLQueryType.QtShow;
                        token = NextDefaultChannelToken();
                        if (token.Type != MySQLLexer.CLOSE_PAR_SYMBOL)
                            return MySQLQueryType.QtShow;

                        token = NextDefaultChannelToken();
                        if (token.Type == Eof)
                            return MySQLQueryType.QtShow;

                        switch (token.Type)
                        {
                            case MySQLLexer.WARNINGS_SYMBOL:
                                return MySQLQueryType.QtShowWarnings;

                            case MySQLLexer.ERRORS_SYMBOL:
                                return MySQLQueryType.QtShowErrors;
                        }

                        return MySQLQueryType.QtShow;
                    }

                    case MySQLLexer.CREATE_SYMBOL:
                    {
                        token = NextDefaultChannelToken();
                        if (token.Type == Eof)
                            return MySQLQueryType.QtShow;

                        switch (token.Type)
                        {
                            case MySQLLexer.DATABASE_SYMBOL:
                                return MySQLQueryType.QtShowCreateDatabase;

                            case MySQLLexer.EVENT_SYMBOL:
                                return MySQLQueryType.QtShowCreateEvent;

                            case MySQLLexer.FUNCTION_SYMBOL:
                                return MySQLQueryType.QtShowCreateFunction;

                            case MySQLLexer.PROCEDURE_SYMBOL:
                                return MySQLQueryType.QtShowCreateProcedure;

                            case MySQLLexer.TABLE_SYMBOL:
                                return MySQLQueryType.QtShowCreateTable;

                            case MySQLLexer.TRIGGER_SYMBOL:
                                return MySQLQueryType.QtShowCreateTrigger;

                            case MySQLLexer.VIEW_SYMBOL:
                                return MySQLQueryType.QtShowCreateView;
                        }

                        return MySQLQueryType.QtShow;
                    }

                    case MySQLLexer.DATABASES_SYMBOL:
                        return MySQLQueryType.QtShowDatabases;

                    case MySQLLexer.ENGINE_SYMBOL:
                        return MySQLQueryType.QtShowEngineStatus;

                    case MySQLLexer.STORAGE_SYMBOL:
                    case MySQLLexer.ENGINES_SYMBOL:
                        return MySQLQueryType.QtShowStorageEngines;

                    case MySQLLexer.ERRORS_SYMBOL:
                        return MySQLQueryType.QtShowErrors;

                    case MySQLLexer.EVENTS_SYMBOL:
                        return MySQLQueryType.QtShowEvents;

                    case MySQLLexer.FUNCTION_SYMBOL:
                    {
                        token = NextDefaultChannelToken();
                        if (token.Type == Eof)
                            return MySQLQueryType.QtAmbiguous;

                        if (token.Type == MySQLLexer.CODE_SYMBOL)
                            return MySQLQueryType.QtShowFunctionCode;
                        return MySQLQueryType.QtShowFunctionStatus;
                    }

                    case MySQLLexer.GRANT_SYMBOL:
                        return MySQLQueryType.QtShowGrants;

                    case MySQLLexer.INDEX_SYMBOL:
                    case MySQLLexer.INDEXES_SYMBOL:
                    case MySQLLexer.KEY_SYMBOL:
                        return MySQLQueryType.QtShowIndexes;

                    case MySQLLexer.MASTER_SYMBOL:
                        return MySQLQueryType.QtShowMasterStatus;

                    case MySQLLexer.OPEN_SYMBOL:
                        return MySQLQueryType.QtShowOpenTables;

                    case MySQLLexer.PLUGIN_SYMBOL:
                    case MySQLLexer.PLUGINS_SYMBOL:
                        return MySQLQueryType.QtShowPlugins;

                    case MySQLLexer.PROCEDURE_SYMBOL:
                    {
                        token = NextDefaultChannelToken();
                        if (token.Type == Eof)
                            return MySQLQueryType.QtShow;

                        if (token.Type == MySQLLexer.STATUS_SYMBOL)
                            return MySQLQueryType.QtShowProcedureStatus;
                        return MySQLQueryType.QtShowProcedureCode;
                    }

                    case MySQLLexer.PRIVILEGES_SYMBOL:
                        return MySQLQueryType.QtShowPrivileges;

                    case MySQLLexer.PROCESSLIST_SYMBOL:
                        return MySQLQueryType.QtShowProcessList;

                    case MySQLLexer.PROFILE_SYMBOL:
                        return MySQLQueryType.QtShowProfile;

                    case MySQLLexer.PROFILES_SYMBOL:
                        return MySQLQueryType.QtShowProfiles;

                    case MySQLLexer.SLAVE_SYMBOL:
                    {
                        token = NextDefaultChannelToken();
                        if (token.Type == Eof)
                            return MySQLQueryType.QtAmbiguous;

                        if (token.Type == MySQLLexer.HOSTS_SYMBOL)
                            return MySQLQueryType.QtShowSlaveHosts;
                        return MySQLQueryType.QtShowSlaveStatus;
                    }

                    case MySQLLexer.STATUS_SYMBOL:
                        return MySQLQueryType.QtShowStatus;

                    case MySQLLexer.VARIABLES_SYMBOL:
                        return MySQLQueryType.QtShowVariables;

                    case MySQLLexer.TABLE_SYMBOL:
                        return MySQLQueryType.QtShowTableStatus;

                    case MySQLLexer.TABLES_SYMBOL:
                        return MySQLQueryType.QtShowTables;

                    case MySQLLexer.TRIGGERS_SYMBOL:
                        return MySQLQueryType.QtShowTriggers;

                    case MySQLLexer.WARNINGS_SYMBOL:
                        return MySQLQueryType.QtShowWarnings;
                }

                return MySQLQueryType.QtShow;
            }

            case MySQLLexer.CACHE_SYMBOL:
                return MySQLQueryType.QtCacheIndex;

            case MySQLLexer.FLUSH_SYMBOL:
                return MySQLQueryType.QtFlush;

            case MySQLLexer.KILL_SYMBOL:
                return MySQLQueryType.QtKill;

            case MySQLLexer.DESCRIBE_SYMBOL: // EXPLAIN is converted to DESCRIBE in the lexer.
            case MySQLLexer.DESC_SYMBOL:
            {
                token = NextDefaultChannelToken();
                if (token.Type == Eof)
                    return MySQLQueryType.QtAmbiguous;

                if (IsIdentifier(token.Type) || token.Type == MySQLLexer.DOT_SYMBOL)
                    return MySQLQueryType.QtExplainTable;

                // EXTENDED is a bit special as it can be both, a table identifier or the keyword.
                if (token.Type == MySQLLexer.EXTENDED_SYMBOL)
                {
                    token = NextDefaultChannelToken();
                    if (token.Type == Eof)
                        return MySQLQueryType.QtExplainTable;

                    switch (token.Type)
                    {
                        case MySQLLexer.DELETE_SYMBOL:
                        case MySQLLexer.INSERT_SYMBOL:
                        case MySQLLexer.REPLACE_SYMBOL:
                        case MySQLLexer.UPDATE_SYMBOL:
                            return MySQLQueryType.QtExplainStatement;
                        default:
                            return MySQLQueryType.QtExplainTable;
                    }
                }

                return MySQLQueryType.QtExplainStatement;
            }

            case MySQLLexer.HELP_SYMBOL:
                return MySQLQueryType.QtHelp;

            case MySQLLexer.USE_SYMBOL:
                return MySQLQueryType.QtUse;
        }

        return MySQLQueryType.QtUnknown;
    }

    public static bool IsRelation(int type)
    {
        switch (type)
        {
            case MySQLLexer.EQUAL_OPERATOR:
            case MySQLLexer.ASSIGN_OPERATOR:
            case MySQLLexer.NULL_SAFE_EQUAL_OPERATOR:
            case MySQLLexer.GREATER_OR_EQUAL_OPERATOR:
            case MySQLLexer.GREATER_THAN_OPERATOR:
            case MySQLLexer.LESS_OR_EQUAL_OPERATOR:
            case MySQLLexer.LESS_THAN_OPERATOR:
            case MySQLLexer.NOT_EQUAL_OPERATOR:
            case MySQLLexer.NOT_EQUAL2_OPERATOR:
            case MySQLLexer.PLUS_OPERATOR:
            case MySQLLexer.MINUS_OPERATOR:
            case MySQLLexer.MULT_OPERATOR:
            case MySQLLexer.DIV_OPERATOR:
            case MySQLLexer.MOD_OPERATOR:
            case MySQLLexer.LOGICAL_NOT_OPERATOR:
            case MySQLLexer.BITWISE_NOT_OPERATOR:
            case MySQLLexer.SHIFT_LEFT_OPERATOR:
            case MySQLLexer.SHIFT_RIGHT_OPERATOR:
            case MySQLLexer.LOGICAL_AND_OPERATOR:
            case MySQLLexer.BITWISE_AND_OPERATOR:
            case MySQLLexer.BITWISE_XOR_OPERATOR:
            case MySQLLexer.LOGICAL_OR_OPERATOR:
            case MySQLLexer.BITWISE_OR_OPERATOR:
            case MySQLLexer.OR_SYMBOL:
            case MySQLLexer.XOR_SYMBOL:
            case MySQLLexer.AND_SYMBOL:
            case MySQLLexer.IS_SYMBOL:
            case MySQLLexer.BETWEEN_SYMBOL:
            case MySQLLexer.LIKE_SYMBOL:
            case MySQLLexer.REGEXP_SYMBOL:
            case MySQLLexer.IN_SYMBOL:
            case MySQLLexer.SOUNDS_SYMBOL:
            case MySQLLexer.NOT_SYMBOL:
                return true;

            default:
                return false;
        }
    }

    public static bool IsNumber(int type)
    {
        switch (type)
        {
            case MySQLLexer.INT_NUMBER:
            case MySQLLexer.LONG_NUMBER:
            case MySQLLexer.ULONGLONG_NUMBER:
            case MySQLLexer.FLOAT_NUMBER:
            case MySQLLexer.HEX_NUMBER:
            case MySQLLexer.BIN_NUMBER:
            case MySQLLexer.DECIMAL_NUMBER:
                return true;

            default:
                return false;
        }
    }

    public static bool IsOperator(int type)
    {
        switch (type)
        {
            case MySQLLexer.EQUAL_OPERATOR:
            case MySQLLexer.ASSIGN_OPERATOR:
            case MySQLLexer.NULL_SAFE_EQUAL_OPERATOR:
            case MySQLLexer.GREATER_OR_EQUAL_OPERATOR:
            case MySQLLexer.GREATER_THAN_OPERATOR:
            case MySQLLexer.LESS_OR_EQUAL_OPERATOR:
            case MySQLLexer.LESS_THAN_OPERATOR:
            case MySQLLexer.NOT_EQUAL_OPERATOR:
            case MySQLLexer.NOT_EQUAL2_OPERATOR:
            case MySQLLexer.PLUS_OPERATOR:
            case MySQLLexer.MINUS_OPERATOR:
            case MySQLLexer.MULT_OPERATOR:
            case MySQLLexer.DIV_OPERATOR:
            case MySQLLexer.MOD_OPERATOR:
            case MySQLLexer.LOGICAL_NOT_OPERATOR:
            case MySQLLexer.BITWISE_NOT_OPERATOR:
            case MySQLLexer.SHIFT_LEFT_OPERATOR:
            case MySQLLexer.SHIFT_RIGHT_OPERATOR:
            case MySQLLexer.LOGICAL_AND_OPERATOR:
            case MySQLLexer.BITWISE_AND_OPERATOR:
            case MySQLLexer.BITWISE_XOR_OPERATOR:
            case MySQLLexer.LOGICAL_OR_OPERATOR:
            case MySQLLexer.BITWISE_OR_OPERATOR:

            case MySQLLexer.DOT_SYMBOL:
            case MySQLLexer.COMMA_SYMBOL:
            case MySQLLexer.SEMICOLON_SYMBOL:
            case MySQLLexer.COLON_SYMBOL:
            case MySQLLexer.OPEN_PAR_SYMBOL:
            case MySQLLexer.CLOSE_PAR_SYMBOL:
            case MySQLLexer.AT_SIGN_SYMBOL:
            case MySQLLexer.AT_AT_SIGN_SYMBOL:
            case MySQLLexer.PARAM_MARKER:
                return true;

            default:
                return false;
        }
    }

    public override IToken NextToken()
    {
        // First respond with pending tokens to the next token request, if there are any.
        if (_pendingTokens.Count > 0)
        {
            var pending = _pendingTokens.Dequeue();
            return pending;
        }

        // Let the main lexer class run the next token recognition.
        // This might create additional tokens again.
        var next = base.NextToken();
        if (_pendingTokens.Count > 0)
        {
            var pending = _pendingTokens.Dequeue();
            _pendingTokens.Enqueue(next);
            return pending;
        }

        return next;
    }

    protected bool checkVersion(string text)
    {
        if (text.Length < 8) // Minimum is: /*!12345
            return false;

        // Skip version comment introducer.
        if (!long.TryParse(text.Substring(3), out long version))
            return false;

        if (version <= common.serverVersion)
        {
            inVersionComment = true;
            return true;
        }

        return false;
    }

    protected int determineFunction(int proposed)
    {
        // Skip any whitespace character if the sql mode says they should be ignored,
        // before actually trying to match the open parenthesis.
        if (common.isSqlModeActive(SqlModeEnum.IgnoreSpace))
        {
            int input = InputStream.LA(1); // Assuming _input is an instance of ICharStream
            while (input == ' ' || input == '\t' || input == '\r' || input == '\n')
            {
                Interpreter.Consume((ICharStream)InputStream);
                Channel = Lexer.Hidden; // Assuming 'Hidden' is a constant for the hidden channel
                Type = MySQLLexer.WHITESPACE; // Assuming 'WHITESPACE' is a token type in MySQLLexer
                input = InputStream.LA(1);
            }
        }

        return InputStream.LA(1) == '(' ? proposed : MySQLLexer.IDENTIFIER;
    }

    private const string long_str = "2147483647";
    private const int long_len = 10;
    private const string signed_long_str = "-2147483648";
    private const string longlong_str = "9223372036854775807";
    private const int longlong_len = 19;
    private const string signed_longlong_str = "-9223372036854775808";
    private const int signed_longlong_len = 19;
    private const string unsigned_longlong_str = "18446744073709551615";
    private const int unsigned_longlong_len = 20;

    protected int determineNumericType(string text)
    {
        // The original code checks for leading +/- but actually that can never happen, neither in the
        // server parser (as a digit is used to trigger processing in the lexer) nor in our parser
        // as our rules are defined without signs. But we do it anyway for maximum compatibility.
        uint length = (uint)text.Length - 1;
        if (length < long_len) // quick normal case
            return MySQLLexer.INT_NUMBER;
        uint negative = 0;
        int i = 0;

        if (text[i] == '+') // Remove sign and pre-zeros
        {
            i++;
            length--;
        }
        else if (text[i] == '-')
        {
            i++;
            length--;
            negative = 1;
        }

        while (text[i] == '0' && length != 0)
        {
            i++;
            length--;
        }

        if (length < long_len)
            return MySQLLexer.INT_NUMBER;

        int smaller, bigger;
        string cmp;
        if (negative != 0)
        {
            if (length == long_len)
            {
                cmp = signed_long_str + 1;
                smaller = MySQLLexer.INT_NUMBER; // If <= signed_long_str
                bigger = MySQLLexer.LONG_NUMBER; // If >= signed_long_str
            }
            else if (length < signed_longlong_len)
                return MySQLLexer.LONG_NUMBER;
            else if (length > signed_longlong_len)
                return MySQLLexer.DECIMAL_NUMBER;
            else
            {
                cmp = signed_longlong_str + 1;
                smaller = MySQLLexer.LONG_NUMBER; // If <= signed_longlong_str
                bigger = MySQLLexer.DECIMAL_NUMBER;
            }
        }
        else
        {
            if (length == long_len)
            {
                cmp = long_str;
                smaller = MySQLLexer.INT_NUMBER;
                bigger = MySQLLexer.LONG_NUMBER;
            }
            else if (length < longlong_len)
                return MySQLLexer.LONG_NUMBER;
            else if (length > longlong_len)
            {
                if (length > unsigned_longlong_len)
                    return MySQLLexer.DECIMAL_NUMBER;
                cmp = unsigned_longlong_str;
                smaller = MySQLLexer.ULONGLONG_NUMBER;
                bigger = MySQLLexer.DECIMAL_NUMBER;
            }
            else
            {
                cmp = longlong_str;
                smaller = MySQLLexer.LONG_NUMBER;
                bigger = MySQLLexer.ULONGLONG_NUMBER;
            }
        }

        int j = 0;
        int cmplen = cmp.Length;
        while (j < cmplen && cmp[j++] == text[i++]) ;

        return (text[i - 1] <= cmp[j - 1]) ? smaller : bigger;
    }

    protected int checkCharset(string text)
    {
        return Charsets.Contains(text) ? MySQLLexer.UNDERSCORE_CHARSET : MySQLLexer.IDENTIFIER;
    }

    protected void emitDot()
    {
        _pendingTokens.Enqueue(TokenFactory.Create(new Tuple<ITokenSource, ICharStream>(this, (ICharStream)InputStream),
            MySQLLexer.DOT_SYMBOL, Text, Channel,
            TokenStartCharIndex, TokenStartCharIndex, TokenStartLine,
            TokenStartColumn));
        var field = typeof(Lexer).GetField("_tokenStartCharIndex", BindingFlags.Instance | BindingFlags.NonPublic);
        field!.SetValue(this, TokenStartCharIndex + 1);
    }

    private Queue<IToken> _pendingTokens = new();

    private Dictionary<string, int> _symbols = new(); // A list of all defined symbols for lookup.

    private IToken NextDefaultChannelToken()
    {
        while (true)
        {
            IToken token = NextToken();
            if (token.Channel == TokenConstants.DefaultChannel)
                return token;
        }
    }

    /**
     *  Skips over a definer clause if possible. Returns true if it was successful and points to the
     *  token after the last definer part.
     *  On entry the DEFINER symbol has been already consumed.
     *  If the syntax is wrong false is returned and the token source state is undetermined.
     */
    private bool skipDefiner(ref IToken token)
    {
        token = NextDefaultChannelToken();
        if (token.Type != MySQLLexer.EQUAL_OPERATOR)
            return false;

        token = NextDefaultChannelToken();
        if (token.Type == MySQLLexer.CURRENT_USER_SYMBOL)
        {
            token = NextDefaultChannelToken();
            if (token.Type == MySQLLexer.OPEN_PAR_SYMBOL)
            {
                token = NextDefaultChannelToken();
                if (token.Type != MySQLLexer.CLOSE_PAR_SYMBOL)
                    return false;
                token = NextDefaultChannelToken();
                if (token.Type == TokenConstants.EOF)
                    return false;
            }

            return true;
        }

        if (token.Type == MySQLLexer.SINGLE_QUOTED_TEXT || IsIdentifier(token.Type))
        {
            // First part of the user definition (mandatory).
            token = NextDefaultChannelToken();
            if (token.Type == MySQLLexer.AT_SIGN_SYMBOL || token.Type == MySQLLexer.AT_TEXT_SUFFIX)
            {
                // Second part of the user definition (optional).
                bool needIdentifier = token.Type == MySQLLexer.AT_SIGN_SYMBOL;
                token = NextDefaultChannelToken();
                if (needIdentifier)
                {
                    if (!IsIdentifier(token.Type) && token.Type != MySQLLexer.SINGLE_QUOTED_TEXT)
                        return false;
                    token = NextDefaultChannelToken();
                    if (token.Type == TokenConstants.EOF)
                        return false;
                }
            }

            return true;
        }

        return false;
    }
}

public enum MySQLQueryType
{
    QtUnknown,
    QtAmbiguous,

    // DDL
    QtAlterDatabase,
    QtAlterLogFileGroup,
    QtAlterFunction,
    QtAlterProcedure,
    QtAlterServer,
    QtAlterTable,
    QtAlterTableSpace,
    QtAlterEvent,
    QtAlterView,

    QtCreateTable,
    QtCreateIndex,
    QtCreateDatabase,
    QtCreateEvent,
    QtCreateView,
    QtCreateRoutine, // All of procedure, function, UDF. Used for parse type.
    QtCreateProcedure,
    QtCreateFunction,
    QtCreateUdf,
    QtCreateTrigger,
    QtCreateLogFileGroup,
    QtCreateServer,
    QtCreateTableSpace,

    QtDropDatabase,
    QtDropEvent,
    QtDropFunction, // Includes UDF.
    QtDropProcedure,
    QtDropIndex,
    QtDropLogfileGroup,
    QtDropServer,
    QtDropTable,
    QtDropTablespace,
    QtDropTrigger,
    QtDropView,

    QtRenameTable,
    QtTruncateTable,

    // DML
    QtCall,
    QtDelete,
    QtDo,

    QtHandler, // Do we need Handler open/close etc.?

    QtInsert,
    QtLoadData,
    QtLoadXML,
    QtReplace,
    QtSelect,
    QtTable, // Explicit table statement.
    QtValues, // Table value constructor.
    QtUpdate,

    QtPartition, // Cannot be used standalone.

    QtStartTransaction,
    QtBeginWork,
    QtCommit,
    QtRollbackWork,
    QtSetAutoCommit, // "set autocommit" is especially mentioned in transaction help, so identify this too.
    QtSetTransaction,

    QtSavepoint,
    QtReleaseSavepoint,
    QtRollbackSavepoint,

    QtLock,
    QtUnlock,

    QtXA, // Do we need xa start, xa end etc.?

    QtPurge,
    QtChangeMaster,
    QtReset,
    QtResetMaster,
    QtResetSlave,
    QtStartSlave,
    QtStopSlave,
    QtLoadDataMaster,
    QtLoadTableMaster,

    QtPrepare,
    QtExecute,
    QtDeallocate,

    // Database administration
    QtAlterUser,
    QtCreateUser,
    QtDropUser,
    QtGrantProxy,
    QtGrant,
    QtRenameUser,
    QtRevokeProxy,
    QtRevoke,

    QtAnalyzeTable,
    QtCheckTable,
    QtChecksumTable,
    QtOptimizeTable,
    QtRepairTable,
    QtBackUpTable,
    QtRestoreTable,

    QtInstallPlugin,
    QtUninstallPlugin,

    QtSet, // Any variable assignment.
    QtSetPassword,

    QtShow,
    QtShowAuthors,
    QtShowBinaryLogs,
    QtShowBinlogEvents,
    QtShowRelaylogEvents,
    QtShowCharset,
    QtShowCollation,
    QtShowColumns,
    QtShowContributors,
    QtShowCreateDatabase,
    QtShowCreateEvent,
    QtShowCreateFunction,
    QtShowCreateProcedure,
    QtShowCreateTable,
    QtShowCreateTrigger,
    QtShowCreateView,
    QtShowDatabases,
    QtShowEngineStatus,
    QtShowStorageEngines,
    QtShowErrors,
    QtShowEvents,
    QtShowFunctionCode,
    QtShowFunctionStatus,
    QtShowGrants,
    QtShowIndexes, // Index, Indexes, Keys
    QtShowMasterStatus,
    QtShowOpenTables,
    QtShowPlugins,
    QtShowProcedureStatus,
    QtShowProcedureCode,
    QtShowPrivileges,
    QtShowProcessList,
    QtShowProfile,
    QtShowProfiles,
    QtShowSlaveHosts,
    QtShowSlaveStatus,
    QtShowStatus,
    QtShowVariables,
    QtShowTableStatus,
    QtShowTables,
    QtShowTriggers,
    QtShowWarnings,

    QtCacheIndex,
    QtFlush,
    QtKill, // Connection, Query
    QtLoadIndex,

    QtExplainTable,
    QtExplainStatement,
    QtHelp,
    QtUse,

    QtSentinel
}

public static class MySQLSymbolInfo
{
    public static bool IsReservedKeyword(string symbol, object numberToVersion)
    {
        return false;
    }

    public static int NumberToVersion(long serverVersion)
    {
        return 0;
    }

    public static bool IsKeyword(string transformed, int numberToVersion)
    {
        return false;
    }
}