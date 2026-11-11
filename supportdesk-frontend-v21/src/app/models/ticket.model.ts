export type Priority = 'Low' | 'Normal' | 'High' | 'Critical';
export type TicketStatusValue = 'New' | 'InProgress' | 'Resolved' | 'Closed';

export interface TicketListItem {
  id: number;
  reference: string;
  title: string;
  customerName: string;
  priority: Priority;
  status: TicketStatusValue;
  assignedAgentName: string | null;
  dueDate: string;
  overdue: boolean;
}

export interface Comment {
  id: number;
  authorName: string;
  body: string;
  createdDate: string;
}

export interface TicketDetail {
  id: number;
  reference: string;
  title: string;
  description: string;
  customerName: string;
  customerEmail: string;
  priority: Priority;
  status: TicketStatusValue;
  assignedAgentId: number | null;
  assignedAgentName: string | null;
  createdDate: string;
  lastModifiedDate: string;
  resolvedDate: string | null;
  closedDate: string | null;
  dueDate: string;
  overdue: boolean;
  comments: Comment[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface TicketListQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: TicketStatusValue;
  priority?: Priority;
  assignedAgentId?: number;
  overdueOnly?: boolean;
}

export interface CreateTicketRequest {
  title: string;
  description: string;
  customerName: string;
  customerEmail: string;
  priority: Priority;
}

export type UpdateTicketRequest = CreateTicketRequest;
export interface ChangeStatusRequest { status: TicketStatusValue; }
export interface AssignAgentRequest { agentId: number; }
export interface AddCommentRequest { authorName: string; body: string; }

// Mirrors TicketService.AllowedTransitions on the backend — UX only, so the
// detail page only ever shows buttons for moves the API will actually accept.
// The backend remains the real enforcement point either way.
export const ALLOWED_TRANSITIONS: Record<TicketStatusValue, TicketStatusValue[]> = {
  New: ['InProgress'],
  InProgress: ['Resolved'],
  Resolved: ['Closed', 'InProgress'],
  Closed: [],
};