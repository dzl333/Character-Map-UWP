﻿using CharacterMap.Core;
using CharacterMap.Helpers;
using CharacterMap.Models;
using CharacterMap.Services;
using CharacterMap.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace CharacterMap.Views
{
    public sealed partial class CalligraphyView : ViewBase
    {
        public CalligraphyViewModel ViewModel { get; }

        public CalligraphyView(CharacterRenderingOptions options)
        {
            this.InitializeComponent();

            ViewModel = new CalligraphyViewModel(options);

            this.Loaded += OnLoaded;

            ResourceHelper.GoToThemeState(this);
            LeakTrackingService.Register(this);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "NormalState", false);
            VisualStateManager.GoToState(this, "OverlayState", false);
            TitleBarHelper.SetTitle(Presenter.Title);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (ViewStates.CurrentState == OverlayState)
                GoToSideBySide();
            else
                GoToOverlay();
        }

        private void GoToOverlay()
        {
            VisualStateManager.GoToState(this, nameof(OverlayState), false);

            var gv = Guide.EnableCompositionTranslation().GetElementVisual();
            var iv = Ink.EnableCompositionTranslation().GetElementVisual();

            gv.StopAnimation(CompositionFactory.TRANSLATION);
            iv.StopAnimation(CompositionFactory.TRANSLATION);

            gv.SetTranslation(0, 0, 0);
            iv.SetTranslation(0, 0, 0);

            gv.Scale = new System.Numerics.Vector3(1f);
            iv.Scale = new System.Numerics.Vector3(1f);
        }

        void GoToSideBySide()
        {
            VisualStateManager.GoToState(this, nameof(SideBySideState), false);

            var v = PresentationRoot.GetElementVisual();
            var gv = Guide.EnableCompositionTranslation().GetElementVisual();
            var iv = Ink.EnableCompositionTranslation().GetElementVisual();

            gv.StartAnimation(
                gv.CreateExpressionAnimation(CompositionFactory.TRANSLATION)
                .SetExpression("Vector3(-(v.Size.X / 4f), 0, 0)")
                .SetParameter("v", v));

            iv.StartAnimation(
                iv.CreateExpressionAnimation(CompositionFactory.TRANSLATION)
                .SetExpression("Vector3((v.Size.X / 4f), 0, 0)")
                .SetParameter("v", v));

            CompositionFactory.StartCentering(gv);
            CompositionFactory.StartCentering(iv);

            gv.Scale = new System.Numerics.Vector3(0.5f, 0.5f, 1f);
            iv.Scale = new System.Numerics.Vector3(0.5f, 0.5f, 1f);
        }

        public async void AddToHistory()
        {
            // 1. Create a history item
            CalligraphyHistoryItem h = new(Ink.InkPresenter.StrokeContainer.GetStrokes());

            // 2. Render a thumbnail of the drawing
            using var m = new MemoryStream();
            await Ink.InkPresenter.StrokeContainer.SaveAsync(m.AsOutputStream());
            m.Seek(0, SeekOrigin.Begin);
            var b = new BitmapImage();
            await b.SetSourceAsync(m.AsRandomAccessStream());

            // 3. Add history item
            h.Thumbnail = b;
            ViewModel.Histories.Add(h);
        }

        private void AddHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            AddToHistory();
            Ink.InkPresenter.StrokeContainer.Clear();
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Ink.InkPresenter.StrokeContainer.Clear();
            if (e.ClickedItem is CalligraphyHistoryItem h)
            {
                Ink.InkPresenter.StrokeContainer.AddStrokes(h.GetStrokes());
                GoToOverlay();
            }
        }
    }

    public partial class CalligraphyView
    {
        public static async Task<WindowInformation> CreateWindowAsync(CharacterRenderingOptions options, string text = null)
        {
            static void CreateView(CharacterRenderingOptions v, string t = null)
            {
                CalligraphyView view = new(v);
                view.ViewModel.Text = String.IsNullOrWhiteSpace(t) ? "Hello" : t;
                Window.Current.Content = view;
                Window.Current.Activate();
            }

            var view = await WindowService.CreateViewAsync(() => CreateView(options, text), false);
            await WindowService.TrySwitchToWindowAsync(view, false);

            return view;
        }
    }

    public class CalligraphicPen : InkToolbarCustomPen
    {
        public CalligraphicPen() { }

        protected override InkDrawingAttributes CreateInkDrawingAttributesCore(Brush brush, double width)
        {
            return new InkDrawingAttributes()
            {
                IgnorePressure = false,
                PenTip = PenTipShape.Circle,
                Size = new (width, 2.0f * width),
                Color = (brush as SolidColorBrush)?.Color ?? Colors.Black,
                PenTipTransform = Matrix3x2.CreateRotation((float)(Math.PI * 45d / 180d))
            };
        }
    }
}
