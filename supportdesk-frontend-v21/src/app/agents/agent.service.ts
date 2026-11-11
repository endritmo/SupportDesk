import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../core/api-config';
import { Agent } from '../models/agent.model';

@Injectable({ providedIn: 'root' })
export class AgentService {
  private readonly baseUrl = `${API_BASE_URL}/agents`;

  constructor(private http: HttpClient) {}

  search(term?: string): Observable<Agent[]> {
    let params = new HttpParams();
    if (term) params = params.set('search', term);
    return this.http.get<Agent[]>(this.baseUrl, { params });
  }

  getById(id: number): Observable<Agent> {
    return this.http.get<Agent>(`${this.baseUrl}/${id}`);
  }
}