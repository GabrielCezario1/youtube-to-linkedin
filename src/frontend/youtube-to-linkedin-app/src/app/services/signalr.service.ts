import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import * as signalR from '@microsoft/signalr';

export type StepId = 'transcript' | 'summary' | 'writing';
export type StepStatus = 'pending' | 'in_progress' | 'completed' | 'error';

export interface PostDraftResult {
  draft: string;
  templateUsed: string;
}

export interface WorkflowEvent {
  step: StepId;
  status: StepStatus;
  message?: string;
  result?: PostDraftResult;
}

export interface WorkflowEventEnvelope {
  sessionId: string;
  event: WorkflowEvent;
}

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private hub = new signalR.HubConnectionBuilder()
    .withUrl('https://localhost:5224/hubs/workflow')
    .withAutomaticReconnect()
    .build();

  workflowEvent$ = new Subject<WorkflowEventEnvelope>();

  async connect(): Promise<void> {
    this.hub.on('workflowEvent', (sessionId: string, event: WorkflowEvent) =>
      this.workflowEvent$.next({ sessionId, event })
    );
    await this.hub.start();
  }
}
