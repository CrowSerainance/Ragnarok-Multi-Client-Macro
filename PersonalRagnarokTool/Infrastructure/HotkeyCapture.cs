using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PersonalRagnarokTool.Services;

namespace PersonalRagnarokTool.Infrastructure;

public static class HotkeyCapture
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(HotkeyCapture),
            new PropertyMetadata(false, OnIsEnabledChanged));

    private static readonly DependencyProperty IsArmedProperty =
        DependencyProperty.RegisterAttached(
            "IsArmed",
            typeof(bool),
            typeof(HotkeyCapture),
            new PropertyMetadata(false));

    private static readonly DependencyProperty PreviousTextProperty =
        DependencyProperty.RegisterAttached(
            "PreviousText",
            typeof(string),
            typeof(HotkeyCapture),
            new PropertyMetadata(string.Empty));

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    private static bool GetIsArmed(DependencyObject element) => (bool)element.GetValue(IsArmedProperty);

    private static void SetIsArmed(DependencyObject element, bool value) => element.SetValue(IsArmedProperty, value);

    private static string GetPreviousText(DependencyObject element) => (string)element.GetValue(PreviousTextProperty);

    private static void SetPreviousText(DependencyObject element, string value) => element.SetValue(PreviousTextProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not System.Windows.Controls.TextBox textBox)
        {
            return;
        }

        if (e.NewValue is true)
        {
            textBox.IsReadOnly = true;
            textBox.Cursor = System.Windows.Input.Cursors.Hand;
            textBox.PreviewMouseDown += OnPreviewMouseDown;
            textBox.PreviewKeyDown += OnPreviewKeyDown;
            textBox.LostKeyboardFocus += OnLostKeyboardFocus;
        }
        else
        {
            textBox.PreviewMouseDown -= OnPreviewMouseDown;
            textBox.PreviewKeyDown -= OnPreviewKeyDown;
            textBox.LostKeyboardFocus -= OnLostKeyboardFocus;
        }
    }

    private static void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox textBox || !GetIsEnabled(textBox))
        {
            return;
        }

        SetPreviousText(textBox, textBox.Text ?? string.Empty);
        SetIsArmed(textBox, true);
        textBox.Text = "Press key...";
        textBox.Focus();
        textBox.SelectAll();
        e.Handled = true;
    }

    private static void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox textBox || !GetIsEnabled(textBox) || !GetIsArmed(textBox))
        {
            return;
        }

        textBox.Text = GetPreviousText(textBox);
        UpdateBinding(textBox);
        SetIsArmed(textBox, false);
    }

    private static void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox textBox || !GetIsEnabled(textBox) || !GetIsArmed(textBox))
        {
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape)
        {
            textBox.Text = GetPreviousText(textBox);
            UpdateBinding(textBox);
            SetIsArmed(textBox, false);
            e.Handled = true;
            return;
        }

        if (key is Key.Back or Key.Delete)
        {
            textBox.Text = string.Empty;
            UpdateBinding(textBox);
            SetIsArmed(textBox, false);
            e.Handled = true;
            return;
        }

        var normalized = VirtualKeyMap.NormalizeKeyName(key);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        textBox.Text = normalized;
        UpdateBinding(textBox);
        SetIsArmed(textBox, false);
        e.Handled = true;
    }

    private static void UpdateBinding(System.Windows.Controls.TextBox textBox)
    {
        textBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();
    }
}
