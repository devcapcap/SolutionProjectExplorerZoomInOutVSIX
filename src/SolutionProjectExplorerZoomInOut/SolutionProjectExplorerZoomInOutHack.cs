using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

//"C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\IDE\devenv.exe"
//C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe
//https://learn.microsoft.com/fr-fr/visualstudio/extensibility/extensibility-hello-world?view=vs-2022#create-an-extensibility-project
namespace SolutionProjectExplorerZoomInOutVSIX
{
    public sealed class SolutionProjectExplorerZoomInOutHack : AsyncPackage
    {
        private readonly AsyncPackage _package;
        
        const string CLASS_NAME_SOLUTION_PIVOT_NAVIGATOR = "SolutionPivotNavigator";
        const string CLASS_NAME_SOLUTION_PIVOT_TREE_VIEW = "SolutionPivotTreeView";
        
        const string SLIDER_ID = "sldZoom";
        const string  COL_NAME_0= "colSliderZoomInOut0";
        const string  COL_NAME_1= "colSliderZoomInOut1";

        const double SLIDER_MINIMUN_VALUE = 1.0;
        const double SLIDER_MAX_VALUE = 5.0;
        const double SLIDER_DEFAULT_VALUE = 1.0;


        private static Slider _currentSlider;
        private static RoutedPropertyChangedEventHandler<double> _sliderValueChangedHandler;
        private static DependencyObject _solutionPivotNavigator;
        private static DependencyObject _solutionPivotTreeView;

        public SolutionProjectExplorerZoomInOutHack(AsyncPackage package)
        {
            this._package = package ?? throw new ArgumentNullException(nameof(package));
        }

        private static bool IsSliderAlreadyPresentRemoveIt()
        {
            if (_currentSlider != null && _sliderValueChangedHandler != null)
            {
                DependencyObject obj = VisualTreeHelper.GetChild(_solutionPivotNavigator, 0);
                if (obj is Grid grid)
                {
                    ColumnDefinition columnDefinition0 = grid.ColumnDefinitions.FirstOrDefault(c => c.Name == COL_NAME_0);
                    ColumnDefinition columnDefinition1 = grid.ColumnDefinitions.FirstOrDefault(c => c.Name == COL_NAME_1);
                    if (columnDefinition0 != null && columnDefinition1 != null)
                    {
                        var parentPanel = _currentSlider.Parent as System.Windows.Controls.Panel;
                        if (parentPanel != null)
                        {
                            _currentSlider.Value = 1.0;
                            _currentSlider.ValueChanged -= _sliderValueChangedHandler;
                            parentPanel.Children.Remove(_currentSlider);
                        }
                        _currentSlider = null;
                        _sliderValueChangedHandler = null;
                        
                        grid.ColumnDefinitions.Remove(columnDefinition1);
                        grid.ColumnDefinitions.Remove(columnDefinition0);

                        _solutionPivotTreeView = null;
                        _solutionPivotNavigator = null;
                        columnDefinition0 = null;
                        columnDefinition1 = null;

                        return true;
                    }
                    
                }
            }
            return false;
        }

        private void SetupZoomInOut() 
        {
            if (_solutionPivotNavigator == null)
                return;
            if (_solutionPivotTreeView == null)
                return;

            FrameworkElement treeView = _solutionPivotTreeView as FrameworkElement;

            treeView.LayoutTransform = new ScaleTransform(1.0, 1.0);
            treeView.RenderTransformOrigin = new Point(0, 0);
            TextOptions.SetTextFormattingMode(treeView, TextFormattingMode.Ideal);
            TextOptions.SetTextRenderingMode(treeView, TextRenderingMode.ClearType);
            TextOptions.SetTextHintingMode(treeView, TextHintingMode.Animated);
            RenderOptions.SetBitmapScalingMode(treeView, BitmapScalingMode.HighQuality);

            DependencyObject obj = VisualTreeHelper.GetChild(_solutionPivotNavigator, 0);
            if (obj is Grid grid)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Name = COL_NAME_0 });
                ColumnDefinition sliderColumn = new ColumnDefinition { Name = COL_NAME_1, Width = GridLength.Auto };
                grid.ColumnDefinitions.Add(sliderColumn);

