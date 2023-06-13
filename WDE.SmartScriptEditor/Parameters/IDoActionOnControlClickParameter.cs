using System.Threading.Tasks;

namespace WDE.SmartScriptEditor.Parameters;

public interface IDoActionOnControlClickParameter
{
    Task Invoke(long value);
}