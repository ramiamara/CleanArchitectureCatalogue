export type Severity = 'Error' | 'Warning' | 'Information';
export type View = 'exceptions' | 'requests';

/** Résultat d'un panneau de détail : afficher les éléments d'un TraceId dans l'une des deux vues. */
export interface TraceTarget {
  traceId: string;
  view: View;
}

export interface ProjectSummary {
  code: string;
  totalLogs: number;
  lastOccurredAtUtc: string | null;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

// ---- Exceptions et logs applicatifs

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
  applications: string[];
}

export interface LogQuery {
  project: string;
  application?: string;
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

// ---- Trace des requêtes

export interface RequestListItem {
  id: number;
  traceId: string;
  cprj: string;
  applicationName: string;
  httpMethod: string | null;
  path: string | null;
  queryString: string | null;
  statusCode: number | null;
  durationMs: number | null;
  userName: string | null;
  occurredAtUtc: string;
}

export interface RequestDetail extends RequestListItem {
  userId: string | null;
  requestBody: string | null;
  responseBody: string | null;
  environmentName: string | null;
  machineName: string | null;
}

export interface StatusClassCount { statusClass: string; count: number; }
export interface MethodCount { method: string; count: number; }
export interface SlowEndpoint { method: string | null; path: string | null; count: number; avgDurationMs: number; maxDurationMs: number; }
export interface RequestTimelinePoint { timestampUtc: string; success: number; clientError: number; serverError: number; avgDurationMs: number; }

export interface RequestStats {
  total: number;
  serverErrors: number;
  clientErrors: number;
  avgDurationMs: number;
  p95DurationMs: number;
  byStatusClass: StatusClassCount[];
  byStatusCode: StatusCodeCount[];
  byMethod: MethodCount[];
  topEndpoints: EndpointCount[];
  slowEndpoints: SlowEndpoint[];
  timeline: RequestTimelinePoint[];
  timelineBucket: 'hour' | 'day';
}

export interface RequestFilters {
  applications: string[];
  methods: string[];
  statusCodes: number[];
}

export interface RequestQuery {
  project: string;
  application?: string;
  from: string;
  to: string;
  method?: string[];
  statusCode?: number[];
  traceId?: string;
  search?: string;
  minDurationMs?: number;
  page: number;
  pageSize: number;
  sortField: string;
  sortDir: 'asc' | 'desc';
}
