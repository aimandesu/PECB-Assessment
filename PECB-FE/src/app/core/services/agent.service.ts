import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable, shareReplay } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiEnvelope } from '../models/api.models';
import { Agent, Department } from '../models/agent.models';

@Injectable({ providedIn: 'root' })
export class AgentService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/agent`;

  /**
   * The roster changes rarely and both feature pages need it to resolve
   * `assignedAgent` ids into names, so the first response is replayed to later
   * subscribers instead of being re-fetched.
   */
  private readonly all$ = this.http
    .get<ApiEnvelope<Agent[]>>(`${this.base}/get-all`)
    .pipe(
      map((response) => response.data ?? []),
      shareReplay({ bufferSize: 1, refCount: false }),
    );

  list(): Observable<Agent[]> {
    return this.all$;
  }

  byDepartment(department: Department): Observable<Agent[]> {
    const params = new HttpParams().set('department', department);
    return this.http
      .get<ApiEnvelope<Agent[]>>(`${this.base}/get-all`, { params })
      .pipe(map((response) => response.data ?? []));
  }

  get(agentId: string): Observable<Agent | null> {
    const params = new HttpParams().set('agentId', agentId);
    return this.http
      .get<ApiEnvelope<Agent | null>>(`${this.base}/get`, { params })
      .pipe(map((response) => response.data ?? null));
  }
}
