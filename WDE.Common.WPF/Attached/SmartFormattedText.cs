using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace WDE.Common.WPF.Attached
{
    public class SmartFormattedText
    {
        public static string GetText(DependencyObject obj) => (string)obj.GetValue(TextProperty);
        public static void SetText(DependencyObject obj, string value) => obj.SetValue(TextProperty, value);
        public static readonly DependencyProperty TextProperty =
        DependencyProperty.RegisterAttached("Text", typeof(string), typeof(SmartFormattedText), new PropertyMetadata("", AllowOnlyString));

        public static Style GetParamStyle(DependencyObject obj) => (Style)obj.GetValue(ParamStyleProperty);
        public static void SetParamStyle(DependencyObject obj, Style value) => obj.SetValue(ParamStyleProperty, value);
        public static readonly DependencyProperty ParamStyleProperty =
        DependencyProperty.RegisterAttached("ParamStyle", typeof(Style), typeof(SmartFormattedText), new PropertyMetadata(null, AllowOnlyString));

        public static IList<object> GetContextArray(DependencyObject obj) => (IList<object>)obj.GetValue(ContextArrayProperty);
        public static void SetContextArray(DependencyObject obj, IList<object> value) => obj.SetValue(ContextArrayProperty, value);
        public static readonly DependencyProperty ContextArrayProperty =
            DependencyProperty.RegisterAttached("ContextArray", typeof(IList<object>), typeof(SmartFormattedText), new PropertyMetadata(null, AllowOnlyString));


        public static Style GetSourceStyle(DependencyObject obj) => (Style)obj.GetValue(SourceStyleProperty);
        public static void SetSourceStyle(DependencyObject obj, Style value) => obj.SetValue(SourceStyleProperty, value);
        public static readonly DependencyProperty SourceStyleProperty =
        DependencyProperty.RegisterAttached("SourceStyle", typeof(Style), typeof(SmartFormattedText), new PropertyMetadata(null, AllowOnlyString));


        private static void AllowOnlyString(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBlock tb)
            {
                var text = GetText(d);
                var paramStyle = GetParamStyle(d);
                var sourceStyle = GetSourceStyle(d);
                var contextArray = GetContextArray(d);

                tb.Inlines.Clear();
                if (text == null)
                    return;

                bool changeRun = false;
                int start = 0;
                State state = State.Text;
                Styles currentStyle = Styles.Normal;
                int currentContextIndex = -1;
                Run? lastRun = null;

                void Append(string s)
                {
                    if (lastRun != null && !changeRun)
                    {
                        lastRun.Text += s;
                        return;
                    }

                    lastRun = new Run(s);

                    if (contextArray != null && currentContextIndex >= 0 && currentContextIndex < contextArray.Count)
                        lastRun.DataContext = contextArray[currentContextIndex];
                    else
                        lastRun.DataContext = null;

                    if (currentStyle.HasFlag(Styles.Parameter) && paramStyle != null)
                        lastRun.Style = paramStyle;

                    if (currentStyle.HasFlag(Styles.Source) && sourceStyle != null)
                        lastRun.Style = sourceStyle;

                    changeRun = false;

                    tb.Inlines.Add(lastRun);
                }

                for (int i = 0; i < text.Length; i++)
                {
                    char l = text[i];
                    if (l == '\\' && state == State.Text)
                    {
                        Append(text.Substring(start, i - start));
                        start = i + 1;
                        state = State.Slash;
                    }
                    else if (l == '[' && state == State.Text)
                    {
                        Append(text.Substring(start, i - start));
                        changeRun = true;
                        currentContextIndex = -1;
                        start = i + 1;
                        state = State.OpeningTag;
                    }
                    else if (l == '/' && state == State.OpeningTag)
                    {
                        start = i + 1;
                        state = State.InClosingTag;
                    }
                    else if (l != ']' && state == State.OpeningTag)
                    {
                        state = State.InTag;
                    }
                    else if (l == ']' && state == State.InTag)
                    {
                        if (text[start] == 'p')
                        {
                            currentStyle = currentStyle | Styles.Parameter;
                        }
                        else if (text[start] == 's')
                        {
                            currentStyle = currentStyle | Styles.Source;
                        }

                        if (text[start + 1] == '=' && char.IsDigit(text[start + 2]))
                            currentContextIndex = text[start + 2] - '0';

                        start = i + 1;
                        state = State.Text;
                    }
                    else if (l == ']' && state == State.InClosingTag)
                    {
                        if (text[start] == 'p')
                            currentStyle = currentStyle & ~Styles.Parameter;
                        else if (text[start] == 's')
                            currentStyle = currentStyle & ~Styles.Source;

                        start = i + 1;
                        state = State.Text;
                    }
                    else if (state == State.Slash)
                    {
                        state = State.Text;
                    }
                }

                if (state == State.Text)
                    Append(text.Substring(start));
            }
        }

        enum State
        {
            Text,
            Slash,
            OpeningTag,
            InTag,
            InClosingTag
        }

        [Flags]
        enum Styles
        {
            Normal = 0,
            Source = 1,
            Parameter = 2
        }
    }
}