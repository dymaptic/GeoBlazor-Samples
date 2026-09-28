using FleetDashboard.Shared;


namespace FleetDashboard.Client.State;


/// <summary>
/// Pipeline/baseline state model: keeps every received observation in arrival order instead of
/// coalescing to the latest report per vehicle. The queue is bounded; reaching the capacity is an
/// explicit safety stop (with a rejected-record count), never a silent drop.
/// </summary>
public sealed class RawTelemetryQueue
{
    public const int Capacity = 20_000;

    public void Accept(FleetFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        lock (_sync)
        {
            if (SafetyStop)
            {
                RejectedRecordCount += frame.Vehicles.Count;
                return;
            }

            for (int index = 0; index < frame.Vehicles.Count; index++)
            {
                if (_pending.Count >= Capacity)
                {
                    SafetyStop = true;
                    RejectedRecordCount += frame.Vehicles.Count - index;
                    return;
                }

                _pending.Enqueue(frame.Vehicles[index]);
            }
        }
    }

    public PendingFleetBatch Capture(int maxRecords)
    {
        if (maxRecords < 1) throw new ArgumentOutOfRangeException(nameof(maxRecords));

        lock (_sync)
        {
            VehicleReport[] captured = new VehicleReport[Math.Min(maxRecords, _pending.Count)];
            for (int index = 0; index < captured.Length; index++)
            {
                captured[index] = _pending.Dequeue();
            }

            return new PendingFleetBatch(Array.AsReadOnly(captured), []);
        }
    }

    public int PendingCount
    {
        get { lock (_sync) return _pending.Count; }
    }

    public bool SafetyStop { get; private set; }

    public long RejectedRecordCount { get; private set; }

    public void Reset()
    {
        lock (_sync)
        {
            _pending.Clear();
            SafetyStop = false;
            RejectedRecordCount = 0;
        }
    }

    private readonly object _sync = new();
    private readonly Queue<VehicleReport> _pending = [];
}
