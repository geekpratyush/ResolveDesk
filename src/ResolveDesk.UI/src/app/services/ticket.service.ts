import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface TicketResponse {
  id: string;
  ticketId: string;
  responderId: string;
  responderName: string;
  message: string;
  createdAt: string;
}

export interface Ticket {
  id: string;
  customerId: string;
  customerEmail: string;
  title: string;
  description: string;
  status: 'Open' | 'InProgress' | 'Closed';
  category: string;
  priority: string;
  createdAt: string;
  updatedAt: string;
  resolvedAt?: string;
  responses: TicketResponse[];
}

@Injectable({
  providedIn: 'root'
})
export class TicketService {
  private apiUrl = 'http://localhost:5000/api/tickets';

  constructor(private http: HttpClient) {}

  getTickets(): Observable<Ticket[]> {
    return this.http.get<Ticket[]>(this.apiUrl);
  }

  getTicket(id: string): Observable<Ticket> {
    return this.http.get<Ticket>(`${this.apiUrl}/${id}`);
  }

  createTicket(title: string, description: string, category: string, priority: string): Observable<Ticket> {
    return this.http.post<Ticket>(this.apiUrl, { title, description, category, priority });
  }

  addResponse(ticketId: string, message: string): Observable<TicketResponse> {
    return this.http.post<TicketResponse>(`${this.apiUrl}/${ticketId}/responses`, { message });
  }

  updateStatus(ticketId: string, status: string): Observable<Ticket> {
    return this.http.put<Ticket>(`${this.apiUrl}/${ticketId}/status`, { status });
  }
}
