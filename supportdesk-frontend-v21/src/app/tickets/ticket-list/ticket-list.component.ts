import { Component, OnInit, ApplicationRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { combineLatest, debounceTime, distinctUntilChanged, skip, startWith } from 'rxjs';
import { TicketService } from '../ticket.service';
import { AgentService } from '../../agents/agent.service';
import { TicketListItem, PagedResult, TicketStatusValue, Priority } from '../../models/ticket.model';
import { Agent } from '../../models/agent.model';

@Component({
  selector: 'app-ticket-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './ticket-list.component.html',
  styleUrl: './ticket-list.component.scss'
})
export class TicketListComponent implements OnInit {
  tickets: TicketListItem[] = [];
  agents: Agent[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 10;
  loading = false;

  searchControl = new FormControl('');
  statusControl = new FormControl<TicketStatusValue | ''>('');
  priorityControl = new FormControl<Priority | ''>('');
  agentControl = new FormControl<number | ''>('');
  overdueOnlyControl = new FormControl(false);

  statuses: TicketStatusValue[] = ['New', 'InProgress', 'Resolved', 'Closed'];
  priorities: Priority[] = ['Low', 'Normal', 'High', 'Critical'];

  constructor(private ticketService: TicketService, private agentService: AgentService, private appRef: ApplicationRef) {}

  ngOnInit(): void {
    this.agentService.search().subscribe(agents => (this.agents = agents));

    // Debounced search + every filter, combined — any change resets to page 1 and refetches.
    combineLatest([
      this.searchControl.valueChanges.pipe(startWith(''), debounceTime(300), distinctUntilChanged()),
      this.statusControl.valueChanges.pipe(startWith('')),
      this.priorityControl.valueChanges.pipe(startWith('')),
      this.agentControl.valueChanges.pipe(startWith('')),
      this.overdueOnlyControl.valueChanges.pipe(startWith(false)),
    ]).pipe(skip(1)) // skip the pipeline's own initial emission — loadTickets() below already handles page 1
    .subscribe(() => {
      this.page = 1;
      this.loadTickets();
    });

    this.loadTickets();
  }

  loadTickets(): void {
    this.loading = true;
    this.ticketService.list({
      page: this.page,
      pageSize: this.pageSize,
      search: this.searchControl.value || undefined,
      status: (this.statusControl.value || undefined) as TicketStatusValue | undefined,
      priority: (this.priorityControl.value || undefined) as Priority | undefined,
      assignedAgentId: (this.agentControl.value || undefined) as number | undefined,
      overdueOnly: this.overdueOnlyControl.value || undefined,
    }).subscribe({
      next: (result: PagedResult<TicketListItem>) => {
        document.title = `Got ${result.items.length} tickets`;   // <-- add this
        this.tickets = result.items;
        this.totalCount = result.totalCount;
        this.loading = false;
        this.appRef.tick();
        console.log('TICK CALLED');
      },
      error: (err) => {
        console.error('REQUEST ERRORED', err);
        this.loading = false;
        this.appRef.tick();
      }
    });
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.page = p;
    this.loadTickets();
  }
}