                _currentSlider = new Slider
                {
                    Minimum = SLIDER_MINIMUN_VALUE,
                    Maximum = SLIDER_MAX_VALUE, // Reduce from 100 to 5 (1 = 100%, 5 = 500% de zoom, more realist for text)
                    Value = SLIDER_DEFAULT_VALUE
                };

                Action<Slider, FrameworkElement> onDo = (sd, fwe) =>
                {
                    if (fwe == null)
                        return;

                    ScaleTransform scaler = fwe.LayoutTransform as ScaleTransform ?? fwe.RenderTransform as ScaleTransform;
                    if (scaler != null)
                    {
                        scaler.ScaleX = sd.Value;
                        scaler.ScaleY = sd.Value;
                    }
                };
                _currentSlider.Name = SLIDER_ID;
                _currentSlider.Margin = new Thickness(5.0d);
                _currentSlider.Orientation = Orientation.Vertical;
                _currentSlider.Background = Brushes.DarkRed;
                grid.Children.Add(_currentSlider);
                Grid.SetColumn(_currentSlider, 1);

                FrameworkElement targetVisualElement = _currentSlider as FrameworkElement;
                _sliderValueChangedHandler = (sender, e) => onDo(_currentSlider, treeView);
                _currentSlider.ValueChanged += _sliderValueChangedHandler;
            }

            obj = VisualTreeHelper.GetChild(_solutionPivotTreeView, 0);
            if (obj is ScrollViewer scrollViewer)
            {
                scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
                obj = VisualTreeHelper.GetChild(scrollViewer, 0);
                scrollViewer.PreviewMouseWheel += (s, e) =>
                {
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        e.Handled = true;
                        if (_currentSlider != null)
                        {
                            double factor = e.Delta > 0 ? 1.1 : (1.0 / 1.1);
                            _currentSlider.Value = Math.Min(_currentSlider.Maximum, Math.Max(_currentSlider.Minimum, _currentSlider.Value * factor));
                        }
                    }
                };
            }
            
        }
        /// <summary>
        /// Explorer the projecty view panel and a slider to the existing panel
        /// </summary>
        public async System.Threading.Tasks.Task HackSolutionExplorerPanelAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            try
            {
                if (!(await _package.GetServiceAsync(typeof(SVsUIShell)) is IVsUIShell uiShell))
                    return;

                uiShell.GetDialogOwnerHwnd(out IntPtr parent);
                if (parent == IntPtr.Zero)
                    return;

                var hwndSource = HwndSource.FromHwnd(parent);
                if (hwndSource == null)
                    return;
                System.Windows.Window window = (System.Windows.Window)hwndSource.RootVisual;
                
                _solutionPivotNavigator = FindVisualByTypeName(window, CLASS_NAME_SOLUTION_PIVOT_NAVIGATOR);
                if (_solutionPivotNavigator == null)
                    return;

                _solutionPivotTreeView = FindVisualByTypeName(window, CLASS_NAME_SOLUTION_PIVOT_TREE_VIEW);
                if (_solutionPivotTreeView == null)
                    return;

                //remove properly if  already activate
                if (IsSliderAlreadyPresentRemoveIt())
                    return;

                SetupZoomInOut();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error --> {ex.Message} \n StackTrace --> \n {ex.StackTrace ?? "No trace"}");
                System.Windows.MessageBox.Show("Cannot add the Zoom In and Zoom Out on the solution Explorer");
            }
        }

        private static DependencyObject FindVisualByTypeName(DependencyObject parent, string typeName)
        {
            if (parent == null)
                return null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child != null && child.GetType().Name == typeName)
                    return child;

                var foundChild = FindVisualByTypeName(child, typeName);
                if (foundChild != null)
                    return foundChild;
            }
            return null;
        }
    }
}
