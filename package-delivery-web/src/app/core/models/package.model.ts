import { DeliveryType, PackageStatus } from './enums';

export interface Address {
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  latitude?: number | null;
  longitude?: number | null;
}

export interface CreatePackageRequest {
  receiverName: string;
  receiverPhone: string;
  receiverAddress: Address;
  weight: number;
  length: number;
  width: number;
  height: number;
  declaredValue: number;
  deliveryType: DeliveryType;
}

export interface PackageResponse {
  id: string;
  senderId: string;
  receiverName: string;
  receiverPhone: string;
  receiverAddress: Address;
  weight: number;
  length: number;
  width: number;
  height: number;
  declaredValue: number;
  deliveryType: string;
  status: PackageStatus;
  createdAt: string;
}

export interface UpdatePackageStatusRequest {
  status: PackageStatus;
}
