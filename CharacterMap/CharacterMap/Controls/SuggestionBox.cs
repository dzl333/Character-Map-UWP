﻿using CharacterMap.Core;
using CharacterMap.Helpers;
using CharacterMap.Models;
using CharacterMap.Services;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace CharacterMap.Controls
{
    public sealed class SuggestionBox : Control
    {
        public event EventHandler<FlowDirection> DetectedFlowDirectionChanged;

        #region Dependency Properties

        #region IsCharacterPickerEnabled

        public bool IsCharacterPickerEnabled
        {
            get { return (bool)GetValue(IsCharacterPickerEnabledProperty); }
            set { SetValue(IsCharacterPickerEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsCharacterPickerEnabledProperty =
            DependencyProperty.Register(nameof(IsCharacterPickerEnabled), typeof(bool), typeof(SuggestionBox), new PropertyMetadata(true, (d,e) =>
            {
                ((SuggestionBox)d).UpdatePickerState(true);
            }));

        #endregion

        #region PlaceholderText

        public string PlaceholderText
        {
            get { return (string)GetValue(PlaceholderTextProperty); }
            set { SetValue(PlaceholderTextProperty, value); }
        }

        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(SuggestionBox), new PropertyMetadata(string.Empty));

        #endregion

        public FlowDirection DetectedFlowDirection
        {
            get { return (FlowDirection)GetValue(DetectedFlowDirectionProperty); }
            set { SetValue(DetectedFlowDirectionProperty, value); }
        }

        public static readonly DependencyProperty DetectedFlowDirectionProperty =
            DependencyProperty.Register(nameof(DetectedFlowDirection), typeof(FlowDirection), typeof(SuggestionBox), new PropertyMetadata(FlowDirection.LeftToRight, (d, e) =>
            {
                ((SuggestionBox)d).DetectedFlowDirectionChanged?.Invoke(d, (FlowDirection)e.NewValue);
            }));

        public CharacterRenderingOptions RenderingOptions
        {
            get { return (CharacterRenderingOptions)GetValue(RenderingOptionsProperty); }
            set { SetValue(RenderingOptionsProperty, value); }
        }

        public static readonly DependencyProperty RenderingOptionsProperty =
            DependencyProperty.Register(nameof(RenderingOptions), typeof(CharacterRenderingOptions), typeof(SuggestionBox), new PropertyMetadata(null));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(SuggestionBox), new PropertyMetadata(string.Empty));

        public object ItemsSource
        {
            get { return (object)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(SuggestionBox), new PropertyMetadata(null));

        #endregion

        private AutoSuggestBox _suggestBox = null;

        private Button _suggestButton = null;

        public SuggestionBox()
        {
            this.DefaultStyleKey = typeof(SuggestionBox);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            
            // Unhook old things
            if (_suggestBox is not null)
            {
                _suggestBox.PointerWheelChanged -= _suggestBox_PointerWheelChanged;
                _suggestBox.TextChanged -= Box_TextChanged;
                _suggestBox.SizeChanged -= _suggestBox_SizeChanged;
                _suggestBox.GotFocus -= _suggestBox_GotFocus;
                _suggestBox.LostFocus -= _suggestBox_LostFocus;
            }

            if (_suggestButton is not null)
                _suggestButton.Click -= _suggestButton_Click;


            if (this.ItemsSource is IEnumerable<Suggestion> sug)
            {
                Text = sug.FirstOrDefault()?.Text ?? string.Empty;
            }

            UpdatePickerState(false);

            // Hook new things
            _suggestBox = this.GetTemplateChild("SuggestionBox") as AutoSuggestBox;
            if (_suggestBox is not null)
            {
                //var h = new PointerEventHandler(_suggestBox_PointerWheelChanged);
                //_suggestBox.AddHandler(FrameworkElement.PointerWheelChangedEvent, h, true);
                _suggestBox.PointerWheelChanged += _suggestBox_PointerWheelChanged;
                _suggestBox.TextChanged += Box_TextChanged;
                _suggestBox.SizeChanged += _suggestBox_SizeChanged;
                _suggestBox.GotFocus += _suggestBox_GotFocus;
                _suggestBox.LostFocus += _suggestBox_LostFocus;
                
            }

            if (this.GetTemplateChild("EditButton") is ListViewItem edit)
            {
                edit.Tapped -= Item_Tapped;
                edit.Tapped += Item_Tapped;
            }

            _suggestButton = this.GetTemplateChild("SuggestionButton") as Button;
            if (_suggestButton is not null)
                _suggestButton.Click += _suggestButton_Click;
        }

        void UpdatePickerState(bool animate)
        {
            VisualStateManager.GoToState(this, IsCharacterPickerEnabled ? "PickerVisibleState" : "PickerHiddenState", animate);
        }

        private void _suggestBox_LostFocus(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "NotFocused", true);
        }

        private void _suggestBox_GotFocus(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "Focused", true);
        }

        private void _suggestBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _suggestBox.SizeChanged -= _suggestBox_SizeChanged;

            if (_suggestBox.GetFirstDescendantOfType<TextBox>() is TextBox t)
            {
                t.TextChanged -= T_TextChanged;
                t.TextChanged += T_TextChanged;
            }

            //if (_suggestBox.GetFirstDescendantOfType<ScrollViewer>() is ScrollViewer tb)
            //{
            //    tb.DirectManipulationStarted += Tb_DirectManipulationStarted;
            //    tb.ManipulationStarting += Tb_ManipulationStarting;
            //    tb.ManipulationMode = ManipulationModes.None;
            //}

            if (_suggestBox.GetFirstDescendantOfType<Popup>() is Popup p)
            {
                p.Opened -= P_Opened;
                p.Opened += P_Opened;
            }
        }

        private void Item_Tapped(object sender, TappedRoutedEventArgs e)
        {
            WeakReferenceMessenger.Default.Send(new EditSuggestionsRequested());
        }

        private void T_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (sender is TextBox box && box.Text is not null && box.Text.Length > 0)
            {
                try
                {
                    // We detect LTR or RTL by checking where the first character is
                    var rect = box.GetRectFromCharacterIndex(0, false);

                    // Note: Probably we can check if rect.X == 0, but I haven't properly tested it.
                    //       The current logic will fail with a small width TextBox with small fonts,
                    //       but that's not a concern for our current use cases.
                    DetectedFlowDirection = rect.X <= box.ActualWidth / 4.0 ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;
                    //if (_presenter is not null)
                    //    _presenter.FlowDirection = DetectedFlowDirection;
                }
                catch
                {
                    // Don't care
                }
            }
        }

        private void P_Opened(object sender, object e)
        {
            if (sender is Popup p)
            {
                // Fix offset problems due to our global Transform3D
                p.Translation = new(0, 0, 0);

                // Force to be width of this entire control
                if (p.Child is FrameworkElement f)
                    f.Width = this.ActualWidth;
                
                //p.MinWidth = this.ActualWidth;
            }
        }

        bool block = false;

        Throttler _throttle = new();

        private void _suggestBox_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            _throttle.Throttle(200, () =>
            {
                var point = e.GetCurrentPoint(this);
                var props = point.Properties;
                if (props.IsHorizontalMouseWheel)
                    return;

                var delta = props.MouseWheelDelta;
                Debug.WriteLine($"Wheel moved {delta}");
                if (delta > -2 && delta < 2)
                    return;


                int idx = -1;
                if (ItemsSource is IReadOnlyList<Suggestion> items)
                {
                    if (items.FirstOrDefault(i => i.Text == Text) is Suggestion match)
                        idx = items.ToList().IndexOf(match);

                    idx += delta < 0 ? 1 : -1;
                    if (idx >= items.Count)
                        idx = 0;
                    else if (idx <= -1)
                        idx = items.Count - 1;

                    Text = items[idx].Text;
                    _suggestBox.IsSuggestionListOpen = false;

                    block = true;
                }
            });
        }

        private void _suggestButton_Click(object sender, RoutedEventArgs e)
        {
            if (_suggestBox is null)
                return;

            _suggestBox.ItemsSource = ItemsSource;
            _suggestBox.IsSuggestionListOpen = true;
        }

        Debouncer _debouncer = new Debouncer();

        private void Box_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (block)
            {
                block = false;
                return;
            }

            if (args.Reason is AutoSuggestionBoxTextChangeReason.SuggestionChosen or AutoSuggestionBoxTextChangeReason.ProgrammaticChange)
            {
                sender.IsSuggestionListOpen = false;
                return;
            }

            string text = sender.Text;

            _debouncer.Debounce(64, () =>
            {
                if (args.CheckCurrent())
                {
                    if (ItemsSource is IEnumerable<Suggestion> options)
                    {
                        var items = options.Where(op => op.Text.Contains(text, StringComparison.InvariantCultureIgnoreCase)).ToList();
                        if (args.CheckCurrent())
                        {
                            if ((items.Count == 1 && items[0].Text == Text) is false)
                            {
                                sender.ItemsSource = items;
                                sender.IsSuggestionListOpen = true;
                            }
                        }
                    }
                }
            });
           
        }
    }
}
