import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-error-display',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './error-display.html',
  styleUrl: './error-display.css'
})
export class ErrorDisplayComponent {
  @Input() message!: string;
  @Output() retry = new EventEmitter<void>();

  get buttonLabel(): string {
    return this.message?.includes('não possui transcrição')
      ? 'Tentar com outro vídeo'
      : 'Tentar novamente';
  }

  onRetry(): void {
    this.retry.emit();
  }
}
