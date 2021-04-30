using System.Windows;

namespace WDE.DatabaseEditors.WPF.Controls
{
    public class FastBoolCellView : FastCellViewBase
    {
        static FastBoolCellView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FastBoolCellView), new FrameworkPropertyMetadata(typeof(FastBoolCellView)));
        }
    }
}
