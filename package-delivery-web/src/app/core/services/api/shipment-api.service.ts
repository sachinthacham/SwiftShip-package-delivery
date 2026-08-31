import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  AssignDriverRequest,
  BulkShipmentResult,
  CheckoutSessionResponse,
  CreateCheckoutSessionRequest,
  CreateRatingRequest,
  CreateShipmentRequest,
  DeliveryAttemptResponse,
  DriverPerformance,
  InvoiceResponse,
  LogDeliveryAttemptRequest,
  PaginatedList,
  RatingResponse,
  ShipmentAnalyticsSummary,
  ShipmentResponse,
  ShipmentStatus,
  UpdatePaymentStatusRequest,
  UpdateShipmentStatusRequest
} from '../../models';

@Injectable({ providedIn: 'root' })
export class ShipmentApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/shipment/api/shipments`;

  create(request: CreateShipmentRequest, idempotencyKey?: string): Observable<ShipmentResponse> {
    const headers = idempotencyKey ? { 'Idempotency-Key': idempotencyKey } : undefined;
    return this.http.post<ShipmentResponse>(this.base, request, { headers });
  }

  createBulk(requests: CreateShipmentRequest[]): Observable<BulkShipmentResult[]> {
    return this.http.post<BulkShipmentResult[]>(`${this.base}/bulk`, requests);
  }

  /** Customers implicitly get only their own shipments; couriers must pass driverId (see GET /api/drivers/me). */
  getPaged(page = 1, pageSize = 20, status?: ShipmentStatus, driverId?: string): Observable<PaginatedList<ShipmentResponse>> {
    const params: Record<string, string | number> = { page, pageSize };
    if (status) params['status'] = status;
    if (driverId) params['driverId'] = driverId;
    return this.http.get<PaginatedList<ShipmentResponse>>(this.base, { params });
  }

  getById(id: string): Observable<ShipmentResponse> {
    return this.http.get<ShipmentResponse>(`${this.base}/${id}`);
  }

  updateStatus(id: string, request: UpdateShipmentStatusRequest): Observable<ShipmentResponse> {
    return this.http.patch<ShipmentResponse>(`${this.base}/${id}/status`, request);
  }

  assignDriver(id: string, request: AssignDriverRequest): Observable<ShipmentResponse> {
    return this.http.post<ShipmentResponse>(`${this.base}/${id}/assign`, request);
  }

  autoAssignDriver(id: string, radiusKm = 10): Observable<ShipmentResponse> {
    return this.http.post<ShipmentResponse>(`${this.base}/${id}/auto-assign`, {}, { params: { radiusKm } });
  }

  logDeliveryAttempt(id: string, request: LogDeliveryAttemptRequest): Observable<DeliveryAttemptResponse> {
    return this.http.post<DeliveryAttemptResponse>(`${this.base}/${id}/attempts`, request);
  }

  uploadProofOfDelivery(id: string, attemptId: string, file: File): Observable<DeliveryAttemptResponse> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<DeliveryAttemptResponse>(`${this.base}/${id}/attempts/${attemptId}/proof`, formData);
  }

  getInvoice(id: string): Observable<InvoiceResponse> {
    return this.http.get<InvoiceResponse>(`${this.base}/${id}/invoice`);
  }

  createCheckoutSession(id: string, request: CreateCheckoutSessionRequest): Observable<CheckoutSessionResponse> {
    return this.http.post<CheckoutSessionResponse>(`${this.base}/${id}/invoice/checkout-session`, request);
  }

  updatePaymentStatus(id: string, request: UpdatePaymentStatusRequest): Observable<InvoiceResponse> {
    return this.http.patch<InvoiceResponse>(`${this.base}/${id}/invoice/payment-status`, request);
  }

  addRating(id: string, request: CreateRatingRequest): Observable<RatingResponse> {
    return this.http.post<RatingResponse>(`${this.base}/${id}/rating`, request);
  }

  getRating(id: string): Observable<RatingResponse | null> {
    return this.http.get<RatingResponse | null>(`${this.base}/${id}/rating`);
  }

  getAnalyticsSummary(): Observable<ShipmentAnalyticsSummary> {
    return this.http.get<ShipmentAnalyticsSummary>(`${this.base}/analytics/summary`);
  }

  getDriverPerformance(): Observable<DriverPerformance[]> {
    return this.http.get<DriverPerformance[]>(`${this.base}/analytics/drivers/performance`);
  }
}
