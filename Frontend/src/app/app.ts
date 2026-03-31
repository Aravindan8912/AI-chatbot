import { Component } from '@angular/core';
import { ChatComponent } from './features/chat/components/chat/chat.component';

@Component({
  selector: 'app-root',
  imports: [ChatComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {}
