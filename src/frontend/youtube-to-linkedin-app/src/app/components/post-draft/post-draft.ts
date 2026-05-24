import { Component, Input, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-post-draft',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './post-draft.html',
  styleUrl: './post-draft.css'
})
export class PostDraftComponent {
  @Input() draft = '';
  @Input() templateUsed = '';
  @Output() reset = new EventEmitter<void>();

  copied = signal(false);
  private copyTimeout?: ReturnType<typeof setTimeout>;

  async copyToClipboard(): Promise<void> {
    await navigator.clipboard.writeText(this.draft);
    this.copied.set(true);
    clearTimeout(this.copyTimeout);
    this.copyTimeout = setTimeout(() => this.copied.set(false), 2000);
  }

  onReset(): void {
    this.reset.emit();
  }
}
