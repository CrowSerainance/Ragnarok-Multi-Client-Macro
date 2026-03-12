using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace PersonalRagnarokTool.Infrastructure;

public static class NumericTextBoxBehavior
{
    private static readonly Regex DigitsRegex = new("^[0-9]+$", RegexOptions.Compiled);
    private static readonly Regex DigitsOrBlankRegex = new("^[0-9]*$", RegexOptions.Compiled);

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(NumericTextBoxBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty AllowBlankProperty =
        DependencyProperty.RegisterAttached(
            "AllowBlank",
            typeof(bool),
            typeof(NumericTextBoxBehavior),
            new PropertyMetadata(false));

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetAllowBlank(DependencyObject element, bool value) => element.SetValue(AllowBlankProperty, value);

    public static bool GetAllowBlank(DependencyObject element) => (bool)element.GetValue(AllowBlankProperty);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not WpfTextBox textBox)
        {
            return;
        }

        if (e.NewValue is true)
        {
            textBox.PreviewTextInput += OnPreviewTextInput;
            textBox.PreviewKeyDown += OnPreviewKeyDown;
            System.Windows.DataObject.AddPastingHandler(textBox, OnPaste);
        }
        else
        {
            textBox.PreviewTextInput -= OnPreviewTextInput;
            textBox.PreviewKeyDown -= OnPreviewKeyDown;
            System.Windows.DataObject.RemovePastingHandler(textBox, OnPaste);
        }
    }

    private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is not WpfTextBox textBox)
        {
            return;
        }

        string candidate = BuildCandidateText(textBox, e.Text);
        e.Handled = !IsValid(textBox, candidate);
    }

    private static void OnPreviewKeyDown(object sender, WpfKeyEventArgs e)
    {
        if (sender is not WpfTextBox)
        {
            return;
        }

        if (e.Key is Key.Space)
        {
            e.Handled = true;
        }
    }

    private static void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (sender is not WpfTextBox textBox)
        {
            return;
        }

        if (!e.DataObject.GetDataPresent(typeof(string)))
        {
            e.CancelCommand();
            return;
        }

        string pastedText = (string)e.DataObject.GetData(typeof(string))!;
        string candidate = BuildCandidateText(textBox, pastedText);
        if (!IsValid(textBox, candidate))
        {
            e.CancelCommand();
        }
    }

    private static string BuildCandidateText(WpfTextBox textBox, string incomingText)
    {
        string text = textBox.Text ?? string.Empty;
        if (textBox.SelectionLength > 0)
        {
            text = text.Remove(textBox.SelectionStart, textBox.SelectionLength);
        }

        return text.Insert(textBox.CaretIndex, incomingText);
    }

    private static bool IsValid(WpfTextBox textBox, string candidate)
    {
        bool allowBlank = GetAllowBlank(textBox);
        if (allowBlank)
        {
            return DigitsOrBlankRegex.IsMatch(candidate);
        }

        return DigitsRegex.IsMatch(candidate);
    }
}
