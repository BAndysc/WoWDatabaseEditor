using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.DbScriptsEditor.Data;
using WDE.DbScriptsEditor.Exporter;
using WDE.DbScriptsEditor.Models;
using WDE.MVVM;
using WDE.SqlQueryGenerator;

namespace WDE.DbScriptsEditor.Editor.ViewModels
{
    // DEBUG-only manual integration test: loads EVERY dbscript of every type through the editor
    // model (the exact load path the editor document uses) and re-generates its SQL, then compares
    // it against SQL built straight from the raw DB lines — comments excluded on both sides. A
    // mismatch means the editor's load/export roundtrip would alter the script on save.
    public class DbScriptRoundtripTestViewModel : ObservableBase, IWindowViewModel
    {
        public class Result
        {
            public required string Script { get; init; }
            public required string Expected { get; init; }
            public required string Actual { get; init; }
        }

        private readonly IDbScriptDatabaseProvider databaseProvider;
        private readonly IDbScriptDataManager dataManager;
        private readonly IParameterFactory parameterFactory;
        private readonly IDbScriptExporter exporter;

        public ObservableCollection<Result> Failures { get; } = new();

        private Result? selectedFailure;
        public Result? SelectedFailure { get => selectedFailure; set => SetProperty(ref selectedFailure, value); }

        private string progress = "Not started";
        public string Progress { get => progress; private set => SetProperty(ref progress, value); }

        private bool isRunning;
        public bool IsRunning { get => isRunning; private set => SetProperty(ref isRunning, value); }

        public AsyncAutoCommand RunCommand { get; }

        public DbScriptRoundtripTestViewModel(IDbScriptDatabaseProvider databaseProvider,
            IDbScriptDataManager dataManager,
            IParameterFactory parameterFactory,
            IDbScriptExporter exporter)
        {
            this.databaseProvider = databaseProvider;
            this.dataManager = dataManager;
            this.parameterFactory = parameterFactory;
            this.exporter = exporter;
            RunCommand = new AsyncAutoCommand(Run, () => !IsRunning);
        }

        public void OnWindowOpened() => RunCommand.Execute(null);

        private async Task Run()
        {
            IsRunning = true;
            Failures.Clear();
            var total = 0;
            var failed = 0;
            try
            {
                foreach (var info in DbScriptTypes.All)
                {
                    var ids = await databaseProvider.GetScriptIds(info.Type);
                    var done = 0;
                    foreach (var id in ids)
                    {
                        total++;
                        done++;
                        if (done % 50 == 0)
                        {
                            Progress = $"{info.ReadableName}: {done}/{ids.Count} (failures so far: {failed})";
                            await Task.Yield(); // keep the window responsive
                        }
                        try
                        {
                            var lines = await databaseProvider.GetScript(info.Type, id);
                            if (lines.Count == 0)
                                continue;

                            var script = new EditableDbScript(info.Type, id, dataManager, parameterFactory);
                            script.Load(lines);
                            var actual = exporter.GenerateSql(script, includeComments: false).QueryString;
                            var expected = BuildExpectedSql(info, id, lines).QueryString;
                            if (actual != expected)
                            {
                                failed++;
                                Failures.Add(new Result { Script = $"{info.ReadableName} {id}", Expected = expected, Actual = actual });
                            }
                        }
                        catch (Exception e)
                        {
                            failed++;
                            Failures.Add(new Result { Script = $"{info.ReadableName} {id}", Expected = "(no exception)", Actual = e.ToString() });
                        }
                    }
                }
            }
            finally
            {
                Progress = failed == 0
                    ? $"OK — all {total} scripts roundtrip byte-identically (comments excluded)"
                    : $"{failed} of {total} scripts DIFFER after a load/export roundtrip";
                IsRunning = false;
            }
        }

        // SQL straight from the raw DB lines, mirroring the exporter's query shape: same delete +
        // one bulk insert with identical anonymous-row property names/order/types, comments empty.
        // Priorities renumber 0..n in execution order exactly like the exporter does on save.
        private static IQuery BuildExpectedSql(DbScriptTypeInfo info, uint id, IReadOnlyList<IDbScriptLine> lines)
        {
            var table = DatabaseTable.WorldTable(info.TableName);
            var query = Queries.BeginTransaction(DataDatabaseType.World);
            query.Comment($"{info.ReadableName} {id}");
            query.Table(table)
                .Where(r => r.Column<long>("id") == (long)id)
                .Delete();

            var rows = new List<object>();
            uint priority = 0;
            foreach (var line in lines.OrderBy(l => l.Delay).ThenBy(l => l.Priority))
            {
                if (DbScriptCommentConvention.IsFakeCommentLine(line))
                    continue; // becomes a comment row in the editor; both sides drop it comment-less
                rows.Add(new
                {
                    id = id,
                    delay = line.Delay,
                    priority = priority++,
                    command = line.Command,
                    datalong = line.DataLong,
                    datalong2 = line.DataLong2,
                    datalong3 = line.DataLong3,
                    buddy_entry = line.BuddyEntry,
                    search_radius = line.SearchRadius,
                    data_flags = line.DataFlags,
                    dataint = line.DataInt,
                    dataint2 = line.DataInt2,
                    dataint3 = line.DataInt3,
                    dataint4 = line.DataInt4,
                    datafloat = line.DataFloat,
                    x = line.X,
                    y = line.Y,
                    z = line.Z,
                    o = line.O,
                    speed = line.Speed,
                    condition_id = line.ConditionId,
                    comments = "",
                });
            }

            if (rows.Count > 0)
                query.Table(table).BulkInsert(rows);

            return query.Close();
        }

        // ---- IWindowViewModel ----
        public int DesiredWidth => 1000;
        public int DesiredHeight => 700;
        public string Title => "DbScripts roundtrip test";
        public bool Resizeable => true;
        public ImageUri? Icon => new("Icons/document_event_script.png");
    }
}
