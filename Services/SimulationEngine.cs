using UBLSingleServerSimulation.Models;

namespace UBLSingleServerSimulation.Services;

public static class SimulationEngine
{
    public static SimulationResult Run(List<Customer> input)
    {
        var result = new SimulationResult();

        foreach (var group in input.GroupBy(x => x.Day).OrderBy(x => x.Key))
        {
            var list = group.OrderBy(x => x.ArrivalTime).ToList();
            if (list.Count == 0) continue;

            // The first recorded service-start time represents the initial
            // availability of the teller for that observation day (3/4/2 min).
            double serverAvailable = list[0].ObservedServiceStartTime;
            double firstArrival = list[0].ArrivalTime;

            for (int i = 0; i < list.Count; i++)
            {
                var c = list[i];
                c.InterArrivalTime = i == 0 ? 0 : c.ArrivalTime - list[i - 1].ArrivalTime;
                c.ServiceStartTime = Math.Max(c.ArrivalTime, serverAvailable);
                c.WaitingTime = c.ServiceStartTime - c.ArrivalTime;
                c.ServiceFinishTime = c.ServiceStartTime + c.ServiceTime;
                c.TimeInSystem = c.ServiceFinishTime - c.ArrivalTime;

                // Only customers who arrived earlier and have not started service
                // yet are waiting in the logical FCFS queue at this instant.
                c.QueueLengthAtArrival = list.Take(i)
                    .Count(x => x.ServiceStartTime > c.ArrivalTime);

                serverAvailable = c.ServiceFinishTime;
                result.Customers.Add(c);
            }

            double lastFinish = list.Max(x => x.ServiceFinishTime);
            double duration = lastFinish - firstArrival;
            double busy = list.Sum(x => x.ServiceTime);

            result.Days.Add(new DayResult
            {
                Day = group.Key,
                Customers = list.Count,
                MeanInterArrival = list.Count <= 1 ? 0 : list.Skip(1).Average(x => x.InterArrivalTime),
                MeanService = list.Average(x => x.ServiceTime),
                MeanWaiting = list.Average(x => x.WaitingTime),
                MeanSystem = list.Average(x => x.TimeInSystem),
                BusyTime = busy,
                SimulationDuration = duration,
                Utilization = duration <= 0 ? 0 : busy / duration * 100.0,
                IdleTime = Math.Max(0, duration - busy),
                MaxWaiting = list.Max(x => x.WaitingTime),
                MinWaiting = list.Min(x => x.WaitingTime),
                MaxQueue = list.Max(x => x.QueueLengthAtArrival)
            });
        }

        return result;
    }

    public static MM1Result CalculateMM1(string scope, double meanInterArrival, double meanService)
    {
        if (meanInterArrival <= 0 || meanService <= 0)
            return new MM1Result { Scope = scope };

        double lambda = 1.0 / meanInterArrival;
        double mu = 1.0 / meanService;

        return new MM1Result
        {
            Scope = scope,
            Lambda = lambda,
            Mu = mu,
            Rho = lambda / mu
        };
    }
}
