using System;
using System.IO;
using System.Text;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using AvaloniaStyles;
using WDE.PacketViewer.Structures;
using WDE.PacketViewer.ViewModels;

namespace WDE.PacketViewer.Avalonia.Views
{
    public class PacketDocumentView : UserControl
    {
        public PacketDocumentView()
        {
            InitializeComponent();
        }

        private TextEditor editor = null!;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            editor = this.FindControl<TextEditor>("TextEditor");
            editor.TextArea.TextEntered += TextAreaOnTextEntered;

            editor.Options.AllowScrollBelowDocument = false;
            
            editor.TextArea.AddHandler(KeyDownEvent, Handler, RoutingStrategies.Tunnel);

            editor.TextArea.CommandBindings.Insert(0, new RoutedCommandBinding(new RoutedCommand("Accept", new KeyGesture(Key.Enter)), OnAccept));
        }

        private void Handler(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
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
                var word = GetLastWord(editor.CaretOffset - 2);

                if (word != "packet" && word != "SMSG" && word != "CMSG")
                    return;
                
                _completionWindow = new CompletionWindow(editor.TextArea);
                _completionWindow.Closed += (o, args) => _completionWindow = null;

                var data = _completionWindow.CompletionList.CompletionData;
                if (word == "packet")
                {
                    data.Add(new CompletionData("id", "(int) Number of packet"));
                    data.Add(new CompletionData("text", "(string) Raw packet output from Packet Parser"));
                    data.Add(new CompletionData("opcode", "(string) String representation of packet opcode (e.g. SMSG_CHAT)")); 
                    data.Add(new CompletionData("entry", "(int) Entry of main object in packet. E.g. caster for SMSG_SPELL_GO. 0 for packets without 'main object' (e.g. SMSG_UPDATE_OBJECT)"));   
                }
                else if (word == "SMSG")
                {
                    foreach (var smsg in Enum.GetNames<ServerOpcodes>())
                        data.Add(new CompletionData(smsg, null));
                }
                else if (word == "CMSG")
                {
                    foreach (var cmsg in Enum.GetNames<ClientOpcodes>())
                        data.Add(new CompletionData(cmsg, null));
                }

                _completionWindow.Show();
            }
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

        private class CompletionData : ICompletionData
        {
            public CompletionData(string text, string? description)
            {
                Text = text;
                Description = description;
            }

            public IBitmap? Image => null;

            public string Text { get; }

            // Use this property if you want to show a fancy UIElement in the list.
            public object Content => new TextBlock() {Text = Text};

            public object? Description { get; }

            public double Priority { get; } = 0;

            public void Complete(TextArea textArea, ISegment completionSegment,
                EventArgs insertionRequestEventArgs)
            {
                textArea.Document.Replace(completionSegment, Text);
            }
        }
    }
}