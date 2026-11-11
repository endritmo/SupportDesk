import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TicketService } from '../ticket.service';
import { Priority } from '../../models/ticket.model';

@Component({
  selector: 'app-ticket-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './ticket-form.component.html',
  styleUrl: './ticket-form.component.scss'
})
export class TicketFormComponent implements OnInit {
  isEditMode = false;
  ticketId: number | null = null;
  submitting = false;
  serverError: string | null = null;

  priorities: Priority[] = ['Low', 'Normal', 'High', 'Critical'];

  form: FormGroup;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private ticketService: TicketService
  ) {
    this.form = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(200)]],
      description: ['', [Validators.required]],
      customerName: ['', [Validators.required, Validators.maxLength(150)]],
      customerEmail: ['', [Validators.required, Validators.email]],
      priority: ['Normal' as Priority, [Validators.required]],
    });
  }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.isEditMode = true;
      this.ticketId = Number(idParam);
      this.ticketService.getById(this.ticketId).subscribe(t => {
        this.form.patchValue({
          title: t.title,
          description: t.description,
          customerName: t.customerName,
          customerEmail: t.customerEmail,
          priority: t.priority,
        });
      });
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched(); // surfaces validation messages even for untouched fields
      return;
    }

    this.submitting = true;
    this.serverError = null;
    const value = this.form.getRawValue();

    const request$ = this.isEditMode
      ? this.ticketService.update(this.ticketId!, value as any)
      : this.ticketService.create(value as any);

    request$.subscribe({
      next: (t) => { this.submitting = false; this.router.navigate(['/tickets', t.id]); },
      error: (err) => {
        this.submitting = false;
        this.serverError = err.error?.message ?? 'Something went wrong saving this ticket.';
      }
    });
  }
}