import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { TrackingUpdatedPayload } from '../models';
import { TokenService } from './token.service';

/**
 * Wraps the TrackingHub connection. Auth is optional: the hub allows anonymous connections for guest
 * tracking, so accessTokenFactory returns whatever token is present (or none) rather than requiring one.
 */
@Injectable({ providedIn: 'root' })
export class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private readonly joinedGroups = new Set<string>();

  private readonly _lastUpdate = signal<TrackingUpdatedPayload | null>(null);
  readonly lastUpdate = this._lastUpdate.asReadonly();

  constructor(private readonly tokenService: TokenService) {}

  private async ensureConnected(): Promise<signalR.HubConnection> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      return this.connection;
    }

    if (!this.connection) {
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(environment.trackingHubUrl, {
          accessTokenFactory: () => this.tokenService.getAccessToken() ?? ''
        })
        .withAutomaticReconnect()
        .build();

      this.connection.on('TrackingUpdated', (payload: TrackingUpdatedPayload) => {
        this._lastUpdate.set(payload);
      });

      this.connection.onreconnected(() => {
        // Re-join any groups the caller subscribed to before the connection dropped.
        this.joinedGroups.forEach((trackingNumber) => {
          this.connection?.invoke('SubscribeToTracking', trackingNumber).catch(() => void 0);
        });
      });
    }

    if (this.connection.state === signalR.HubConnectionState.Disconnected) {
      await this.connection.start();
    }

    return this.connection;
  }

  async subscribeToTracking(trackingNumber: string): Promise<void> {
    const connection = await this.ensureConnected();
    this.joinedGroups.add(trackingNumber);
    await connection.invoke('SubscribeToTracking', trackingNumber);
  }

  async unsubscribeFromTracking(trackingNumber: string): Promise<void> {
    this.joinedGroups.delete(trackingNumber);
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('UnsubscribeFromTracking', trackingNumber);
    }
  }

  async disconnect(): Promise<void> {
    await this.connection?.stop();
  }
}
