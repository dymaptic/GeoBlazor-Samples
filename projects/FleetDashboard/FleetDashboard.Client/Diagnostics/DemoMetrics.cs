namespace FleetDashboard.Client.Diagnostics;


/// <summary>
/// Bounded demo counters for the on-screen strip. Rates use a fixed five-second window of
/// one-second buckets; apply-duration statistics keep the most recent 100 samples.
/// </summary>
public sealed class DemoMetrics(TimeProvider? clock = null)
{
    private const int WindowSeconds = 5;
    private const int MaxApplySamples = 100;

    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    public void RecordIncoming(int recordCount)
    {
        if (recordCount <= 0) return;

        lock (_sync) BucketAdd(_incomingBuckets, _incomingBucketSeconds, recordCount);
    }

    public void RecordMapCall(int featureCount, double applyMilliseconds)
    {
        if (featureCount < 0 || !double.IsFinite(applyMilliseconds) || applyMilliseconds < 0) return;

        lock (_sync)
        {
            BucketAdd(_mapCallBuckets, _mapCallBucketSeconds, 1);
            _applySamples.Enqueue(applyMilliseconds);
            while (_applySamples.Count > MaxApplySamples) _applySamples.Dequeue();
            _featuresSum += featureCount;
        }
    }

    public void RecordFailure()
    {
        lock (_sync) _failures++;
    }

    public double IncomingRecordsPerSecond
    {
        get { lock (_sync) return BucketRate(_incomingBuckets, _incomingBucketSeconds); }
    }

    public double MapCallsPerSecond
    {
        get { lock (_sync) return BucketRate(_mapCallBuckets, _mapCallBucketSeconds); }
    }

    public long Failures
    {
        get { lock (_sync) return _failures; }
    }

    /// <summary>Median and 95th percentile of the most recent apply durations, in milliseconds.</summary>
    public (double Median, double P95) ApplyDurationMilliseconds
    {
        get
        {
            lock (_sync)
            {
                if (_applySamples.Count == 0) return (0, 0);
                double[] sorted = [.._applySamples];
                Array.Sort(sorted);
                double median = sorted.Length % 2 == 1
                    ? sorted[sorted.Length / 2]
                    : (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2;
                return (median, sorted[(int)Math.Ceiling(sorted.Length * 0.95) - 1]);
            }
        }
    }

    public long FeaturesSubmitted
    {
        get { lock (_sync) return _featuresSum; }
    }

    private long CurrentSecond() => _clock.GetUtcNow().ToUnixTimeSeconds();

    private void BucketAdd(long[] buckets, long[] bucketSeconds, int count)
    {
        long second = CurrentSecond();
        int index = (int)(second % WindowSeconds);
        if (bucketSeconds[index] != second)
        {
            bucketSeconds[index] = second;
            buckets[index] = 0;
        }

        buckets[index] += count;
    }

    private double BucketRate(long[] buckets, long[] bucketSeconds)
    {
        long second = CurrentSecond();
        long sum = 0;
        for (int index = 0; index < WindowSeconds; index++)
        {
            if (bucketSeconds[index] > second - WindowSeconds) sum += buckets[index];
        }

        return (double)sum / WindowSeconds;
    }

    private readonly object _sync = new();
    private readonly long[] _incomingBuckets = new long[WindowSeconds];
    private readonly long[] _incomingBucketSeconds = new long[WindowSeconds];
    private readonly long[] _mapCallBuckets = new long[WindowSeconds];
    private readonly long[] _mapCallBucketSeconds = new long[WindowSeconds];
    private readonly Queue<double> _applySamples = [];
    private long _failures;
    private long _featuresSum;
}
