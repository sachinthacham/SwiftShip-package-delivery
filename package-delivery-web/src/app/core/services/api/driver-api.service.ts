import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  CreateDriverRequest,
  DriverResponse,
  PaginatedList,
  SetDriverAvailabilityRequest,
  UpdateDriverLocationRequest
} from '../../models';

@Injectable({ providedIn: 'root' })
export class DriverApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/driver/api/drivers`;

  create(request: CreateDriverRequest): Observable<DriverResponse> {
    return this.http.post<DriverResponse>(this.base, request);
  }

  getPaged(page = 1, pageSize = 20, isAvailable?: boolean): Observable<PaginatedList<DriverResponse>> {
    const params: Record<string, string | number | boolean> = { page, pageSize };
    if (isAvailable !== undefined) params['isAvailable'] = isAvailable;
    return this.http.get<PaginatedList<DriverResponse>>(this.base, { params });
  }

  getById(id: string): Observable<DriverResponse> {
    return this.http.get<DriverResponse>(`${this.base}/${id}`);
  }

  /** Returns the driver profile linked to the current Courier's account. */
  getMe(): Observable<DriverResponse> {
    return this.http.get<DriverResponse>(`${this.base}/me`);
  }

  setAvailability(id: string, request: SetDriverAvailabilityRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}/availability`, request);
  }

  updateMyLocation(request: UpdateDriverLocationRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/me/location`, request);
  }
}
