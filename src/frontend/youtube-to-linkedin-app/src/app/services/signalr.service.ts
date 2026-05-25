import { Injectable, NgZone } from '@angular/core';
import { ReplaySubject } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../environments/environment';

export type StepId = 'transcript' | 'summary' | 'writing';
export type StepStatus = 'pending' | 'in_progress' | 'completed' | 'error' | 'awaiting_input';

export interface PostDraftResult {
  draft: string;
  templateUsed: string;
}

export interface WorkflowEvent {
  step: StepId;
  status: StepStatus;
  message?: string;
  result?: PostDraftResult;
  questions?: string[];
  errorCode?: string;
}

export interface WorkflowEventEnvelope {
  sessionId: string;
  event: WorkflowEvent;
}

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private hub = new signalR.HubConnectionBuilder()
    .withUrl(`${environment.backendUrl}/hubs/workflow`)
    .withAutomaticReconnect()
    .build();

  workflowEvent$ = new ReplaySubject<WorkflowEventEnvelope>(20);

  constructor(private zone: NgZone) {}

  async connect(): Promise<void> {
    this.hub.on('workflowEvent', (sessionId: string, event: WorkflowEvent) => {
      this.zone.run(() => this.workflowEvent$.next({ sessionId, event }));
    });
    await this.hub.start();
  }
}
