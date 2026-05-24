import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { SignalRService, PostDraftResult } from './services/signalr.service';
import { WorkflowService } from './services/workflow.service';
import { WorkflowProgressComponent } from './components/workflow-progress/workflow-progress';
import { PostDraftComponent } from './components/post-draft/post-draft';

@Component({
  selector: 'app-root',
  imports: [FormsModule, WorkflowProgressComponent, PostDraftComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit, OnDestroy {
  private signalRService = inject(SignalRService);
  private workflowService = inject(WorkflowService);

  url = '';
  postType = '';
  mode = '';

  view: 'form' | 'progress' = 'form';
  currentSessionId = '';
  postDraft: PostDraftResult | null = null;

  private savedForm = { url: '', postType: '', mode: '' };
  private sub?: Subscription;

  async ngOnInit(): Promise<void> {
    await this.signalRService.connect();
    this.sub = this.signalRService.workflowEvent$.subscribe(({ sessionId, event }) => {
      if (sessionId !== this.currentSessionId) return;
      if (event.step === 'writing' && event.status === 'completed' && event.result) {
        this.postDraft = event.result;
      }
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  onSubmit(): void {
    this.savedForm = { url: this.url, postType: this.postType, mode: this.mode };

    this.workflowService.start(this.url, this.postType, this.mode).subscribe({
      next: (res) => {
        this.currentSessionId = res.sessionId;
        this.postDraft = null;
        this.view = 'progress';
      },
      error: (err) => console.error('[Workflow] error:', err)
    });
  }

  onRetry(): void {
    this.url = this.savedForm.url;
    this.postType = this.savedForm.postType;
    this.mode = this.savedForm.mode;
    this.postDraft = null;
    this.view = 'form';
  }

  onReset(): void {
    this.postDraft = null;
    this.currentSessionId = '';
    this.view = 'form';
  }
}
