using ClosedXML.Excel;
using UBLSingleServerSimulation.Models;
using System.Globalization;

namespace UBLSingleServerSimulation.Services;

public static class ExcelService
{
    public static List<Customer> Import(string filePath)
    {
        using var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheets.First();
        var customers = new List<Customer>();
        int dayNumber = 1;

        foreach (var row in ws.RowsUsed())
        {
            string id = row.Cell(1).GetString().Trim();

            if (string.IsNullOrWhiteSpace(id))
                continue;

            // Repeated header rows separate the three observation blocks.
            if (id.Equals("Customer ID", StringComparison.OrdinalIgnoreCase))
            {
                if (customers.Count > 0)
                    dayNumber++;
                continue;
            }

            if (!id.StartsWith("C", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!TryReadDouble(row.Cell(2), out double arrival))
                continue;

            double observedStart = ReadDouble(row.Cell(4));
            double service = ReadDouble(row.Cell(6));

            customers.Add(new Customer
            {
                Day = $"Day {dayNumber}",
                CustomerId = id,
                ArrivalTime = arrival,
                ObservedServiceStartTime = observedStart,
                ServiceTime = service
            });
        }

        // Safety fallback for files that contain no repeated headers.
        if (customers.Count > 0 && customers.Select(x => x.Day).Distinct().Count() == 1)
        {
            for (int i = 0; i < customers.Count; i++)
                customers[i].Day = $"Day {(i / 30) + 1}";
        }

        Validate(customers);
        return customers;
    }

    private static void Validate(List<Customer> customers)
    {
        if (customers.Count == 0)
            throw new InvalidOperationException("No valid customer records were found.");

        var bad = customers.Where(x => x.ServiceTime < 0 || x.ArrivalTime < 0).ToList();
        if (bad.Count > 0)
            throw new InvalidOperationException(
                "Invalid data found for: " + string.Join(", ", bad.Select(x => x.CustomerId)));
    }

    private static bool TryReadDouble(IXLCell cell, out double value)
    {
        if (cell.TryGetValue<double>(out value))
            return true;

        return double.TryParse(cell.GetString().Trim(), NumberStyles.Any,
            CultureInfo.InvariantCulture, out value);
    }

    private static double ReadDouble(IXLCell cell)
    {
        return TryReadDouble(cell, out double value) ? value : 0.0;
    }

    public static void Export(string filePath, SimulationResult result)
    {
        using var wb = new XLWorkbook();

        var ws = wb.Worksheets.Add("FINAL RESULTS");
        ws.Cell(1, 1).Value = "UBL Gulshan Chowrangi - Single Server Simulation / M/M/1";
        ws.Range(1, 1, 1, 10).Merge();
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 16;

        ws.Cell(3, 1).Value = "Part A - Trace-Driven Simulation";
        ws.Cell(3, 1).Style.Font.Bold = true;

        string[] headers = { "Scope", "Customers", "Mean Inter-Arrival (min)", "Mean Service (min)",
            "Mean Waiting (min)", "Mean Time in System (min)", "Busy Time (min)",
            "Simulation Duration (min)", "Utilization (%)", "Max Queue" };
        for (int c = 0; c < headers.Length; c++) ws.Cell(4, c + 1).Value = headers[c];

        int r = 5;
        foreach (var d in result.Days.Append(result.Overall))
        {
            ws.Cell(r, 1).Value = d.Day;
            ws.Cell(r, 2).Value = d.Customers;
            ws.Cell(r, 3).Value = d.MeanInterArrival;
            ws.Cell(r, 4).Value = d.MeanService;
            ws.Cell(r, 5).Value = d.MeanWaiting;
            ws.Cell(r, 6).Value = d.MeanSystem;
            ws.Cell(r, 7).Value = d.BusyTime;
            ws.Cell(r, 8).Value = d.SimulationDuration;
            ws.Cell(r, 9).Value = d.Utilization;
            ws.Cell(r, 10).Value = d.MaxQueue;
            r++;
        }

        int mmStart = r + 2;
        ws.Cell(mmStart, 1).Value = "Part B - M/M/1 Analytical Results";
        ws.Cell(mmStart, 1).Style.Font.Bold = true;
        string[] mmHeaders = { "Scope", "Lambda", "Mu", "Rho", "Idle Probability", "Lq", "Wq (min)", "W (min)", "L", "Status" };
        for (int c = 0; c < mmHeaders.Length; c++) ws.Cell(mmStart + 1, c + 1).Value = mmHeaders[c];

        r = mmStart + 2;
        foreach (var d in result.Days.Append(result.Overall))
        {
            var m = SimulationEngine.CalculateMM1(d.Day, d.MeanInterArrival, d.MeanService);
            ws.Cell(r, 1).Value = m.Scope;
            ws.Cell(r, 2).Value = m.Lambda;
            ws.Cell(r, 3).Value = m.Mu;
            ws.Cell(r, 4).Value = m.Rho;
            ws.Cell(r, 5).Value = m.Stable ? m.IdleProbability : "N/A";
            ws.Cell(r, 6).Value = m.Stable ? m.Lq : "Undefined";
            ws.Cell(r, 7).Value = m.Stable ? m.Wq : "Undefined";
            ws.Cell(r, 8).Value = m.Stable ? m.W : "Undefined";
            ws.Cell(r, 9).Value = m.Stable ? m.L : "Undefined";
            ws.Cell(r, 10).Value = m.Stable ? "Stable (rho < 1)" : "Not stable (rho >= 1)";
            r++;
        }

        var detail = wb.Worksheets.Add("Customer Calculations");
        string[] dh = { "Day", "Customer ID", "Arrival Time (min)", "Inter-Arrival (min)",
            "Observed Service Start (min)", "Calculated Service Start (min)", "Service Time (min)",
            "Calculated Service Finish (min)", "Waiting (min)", "Time in System (min)", "Queue Length at Arrival" };
        for (int c = 0; c < dh.Length; c++) detail.Cell(1, c + 1).Value = dh[c];

        int dr = 2;
        foreach (var x in result.Customers)
        {
            detail.Cell(dr, 1).Value = x.Day;
            detail.Cell(dr, 2).Value = x.CustomerId;
            detail.Cell(dr, 3).Value = x.ArrivalTime;
            detail.Cell(dr, 4).Value = x.InterArrivalTime;
            detail.Cell(dr, 5).Value = x.ObservedServiceStartTime;
            detail.Cell(dr, 6).Value = x.ServiceStartTime;
            detail.Cell(dr, 7).Value = x.ServiceTime;
            detail.Cell(dr, 8).Value = x.ServiceFinishTime;
            detail.Cell(dr, 9).Value = x.WaitingTime;
            detail.Cell(dr, 10).Value = x.TimeInSystem;
            detail.Cell(dr, 11).Value = x.QueueLengthAtArrival;
            dr++;
        }

        ws.Columns().AdjustToContents();
        detail.Columns().AdjustToContents();
        wb.SaveAs(filePath);
    }
}
