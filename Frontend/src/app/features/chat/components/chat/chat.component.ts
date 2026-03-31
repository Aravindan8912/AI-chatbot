import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { TimeoutError, finalize } from 'rxjs';
import { ChatService } from '../../services/chat.service';
import { LoggerService } from '../../../../core/logging/logger.service';
import { ChatMessage } from '../../../../shared/models/chat-message.model';
import { ChatComposerComponent } from '../../../../shared/ui/chat-composer/chat-composer.component';
import { ChatMessageListComponent } from '../../../../shared/ui/chat-message-list/chat-message-list.component';
import { ChatSidebarComponent } from '../../../../shared/ui/chat-sidebar/chat-sidebar.component';
import { ChatTopbarComponent } from '../../../../shared/ui/chat-topbar/chat-topbar.component';

function problemDetail(error: unknown): string | undefined {
  if (error && typeof error === 'object' && 'detail' in error) {
    const d = (error as { detail: unknown }).detail;
    return typeof d === 'string' ? d : undefined;
  }
  return undefined;
}

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [ChatSidebarComponent, ChatTopbarComponent, ChatMessageListComponent, ChatComposerComponent],
  templateUrl: './chat.component.html',
  styleUrl: './chat.component.css',
})
export class ChatComponent {
  private readonly chat = inject(ChatService);
  private readonly logger = inject(LoggerService);
  private readonly cdr = inject(ChangeDetectorRef);

  protected readonly productName = 'Tree AI';
  message = '';
  error = '';
  loading = false;
  messages: ChatMessage[] = [
    {
      role: 'assistant',
      content:
        "Hi, I'm Tree AI. Tell me what you want to work on, and I'll help you get there.",
    },
  ];

  private static extractReply(res: unknown): string {
    if (typeof res === 'string') return res.trim();

    if (res && typeof res === 'object' && 'response' in res) {
      const value = (res as { response?: unknown }).response;
      return typeof value === 'string' ? value.trim() : '';
    }

    return '';
  }

  send(): void {
    const text = this.message.trim();
    if (!text || this.loading) return;

    this.logger.info('Chat send initiated', { textLength: text.length });
    this.loading = true;
    this.error = '';
    this.messages.push({ role: 'user', content: text });
    this.cdr.detectChanges();
    const hardStopTimer = window.setTimeout(() => {
      if (!this.loading) return;
      this.logger.warn('Chat hard-stop timer fired');
      this.loading = false;
      if (!this.error) {
        this.error = 'Request took too long. Please try again.';
      }
      this.cdr.detectChanges();
    }, 35000);

    this.chat
      .ask({ message: text })
      .pipe(
        finalize(() => {
          window.clearTimeout(hardStopTimer);
          this.loading = false;
          this.logger.info('Chat request finalized');
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (res) => {
          const reply = ChatComponent.extractReply(res);
          this.logger.info('Chat response parsed', { hasReply: !!reply, replyLength: reply.length });
          this.messages.push({
            role: 'assistant',
            content:
              reply || "I'm here and ready to help. Tell me what you need, and we'll work through it together.",
          });
          this.message = '';
          this.cdr.detectChanges();
        },
        error: (err: HttpErrorResponse | TimeoutError) => {
          if (err instanceof TimeoutError) {
            this.logger.warn('Chat request timed out');
            this.error = 'The request timed out after 30 seconds. Please try again.';
            this.cdr.detectChanges();
            return;
          }

          this.logger.error('Chat request failed', {
            status: err.status,
            message: err.message,
            details: err.error,
          });
          const fromApi = problemDetail(err.error);
          this.error =
            fromApi ??
            (err.status === 0
              ? 'Could not reach the API. Start the backend (e.g. dotnet run in Backend/Api) and check http://localhost:5000.'
              : `Request failed (${err.status}). Check the API and OpenAI configuration.`);
          this.cdr.detectChanges();
        },
      });
  }

}
