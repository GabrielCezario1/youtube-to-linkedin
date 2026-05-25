import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { SignalRService, PostDraftResult } from './services/signalr.service';
import { WorkflowService } from './services/workflow.service';
import { WorkflowProgressComponent } from './components/workflow-progress/workflow-progress';
import { PostDraftComponent } from './components/post-draft/post-draft';
import { ConsultedQuestionsComponent } from './components/consulted-questions/consulted-questions';

const FRONTEND_FLOW = `
╔═══════════════════════════════════════════════════════════╗
║       yt → linkedin  ·  FRONTEND FLOW                    ║
╠═══════════════════════════════════════════════════════════╣
║                                                           ║
║  1. App.ngOnInit()                                        ║
║       └─▶ SignalRService.connect()                        ║
║             hub: ws://backend/hubs/workflow               ║
║             └─▶ workflowEvent$.subscribe()                ║
║                                                           ║
║  2. User → [Gerar Post] → onSubmit()                      ║
║       └─▶ WorkflowService  POST /api/workflow/start       ║
║            │  ╔──────────────────────────╗               ║
║            └─▶║    see  BACKEND FLOW     ║               ║
║               ╚──────────────────────────╝               ║
║            ◀─ 200 { sessionId }                           ║
║       └─▶ view = 'progress'                               ║
║             └─▶ WorkflowProgress.ngOnInit()               ║
║                   └─▶ workflowEvent$.subscribe()          ║
║                                                           ║
║  3. SignalR events ────────────────────────────────────   ║
║       transcript:  … → in_progress → completed           ║
║       summary:     … → in_progress → completed           ║
║       writing:     … → in_progress                       ║
║         [auto]       ──▶ completed  (post draft shown)   ║
║         [consultado] ──▶ awaiting_input (Q&A panel)      ║
║                       ──▶ in_progress → completed        ║
║                                                           ║
╚═══════════════════════════════════════════════════════════╝`;

const BACKEND_FLOW = `
╔═══════════════════════════════════════════════════════════╗
║       yt → linkedin  ·  BACKEND FLOW                     ║
╠═══════════════════════════════════════════════════════════╣
║                                                           ║
║  POST /api/workflow/start                                 ║
║    ├─ validate URL, postType, mode  → 400 if invalid      ║
║    ├─ sessionManager.Register(sessionId, cts, postType)   ║
║    ├─ Task.Run  [fire & forget] ─────────────────────     ║
║    └─▶ 200 { sessionId }  ◀──── frontend receives         ║
║                                                           ║
║  [Background Task]                                        ║
║    ▼                                                      ║
║  TranscriptExecutor.ExecuteAsync(url)                     ║
║    ├─▶ SignalR  transcript / in_progress                  ║
║    ├─▶ YoutubeClient.ClosedCaptions fetch                 ║
║    └─▶ SignalR  transcript / completed                    ║
║    ▼                                                      ║
║  SummaryExecutor.ExecuteAsync(transcript)                 ║
║    ├─▶ SignalR  summary / in_progress                     ║
║    ├─▶ Azure OpenAI  gpt-4o-mini                          ║
║    └─▶ SignalR  summary / completed                       ║
║    ▼                                                      ║
║  LinkedInWriterExecutor.ExecuteAsync(summary)             ║
║    ├─▶ SignalR  writing / in_progress                     ║
║    ├─ [auto]  ──▶ LLM ──▶ writing / completed             ║
║    └─ [consultado]                                        ║
║         ├─▶ SignalR  writing / awaiting_input + questions ║
║         ├─▶ POST /api/workflow/{id}/respond               ║
║         ├─▶ SignalR  writing / in_progress                ║
║         └─▶ LLM ──▶ SignalR  writing / completed + result ║
║                                                           ║
╚═══════════════════════════════════════════════════════════╝`;

