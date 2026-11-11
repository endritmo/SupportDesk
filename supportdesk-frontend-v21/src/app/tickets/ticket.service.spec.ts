import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router'; // Add this import
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { TicketService } from './ticket.service';

describe('TicketService', () => {
  let service: TicketService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [TicketService, provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });
    service = TestBed.inject(TicketService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('list() always sends page and pageSize, and omits unset optional filters', () => {
    service.list({ page: 2, pageSize: 10 }).subscribe();

    const req = httpMock.expectOne(r => r.url.endsWith('/tickets') && r.method === 'GET');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('10');
    expect(req.request.params.has('search')).toBe(false);
    expect(req.request.params.has('status')).toBe(false);
    expect(req.request.params.has('overdueOnly')).toBe(false);

    req.flush({ items: [], totalCount: 0 });
  });

  it('list() includes provided optional filters in the request', () => {
    service.list({
      page: 1,
      pageSize: 10,
      search: 'printer',
      status: 'New',
      priority: 'High',
      assignedAgentId: 3,
      overdueOnly: true
    }).subscribe();

    const req = httpMock.expectOne(r => r.url.endsWith('/tickets') && r.method === 'GET');
    expect(req.request.params.get('search')).toBe('printer');
    expect(req.request.params.get('status')).toBe('New');
    expect(req.request.params.get('assignedAgentId')).toBe('3');
    expect(req.request.params.get('overdueOnly')).toBe('true');

    req.flush({ items: [], totalCount: 0 });
  });
});
