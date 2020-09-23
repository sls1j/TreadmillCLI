using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace RealtimePlotter
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    private string _fileName;
    private volatile bool _running;
    private Value[] _values;
    private volatile int _index;
    private DispatcherTimer _timer;
    private WriteableBitmap _bitmap;
    private Value _null;

    public MainWindow()
    {
      _null = new Value();
      _values = new Value[1000];

      for (int i = 0; i < _values.Length; i++)
        _values[i] = _null;

      _index = 0;
      _timer = new DispatcherTimer();
      _timer.Interval = TimeSpan.FromMilliseconds(150);
      _timer.Tick += _timer_Tick;

      InitializeComponent();

      _bitmap = BitmapFactory.New((int)GraphContainer.ActualWidth, (int)GraphContainer.ActualHeight);
      _bitmap.Clear(Colors.Black);
      Graph.Source = _bitmap;
    }

    private int Scale(double value, double max, int outputMax)
    {
      return (int)(value * outputMax / max);
    }

    private void _timer_Tick(object sender, EventArgs e)
    {
      const double timeMs = 10000;
      // draw the graph
      int index = _index;
      using (var ctx = _bitmap.GetBitmapContext())
      {
        _bitmap.Clear(Colors.Black);
        int w = ctx.Width;
        int h = ctx.Height;
        int l = _values.Length;
        for (int i = 0; i < 10; i++)
        {
          int y = h - i * 100 * h / 1024;
          _bitmap.DrawLine(0, y, w, y, Colors.Green);
        }
        long start = _values[index].time;
        for (int i = 0; i < l-1; i++)
        {
          Value a = _values[(l + index - i) % l];
          Value b = _values[(l + index - i - 1) % l];

          if (a == _null || b == _null)
            continue;

          int x1 = Scale(start - a.time, timeMs, w);
          int y1 = Scale(1024 - a.value, 1024, h);

          int x2 = Scale(start - b.time, timeMs, w);
          int y2 = Scale(1024 - b.value, 1024, h);

          _bitmap.DrawLineAa(x1, y1, x2, y2, Colors.Red);
        }
      }
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
      Debug.WriteLine("Start Button");
      _fileName = FileName.Text;
      _running = true;
      ThreadPool.QueueUserWorkItem(DoRead);
      StopButton.IsEnabled = true;
      StartButton.IsEnabled = false;
      _timer.Start();
    }

    private void DoRead(object state)
    {
      using (StreamReader reader = new StreamReader(new FileStream(_fileName,
                     FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
      {
        long lastMaxOffset = reader.BaseStream.Length;

        while (_running)
        {
          System.Threading.Thread.Sleep(100);

          //if the file size has not changed, idle
          if (reader.BaseStream.Length == lastMaxOffset)
            continue;

          //seek to the last max offset
          reader.BaseStream.Seek(lastMaxOffset, SeekOrigin.Begin);

          //read out of the file until the EOF
          string line = "";
          while ((line = reader.ReadLine()) != null)
          {
            if (line.StartsWith("v"))
            {
              Value v = new Value(line);
              _index = (_index + 1) % _values.Length;
              _values[_index] = v;
            }
          }

          //update the last max offset
          lastMaxOffset = reader.BaseStream.Position;
        }
      }
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
      _timer.Stop();
      _running = false;

      for (int i = 0; i < _values.Length; i++)
        _values[i] = _null;

      StopButton.IsEnabled = false;
      StartButton.IsEnabled = true;
    }

    private void Graph_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      _bitmap = BitmapFactory.New((int)GraphContainer.ActualWidth, (int)GraphContainer.ActualHeight);
      Graph.Source = _bitmap;
    }
  }

  class Value
  {
    public Value()
    {
    }

    public Value(string line)
    {
      string[] values = line.Split(' ');      

      time = long.Parse(values[1]);
      value = int.Parse(values[2]);
    }
    public long time;
    public int value;

    public override string ToString()
    {
      return $"t:{time} v:{value}";
    }
  }
}