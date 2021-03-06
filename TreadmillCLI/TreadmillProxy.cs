﻿using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TreadmillCLI
{
  class TreadmillProxy : ITreadmillProxy
  {
    private string _comPort;
    private SerialPort _com;
    private const double treadmillBeltLength = 2.864; // meters
    private ManualResetEvent _quit;
    public TreadmillProxy(string comPort)
    {
      _comPort = comPort;
      _quit = new ManualResetEvent(false);
      System.Threading.ThreadPool.QueueUserWorkItem(DoWork, null);
    }

    public OdometerEvent OnOdometer { get; set; }
    public ErrorEvent OnError { get; set; }
    public PingEvent OnPing { get; set; }
    public ValueEvent OnValue { get; set; }

    private void DoWork(object state)
    {
      while (true)
      {
        if (_quit.WaitOne(0))
        {
          if (_com != null)
          {
            _com.Close();
            _com = null;
          }
        }
        try
        {
          if (null == _com)
            throw new Exception();

          string line = _com.ReadLine();
          if (!string.IsNullOrEmpty(line))
          {
            string[] values = line.Split(' ');
            switch (values[0])
            {
              case "o": // odomemeter reading
                double interval = int.Parse(values[1]) / 1000.0; // in seconds
                if (null != OnOdometer)
                {
                  OnOdometer(treadmillBeltLength, interval);
                }
                break;
              case "p": // pedometer reading
                break;
              case "t": // tilt reading
                break;
            }
          }
        }
        catch (Exception)
        {
          if (OnError != null)
            OnError(true);

          Thread.Sleep(2000);
          OpenPort();

          if (_com.IsOpen)
          {
            OnError(false);
          }
          else
            _com = null;
        }
      }
    }

    private bool OpenPort()
    {
      try
      {
        if (_com != null)
        {
          _com.Close();
          _com = null;
        }
      }
      catch (Exception)
      {

      }
      try
      {
        _com = new SerialPort($"{_comPort}", 115200, Parity.None, 8, StopBits.One);
        _com.Open();
        return _com.IsOpen;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public void Stop()
    {
      _quit.Set();
    }

    public void Reset()
    {
    }
  }
}
