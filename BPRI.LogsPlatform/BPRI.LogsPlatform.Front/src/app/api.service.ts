import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  Filters, LogDetail, LogListItem, LogQuery, PagedResult, ProjectSummary, RequestDetail, RequestFilters,
  RequestListItem, RequestQuery, RequestStats, Stats,
} from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api';

  projects(): Observable<ProjectSummary[]> {
    return this.http.get<ProjectSummary[]>(`${this.base}/projects`);
  }

  // ---- Exceptions

  stats(project: string, from: string, to: string, application?: string): Observable<Stats> {
    return this.http.get<Stats>(`${this.base}/stats`, { params: this.scope(project, from, to, application) });
  }

  filters(project: string, from: string, to: string): Observable<Filters> {
    return this.http.get<Filters>(`${this.base}/filters`, { params: this.scope(project, from, to) });
  }

  logs(q: LogQuery): Observable<PagedResult<LogListItem>> {
    let params = this.scope(q.project, q.from, q.to, q.application)
      .set('page', q.page)
      .set('pageSize', q.pageSize)
      .set('sortField', q.sortField)
      .set('sortDir', q.sortDir);
    q.severity?.forEach(s => (params = params.append('severity', s)));
    q.statusCode?.forEach(s => (params = params.append('statusCode', s)));
    if (q.exceptionType) params = params.set('exceptionType', q.exceptionType);
    if (q.traceId) params = params.set('traceId', q.traceId);
    if (q.search) params = params.set('search', q.search);
    return this.http.get<PagedResult<LogListItem>>(`${this.base}/logs`, { params });
  }

  log(id: number): Observable<LogDetail> {
    return this.http.get<LogDetail>(`${this.base}/logs/${id}`);
  }

  // ---- Requêtes tracées

  requestStats(project: string, from: string, to: string, application?: string): Observable<RequestStats> {
    return this.http.get<RequestStats>(`${this.base}/requests/stats`, { params: this.scope(project, from, to, application) });
  }

  requestFilters(project: string, from: string, to: string): Observable<RequestFilters> {
    return this.http.get<RequestFilters>(`${this.base}/requests/filters`, { params: this.scope(project, from, to) });
  }

  requests(q: RequestQuery): Observable<PagedResult<RequestListItem>> {
    let params = this.scope(q.project, q.from, q.to, q.application)
      .set('page', q.page)
      .set('pageSize', q.pageSize)
      .set('sortField', q.sortField)
      .set('sortDir', q.sortDir);
    q.method?.forEach(m => (params = params.append('method', m)));
    q.statusCode?.forEach(s => (params = params.append('statusCode', s)));
    if (q.traceId) params = params.set('traceId', q.traceId);
    if (q.search) params = params.set('search', q.search);
    if (q.minDurationMs) params = params.set('minDurationMs', q.minDurationMs);
    return this.http.get<PagedResult<RequestListItem>>(`${this.base}/requests`, { params });
  }

  request(id: number): Observable<RequestDetail> {
    return this.http.get<RequestDetail>(`${this.base}/requests/${id}`);
  }

  private scope(project: string, from: string, to: string, application?: string): HttpParams {
    let params = new HttpParams().set('project', project).set('from', from).set('to', to);
    if (application) params = params.set('application', application);
    return params;
  }
}
