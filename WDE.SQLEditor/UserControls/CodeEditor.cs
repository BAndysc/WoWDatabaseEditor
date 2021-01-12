using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;

namespace WDE.SQLEditor.UserControls
{
    /// https://stackoverflow.com/a/28137792/1616645
    /// <summary>
    ///     Class that inherits from the AvalonEdit TextEditor control to
    ///     enable MVVM interaction.
    /// </summary>
    public class CodeEditor : TextEditor, INotifyPropertyChanged
    {
        // Vars.
        private static bool canScroll = true;

        /// <summary>
        ///     Default constructor to set up event handlers.
        /// </summary>
        public CodeEditor()
        {
            // Default options.
            FontSize = 12;
            FontFamily = new FontFamily("Consolas");
            Options = new TextEditorOptions
            {
                IndentationSize = 3,
                ConvertTabsToSpaces = true
            };
        }

        #region Text.

        /// <summary>
        ///     Dependancy property for the editor text property binding.
        /// </summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text",
            typeof(string),
            typeof(CodeEditor),
            new PropertyMetadata((obj, args) =>
            {
                CodeEditor target = (CodeEditor) obj;
                target.Text = (string) args.NewValue;
            }));

        /// <summary>
        ///     Provide access to the Text.
        /// </summary>
        public new string Text
        {
            get => base.Text;
            set => base.Text = value;
        }

        /// <summary>
        ///     Return the current text length.
        /// </summary>
        public int Length => base.Text.Length;

        /// <summary>
        ///     Override of OnTextChanged event.
        /// </summary>
        protected override void OnTextChanged(EventArgs e)
        {
            RaisePropertyChanged("Length");
            base.OnTextChanged(e);
        }

        /// <summary>
        ///     Event handler to update properties based upon the selection changed event.
        /// </summary>
        private void TextArea_SelectionChanged(object sender, EventArgs e)
        {
            SelectionStart = SelectionStart;
            SelectionLength = SelectionLength;
        }

        /// <summary>
        ///     Event that handles when the caret changes.
        /// </summary>
        private void TextArea_CaretPositionChanged(object sender, EventArgs e)
        {
            try
            {
                canScroll = false;
                TextLocation = TextLocation;
            }
            finally
            {
                canScroll = true;
            }
        }

        #endregion // Text.

        #region Caret Offset.

        /// <summary>
        ///     DependencyProperty for the TextEditorCaretOffset binding.
        /// </summary>
        public static DependencyProperty CaretOffsetProperty = DependencyProperty.Register("CaretOffset",
            typeof(int),
            typeof(CodeEditor),
            new PropertyMetadata((obj, args) =>
            {
                CodeEditor target = (CodeEditor) obj;
                if (target.CaretOffset != (int) args.NewValue)
                    target.CaretOffset = (int) args.NewValue;
            }));

        /// <summary>
        ///     Access to the SelectionStart property.
        /// </summary>
        public new int CaretOffset
        {
            get => base.CaretOffset;
            set => SetValue(CaretOffsetProperty, value);
        }

        #endregion // Caret Offset.

        #region Selection.

        /// <summary>
        ///     DependencyProperty for the TextLocation. Setting this value
        ///     will scroll the TextEditor to the desired TextLocation.
        /// </summary>
        public static readonly DependencyProperty TextLocationProperty = DependencyProperty.Register("TextLocation",
            typeof(TextLocation),
            typeof(CodeEditor),
            new PropertyMetadata((obj, args) =>
            {
                CodeEditor target = (CodeEditor) obj;
                TextLocation loc = (TextLocation) args.NewValue;
                if (canScroll)
                    target.ScrollTo(loc.Line, loc.Column);
            }));

        /// <summary>
        ///     Get or set the TextLocation. Setting will scroll to that location.
        /// </summary>
        public TextLocation TextLocation
        {
            get => Document.GetLocation(SelectionStart);
            set => SetValue(TextLocationProperty, value);
        }

        /// <summary>
        ///     DependencyProperty for the TextEditor SelectionLength property.
        /// </summary>
        public static readonly DependencyProperty SelectionLengthProperty = DependencyProperty.Register("SelectionLength",
            typeof(int),
            typeof(CodeEditor),
            new PropertyMetadata((obj, args) =>
            {
                CodeEditor target = (CodeEditor) obj;
                if (target.SelectionLength != (int) args.NewValue)
                {
                    target.SelectionLength = (int) args.NewValue;
                    target.Select(target.SelectionStart, (int) args.NewValue);
                }
            }));

        /// <summary>
        ///     Access to the SelectionLength property.
        /// </summary>
        public new int SelectionLength
        {
            get => base.SelectionLength;
            set => SetValue(SelectionLengthProperty, value);
        }

        /// <summary>
        ///     DependencyProperty for the TextEditor SelectionStart property.
        /// </summary>
        public static readonly DependencyProperty SelectionStartProperty = DependencyProperty.Register("SelectionStart",
            typeof(int),
            typeof(CodeEditor),
            new PropertyMetadata((obj, args) =>
            {
                CodeEditor target = (CodeEditor) obj;
                if (target.SelectionStart != (int) args.NewValue)
                {
                    target.SelectionStart = (int) args.NewValue;
                    target.Select((int) args.NewValue, target.SelectionLength);
                }
            }));

        /// <summary>
        ///     Access to the SelectionStart property.
        /// </summary>
        public new int SelectionStart
        {
            get => base.SelectionStart;
            set => SetValue(SelectionStartProperty, value);
        }

        #endregion // Selection.

        #region Raise Property Changed.

        /// <summary>
        ///     Implement the INotifyPropertyChanged event handler.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged([CallerMemberName]
            string caller = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }

        #endregion // Raise Property Changed.
    }
}