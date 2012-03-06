using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Floaters
{
    public partial class FloatingControl : UserControl
    {
        private readonly Canvas _owner;
        private bool isMouseCaptured;
        private double mouseX;
        private double mouseY;
        private double offsetX;
        private double offsetY;
        private int zIndex = 0;
        Window parentWindow;
        Window dragWindow;

        public FloatingControl(Canvas owner)
        {
            InitializeComponent();

            _owner = owner;

            Chrome.MouseLeftButtonDown += new MouseButtonEventHandler(FloatingControl_MouseLeftButtonDown);
            Chrome.MouseMove += new MouseEventHandler(FloatingControl_MouseMove);
            Chrome.MouseLeftButtonUp += new MouseButtonEventHandler(FloatingControl_MouseLeftButtonUp);

            this.Closed += Control_Closed;
        }

        public event EventHandler Closed;
        public event EventHandler Moved;

        public string Title { get; set; }
        public object Data { get; set; }

        public double Top
        {
            get
            {
                if (Application.Current.IsRunningOutOfBrowser)
                {
                    var window = Window.GetWindow(this);
                    return
                        window != Application.Current.MainWindow
                        ? window.Top
                        : Canvas.GetTop(this);
                }
                else return Canvas.GetTop(this);
            }
        }

        public double Left
        {
            get
            {
                if (Application.Current.IsRunningOutOfBrowser)
                {
                    var window = Window.GetWindow(this);
                    return
                        window != Application.Current.MainWindow
                        ? window.Left
                        : Canvas.GetLeft(this);
                }
                else return Canvas.GetLeft(this);
            }
        }

        public bool IsExternal
        {
            get
            {
                if (Application.Current.IsRunningOutOfBrowser)
                {
                    var window = Window.GetWindow(this);
                    return window != Application.Current.MainWindow;
                }
                else return false;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Chrome.MouseLeftButtonDown -= new MouseButtonEventHandler(FloatingControl_MouseLeftButtonDown);
            Chrome.MouseMove -= new MouseEventHandler(FloatingControl_MouseMove);
            Chrome.MouseLeftButtonUp -= new MouseButtonEventHandler(FloatingControl_MouseLeftButtonUp);

            var closed = Closed;
            if (closed != null) closed(this, new EventArgs());
        }

        void Control_Closed(object sender, EventArgs e)
        {
            this.Closed -= Control_Closed;
            this._owner.Children.Remove(this);
        }

        public void Window_Closed(object sender, EventArgs e)
        {
            this.Closed -= Window_Closed;
            Window.GetWindow(sender as DependencyObject).Close();
        }

        bool IsInsideMainWindow(Point position)
        {
            var mainWindow = Application.Current.MainWindow;
            double mouseX = position.X + parentWindow.Left - mainWindow.Left;
            double mouseY = position.Y + parentWindow.Top - mainWindow.Top;
            return mouseX >= 0 && mouseX < mainWindow.Width && mouseY >= 0 && mouseY <= mainWindow.Height;
        }

        void FloatingControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!isMouseCaptured) return;

            isMouseCaptured = false;
            this.Chrome.ReleaseMouseCapture();
            mouseY = -1;
            mouseX = -1;
            this.Opacity = 1.0;
            if (Application.Current.IsRunningOutOfBrowser)
            {
                DropWindow(e.GetPosition(null));
            }
            var moved = Moved;
            if (moved != null) moved(this, new EventArgs());
        }

        private void DropWindow(Point position)
        {
            if (IsInsideMainWindow(position))
            {
                if (dragWindow.Content is Image)
                {
                    dragWindow.Content = null;
                    dragWindow.Close();
                }
                else
                {
                    var content = dragWindow.Content;
                    dragWindow.Content = null;
                    dragWindow.Close();
                    this._owner.Children.Add(content);
                    var mainWindow = Application.Current.MainWindow;
                    var x = dragWindow.Left - mainWindow.Left;
                    var y = dragWindow.Top - mainWindow.Top;

                    var style = Deployment.Current.OutOfBrowserSettings.WindowSettings.WindowStyle;
                    if (style == WindowStyle.SingleBorderWindow)
                    {
                        offsetX = 10;
                        offsetY = 32;
                    }
                    else
                    {
                        offsetX = 0;
                        offsetY = 0;
                    }

                    Canvas.SetTop(this, y - offsetY);
                    Canvas.SetLeft(this, x - offsetX);
                    this.Closed += this.Control_Closed;
                    this.Closed -= this.Window_Closed;
                }
            }
            else
            {
                if (this._owner.Children.Remove(this))
                {
                    Canvas.SetTop(this, 0.0);
                    Canvas.SetLeft(this, 0.0);
                    dragWindow.Content = this;
                    this.Closed -= this.Control_Closed;
                    this.Closed += new EventHandler(Window_Closed);
                }
            }
        }

        void FloatingControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMouseCaptured)
            {
                var position = e.GetPosition(null);
                double deltaY = position.Y - mouseY;
                double deltaX = position.X - mouseX;
                double newTop = deltaY + (double)this.GetValue(Canvas.TopProperty);
                double newLeft = deltaX + (double)this.GetValue(Canvas.LeftProperty);

                this.SetValue(Canvas.TopProperty, newTop);
                this.SetValue(Canvas.LeftProperty, newLeft);

                mouseY = position.Y;
                mouseX = position.X;

                if (Application.Current.IsRunningOutOfBrowser)
                {
                    MoveWindow(position, newTop, newLeft);
                }
            }
        }

        private void MoveWindow(Point position, double newTop, double newLeft)
        {
            dragWindow.Top = parentWindow.Top + newTop + offsetY;
            dragWindow.Left = parentWindow.Left + newLeft + offsetX;

            dragWindow.Content.Opacity =
                IsInsideMainWindow(position)
                ? 0.5
                : 1.0;
        }

        void FloatingControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mouseY = e.GetPosition(null).Y;
            mouseX = e.GetPosition(null).X;

            isMouseCaptured = true;
            this.Chrome.CaptureMouse();

            this.SetValue(Canvas.ZIndexProperty, zIndex);
            zIndex++;
            
            if (Application.Current.IsRunningOutOfBrowser)
            {
                InitDragWindow();
            }
        }

        private void InitDragWindow()
        {
            WriteableBitmap bitmap = new WriteableBitmap((int)this.Width, (int)this.Height);
            bitmap.Render(this, new TranslateTransform());
            bitmap.Invalidate();

            var x = Canvas.GetLeft(this);
            var y = Canvas.GetTop(this);

            parentWindow = Window.GetWindow(this);
            if (parentWindow != Application.Current.MainWindow)
            {
                dragWindow = parentWindow;
                offsetX = 0;
                offsetY = 0;
            }
            else
            {
                this.Opacity = 0.25;
                var style = Deployment.Current.OutOfBrowserSettings.WindowSettings.WindowStyle;
                if (parentWindow == Application.Current.MainWindow && style == WindowStyle.SingleBorderWindow)
                {
                    offsetX = 10;
                    offsetY = 32;
                }
                else
                {
                    offsetX = 0;
                    offsetY = 0;
                }
                dragWindow =
                    new Window
                    {
                        Top = parentWindow.Top + offsetY + y,
                        Left = parentWindow.Left + offsetX + x,
                        Title = this.Title,
                        TopMost = false,
                        WindowStyle = WindowStyle.None,
                        Visibility = Visibility.Visible
                    };

                dragWindow.Width = this.Width;
                dragWindow.Height = this.Height;
                dragWindow.Content = new Image { Source = bitmap };
            }
        }
    }
}
