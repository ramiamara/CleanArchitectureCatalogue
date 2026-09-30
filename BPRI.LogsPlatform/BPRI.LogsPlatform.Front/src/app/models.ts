export type Severity = 'Error' | 'Warning' | 'Information';

export interface ProjectSummary {
  code: string;
  totalLogs: number;
  lastOccurredAtUtc: string | null;
}

export interface LogListItem {
  id: number;
  traceId: string;
  cprj: string;
  applicationName: string;
  severity: Severity;
  statusCode: number | null;
  exceptionType: string | null;
  sourceContext: string | null;
  message: string;
  httpMethod: string | null;
  path: string | null;
  userName: string | null;
  occurredAtUtc: string;
}

export interface LogDetail extends LogListItem {
  userId: string | null;
  /** JSON des claims JWT autorisés. */
  claims: string | null;
  exception: string | null;
  innerException: string | null;
  environmentName: string | null;
  machineName: string | null;
  fingerprint: string | null;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface SeverityCount { severity: Severity; count: number; }
export interface StatusCodeCount { statusCode: number; count: number; }
export interface ExceptionTypeCount { exceptionType: string; count: number; }
export interface EndpointCount { method: string | null; path: string | null; count: number; }
export interface TimelinePoint { timestampUtc: string; error: number; warning: number; information: number; }

export interface Stats {
  total: number;
  errors: number;
  warnings: number;
  informations: number;
  bySeverity: SeverityCount[];
  byStatusCode: StatusCodeCount[];
  byExceptionType: ExceptionTypeCount[];
  topEndpoints: EndpointCount[];
  timeline: TimelinePoint[];
  timelineBucket: 'hour' | 'day';
}

export interface Filters {
  exceptionTypes: string[];
  statusCodes: number[];
}

export interface LogQuery {
  project: string;
  from: string;
  to: string;
  severity?: Severity[];
  statusCode?: number[];
  exceptionType?: string;
  traceId?: string;
  search?: string;
  page: number;
  pageSize: number;
  sortField: string;
  sortDir: 'asc' | 'desc';
}
