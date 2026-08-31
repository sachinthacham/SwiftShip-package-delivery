import { Address } from './package.model';
import { PaymentStatus, ShipmentStatus } from './enums';

export interface CreateShipmentRequest {
  packageId: string;
  pickupAddress: Address;
  deliveryAddress: Address;
}

export interface ShipmentResponse {
  id: string;
  packageId: string;
  customerId: string;
  driverId?: string | null;
  trackingNumber: string;
  status: ShipmentStatus;
  pickupAddress: Address;
  deliveryAddress: Address;
  cost: number;
  currency: string;
  createdAt: string;
}

export interface BulkShipmentResult {
  packageId: string;
  success: boolean;
  shipment?: ShipmentResponse | null;
  error?: string | null;
}

export interface UpdateShipmentStatusRequest {
  status: ShipmentStatus;
}

export interface AssignDriverRequest {
  driverId: string;
}

export interface LogDeliveryAttemptRequest {
  successful: boolean;
  failureReason?: string | null;
  notes?: string | null;
}

export interface DeliveryAttemptResponse {
  id: string;
  shipmentId: string;
  successful: boolean;
  failureReason?: string | null;
  notes?: string | null;
  proofOfDeliveryUrl?: string | null;
  attemptedAt: string;
  shipmentStatus: string;
}

export interface InvoiceResponse {
  id: string;
  shipmentId: string;
  customerId: string;
  invoiceNumber: string;
  amount: number;
  currency: string;
  paymentStatus: PaymentStatus;
  issuedAt: string;
  paidAt?: string | null;
}

export interface UpdatePaymentStatusRequest {
  status: PaymentStatus;
}

export interface CreateCheckoutSessionRequest {
  successUrl: string;
  cancelUrl: string;
}

export interface CheckoutSessionResponse {
  sessionId: string;
  checkoutUrl: string;
}

export interface CreateRatingRequest {
  stars: number;
  comment?: string | null;
}

export interface RatingResponse {
  id: string;
  shipmentId: string;
  customerId: string;
  stars: number;
  comment?: string | null;
  createdAt: string;
}

export interface ShipmentAnalyticsSummary {
  totalShipments: number;
  countsByStatus: Record<string, number>;
  deliveredToday: number;
  failedAttemptsToday: number;
  slaBreachedTotal: number;
  slaBreachedToday: number;
}

export interface DriverPerformance {
  driverId: string;
  totalAssigned: number;
  delivered: number;
  failed: number;
  onTimeRate: number;
}
