import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormControl, FormGroup, Validators } from '@angular/forms';
import { TicketService } from '../ticket.service';
import { AgentService } from '../../agents/agent.service';
import { TicketDetail, ALLOWED_TRANSITIONS, TicketStatusValue } from '../../models/ticket.model';
import { Agent } from '../../models/agent.model';

@Component({
  selector: 'app-ticket-detail',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './ticket-detail.component.html',
  styleUrl: './ticket-detail.component.scss'
})
export class TicketDetailComponent implements OnInit {
  ticket: TicketDetail | null = null;
  agents: Agent[] = [];
  loading = true;
  errorMessage: string | null = null;

  commentForm = new FormGroup({
    authorName: new FormControl('', Validators.required),
    body: new FormControl('', Validators.required),
  });

  assignedAgentControl = new FormControl<number | ''>('');

  private ticketId!: number;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private ticketService: TicketService,
    private agentService: AgentService
  ) {}

  ngOnInit(): void {
    this.ticketId = Number(this.route.snapshot.paramMap.get('id'));
    // Rule 4: inactive agents can't be assigned — so the dropdown only ever offers active ones.
    this.agentService.search().subscribe(agents => (this.agents = agents.filter(a => a.active)));
    this.loadTicket();
  }

  loadTicket(): void {
    this.loading = true;
    this.ticketService.getById(this.ticketId).subscribe({
      next: (t) => {
        this.ticket = t;
        this.assignedAgentControl.setValue(t.assignedAgentId ?? '');
        this.loading = false;
      },
      error: () => { this.loading = false; this.errorMessage = 'Ticket not found.'; }
    });
  }

  get isClosed(): boolean {
    return this.ticket?.status === 'Closed';
  }

  get allowedNextStatuses(): TicketStatusValue[] {
    return this.ticket ? ALLOWED_TRANSITIONS[this.ticket.status] : [];
  }

  changeStatus(status: TicketStatusValue): void {
    this.errorMessage = null;
    this.ticketService.changeStatus(this.ticketId, { status }).subscribe({
      next: (t) => (this.ticket = t),
      error: (err) => { this.errorMessage = err.error?.message ?? 'Could not change status.'; }
    });
  }

  assignAgent(): void {
    const agentId = this.assignedAgentControl.value;
    if (!agentId) return;
    this.errorMessage = null;
    this.ticketService.assignAgent(this.ticketId, { agentId: Number(agentId) }).subscribe({
      next: (t) => (this.ticket = t),
      error: (err) => { this.errorMessage = err.error?.message ?? 'Could not assign agent.'; }
    });
  }

  unassignAgent(): void {
    this.errorMessage = null;
    this.ticketService.unassignAgent(this.ticketId).subscribe({
      next: (t) => { this.ticket = t; this.assignedAgentControl.setValue(''); },
      error: (err) => { this.errorMessage = err.error?.message ?? 'Could not unassign agent.'; }
    });
  }

  addComment(): void {
    if (this.commentForm.invalid) return;
    const { authorName, body } = this.commentForm.value;
    this.ticketService.addComment(this.ticketId, { authorName: authorName!, body: body! }).subscribe({
      next: () => { this.commentForm.reset(); this.loadTicket(); },
      error: (err) => { this.errorMessage = err.error?.message ?? 'Could not add comment.'; }
    });
  }

  deleteTicket(): void {
    if (!confirm(`Delete ticket ${this.ticket?.reference}? This cannot be undone.`)) return;
    this.ticketService.delete(this.ticketId).subscribe({
      next: () => this.router.navigate(['/tickets']),
      error: (err) => { this.errorMessage = err.error?.message ?? 'Could not delete ticket.'; }
    });
  }
}