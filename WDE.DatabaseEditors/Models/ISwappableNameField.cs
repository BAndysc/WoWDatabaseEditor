using System.ComponentModel;
using WDE.DatabaseEditors.Data;

namespace WDE.DatabaseEditors.Models
{
    public interface ISwappableNameField
    {
        string OriginalName { get; }

        void RegisterNameSwapHandler(IDbTableFieldNameSwapHandler nameSwapHandler);

        void UnregisterNameSwapHandler();
        
        void UpdateFieldName(string newName);
    }
}