@Component({
  selector: 'app-root',
  imports: [FormsModule, WorkflowProgressComponent, PostDraftComponent, ConsultedQuestionsComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit, OnDestroy {
  private signalRService = inject(SignalRService);
  private workflowService = inject(WorkflowService);

  url = '';
  postType = '';
  mode = '';

  view = signal<'form' | 'progress'>('form');
  currentSessionId = '';
  postDraft = signal<PostDraftResult | null>(null);
  consultedQuestions = signal<string[] | null>(null);
  lastError = signal<{ errorCode: string; message: string } | null>(null);

  private savedForm = { url: '', postType: '', mode: '' };
  private sub?: Subscription;

  async ngOnInit(): Promise<void> {
    try {
      await this.signalRService.connect();
    } catch (err) {
      console.error('[App] SignalR connection failed:', err);
    }
    this.sub = this.signalRService.workflowEvent$.subscribe(({ sessionId, event }) => {
      console.log(`%c[App] ◀ event  ${event.step}/${event.status}`, 'color:#a78bfa;font-weight:bold', `session=${sessionId.slice(0, 8)}…`);
      if (sessionId !== this.currentSessionId) return;
      if (event.status === 'error') {
        const errorCode = event.errorCode ?? 'llm_error';
        if (errorCode === 'session_expired') {
          this.url = this.savedForm.url;
          this.postType = this.savedForm.postType;
          this.mode = this.savedForm.mode;
          this.consultedQuestions.set(null);
          this.postDraft.set(null);
          this.view.set('form');
        } else {
          this.lastError.set({ errorCode, message: event.message ?? '' });
        }
        return;
      }
      if (event.step === 'writing' && event.status === 'completed' && event.result) {
        this.postDraft.set(event.result);
      }
      if (event.step === 'writing' && event.status === 'awaiting_input') {
        this.consultedQuestions.set(event.questions ?? []);
      }
      if (event.step === 'writing' && event.status === 'in_progress') {
        this.consultedQuestions.set(null);
      }
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  onSubmit(): void {
    console.log('%c[App] form submitted', 'color:#fb923c;font-weight:bold', { url: this.url, postType: this.postType, mode: this.mode });
    this.savedForm = { url: this.url, postType: this.postType, mode: this.mode };
    this.consultedQuestions.set(null);
    this.lastError.set(null);

    this.workflowService.start(this.url, this.postType, this.mode).subscribe({
      next: (res) => {
        console.log('%c[App] ✅ session started', 'color:#4ade80;font-weight:bold', res.sessionId);
        this.currentSessionId = res.sessionId;
        this.postDraft.set(null);
        this.view.set('progress');
      },
      error: (err) => {
        const message = err.error?.error ?? 'Ocorreu um erro ao iniciar o workflow. Tente novamente.';
        const errorCode = err.error?.field === 'url' ? 'invalid_url' : 'llm_error';
        console.error('[App] ❌ HTTP error:', { errorCode, message, status: err.status });
        this.lastError.set({ errorCode, message });
        this.view.set('progress');
      }
    });
  }

  onCancel(): void {
    this.workflowService.cancel(this.currentSessionId).subscribe({
      error: () => {} // Ignore errors (e.g. 404 if session already completed)
    });
    this.url = this.savedForm.url;
    this.postType = this.savedForm.postType;
    this.mode = this.savedForm.mode;
    this.postDraft.set(null);
    this.consultedQuestions.set(null);
    this.lastError.set(null);
    this.view.set('form');
  }

  onRetry(): void {
    const error = this.lastError();
    this.url = error?.errorCode === 'no_transcript' ? '' : this.savedForm.url;
    this.postType = this.savedForm.postType;
    this.mode = this.savedForm.mode;
    this.lastError.set(null);
    this.postDraft.set(null);
    this.consultedQuestions.set(null);
    this.view.set('form');
  }

  onReset(): void {
    this.postDraft.set(null);
    this.consultedQuestions.set(null);
    this.lastError.set(null);
    this.currentSessionId = '';
    this.view.set('form');
  }

  onConsultedSubmitted(): void {
    this.consultedQuestions.set(null);
  }
}

