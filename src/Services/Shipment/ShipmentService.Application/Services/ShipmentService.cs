using FluentValidation;
using Microsoft.Extensions.Logging;
using ShipmentService.Application.DTOs;
using ShipmentService.Application.Abstractions;
using ShipmentService.Application.Pricing;
using BuildingBlocks.Common;
using BuildingBlocks.FileStorage;
using BuildingBlocks.IntegrationEvents;
using ShipmentService.Domain.Abstractions;
using ShipmentService.Domain.Entities;
using ShipmentService.Domain.Enums;
using ShipmentService.Domain.ValueObjects;

namespace ShipmentService.Application.Services;

public class ShipmentService : IShipmentService
{
    private readonly IShipmentRepository _shipments;
    private readonly IPackageValidationClient _packageValidationClient;
    private readonly IShipmentEventPublisher _shipmentEventPublisher;
    private readonly IShipmentPricingCalculator _pricingCalculator;
    private readonly IFileStorageService _fileStorage;
    private readonly IDriverAvailabilityClient _driverAvailabilityClient;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IValidator<CreateShipmentRequest> _createShipmentValidator;
    private readonly ILogger<ShipmentService> _logger;

    public ShipmentService(
        IShipmentRepository shipments,
        IPackageValidationClient packageValidationClient,
        IShipmentEventPublisher shipmentEventPublisher,
        IShipmentPricingCalculator pricingCalculator,
        IFileStorageService fileStorage,
        IDriverAvailabilityClient driverAvailabilityClient,
        IPaymentGateway paymentGateway,
        IValidator<CreateShipmentRequest> createShipmentValidator,
        ILogger<ShipmentService> logger)
    {
        _shipments = shipments;
        _packageValidationClient = packageValidationClient;
        _shipmentEventPublisher = shipmentEventPublisher;
        _pricingCalculator = pricingCalculator;
        _fileStorage = fileStorage;
        _driverAvailabilityClient = driverAvailabilityClient;
        _paymentGateway = paymentGateway;
        _createShipmentValidator = createShipmentValidator;
        _logger = logger;
    }

    public async Task<ShipmentResponse> CreateAsync(
        CreateShipmentRequest request,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existingShipmentId = await _shipments.GetShipmentIdByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            if (existingShipmentId.HasValue)
            {
                var existingShipment = await _shipments.GetByIdAsync(existingShipmentId.Value, cancellationToken);
                if (existingShipment is not null)
                {
                    return Map(existingShipment);
                }
            }

            var reserved = await _shipments.TryReserveIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            if (!reserved)
            {
                existingShipmentId = await _shipments.GetShipmentIdByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
                if (existingShipmentId.HasValue)
                {
                    var existingShipment = await _shipments.GetByIdAsync(existingShipmentId.Value, cancellationToken);
                    if (existingShipment is not null)
                    {
                        return Map(existingShipment);
                    }
                }

                throw new InvalidOperationException("A request with the same Idempotency-Key is currently being processed.");
            }
        }

        try
        {
            var package = await _packageValidationClient.GetPackageAsync(request.PackageId, cancellationToken);

            if (package is null)
                throw new KeyNotFoundException("Package not found");

            var pickupAddress = MapAddress(request.PickupAddress);
            var deliveryAddress = MapAddress(request.DeliveryAddress);

            var shipment = new Shipment
            {
                Id = Guid.NewGuid(),
                PackageId = request.PackageId,
                CustomerId = package.SenderId,
                TrackingNumber = GenerateTrackingNumber(),
                Status = ShipmentStatus.Created,
                DeliveryType = package.DeliveryType,
                PickupAddress = pickupAddress,
                DeliveryAddress = deliveryAddress,
                Cost = _pricingCalculator.Calculate(package.Weight, package.DeliveryType),
                Currency = "USD",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var initialStatusHistory = new ShipmentStatusHistory
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipment.Id,
                Status = ShipmentStatus.Created,
                Location = FormatAddress(pickupAddress),
                Timestamp = DateTime.UtcNow
            };

            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipment.Id,
                CustomerId = shipment.CustomerId,
                InvoiceNumber = GenerateInvoiceNumber(),
                Amount = shipment.Cost,
                Currency = shipment.Currency,
                PaymentStatus = PaymentStatus.Pending,
                IssuedAt = DateTime.UtcNow
            };

