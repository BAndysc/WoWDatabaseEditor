using System.Windows;

namespace WDE.Common.Managers
{
    public interface ITool
    {
        string Title { get; }
        public Visibility Visibility { get; set; }
    }
}