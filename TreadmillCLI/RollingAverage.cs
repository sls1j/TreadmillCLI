using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreadmillCLI
{
  class RollingAverage
  {
    private Queue<double> _values;
    private int _rollingCount;
    private double _total;

    public RollingAverage(int rollingCount)
    {
      _values = new Queue<double>();
      _rollingCount = rollingCount;
    }

    public double Add(double v)
    {
      _values.Enqueue(v);
      _total += v;
      while ( _values.Count > _rollingCount)
      {
        double old_value = _values.Dequeue();
        _total -= old_value;
      }

      return _total / _values.Count;
    }
  }
}
