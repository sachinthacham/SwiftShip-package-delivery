namespace PackageService.Domain.Enums;

public enum PackageStatus
{
    Created,
    PickedUp,
    InTransit,
    OutForDelivery,
    Delivered,
    FailedDelivery,
    Returned,
    Cancelled
}
