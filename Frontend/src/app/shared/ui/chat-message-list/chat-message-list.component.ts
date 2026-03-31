import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { ChatMessage } from '../../models/chat-message.model';

@Component({
  selector: 'app-chat-message-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './chat-message-list.component.html',
  styleUrl: './chat-message-list.component.css',
})
export class ChatMessageListComponent {
  @Input({ required: true }) messages: ChatMessage[] = [];
  @Input() loading = false;
  @Input() error = '';
}
