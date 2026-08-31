import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { CreatePackageRequest, PackageResponse, PackageStatus, PaginatedList, UpdatePackageStatusRequest } from '../../models';

@Injectable({ providedIn: 'root' })
export class PackageApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/package/api/packages`;

  create(request: CreatePackageRequest): Observable<PackageResponse> {
    return this.http.post<PackageResponse>(this.base, request);
  }

  getPaged(page = 1, pageSize = 20, status?: PackageStatus): Observable<PaginatedList<PackageResponse>> {
    const params: Record<string, string | number> = { page, pageSize };
    if (status) params['status'] = status;
    return this.http.get<PaginatedList<PackageResponse>>(this.base, { params });
  }

  getById(id: string): Observable<PackageResponse> {
    return this.http.get<PackageResponse>(`${this.base}/${id}`);
  }

  updateStatus(id: string, request: UpdatePackageStatusRequest): Observable<PackageResponse> {
    return this.http.patch<PackageResponse>(`${this.base}/${id}/status`, request);
  }

  cancel(id: string): Observable<PackageResponse> {
    return this.http.post<PackageResponse>(`${this.base}/${id}/cancel`, {});
  }
}
