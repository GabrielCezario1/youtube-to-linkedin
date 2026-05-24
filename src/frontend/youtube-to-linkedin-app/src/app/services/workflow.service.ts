import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

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
      'https://localhost:5001/api/workflow/start',
      body
    );
  }
}
