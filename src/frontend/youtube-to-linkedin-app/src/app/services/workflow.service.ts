import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface StartWorkflowRequest {
  url: string;
  postType: string;
  mode: string;
}

export interface StartWorkflowResponse {
  sessionId: string;
}

@Injectable({ providedIn: 'root' })
export class WorkflowService {
  private http = inject(HttpClient);

  start(url: string, postType: string, mode: string): Observable<StartWorkflowResponse> {
    const body: StartWorkflowRequest = { url, postType, mode };
    return this.http.post<StartWorkflowResponse>(
      `${environment.backendUrl}/api/workflow/start`,
      body
    );
  }

  cancel(sessionId: string): Observable<void> {
    return this.http.delete<void>(
      `${environment.backendUrl}/api/workflow/${sessionId}`
    );
  }

  respond(sessionId: string, answers: string[]): Observable<void> {
    return this.http.post<void>(
      `${environment.backendUrl}/api/workflow/${sessionId}/respond`,
      { answers }
    );
  }
}

