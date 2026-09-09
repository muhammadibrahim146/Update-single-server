namespace UBLSingleServerSimulation.Models;

public class Customer
{
    public string Day { get; set; } = "";
    public string CustomerId { get; set; } = "";
    // The Excel file stores time as elapsed minutes from the start of each day.
    public double ArrivalTime { get; set; }
    public double InterArrivalTime { get; set; }
    public double ObservedServiceStartTime { get; set; }
    public double ServiceStartTime { get; set; }
    public double ServiceTime { get; set; }
    public double ServiceFinishTime { get; set; }
    public double WaitingTime { get; set; }
    public double TimeInSystem { get; set; }
    public int QueueLengthAtArrival { get; set; }
}
