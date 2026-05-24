import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SignalRService } from './services/signalr.service';
import { WorkflowService } from './services/workflow.service';

@Component({
  selector: 'app-root',
  imports: [FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private signalRService = inject(SignalRService);
  private workflowService = inject(WorkflowService);

  url = '';
  postType = '';
  mode = '';

  async ngOnInit(): Promise<void> {
    this.signalRService.progress$.subscribe(event => {
      console.log('[SignalR] SendProgress:', event);
    });
    await this.signalRService.connect();
  }

  onSubmit(): void {
    this.workflowService.start(this.url, this.postType, this.mode).subscribe({
      next: (res) => console.log('[Workflow] sessionId:', res.sessionId),
      error: (err) => console.error('[Workflow] error:', err)
    });
  }
}
