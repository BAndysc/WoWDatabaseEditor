using WDBXEditor.Reader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static WDBXEditor.Common.Constants;
using System.Data;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace WDBXEditor.Storage
{
	class Database
	{
		public static Definition Definitions { get; set; } = new Definition();
		public static List<DBEntry> Entries { get; set; } = new List<DBEntry>();
        public static int BuildNumber { get; set; }


        internal enum ErrorType
		{
			Warning,
			Error
		}

		private static string FormatError(string f, ErrorType t, string s)
		{
			return $"{t.ToString().ToUpper()} {Path.GetFileName(f)} : {s}";
		}

		public static List<string> LoadFile(string file)
        {
            List<string> _errors = new List<string>();

            try
			{
				DBReader reader = new DBReader();
				DBEntry entry = reader.Read(file);
				if (entry != null)
				{
					var current = Entries.FirstOrDefault(x => x.FileName == entry.FileName && x.Build == entry.Build);
					if (current != null)
						Entries.Remove(current);

					Entries.Add(entry);
					//if (file != firstFile)
					//    entry.Detach();

					if (!string.IsNullOrWhiteSpace(reader.ErrorMessage))
						_errors.Add(FormatError(file, ErrorType.Warning, reader.ErrorMessage));
				}
			}
			catch (ConstraintException) { _errors.Add(FormatError(file, ErrorType.Error, "Id column contains duplicates.")); }
			catch (Exception ex) { _errors.Add(FormatError(file, ErrorType.Error, ex.Message)); }

            return _errors;
        }
        
		#region Defintions
		public static void LoadDefinitions()
		{
            foreach (var file in Directory.GetFiles(DEFINITION_DIR, "*.xml"))
                Definitions.LoadDefinition(file);
        }
		#endregion

		public static void ForceGC()
		{
			GC.Collect();
			GC.WaitForFullGCComplete();

#if DEBUG
			Debug.WriteLine((GC.GetTotalMemory(false) / 1024 / 1024).ToString() + "mb");
#endif
		}
	}
}
