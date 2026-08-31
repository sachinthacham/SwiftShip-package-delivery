export enum UserRole {
  Customer = 'Customer',
  Courier = 'Courier',
  Dispatcher = 'Dispatcher',
  Admin = 'Admin'
}

export enum PackageStatus {
  Created = 'Created',
  PickedUp = 'PickedUp',
  InTransit = 'InTransit',
  OutForDelivery = 'OutForDelivery',
  Delivered = 'Delivered',
  FailedDelivery = 'FailedDelivery',
  Returned = 'Returned',
  Cancelled = 'Cancelled'
}

export enum ShipmentStatus {
  Created = 'Created',
  PickedUp = 'PickedUp',
  InTransit = 'InTransit',
  OutForDelivery = 'OutForDelivery',
  Delivered = 'Delivered',
  FailedDelivery = 'FailedDelivery',
  Returned = 'Returned',
  Cancelled = 'Cancelled'
}

export enum DeliveryType {
  Standard = 'Standard',
  Express = 'Express',
  SameDay = 'SameDay'
}

export enum VehicleType {
  Bicycle = 'Bicycle',
  Motorcycle = 'Motorcycle',
  Car = 'Car',
  Van = 'Van',
  Truck = 'Truck'
}

export enum PaymentStatus {
  Pending = 'Pending',
  Paid = 'Paid',
  Failed = 'Failed',
  Refunded = 'Refunded'
}

export enum DeliveryAttemptFailureReason {
  RecipientAbsent = 'RecipientAbsent',
  WrongAddress = 'WrongAddress',
  Refused = 'Refused',
  Other = 'Other'
}
