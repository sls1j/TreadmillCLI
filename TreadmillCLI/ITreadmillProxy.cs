﻿namespace TreadmillCLI
{
  interface ITreadmillProxy
  {
    ErrorEvent OnError { get; set; }
    OdometerEvent OnOdometer { get; set; }
    PingEvent OnPing { get; set; }
    ValueEvent OnValue { get; set; }
    void Reset();
    void Stop();
  }

  delegate void OdometerEvent(double meters, double seconds);
  delegate void PingEvent();
  delegate void ErrorEvent(bool error);
  delegate void ValueEvent(int time, int ticks, int max, int min);
}