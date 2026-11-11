import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../core/api-config';
import {
  TicketListItem, TicketDetail, PagedResult, TicketListQuery,
  CreateTicketRequest, UpdateTicketRequest, ChangeStatusRequest,
  AssignAgentRequest, AddCommentRequest, Comment
} from '../models/ticket.model';

@Injectable({ providedIn: 'root' })
export class TicketService {
  private readonly baseUrl = `${API_BASE_URL}/tickets`;

  constructor(private http: HttpClient) {}

  list(query: TicketListQuery): Observable<PagedResult<TicketListItem>> {
    let params = new HttpParams()
      .set('page', query.page ?? 1)
      .set('pageSize', query.pageSize ?? 20);

    if (query.search) params = params.set('search', query.search);
    if (query.status) params = params.set('status', query.status);
    if (query.priority) params = params.set('priority', query.priority);
    if (query.assignedAgentId) params = params.set('assignedAgentId', query.assignedAgentId);
    if (query.overdueOnly) params = params.set('overdueOnly', query.overdueOnly);

    return this.http.get<PagedResult<TicketListItem>>(this.baseUrl, { params });
  }

  getById(id: number): Observable<TicketDetail> {
    return this.http.get<TicketDetail>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateTicketRequest): Observable<TicketDetail> {
    return this.http.post<TicketDetail>(this.baseUrl, request);
  }

  update(id: number, request: UpdateTicketRequest): Observable<TicketDetail> {
    return this.http.put<TicketDetail>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  changeStatus(id: number, request: ChangeStatusRequest): Observable<TicketDetail> {
    return this.http.patch<TicketDetail>(`${this.baseUrl}/${id}/status`, request);
  }

  assignAgent(id: number, request: AssignAgentRequest): Observable<TicketDetail> {
    return this.http.post<TicketDetail>(`${this.baseUrl}/${id}/assign`, request);
  }

  unassignAgent(id: number): Observable<TicketDetail> {
    return this.http.delete<TicketDetail>(`${this.baseUrl}/${id}/assign`);
  }

  addComment(id: number, request: AddCommentRequest): Observable<Comment> {
    return this.http.post<Comment>(`${this.baseUrl}/${id}/comments`, request);
  }
}