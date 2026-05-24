# Spec: Frontend — Angular App e SignalR Client

## Overview

An Angular 21 standalone application that connects to the backend SignalR hub and calls the workflow start endpoint. This phase establishes the transport layer with a minimal UI.

## Project Setup

- Created with `ng new youtube-to-linkedin-app --standalone --routing=false --style=css`
- Located at `src/frontend/youtube-to-linkedin-app/`

## SignalRService

**File**: `src/app/services/signalr.service.ts`

**Responsibilities**:
- Build a `HubConnection` to `https://localhost:5001/hubs/workflow`
- Start the connection
- Expose a `progress$: Observable<{sessionId: string, message: string}>` Subject
- Register handler for `SendProgress` server method

**Key implementation**:
```typescript
import * as signalR from '@microsoft/signalr';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private hub = new signalR.HubConnectionBuilder()
    .withUrl('https://localhost:5001/hubs/workflow')
    .withAutomaticReconnect()
    .build();

  progress$ = new Subject<{sessionId: string, message: string}>();

  async connect() {
    this.hub.on('SendProgress', (sessionId, message) => 
      this.progress$.next({ sessionId, message }));
    await this.hub.start();
  }
}
```

## WorkflowService

**File**: `src/app/services/workflow.service.ts`

**Responsibilities**:
- Inject `HttpClient`
- Expose `start(url: string, postType: string, mode: string): Observable<{sessionId: string}>`
- POST to `https://localhost:5001/api/workflow/start`

## AppComponent

**File**: `src/app/app.component.ts`

**Responsibilities**:
- Inject `SignalRService` and `WorkflowService`
- Call `signalRService.connect()` on init
- Display a form with 3 fields: `url`, `postType`, `mode`
- On submit, call `workflowService.start()` and log the sessionId
- Subscribe to `signalRService.progress$` and log events to the console

## npm Packages

```bash
npm install @microsoft/signalr
```

## Angular Configuration

`provideHttpClient()` must be added to `app.config.ts` providers.
