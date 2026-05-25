import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { SignalRService, StepId, StepStatus } from '../../services/signalr.service';
import { ErrorDisplayComponent } from '../error-display/error-display';

interface WorkflowStep {
  id: StepId;
  label: string;
  status: StepStatus;
  errorMessage?: string;
}

@Component({
  selector: 'app-workflow-progress',
  standalone: true,
  imports: [CommonModule, ErrorDisplayComponent],
  templateUrl: './workflow-progress.html',
  styleUrl: './workflow-progress.css'
})
export class WorkflowProgressComponent implements OnInit, OnDestroy {
  @Input() sessionId!: string;
  @Output() retry = new EventEmitter<void>();

  private sub?: Subscription;

  steps: WorkflowStep[] = [
    { id: 'transcript', label: 'Extraindo transcrição', status: 'pending' },
    { id: 'summary', label: 'Gerando resumo', status: 'pending' },
    { id: 'writing', label: 'Criando post', status: 'pending' }
  ];

  get errorStep(): WorkflowStep | undefined {
    return this.steps.find(s => s.status === 'error');
  }

  get allDone(): boolean {
    return this.steps.every(s => s.status === 'completed' || s.status === 'error');
  }

  constructor(private signalR: SignalRService) {}

  ngOnInit(): void {
    this.sub = this.signalR.workflowEvent$.subscribe(({ sessionId, event }) => {
      if (sessionId !== this.sessionId) return;
      const step = this.steps.find(s => s.id === event.step);
      if (step) {
        step.status = event.status;
        step.errorMessage = event.message;
      }
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  stepIcon(status: StepStatus): string {
    switch (status) {
      case 'pending':        return '○';
      case 'in_progress':    return '⏳';
      case 'awaiting_input': return '⏸';
      case 'completed':      return '✅';
      case 'error':          return '❌';
    }
  }

  onRetry(): void {
    this.retry.emit();
  }
}
