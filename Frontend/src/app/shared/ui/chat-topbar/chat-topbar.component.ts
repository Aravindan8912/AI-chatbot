import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-chat-topbar',
  standalone: true,
  templateUrl: './chat-topbar.component.html',
  styleUrl: './chat-topbar.component.css',
})
export class ChatTopbarComponent {
  @Input({ required: true }) productName = 'Tree AI';
}
