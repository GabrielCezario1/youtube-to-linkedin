import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SignalRService } from './services/signalr.service';
import { WorkflowService } from './services/workflow.service';
import { WorkflowProgressComponent } from './components/workflow-progress/workflow-progress';

@Component({
  selector: 'app-root',
  imports: [FormsModule, WorkflowProgressComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private signalRService = inject(SignalRService);
  private workflowService = inject(WorkflowService);

  url = '';
  postType = '';
  mode = '';

  view: 'form' | 'progress' = 'form';
  currentSessionId = '';

  private savedForm = { url: '', postType: '', mode: '' };

  async ngOnInit(): Promise<void> {
    await this.signalRService.connect();
  }

  onSubmit(): void {
    this.savedForm = { url: this.url, postType: this.postType, mode: this.mode };

    this.workflowService.start(this.url, this.postType, this.mode).subscribe({
      next: (res) => {
        this.currentSessionId = res.sessionId;
        this.view = 'progress';
      },
      error: (err) => console.error('[Workflow] error:', err)
    });
  }

  onRetry(): void {
    this.url = this.savedForm.url;
    this.postType = this.savedForm.postType;
    this.mode = this.savedForm.mode;
    this.view = 'form';
  }
}
