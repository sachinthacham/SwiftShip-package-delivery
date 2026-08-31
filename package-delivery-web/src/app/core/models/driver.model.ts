import { VehicleType } from './enums';

export interface CreateDriverRequest {
  userId: string;
  name: string;
  vehicleNumber: string;
  vehicleType: VehicleType;
}

export interface DriverResponse {
  id: string;
  userId: string;
  name: string;
  vehicleNumber: string;
  vehicleType: string;
  isAvailable: boolean;
  currentLatitude?: number | null;
  currentLongitude?: number | null;
  createdAtUtc: string;
}

export interface SetDriverAvailabilityRequest {
  isAvailable: boolean;
}

export interface UpdateDriverLocationRequest {
  latitude: number;
  longitude: number;
}

export interface NearbyDriverResponse {
  driverId: string;
  userId: string;
  name: string;
  vehicleType: string;
  currentLatitude: number;
  currentLongitude: number;
  distanceKm: number;
}
