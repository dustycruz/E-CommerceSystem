namespace ECommerceSystem.CatalogService.Chaos;

/// <summary>
/// Singleton counter for the retry demo: while RemainingFailures > 0, each product lookup
/// (GET /catalog/v1/products/{id}) consumes one failure and returns 503. Thread-safe.
/// </summary>
public sealed class ChaosState
{
    private int _remainingFailures;

    public int RemainingFailures => Volatile.Read(ref _remainingFailures);

    public void SetFailNextRequests(int count)
    {
        Interlocked.Exchange(ref _remainingFailures, Math.Max(0, count));
    }

    /// <summary>Returns true (and decrements) when the current request must fail.</summary>
    public bool TryConsumeFailure()
    {
        while (true)
        {
            var current = Volatile.Read(ref _remainingFailures);
            if (current <= 0)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref _remainingFailures, current - 1, current) == current)
            {
                return true;
            }
        }
    }
}