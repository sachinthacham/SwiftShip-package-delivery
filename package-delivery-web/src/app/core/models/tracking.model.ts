export interface AddTrackingRequest {
  packageId: string;
  location: string;
  status: string;
}

export interface TrackingEvent {
  id: string;
  packageId: string;
  shipmentId?: string | null;
  trackingNumber?: string | null;
  location: string;
  status: string;
  timestampUtc: string;
}

/** Payload broadcast by the TrackingHub's `TrackingUpdated` SignalR event — same shape as TrackingEvent. */
export type TrackingUpdatedPayload = TrackingEvent;
