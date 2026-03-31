import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { tap } from 'rxjs';
import { LoggerService } from './logger.service';

export const httpLoggingInterceptor: HttpInterceptorFn = (req, next) => {
  const logger = inject(LoggerService);
  const startedAt = performance.now();

  logger.info(`HTTP request -> ${req.method} ${req.url}`);

  return next(req).pipe(
    tap({
      next: (event) => {
        const elapsedMs = Math.round(performance.now() - startedAt);

        if ('status' in event) {
          logger.info(`HTTP response <- ${req.method} ${req.url} (${event.status})`, {
            elapsedMs,
          });
        }
      },
      error: (error: unknown) => {
        const elapsedMs = Math.round(performance.now() - startedAt);

        if (error instanceof HttpErrorResponse) {
          logger.error(`HTTP error <- ${req.method} ${req.url} (${error.status})`, {
            elapsedMs,
            message: error.message,
            error: error.error,
          });
          return;
        }

        logger.error(`HTTP unknown error <- ${req.method} ${req.url}`, { elapsedMs, error });
      },
    }),
  );
};
