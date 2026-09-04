import { UserRole } from './enums';

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  /** Server enforces self-registration is limited to Customer/Courier (see BuildingBlocks Roles.SelfRegisterable). */
  role: UserRole;
  phoneNumber?: string | null;
}

export interface MeResponse {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
  createdAt: string;
  phoneNumber?: string | null;
}

export interface UserSummary {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
  createdAt: string;
  phoneNumber?: string | null;
}

export interface AdminCreateUserRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  role: UserRole;
  phoneNumber?: string | null;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface SavedAddress {
  id: string;
  label: string;
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  latitude?: number | null;
  longitude?: number | null;
  isDefault: boolean;
  createdAt: string;
}

export interface CreateSavedAddressRequest {
  label: string;
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  latitude?: number | null;
  longitude?: number | null;
  isDefault: boolean;
}

export type UpdateSavedAddressRequest = Omit<CreateSavedAddressRequest, 'isDefault'>;
