namespace TreadmillCLI
{
    interface ITreadmillProxy
    {
        ErrorEvent OnError { get; set; }
        OdometerEvent OnOdometer { get; set; }

        void Stop();
    }
}