using ECommerceSystem.PaymentService.Contracts;

namespace ECommerceSystem.PaymentService.Services;

/// <summary>Result of a charge: Created = true for a new payment (201), false for an idempotent replay (200).</summary>
public sealed record ChargeResult(PaymentResponse Payment, bool Created);