            await _shipments.AddAsync(shipment, initialStatusHistory, invoice, cancellationToken);

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                await _shipments.SetIdempotencyResultAsync(idempotencyKey, shipment.Id, cancellationToken);
            }

            await _shipmentEventPublisher.PublishShipmentCreatedAsync(
                new ShipmentCreatedEvent(
                    shipment.Id,
                    shipment.PackageId,
                    shipment.CustomerId,
                    shipment.TrackingNumber,
                    FormatAddress(pickupAddress),
                    DateTime.UtcNow),
                cancellationToken);

            return Map(shipment);
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                await _shipments.ReleaseIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            }

            throw;
        }
    }

    public async Task<ShipmentResponse?> GetByIdAsync(Guid id)
    {
        var shipment = await _shipments.GetByIdAsync(id);
        return shipment is null ? null : Map(shipment);
    }

    public async Task<DeliveryAttemptResponse?> LogDeliveryAttemptAsync(
        Guid shipmentId,
        LogDeliveryAttemptRequest request,
        CancellationToken cancellationToken = default)
    {
        var failureReason = request.Successful
            ? (DeliveryAttemptFailureReason?)null
            : Enum.Parse<DeliveryAttemptFailureReason>(request.FailureReason!);

        var attempt = new DeliveryAttempt
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentId,
            Successful = request.Successful,
            FailureReason = failureReason,
            Notes = request.Notes,
            AttemptedAt = DateTime.UtcNow
        };

        var newStatus = request.Successful ? ShipmentStatus.Delivered : ShipmentStatus.FailedDelivery;

        var historyEntry = new ShipmentStatusHistory
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentId,
            Status = newStatus,
            Location = request.Successful ? "Delivered" : $"Delivery attempt failed: {failureReason}",
            Timestamp = attempt.AttemptedAt
        };

        var shipment = await _shipments.AddDeliveryAttemptAsync(shipmentId, attempt, newStatus, historyEntry, cancellationToken);

        if (shipment is not null)
        {
            await _shipmentEventPublisher.PublishShipmentStatusChangedAsync(
                new ShipmentStatusChangedEvent(
                    shipment.Id,
                    shipment.PackageId,
                    shipment.CustomerId,
                    shipment.TrackingNumber,
                    newStatus.ToString(),
                    historyEntry.Location,
                    DateTime.UtcNow),
                cancellationToken);
        }

        return shipment is null
            ? null
            : new DeliveryAttemptResponse(
                attempt.Id,
                attempt.ShipmentId,
                attempt.Successful,
                attempt.FailureReason?.ToString(),
                attempt.Notes,
                attempt.ProofOfDeliveryUrl,
                attempt.AttemptedAt,
                shipment.Status.ToString());
    }

    public async Task<DeliveryAttemptResponse?> AttachProofOfDeliveryAsync(
        Guid shipmentId, Guid attemptId, Stream fileContent, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var attempt = await _shipments.GetDeliveryAttemptAsync(shipmentId, attemptId, cancellationToken);
        if (attempt is null) return null;

        var shipment = await _shipments.GetByIdAsync(shipmentId, cancellationToken);
        if (shipment is null) return null;

        var result = await _fileStorage.SaveAsync("delivery-proofs", fileName, fileContent, contentType, cancellationToken);
        await _shipments.SetDeliveryAttemptProofUrlAsync(attemptId, result.Url, cancellationToken);

        return new DeliveryAttemptResponse(
            attempt.Id,
            attempt.ShipmentId,
            attempt.Successful,
            attempt.FailureReason?.ToString(),
            attempt.Notes,
            result.Url,
            attempt.AttemptedAt,
            shipment.Status.ToString());
    }

    public async Task<InvoiceResponse?> GetInvoiceAsync(Guid shipmentId, CancellationToken cancellationToken = default)
    {
        var invoice = await _shipments.GetInvoiceByShipmentIdAsync(shipmentId, cancellationToken);
        return invoice is null ? null : MapInvoice(invoice);
    }

    public async Task<InvoiceResponse?> UpdatePaymentStatusAsync(Guid shipmentId, PaymentStatus status, CancellationToken cancellationToken = default)
    {
        var invoice = await _shipments.UpdatePaymentStatusAsync(shipmentId, status, cancellationToken);
        return invoice is null ? null : MapInvoice(invoice);
    }

    public async Task<CheckoutSessionResponse> CreateCheckoutSessionAsync(
        Guid shipmentId, CreateCheckoutSessionRequest request, CancellationToken cancellationToken = default)
    {
        var shipment = await _shipments.GetByIdAsync(shipmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Shipment not found");

        var invoice = await _shipments.GetInvoiceByShipmentIdAsync(shipmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Invoice not found for this shipment");

        if (invoice.PaymentStatus == PaymentStatus.Paid)
        {
            throw new InvalidOperationException("This invoice has already been paid.");
        }

        var session = await _paymentGateway.CreateCheckoutSessionAsync(
            invoice.Id, shipment.Id, invoice.Amount, invoice.Currency,
            request.SuccessUrl, request.CancelUrl, cancellationToken);

        await _shipments.SetInvoiceStripeSessionIdAsync(invoice.Id, session.SessionId, cancellationToken);

        return new CheckoutSessionResponse(session.SessionId, session.CheckoutUrl);
    }

    public async Task HandleStripeWebhookAsync(string requestBody, string? signatureHeader, CancellationToken cancellationToken = default)
    {
        var invoiceId = _paymentGateway.VerifyAndParseCheckoutCompleted(requestBody, signatureHeader);
        if (invoiceId is null)
        {
            return;
        }

        var updated = await _shipments.MarkInvoicePaidAsync(invoiceId.Value, cancellationToken);
        if (updated is null)
        {
            _logger.LogWarning("Stripe webhook referenced invoice {InvoiceId}, which does not exist.", invoiceId.Value);
        }
    }

    public async Task<PaginatedList<ShipmentResponse>> GetPagedAsync(
        int pageNumber, int pageSize, ShipmentStatus? status, Guid? customerId, Guid? driverId,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount, resolvedPageNumber, resolvedPageSize) =
            await _shipments.GetPagedAsync(pageNumber, pageSize, status, customerId, driverId, cancellationToken);

        return new PaginatedList<ShipmentResponse>(items.Select(Map).ToList(), totalCount, resolvedPageNumber, resolvedPageSize);
    }

    public async Task<ShipmentResponse?> UpdateStatusAsync(Guid id, ShipmentStatus newStatus, CancellationToken cancellationToken = default)
    {
        var current = await _shipments.GetByIdAsync(id, cancellationToken);
        if (current is null) return null;

        if (current.Status is ShipmentStatus.Delivered or ShipmentStatus.Cancelled)
            throw new InvalidOperationException($"Shipment is already {current.Status} and cannot be updated.");

        var historyEntry = new ShipmentStatusHistory
        {
            Id = Guid.NewGuid(),
            ShipmentId = id,
            Status = newStatus,
            Location = FormatAddress(newStatus == ShipmentStatus.Delivered ? current.DeliveryAddress : current.PickupAddress),
            Timestamp = DateTime.UtcNow
        };

        var updated = await _shipments.UpdateStatusAsync(id, newStatus, historyEntry, cancellationToken);

        if (updated is not null)
        {
            await _shipmentEventPublisher.PublishShipmentStatusChangedAsync(
                new ShipmentStatusChangedEvent(
                    updated.Id,
                    updated.PackageId,
                    updated.CustomerId,
                    updated.TrackingNumber,
                    newStatus.ToString(),
                    historyEntry.Location,
                    DateTime.UtcNow),
                cancellationToken);
        }

        return updated is null ? null : Map(updated);
    }

    public async Task<ShipmentResponse?> AssignDriverAsync(Guid id, Guid driverId, CancellationToken cancellationToken = default)
    {
        var updated = await _shipments.AssignDriverAsync(id, driverId, cancellationToken);
        return updated is null ? null : Map(updated);
    }

    public async Task<IReadOnlyList<BulkShipmentResult>> CreateBulkAsync(
        IReadOnlyList<CreateShipmentRequest> requests, CancellationToken cancellationToken = default)
    {
        var results = new List<BulkShipmentResult>(requests.Count);

        foreach (var request in requests)
        {
            var validation = await _createShipmentValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                var error = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                results.Add(new BulkShipmentResult(request.PackageId, false, null, error));
                continue;
            }

            try
            {
                var shipment = await CreateAsync(request, idempotencyKey: null, cancellationToken);
                results.Add(new BulkShipmentResult(request.PackageId, true, shipment, null));
            }
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
            {
                results.Add(new BulkShipmentResult(request.PackageId, false, null, ex.Message));
            }
        }

        return results;
    }

    public async Task<ShipmentResponse> AutoAssignDriverAsync(Guid shipmentId, double radiusKm, CancellationToken cancellationToken = default)
    {
        var shipment = await _shipments.GetByIdAsync(shipmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Shipment not found");

        var lat = shipment.PickupAddress.Latitude;
        var lng = shipment.PickupAddress.Longitude;
        if (lat is null || lng is null)
        {
            throw new InvalidOperationException("Shipment's pickup address has no coordinates to search around.");
        }

        var candidates = await _driverAvailabilityClient.GetAvailableNearbyAsync(lat.Value, lng.Value, radiusKm, cancellationToken);
        var nearest = candidates.FirstOrDefault();
        if (nearest is null)
        {
            throw new InvalidOperationException($"No available driver found within {radiusKm}km of the pickup address.");
        }

        var updated = await _shipments.AssignDriverAsync(shipmentId, nearest.DriverId, cancellationToken)
            ?? throw new KeyNotFoundException("Shipment not found");

        return Map(updated);
    }

    public async Task<ShipmentAnalyticsSummaryResponse> GetAnalyticsSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _shipments.GetAnalyticsSummaryAsync(cancellationToken);
        return new ShipmentAnalyticsSummaryResponse(
            summary.TotalShipments,
            summary.CountsByStatus,
            summary.DeliveredToday,
            summary.FailedAttemptsToday,
            summary.SlaBreachedTotal,
            summary.SlaBreachedToday);
    }

    public async Task<IReadOnlyList<DriverPerformanceResponse>> GetDriverPerformanceAsync(CancellationToken cancellationToken = default)
    {
        var performance = await _shipments.GetDriverPerformanceAsync(cancellationToken);
        return performance
            .Select(x => new DriverPerformanceResponse(x.DriverId, x.TotalAssigned, x.Delivered, x.Failed, x.OnTimeRate))
            .ToList();
    }

    public async Task<RatingResponse> AddRatingAsync(Guid shipmentId, Guid customerId, CreateRatingRequest request, CancellationToken cancellationToken = default)
    {
        var shipment = await _shipments.GetByIdAsync(shipmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Shipment not found");

        if (shipment.Status != ShipmentStatus.Delivered)
        {
            throw new InvalidOperationException("Only delivered shipments can be rated.");
        }

        var existing = await _shipments.GetRatingAsync(shipmentId, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException("This shipment has already been rated.");
        }

        var rating = new Rating
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentId,
            CustomerId = customerId,
            Stars = request.Stars,
            Comment = request.Comment,
            CreatedAt = DateTime.UtcNow
        };

        await _shipments.AddRatingAsync(rating, cancellationToken);

        return MapRating(rating);
    }

    public async Task<RatingResponse?> GetRatingAsync(Guid shipmentId, CancellationToken cancellationToken = default)
    {
        var rating = await _shipments.GetRatingAsync(shipmentId, cancellationToken);
        return rating is null ? null : MapRating(rating);
    }

    private static RatingResponse MapRating(Rating rating) =>
        new(rating.Id, rating.ShipmentId, rating.CustomerId, rating.Stars, rating.Comment, rating.CreatedAt);

    private static string GenerateTrackingNumber()
    {
        return $"TRK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6]}";
    }

    private static string GenerateInvoiceNumber()
    {
        return $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6]}";
    }

    private static Address MapAddress(AddressDto dto)
    {
        return new Address
        {
            Street = dto.Street,
            City = dto.City,
            State = dto.State,
            PostalCode = dto.PostalCode,
            Country = dto.Country,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude
        };
    }

    private static AddressDto MapAddressDto(Address address)
    {
        return new AddressDto(
            address.Street,
            address.City,
            address.State,
            address.PostalCode,
            address.Country,
            address.Latitude,
            address.Longitude);
    }

    private static string FormatAddress(Address address) => $"{address.Street}, {address.City}";

    private static ShipmentResponse Map(Shipment s)
    {
        return new ShipmentResponse(
            s.Id,
            s.PackageId,
            s.CustomerId,
            s.DriverId,
            s.TrackingNumber,
            s.Status.ToString(),
            MapAddressDto(s.PickupAddress),
            MapAddressDto(s.DeliveryAddress),
            s.Cost,
            s.Currency,
            s.CreatedAt
        );
    }

    private static InvoiceResponse MapInvoice(Invoice invoice)
    {
        return new InvoiceResponse(
            invoice.Id,
            invoice.ShipmentId,
            invoice.CustomerId,
            invoice.InvoiceNumber,
            invoice.Amount,
            invoice.Currency,
            invoice.PaymentStatus.ToString(),
            invoice.IssuedAt,
            invoice.PaidAt
        );
    }
}
