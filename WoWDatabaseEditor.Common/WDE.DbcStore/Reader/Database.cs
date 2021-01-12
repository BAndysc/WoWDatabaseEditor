using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using WDBXEditor.Reader;
using static WDBXEditor.Common.Constants;

namespace WDBXEditor.Storage
{
    internal class Database
    {
        public static Definition Definitions { get; set; } = new();
        public static List<DBEntry> Entries { get; set; } = new();
        public static int BuildNumber { get; set; }

        private static string FormatError(string f, ErrorType t, string s)
        {
            return $"{t.ToString().ToUpper()} {Path.GetFileName(f)} : {s}";
        }

        public static List<string> LoadFile(string file)
        {
            var errors = new List<string>();

            try
            {
                DBReader reader = new();
                DBEntry entry = reader.Read(file);
                if (entry != null)
                {
                    DBEntry? current = Entries.FirstOrDefault(x => x.FileName == entry.FileName && x.Build == entry.Build);
                    if (current != null)
                        Entries.Remove(current);

                    Entries.Add(entry);
                    //if (file != firstFile)
                    //    entry.Detach();

                    if (!string.IsNullOrWhiteSpace(reader.ErrorMessage))
                        errors.Add(FormatError(file, ErrorType.Warning, reader.ErrorMessage));
                }
            }
            catch (ConstraintException)
            {
                errors.Add(FormatError(file, ErrorType.Error, "Id column contains duplicates."));
            }
            catch (Exception ex)
            {
                errors.Add(FormatError(file, ErrorType.Error, ex.Message));
            }

            return errors;
        }

        #region Defintions

        public static void LoadDefinitions()
        {
            foreach (string file in Directory.GetFiles(DefinitionDir, "*.xml"))
                Definitions.LoadDefinition(file);
        }

        #endregion

        public static void ForceGC()
        {
            GC.Collect();
            GC.WaitForFullGCComplete();

#if DEBUG
            Debug.WriteLine((GC.GetTotalMemory(false) / 1024 / 1024) + "mb");
#endif
        }


        internal enum ErrorType
        {
            Warning,
            Error
        }
    }
}