using System.Security.Cryptography;
using System.Text;

namespace ECommerceSystem.InventoryService.Services;

/// <summary>
/// Creates the SAME Guid for the same input every time. Used as the MessageId of the result event
/// (StockReserved / StockReservationFailed) so that re-publishing an outcome after a failure is
/// recognised as a duplicate by the Order Service.
/// (MD5 is used only as a stable hash here, not for security.)
/// </summary>
public static class DeterministicGuid
{
    public static Guid Create(Guid source, string purpose)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes($"{purpose}:{source:N}"));
        return new Guid(bytes);
    }
}