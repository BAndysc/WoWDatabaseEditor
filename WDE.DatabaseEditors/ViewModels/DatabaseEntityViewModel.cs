using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using WDE.Common.Parameters;
using WDE.DatabaseEditors.Models;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.DatabaseEditors.ViewModels
{
    public class DatabaseEntityViewModel : ObservableBase
    {
        private readonly TemplateDbTableEditorViewModel viewModel;
        private readonly DatabaseEntity entity;
        private SourceList<DatabaseCellViewModel> sourceFields = new();
        public ReadOnlyObservableCollection<DatabaseCellsCategoryViewModel> FilteredFields { get; }

        public System.IObservable<Unit> Observable { get; }
        public bool IsFirstEntity => viewModel.Rows.Count > 0 && viewModel.Rows[0] == this;
        
        public DatabaseEntityViewModel(IParameterFactory parameterFactory, TemplateDbTableEditorViewModel viewModel, DatabaseEntity entity)
        {
            this.viewModel = viewModel;
            this.entity = entity;
            Observable = entity.GetCell("type")!.ToObservable();
            
            int categoryIndex = 0;
            int index = 0;
            foreach (var group in viewModel.TableDefinition.Groups)
            {
                categoryIndex++;
                IObservable<bool>? showGroup = null;
                if (group.ShowIf.HasValue)
                {
                    var cell = entity.GetCell(group.ShowIf.Value.ColumnName);
                    if (cell is DatabaseField<long> longField)
                    {
                        showGroup = FunctionalExtensions.Select(longField.Parameter
                                .ToObservable(p => p.Value), val => val == group.ShowIf.Value.Value);
                    }
                }

                foreach (var field in group.Fields)
                {
                    var databaseField = entity.GetCell(field.DbColumnName);
                    if (databaseField == null)
                        throw new Exception("Internal loading error: no cell with column name" + field.DbColumnName + " (this should never happen)");

                    IParameterValue parameterValue = null!;
                    if (databaseField is DatabaseField<long> longParam)
                    {
                        parameterValue = new ParameterValue<long>(longParam.Parameter, parameterFactory.Factory(field.ValueType));
                    }
                    else if (databaseField is DatabaseField<string> stringParam)
                    {
                        parameterValue = new ParameterValue<string>(stringParam.Parameter, new StringParameter());
                    }
                    else if (databaseField is DatabaseField<float> floatParameter)
                    {
                        parameterValue = new ParameterValue<float>(floatParameter.Parameter, new FloatParameter());
                    }
                    
                    var cell = new DatabaseCellViewModel(this, databaseField, parameterValue, field, group.Name, categoryIndex, index++, showGroup);
                    sourceFields.Add(cell);
                }
            }

            AutoDispose(sourceFields.Connect()
                .Filter(viewModel.CurrentFilter)
                .GroupOn(t => (t.CategoryName, t.CategoryIndex))
                .Transform(group => new DatabaseCellsCategoryViewModel(group, viewModel.GetGroupVisibility(group.GroupKey.CategoryName)))
                .DisposeMany()
                .FilterOnObservable(t => t.ShowGroup)
                //.Filter(model => Collect(model.ShowGroup))
                .Sort(Comparer<DatabaseCellsCategoryViewModel>.Create((x, y) => x.GroupOrder.CompareTo(y.GroupOrder)))
                .Bind(out ReadOnlyObservableCollection<DatabaseCellsCategoryViewModel> filteredFields)
                .Subscribe(a =>
                {
                    
                }, b => throw b));
            FilteredFields = filteredFields;
        }
        
        private bool Collect(System.IObservable<bool> b)
        {
            bool report = true;
            b.SubscribeAction(val => report = val).Dispose();
            return report;
        }

        public IDatabaseField? GetCell(string columnName)
        {
            return entity.GetCell(columnName);
        }
    }
}