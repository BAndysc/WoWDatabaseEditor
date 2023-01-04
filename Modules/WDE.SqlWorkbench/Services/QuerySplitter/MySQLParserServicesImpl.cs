using System.Collections.Generic;
using System.Threading;
using AvaloniaEdit.Document;

namespace WDE.SqlWorkbench.Services.QuerySplitter;

internal class MySQLParserServicesImpl
{
    public readonly struct TextDocumentView
    {
        private readonly ITextSource doc;
        public readonly int Length;
        
        public TextDocumentView(ITextSource doc)
        {
            this.doc = doc;
            Length = doc.TextLength;
        }
        
        public char this[int index]
        {
            get
            {
                if (index < 0 || index >= Length)
                    return '\0';
                return doc.GetCharAt(index);
            }
        }

        public string TrimmedSubstring(int start, int length)
        {
            int end = start + length;
            while (start < end && (this[start] == ' ' || this[start] == '\t'))
                start++;
            while (end > start && (this[end - 1] == ' ' || this[end - 1] == '\t'))
                end--;
            return doc.GetText(start, end - start);
        }
    }

    // Adapted
    // https://github.com/mysql/mysql-workbench/blob/8.0/modules/db.mysql.parser/src/mysql_parser_module.cpp#L2271
    public static bool DetermineStatementRanges(ITextSource source, List<StatementRange> ranges, CancellationToken cancellationToken, string initialDelimiter = ";", string lineBreak = "\n")
    {
        const string keyword = "delimiter";
        var sql = new TextDocumentView(source);
        int length = sql.Length;

        string delimiter = string.IsNullOrEmpty(initialDelimiter) ? ";" : initialDelimiter;
        int delimiterHead = 0;
        int start = 0;
        int head = start;
        int tail = head;
        int end = head + length;
        //int newLine = 0;

        int currentLine = 0;
        int statementStart = 0;
        bool haveContent = false; // Set when anything else but comments were found for the current statement.
        while (tail < end)
        {
            if (cancellationToken.IsCancellationRequested)
                return false;
            
            switch (sql[tail])
            {
                case '/':
                {
                    // Possible multi line comment or hidden (conditional) command.
                    if (sql[tail + 1] == '*')
                    {
                        tail += 2;
                        bool isHiddenCommand = (sql[tail] == '!');
                        while (true)
                        {
                            while (tail < end && sql[tail] != '*')
                            {
                                if (IsLineBreak(sql, tail, lineBreak))
                                    ++currentLine;
                                tail++;
                            }

                            if (tail == end) // Unfinished comment.
                                break;
                            else
                            {
                                if (sql[++tail] == '/')
                                {
                                    tail++; // Skip the slash too.
                                    break;
                                }
                            }
                        }

                        if (isHiddenCommand)
                            haveContent = true;
                        if (!haveContent)
                        {
                            head = tail; // Skip over the comment.
                            statementStart = currentLine;
                        }
                    }
                    else
                        tail++;

                    break;
                }

                case '-':
                {
                    // Possible single line comment.
                    int end_char = tail + 2;
                    if (sql[tail + 1] == '-' && (sql[end_char] == ' ' || sql[end_char] == '\t' ||
                                                 IsLineBreak(sql, end_char, lineBreak)))
                    {
                        // Skip everything until the end of the line.
                        tail += 2;
                        while (tail < end && !IsLineBreak(sql, tail, lineBreak))
                            tail++;

                        if (!haveContent)
                        {
                            head = tail;
                            statementStart = currentLine;
                        }
                    }
                    else
                        tail++;

                    break;
                }

                case '#':
                {
                    // MySQL single line comment.
                    while (tail < end && !IsLineBreak(sql, tail, lineBreak))
                        tail++;

                    if (!haveContent)
                    {
                        head = tail;
                        statementStart = currentLine;
                    }

                    break;
                }

                case '"':
                case '\'':
                case '`':
                {
                    // Quoted string/id. Skip this in a local loop.
                    haveContent = true;
                    char quote = sql[tail++];
                    while (tail < end && sql[tail] != quote)
                    {
                        // Skip any escaped character too.
                        if (sql[tail] == '\\')
                            tail++;
                        tail++;
                    }

                    if (tail < end && sql[tail] == quote)
                        tail++; // Skip trailing quote char if one was there.

                    break;
                }

                case 'd':
                case 'D':
                {
                    haveContent = true;

                    // Possible start of the keyword DELIMITER. Must be at the start of the text or a character,
                    // which is not part of a regular MySQL identifier (0-9, A-Z, a-z, _, $, \u0080-\uffff).
                    char previous = tail > start ? sql[tail - 1] : '\0';
                    bool is_identifier_char = previous >= 0x80 || (previous >= '0' && previous <= '9') ||
                                              ((previous | 0x20) >= 'a' && (previous | 0x20) <= 'z') ||
                                              previous == '$' ||
                                              previous == '_';
                    if (tail == start || !is_identifier_char)
                    {
                        int run = tail + 1;
                        int kw = 1;
                        int count = 9;
                        while (count-- > 1 && (sql[run++] | 0x20) == keyword[kw++])
                            ;
                        if (count == 0 && sql[run] == ' ')
                        {
                            // Delimiter keyword found. Get the new delimiter (everything until the end of the line).
                            tail = run++;
                            while (run < end && !IsLineBreak(sql, run, lineBreak))
                                ++run;
                            delimiter = sql.TrimmedSubstring(tail, run - tail);
                            delimiterHead = 0;

                            // Skip over the delimiter statement and any following line breaks.
                            while (IsLineBreak(sql, run, lineBreak))
                            {
                                ++currentLine;
                                ++run;
                            }

                            tail = run;
                            head = tail;
                            statementStart = currentLine;
                        }
                        else
                            ++tail;
                    }
                    else
                        ++tail;

                    break;
                }

                default:
                    if (IsLineBreak(sql, tail, lineBreak))
                    {
                        ++currentLine;
                        if (!haveContent)
                            ++statementStart;
                    }

                    if (sql[tail] > ' ')
                        haveContent = true;
                    tail++;
                    break;
            }

            if (tail < sql.Length && delimiterHead < delimiter.Length && sql[tail] == delimiter[delimiterHead])
            {
                // Found possible start of the delimiter. Check if it really is.
                int count = delimiter.Length;
                if (count == 1)
                {
                    // Most common case. Trim the statement and check if it is not empty before adding the range.
                    head = SkipLeadingWhitespace(sql, head, tail);
                    if (head < tail)
                        ranges.Add(new StatementRange(head - start, statementStart, tail - head));
                    head = ++tail;
                    statementStart = currentLine;
                    haveContent = false;
                }
                else
                {
                    int run = tail + 1;
                    int del = delimiterHead + 1;
                    while (count-- > 1 && (sql[run++] == delimiter[del++]))
                        ;

                    if (count == 0)
                    {
                        // Multi char delimiter is complete. Tail still points to the start of the delimiter.
                        // Run points to the first character after the delimiter.
                        head = SkipLeadingWhitespace(sql, head, tail);
                        if (head < tail)
                            ranges.Add(new StatementRange(head - start, statementStart, tail - head));
                        tail = run;
                        head = run;
                        statementStart = currentLine;
                        haveContent = false;
                    }
                }
            }
        }

        // Add remaining text to the range list.
        head = SkipLeadingWhitespace(sql, head, tail);
        if (head < tail)
            ranges.Add(new StatementRange(head - start, statementStart, tail - head));

        return true;
    }

    private static int SkipLeadingWhitespace(TextDocumentView sql, int index, int end)
    {
        while (index < end && (sql[index] == ' ' || sql[index] == '\t' || sql[index] == '\n'))
            index++;
        return index;
    }

    private static bool IsLineBreak(TextDocumentView sql, int index, string lineBreak)
    {
        int lineBreakLength = lineBreak.Length;
        if (lineBreakLength == 1)
            return sql[index] == lineBreak[0];

        for (int i = 0; i < lineBreakLength; i++)
        {
            if (index + i >= sql.Length || sql[index + i] != lineBreak[i])
                return false;
        }

        return true;
    }
}