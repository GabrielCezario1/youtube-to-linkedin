import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import * as signalR from '@microsoft/signalr';

export interface ProgressEvent {
  sessionId: string;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private hub = new signalR.HubConnectionBuilder()
    .withUrl('https://localhost:5001/hubs/workflow')
    .withAutomaticReconnect()
    .build();

  progress$ = new Subject<ProgressEvent>();

  async connect(): Promise<void> {
    this.hub.on('SendProgress', (sessionId: string, message: string) =>
      this.progress$.next({ sessionId, message })
    );
    await this.hub.start();
  }
}
