# UBL Gulshan Chowrangi - Single Server M/M/1 Simulation

C# .NET 8 WinForms project for the Modeling & Simulation course.

## What is included

### Part A - Trace-Driven Single Server Simulation
- Imports the UBL Excel observation data.
- Uses FCFS discipline.
- Uses exactly one teller/server.
- Calculates:
  - Inter-arrival time
  - Service start time
  - Service finish time
  - Waiting time
  - Time in system
  - Queue length at arrival
  - Mean-wise parameters
  - Server utilization
  - Busy/idle time
  - Maximum queue and waiting time

### Part B - M/M/1 Analytical Model
Uses the standard formulas:
- λ = 1 / mean inter-arrival time
- μ = 1 / mean service time
- ρ = λ / μ
- P0 = 1 - ρ
- Lq = ρ² / (1 - ρ)
- Wq = Lq / λ
- W = Wq + 1/μ
- L = λW

The analytical steady-state formulas are shown only when ρ < 1.
If ρ >= 1, the application reports "Not Stable" and keeps the trace-driven simulation results.

## Run

```bash
dotnet restore
dotnet run
```

    ## 🏗️ System Architecture & Data Flow

```text
                    UBL CUSTOMER DATA
                           │
                           ▼
                      Excel File
                           │
                           ▼
                    ExcelService.cs
                    "Read Excel Data"
                           │
                           ▼
                       Customer.cs
                  "Create Customer Objects"
                           │
                           ▼
                  SimulationEngine.cs
                     "Run Simulation"
                           │
              ┌────────────┴────────────┐
              │                         │
              ▼                         ▼
           PART A                    PART B
      Trace-Driven Simulation      M/M/1 Analytical
              │                         │
              ▼                         ▼
        ┌──────────────┐          ┌──────────────┐
        │ Arrival Time │          │ Lambda (λ)   │
        │ Service Time │          │ Mu (μ)       │
        │ Waiting Time │          │ Rho (ρ)      │
        │ Queue Length │          │ Lq, Wq, W, L │
        └──────────────┘          └──────────────┘
              │                         │
              └────────────┬────────────┘
                           ▼
                  SimulationResult.cs
                      "Store Results"
                           │
                           ▼
                      MainForm.cs
                           │
                    ┌──────┴──────┐
                    │             │
                    ▼             ▼
                  Screen       Excel Export
    
