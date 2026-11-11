import { Routes } from '@angular/router';
import { TicketListComponent } from './tickets/ticket-list/ticket-list.component';
import { TicketDetailComponent } from './tickets/ticket-detail/ticket-detail.component';
import { TicketFormComponent } from './tickets/ticket-form/ticket-form.component';

export const routes: Routes = [
  { path: '', redirectTo: 'tickets', pathMatch: 'full' },
  { path: 'tickets', component: TicketListComponent },
  { path: 'tickets/new', component: TicketFormComponent },
  { path: 'tickets/:id', component: TicketDetailComponent },
  { path: 'tickets/:id/edit', component: TicketFormComponent },
];