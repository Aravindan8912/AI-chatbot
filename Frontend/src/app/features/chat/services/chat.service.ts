import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { Observable, timeout } from 'rxjs';
import { ChatRequest } from '../models/chat-request.model';
import { ChatResponse } from '../models/chat-response.model';

@Injectable({ providedIn: 'root' })
export class ChatService {

  private apiUrl = `${environment.apiUrl}/chat`;

  constructor(private http: HttpClient) {}

  ask(request: ChatRequest): Observable<ChatResponse> {
    return this.http.post<ChatResponse>(this.apiUrl, request).pipe(timeout(30000));
  }
}