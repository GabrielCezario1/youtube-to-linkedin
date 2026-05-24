import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { WorkflowService } from '../../services/workflow.service';

@Component({
  selector: 'app-consulted-questions',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './consulted-questions.html'
})
export class ConsultedQuestionsComponent {
  @Input() questions: string[] = [];
  @Input() sessionId = '';
  @Output() submitted = new EventEmitter<void>();

  answers: string[] = [];

  private workflowService = inject(WorkflowService);

  onAnswerChange(index: number, value: string): void {
    this.answers[index] = value;
  }

  onSubmit(): void {
    const answers = this.questions.map((_, i) => this.answers[i] ?? '');
    this.workflowService.respond(this.sessionId, answers).subscribe({
      next: () => this.submitted.emit(),
      error: () => this.submitted.emit()
    });
  }
}
