using System;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using WDE.PacketViewer.Structures;
using WDE.PacketViewer.ViewModels;

namespace WDE.PacketViewer.Avalonia.Views
{
    public partial class PacketDocumentView : UserControl
    {
        public PacketDocumentView()
        {
            InitializeComponent();
        }

        private TextEditor editor = null!;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            editor = this.GetControl<TextEditor>("TextEditor");
            editor.TextArea.TextEntered += TextAreaOnTextEntered;

            editor.Options.AllowScrollBelowDocument = false;
            
            editor.TextArea.AddHandler(KeyDownEvent, Handler, RoutingStrategies.Tunnel);

            editor.TextArea.CommandBindings.Insert(0, new RoutedCommandBinding(new RoutedCommand("Accept", new KeyGesture(Key.Enter)), OnAccept));
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
        }

        private void Handler(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (_completionWindow != null)
                {
                    if (_completionWindow.CompletionList.CurrentList.Count == 0)
                        _completionWindow.Close();
                    else
                        return;
                }
                
                if (e.KeyModifiers == KeyModifiers.Shift)
                {
                    editor.TextArea.PerformTextInput("\n");
                }
                else
                {
                    ((PacketDocumentViewModel)DataContext!).ApplyFilterCommand.ExecuteAsync();
                }
                e.Handled = true;
            }
        }

        private static void OnAccept(object? sender, ExecutedRoutedEventArgs args)
        {
            var editor = (TextEditor?) sender;
            args.Handled = true;
            ((PacketDocumentViewModel) editor!.DataContext!).ApplyFilterCommand.ExecuteAsync();
        }
        
        private CompletionWindow? _completionWindow;
        private void TextAreaOnTextEntered(object? sender, TextInputEventArgs e)
        {
            if (e.Text == ".")
            {
                var word = GetLastWord(editor.CaretOffset - 2).ToLower();

                if (word != "packet" && word != "smsg" && word != "cmsg")
                    return;
                
                _completionWindow = new CompletionWindow(editor.TextArea);
                _completionWindow.Styles.Add(new StyleInclude(new Uri("resm:Styles?assembly=WDE.PacketViewer.Avalonia")){Source = new Uri("avares://WDE.PacketViewer.Avalonia/Generic.axaml")});
                _completionWindow.Closed += CompletionWindowOnClosed;

                var data = _completionWindow.CompletionList.CompletionData;
                if (word == "packet")
                {
                    data.Add(new PacketCompletionData("id", "(int) Number of packet"));
                    data.Add(new PacketCompletionData("originalId", "(int) Original number of packet. If split UPDATE_OBJECT is disabled, then this is the same as Id. If splitting is enabled, then this field contains original packet number"));
                    data.Add(new PacketCompletionData("text", "(string) Raw packet output from Packet Parser"));
                    data.Add(new PacketCompletionData("opcode", "(string) String representation of packet opcode (e.g. SMSG_CHAT)"));
                    data.Add(new PacketCompletionData("entry", "(int) Entry of main object in packet. E.g. caster for SMSG_SPELL_GO. 0 for packets without 'main object' (e.g. SMSG_UPDATE_OBJECT)"));
                }
                else if (word == "smsg")
                {
                    foreach (var smsg in Enum.GetNames<ServerOpcodes>())
                        data.Add(new PacketCompletionData(smsg, null));
                }
                else if (word == "cmsg")
                {
                    foreach (var cmsg in Enum.GetNames<ClientOpcodes>())
                        data.Add(new PacketCompletionData(cmsg, null));
                }

                _completionWindow.Show();
            }
        }

        private void CompletionWindowOnClosed(object? sender, EventArgs e)
        {
            if (_completionWindow != null)
                _completionWindow.Closed -= CompletionWindowOnClosed;
            _completionWindow = null;
        }

        private string GetLastWord(int offset)
        {
            StringBuilder sb = new();
            while (offset >= 0)
            {
                char c = editor.Document.GetCharAt(offset);
                if (char.IsLetter(c))
                    sb.Insert(0, c);
                else
                    break;
                offset--;
            }

            return sb.ToString();
        }
    }

    public class PacketCompletionData : ICompletionData
    {
        public PacketCompletionData(string text, string? description)
        {
            Text = text;
            Description = description;
        }

        public IImage? Image => null;

        public string Text { get; }

        public object? Content => null;

        public object? Description { get; }

        public double Priority { get; } = 0;

        public void Complete(TextArea textArea, ISegment completionSegment,
            EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, Text);
        }
    }
}