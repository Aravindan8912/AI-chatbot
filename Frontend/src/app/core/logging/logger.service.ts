import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class LoggerService {
  info(message: string, data?: unknown): void {
    this.write('INFO', message, data);
  }

  warn(message: string, data?: unknown): void {
    this.write('WARN', message, data);
  }

  error(message: string, data?: unknown): void {
    this.write('ERROR', message, data);
  }

  private write(level: 'INFO' | 'WARN' | 'ERROR', message: string, data?: unknown): void {
    const timestamp = new Date().toISOString();
    const prefix = `[${timestamp}] [frontend] [${level}] ${message}`;

    if (level === 'ERROR') {
      console.error(prefix, data ?? '');
      return;
    }

    if (level === 'WARN') {
      console.warn(prefix, data ?? '');
      return;
    }

    console.log(prefix, data ?? '');
  }
}
