import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { AddTrackingRequest, TrackingEvent } from '../../models';

@Injectable({ providedIn: 'root' })
export class TrackingApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/tracking/api/tracking`;

  addEvent(request: AddTrackingRequest): Observable<TrackingEvent> {
    return this.http.post<TrackingEvent>(this.base, request);
  }

  getByPackageId(packageId: string): Observable<TrackingEvent[]> {
    return this.http.get<TrackingEvent[]>(`${this.base}/${packageId}`);
  }

  /** Anonymous — no auth header is attached (see NO_AUTH_HEADER_PATTERNS in auth.interceptor.ts). */
  getPublicByTrackingNumber(trackingNumber: string): Observable<TrackingEvent[]> {
    return this.http.get<TrackingEvent[]>(`${this.base}/public/${trackingNumber}`);
  }
}
