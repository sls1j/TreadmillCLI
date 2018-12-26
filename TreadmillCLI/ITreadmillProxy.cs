namespace TreadmillCLI
{
  interface ITreadmillProxy
  {
    ErrorEvent OnError { get; set; }
    OdometerEvent OnOdometer { get; set; }
    PingEvent OnPing { get; set; }
    void Stop();
  }

  delegate void OdometerEvent(double meters, double seconds);
  delegate void PingEvent();
  delegate void ErrorEvent(bool error);
}