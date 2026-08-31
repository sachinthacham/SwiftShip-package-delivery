import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  AdminCreateUserRequest,
  ChangePasswordRequest,
  CreateSavedAddressRequest,
  PaginatedList,
  SavedAddress,
  UpdateSavedAddressRequest,
  UserSummary
} from '../../models';

@Injectable({ providedIn: 'root' })
export class IdentityApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/identity/api`;

  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/auth/change-password`, request);
  }

  // Saved addresses (api/me/addresses)
  listAddresses(): Observable<SavedAddress[]> {
    return this.http.get<SavedAddress[]>(`${this.base}/me/addresses`);
  }

  getAddress(id: string): Observable<SavedAddress> {
    return this.http.get<SavedAddress>(`${this.base}/me/addresses/${id}`);
  }

  createAddress(request: CreateSavedAddressRequest): Observable<SavedAddress> {
    return this.http.post<SavedAddress>(`${this.base}/me/addresses`, request);
  }

  updateAddress(id: string, request: UpdateSavedAddressRequest): Observable<SavedAddress> {
    return this.http.put<SavedAddress>(`${this.base}/me/addresses/${id}`, request);
  }

  deleteAddress(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/me/addresses/${id}`);
  }

  setDefaultAddress(id: string): Observable<void> {
    return this.http.put<void>(`${this.base}/me/addresses/${id}/default`, {});
  }

  // Admin (api/admin/users) — role Admin only
  adminCreateUser(request: AdminCreateUserRequest): Observable<UserSummary> {
    return this.http.post<UserSummary>(`${this.base}/admin/users`, request);
  }

  adminListUsers(page = 1, pageSize = 20): Observable<PaginatedList<UserSummary>> {
    return this.http.get<PaginatedList<UserSummary>>(`${this.base}/admin/users`, { params: { page, pageSize } });
  }
}
