import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-chat-composer',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat-composer.component.html',
  styleUrl: './chat-composer.component.css',
})
export class ChatComposerComponent {
  @Input({ required: true }) message = '';
  @Input() loading = false;
  @Input({ required: true }) productName = 'Tree AI';

  @Output() messageChange = new EventEmitter<string>();
  @Output() sendMessage = new EventEmitter<void>();

  onModelChange(value: string): void {
    this.messageChange.emit(value);
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key !== 'Enter' || event.shiftKey) return;
    event.preventDefault();
    this.sendMessage.emit();
  }
}
