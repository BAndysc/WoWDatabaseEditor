using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.CoreVersion;
using WDE.Common.Services.Mcp;
using WDE.Conditions.Data;
using WDE.Module.Attributes;

namespace WDE.Conditions.Mcp
{
    public class ConditionValidateInput
    {
        [Description("conditions.SourceTypeOrReferenceId the conditions are meant for - needed to verify targets (see trinity_condition_sources).")]
        public required int SourceType { get; set; }

        [Description("Condition groups in the same shape trinity_condition_get returns: conditions inside one group are ANDed, groups are ORed.")]
        public required List<List<McpConditionJson>> Groups { get; set; }
    }

    public class ConditionValidateOutput
    {
        [Description("True when no error-severity problems were found.")]
        public required bool Valid { get; set; }

        public required int ErrorCount { get; set; }

        public required int WarningCount { get; set; }

        public required List<McpConditionProblemJson> Problems { get; set; }

        [Description("What kind of validation this is.")]
        public required string Note { get; set; }
    }

    [AutoRegister]
    [SingleInstance]
    internal class ConditionValidateTool : McpTool<ConditionValidateInput, ConditionValidateOutput>
    {
        private readonly Lazy<IConditionDataManager> dataManager;
        private readonly Lazy<ICurrentCoreVersion> currentCoreVersion;

        public ConditionValidateTool(Lazy<IConditionDataManager> dataManager,
            Lazy<ICurrentCoreVersion> currentCoreVersion)
        {
            this.dataManager = dataManager;
            this.currentCoreVersion = currentCoreVersion;
        }

        public override string Name => "trinity_condition_validate";

        public override string Description => "Validates TrinityCore conditions given in the same JSON shape trinity_condition_get returns / trinity_condition_update accepts, without touching the database. Reports unknown condition types, wrong parameter names/counts, values outside the defined enum/flag options, invalid targets for the source type and string-parameter support - the same constraints the conditions editor enforces. trinity_condition_update runs the same checks and refuses to build SQL on errors.";

        protected override Task<ConditionValidateOutput> Execute(ConditionValidateInput input, CancellationToken token)
        {
            var coreHasStringValue = currentCoreVersion.Value.Current.ConditionFeatures.HasConditionStringValue;
            var problems = ConditionsMcpModelBuilder.Validate(input.SourceType, input.Groups, dataManager.Value, coreHasStringValue);
            var errorCount = problems.Count(p => p.Severity == McpConditionProblemSeverity.Error);

            return Task.FromResult(new ConditionValidateOutput
            {
                Valid = errorCount == 0,
                ErrorCount = errorCount,
                WarningCount = problems.Count - errorCount,
                Problems = problems,
                Note = "The conditions module has no dedicated validator service; these are definition-based checks against the editor's condition type/source definitions (types, parameter names and counts, enum/flag options, targets, string parameter support)."
            });
        }
    }
}
