﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TreadmillCLI
{
  class TreadmillProxyUdp : ITreadmillProxy
  {
    private int _port;
    private UdpClient _client;
    private const double treadmillBeltLength = 2.864; // meters
    private ManualResetEvent _quit;
    private int _lastTickCount = -1;
    public TreadmillProxyUdp(int port)
    {
      _port = port;
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
          if (_client != null)
          {
            _client.Close();
            _client = null;
          }
        }
        try
        {
          if (null == _client)
            throw new Exception();

          IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, _port);
          byte[] buffer = _client.Receive(ref remoteEndPoint);
          string line = Encoding.UTF8.GetString(buffer);
          if (!string.IsNullOrEmpty(line))
          {
            string[] values = line.Split(' ');
            switch (values[0])
            {
              case "d":
                break;
              case "o": // odomemeter reading
                double interval = int.Parse(values[1]) / 1000.0; // in seconds
                int tickCount = int.Parse(values[2]);
                int diff;

                // to handle wrap around
                if (_lastTickCount > tickCount)
                  diff = UInt16.MaxValue - _lastTickCount + tickCount;
                else
                  diff = tickCount - _lastTickCount;

                _lastTickCount = tickCount;

                if (null != OnOdometer)
                {
                  OnOdometer(treadmillBeltLength * diff, interval);
                }
                break;
              case "p": // ping
                if (null != OnPing)
                {
                  OnPing();
                }
                break;
              case "v":
                {
                  int time = int.Parse(values[1]);
                  int ticks = int.Parse(values[2]);
                  int max = int.Parse(values[3]);
                  int min = int.Parse(values[4]);
                  if (null != OnValue)
                  {
                    OnValue(time, ticks, max, min);
                  }
                }
                break;
            }
          }
        }
        catch (Exception)
        {
          if (OnError != null)
            OnError(true);

          Thread.Sleep(2000);
          if (OpenPort())
            OnError(false);
        }
      }
    }

    private bool OpenPort()
    {
      try
      {
        if (_client != null)
        {
          _client.Close();
          _client = null;
        }
      }
      catch (Exception)
      {

      }
      try
      {
        _client = new UdpClient(new IPEndPoint(IPAddress.Any, _port));
        return true;
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
      UdpClient client = new UdpClient("192.168.4.11", 7123);

      byte[] buffer = Encoding.UTF8.GetBytes("r");
      client.Send(buffer, buffer.Length);
    }
  }
}
