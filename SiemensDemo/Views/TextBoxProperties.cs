using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SiemensDemo.Views
{
    public static class TextBoxProperties
    {
        // 宣告一個附加屬性 僅可以輸入數字+一個小數點
        public static readonly DependencyProperty IsNumericAndPointProperty =
            DependencyProperty.RegisterAttached("IsNumericAndPoint", typeof(bool), typeof(TextBoxProperties),
                new PropertyMetadata(false, OnIsNumericAndPointChanged));

        public static bool GetIsNumericAndPoint(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsNumericAndPointProperty);
        }

        public static void SetIsNumericAndPoint(DependencyObject obj, bool value)
        {
            obj.SetValue(IsNumericAndPointProperty, value);
        }

        // 當屬性值變更時觸發的方法
        private static void OnIsNumericAndPointChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is System.Windows.Controls.TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    // 啟用只允許數字的邏輯
                    textBox.PreviewTextInput += OnPreviewTextInput;
                    textBox.PreviewKeyDown += OnPreviewKeyDown; // 處理空白鍵
                    DataObject.AddPastingHandler(textBox, OnPasting); // 處理貼上
                }
                else
                {
                    // 移除邏輯
                    textBox.PreviewTextInput -= OnPreviewTextInput;
                    textBox.PreviewKeyDown -= OnPreviewKeyDown;
                    DataObject.RemovePastingHandler(textBox, OnPasting);
                }
            }
        }

        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            // 允許數字，並且只允許一個小數點
            e.Handled = !e.Text.All(c => char.IsDigit(c) || (c == '.' && !textBox.Text.Contains(".")));
        }

        // 處理空白鍵輸入
        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        // 處理貼上事件
        private static void OnPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!text.All(c => char.IsDigit(c) || c == '.'))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        // 宣告一個附加屬性 僅可以輸入數字
        public static readonly DependencyProperty IsIntegerOnlyProperty =
            DependencyProperty.RegisterAttached("IsIntegerOnly", typeof(bool), typeof(TextBoxProperties),
                new PropertyMetadata(false, OnIsIntegerOnlyChanged));

        public static bool GetIsIntegerOnly(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsIntegerOnlyProperty);
        }

        public static void SetIsIntegerOnly(DependencyObject obj, bool value)
        {
            obj.SetValue(IsIntegerOnlyProperty, value);
        }

        private static void OnIsIntegerOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is System.Windows.Controls.TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    // 啟用只允許整數的邏輯
                    textBox.PreviewTextInput += OnIntegerPreviewTextInput;
                    textBox.PreviewKeyDown += OnPreviewKeyDown; // 沿用原本的處理
                    DataObject.AddPastingHandler(textBox, OnIntegerPasting); // 新增專屬的貼上處理
                }
                else
                {
                    // 移除邏輯
                    textBox.PreviewTextInput -= OnIntegerPreviewTextInput;
                    textBox.PreviewKeyDown -= OnPreviewKeyDown;
                    DataObject.RemovePastingHandler(textBox, OnIntegerPasting);
                }
            }
        }

        private static void OnIntegerPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 判斷輸入的字元是否為數字
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private static void OnIntegerPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!text.All(char.IsDigit))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}

