import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { of } from 'rxjs';
import { TicketListComponent } from './ticket-list.component';
import { TicketService } from '../ticket.service';
import { AgentService } from '../../agents/agent.service';

describe('TicketListComponent', () => {
  let ticketServiceMock: { list: ReturnType<typeof vi.fn> };
  let agentServiceMock: { search: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    ticketServiceMock = { list: vi.fn().mockReturnValue(of({ items: [], totalCount: 0 })) };
    agentServiceMock = { search: vi.fn().mockReturnValue(of([])) };

    TestBed.configureTestingModule({
      imports: [TicketListComponent],
      providers: [
        provideRouter([]),
        { provide: TicketService, useValue: ticketServiceMock },
        { provide: AgentService, useValue: agentServiceMock }
      ]
    });
  });

  it('fetches tickets exactly once on initial load (no duplicate fetch from the filter pipeline)', () => {
    const fixture = TestBed.createComponent(TicketListComponent);
    fixture.detectChanges(); // triggers ngOnInit

    expect(ticketServiceMock.list).toHaveBeenCalledTimes(1);
    expect(ticketServiceMock.list).toHaveBeenCalledWith(
      expect.objectContaining({ page: 1, pageSize: 10 })
    );
  });

  it('totalPages is computed correctly from totalCount and pageSize', () => {
    const fixture = TestBed.createComponent(TicketListComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.totalCount = 25;
    component.pageSize = 10;
    expect(component.totalPages).toBe(3);

    component.totalCount = 0;
    expect(component.totalPages).toBe(1); // Math.max(1, ...) floor
  });
});
