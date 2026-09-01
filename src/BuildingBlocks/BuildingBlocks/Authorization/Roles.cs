namespace BuildingBlocks.Authorization;

public static class Roles
{
    public const string Customer = "Customer";
    public const string Courier = "Courier";
    public const string Dispatcher = "Dispatcher";
    public const string Admin = "Admin";

    /// <summary>Roles a user may select for themselves at registration time.</summary>
    public static readonly IReadOnlyCollection<string> SelfRegisterable = new[] { Customer, Courier };

    /// <summary>All valid roles. Dispatcher/Admin accounts are only assignable by an existing Admin.</summary>
    public static readonly IReadOnlyCollection<string> All = new[] { Customer, Courier, Dispatcher, Admin };

    public const string DispatcherOrAdmin = Dispatcher + "," + Admin;
    public const string CourierOrAdmin = Courier + "," + Admin;
    public const string CustomerDispatcherOrAdmin = Customer + "," + Dispatcher + "," + Admin;
    public const string CourierDispatcherOrAdmin = Courier + "," + Dispatcher + "," + Admin;
}
