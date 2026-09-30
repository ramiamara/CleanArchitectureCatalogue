import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Filters, LogDetail, LogListItem, LogQuery, PagedResult, ProjectSummary, Stats } from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api';

  projects(): Observable<ProjectSummary[]> {
    return this.http.get<ProjectSummary[]>(`${this.base}/projects`);
  }

  stats(project: string, from: string, to: string): Observable<Stats> {
    const params = new HttpParams().set('project', project).set('from', from).set('to', to);
    return this.http.get<Stats>(`${this.base}/stats`, { params });
  }

  filters(project: string, from: string, to: string): Observable<Filters> {
    const params = new HttpParams().set('project', project).set('from', from).set('to', to);
    return this.http.get<Filters>(`${this.base}/filters`, { params });
  }

  logs(q: LogQuery): Observable<PagedResult<LogListItem>> {
    let params = new HttpParams()
      .set('project', q.project)
      .set('from', q.from)
      .set('to', q.to)
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
}
