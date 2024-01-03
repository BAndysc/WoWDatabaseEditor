using System;
using MySqlConnector;

namespace WDE.SqlWorkbench.Models;

internal interface IMySqlDataReader
{
    bool IsDBNull(int ordinal);
    object? GetValue(int ordinal);
    string? GetString(int ordinal);
    bool GetBoolean(int ordinal);
    byte GetByte(int ordinal);
    sbyte GetSByte(int ordinal);
    short GetInt16(int ordinal);
    ushort GetUInt16(int ordinal);
    int GetInt32(int ordinal);
    uint GetUInt32(int ordinal);
    long GetInt64(int ordinal);
    ulong GetUInt64(int ordinal);
    char GetChar(int ordinal);
    PublicMySqlDecimal GetDecimal(int ordinal);
    double GetDouble(int ordinal);
    float GetFloat(int ordinal);
    DateTime GetDateTime(int ordinal);
    TimeSpan GetTimeSpan(int ordinal);
    long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length);
    MySqlDateTime GetMySqlDateTime(int ordinal);
}