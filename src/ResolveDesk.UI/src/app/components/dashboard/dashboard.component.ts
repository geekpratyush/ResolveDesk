import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService, User } from '../../services/auth.service';
import { TicketService, Ticket } from '../../services/ticket.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  currentUser: User | null = null;
  tickets: Ticket[] = [];
  filteredTickets: Ticket[] = [];
  selectedTicket: Ticket | null = null;
  
  // Search and filters
  searchTerm = '';
  statusFilter = 'ALL';
  categoryFilter = 'ALL';
  priorityFilter = 'ALL';

  // Ticket creation
  showCreateModal = false;
  newTicketTitle = '';
  newTicketDescription = '';
  newTicketCategory = 'Software';
  newTicketPriority = 'Medium';
  isCreatingTicket = false;

  // Ticket replies
  replyMessage = '';
  isSendingReply = false;

  // Dashboard Statistics
  totalTicketsCount = 0;
  openTicketsCount = 0;
  inProgressTicketsCount = 0;
  closedTicketsCount = 0;
  averageResolutionHours = 0;
  categoryDistribution: { category: string, count: number, percentage: number }[] = [];
  priorityDistribution: { priority: string, count: number, percentage: number }[] = [];

  constructor(
    private authService: AuthService,
    private ticketService: TicketService
  ) {
    this.currentUser = this.authService.currentUser();
  }

  ngOnInit() {
    this.loadTickets();
  }

  loadTickets() {
    this.ticketService.getTickets().subscribe({
      next: (data) => {
        this.tickets = data;
        this.calculateStats();
        this.applyFilter();
        
        // Preserve selected ticket selection if still in the list
        if (this.selectedTicket) {
          const updated = data.find(t => t.id === this.selectedTicket?.id);
          if (updated) {
            this.selectedTicket = updated;
          }
        }
      },
      error: (err) => {
        console.error('Failed to load tickets', err);
      }
    });
  }

  calculateStats() {
    this.totalTicketsCount = this.tickets.length;
    this.openTicketsCount = this.tickets.filter(t => t.status.toLowerCase() === 'open').length;
    this.inProgressTicketsCount = this.tickets.filter(t => t.status.toLowerCase() === 'inprogress').length;
    this.closedTicketsCount = this.tickets.filter(t => t.status.toLowerCase() === 'closed').length;

    // Calculate average resolution time (for closed/resolved tickets)
    const closedTickets = this.tickets.filter(t => t.status.toLowerCase() === 'closed' && t.resolvedAt);
    if (closedTickets.length > 0) {
      const totalHours = closedTickets.reduce((sum, t) => {
        const created = new Date(t.createdAt).getTime();
        const resolved = new Date(t.resolvedAt!).getTime();
        const diffHrs = Math.max(0, (resolved - created) / (1000 * 60 * 60));
        return sum + diffHrs;
      }, 0);
      this.averageResolutionHours = Math.round((totalHours / closedTickets.length) * 10) / 10;
    } else {
      this.averageResolutionHours = 0;
    }

    // Category distribution
    const categories = ['Software', 'Hardware', 'Network', 'Database', 'Security', 'Billing'];
    this.categoryDistribution = categories.map(cat => {
      const count = this.tickets.filter(t => t.category?.toLowerCase() === cat.toLowerCase()).length;
      const pct = this.totalTicketsCount > 0 ? Math.round((count / this.totalTicketsCount) * 100) : 0;
      return { category: cat, count, percentage: pct };
    });

    // Priority distribution
    const priorities = ['Critical', 'High', 'Medium', 'Low'];
    this.priorityDistribution = priorities.map(pri => {
      const count = this.tickets.filter(t => t.priority?.toLowerCase() === pri.toLowerCase()).length;
      const pct = this.totalTicketsCount > 0 ? Math.round((count / this.totalTicketsCount) * 100) : 0;
      return { priority: pri, count, percentage: pct };
    });
  }

  selectTicket(ticket: Ticket) {
    this.ticketService.getTicket(ticket.id).subscribe({
      next: (fullTicket) => {
        this.selectedTicket = fullTicket;
      },
      error: (err) => {
        console.error('Failed to get ticket details', err);
      }
    });
  }

  applyFilter() {
    this.filteredTickets = this.tickets.filter(t => {
      const matchesSearch = t.title.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
                            t.description.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
                            t.customerEmail.toLowerCase().includes(this.searchTerm.toLowerCase());
                            
      const matchesStatus = this.statusFilter === 'ALL' || t.status.toUpperCase() === this.statusFilter.toUpperCase();
      const matchesCategory = this.categoryFilter === 'ALL' || t.category?.toUpperCase() === this.categoryFilter.toUpperCase();
      const matchesPriority = this.priorityFilter === 'ALL' || t.priority?.toUpperCase() === this.priorityFilter.toUpperCase();
      
      return matchesSearch && matchesStatus && matchesCategory && matchesPriority;
    });
  }

  onCreateTicket() {
    if (!this.newTicketTitle || !this.newTicketDescription) return;

    this.isCreatingTicket = true;
    this.ticketService.createTicket(
      this.newTicketTitle, 
      this.newTicketDescription,
      this.newTicketCategory,
      this.newTicketPriority
    ).subscribe({
      next: (ticket) => {
        this.isCreatingTicket = false;
        this.newTicketTitle = '';
        this.newTicketDescription = '';
        this.newTicketCategory = 'Software';
        this.newTicketPriority = 'Medium';
        this.showCreateModal = false;
        this.loadTickets();
      },
      error: (err) => {
        this.isCreatingTicket = false;
        console.error('Failed to create ticket', err);
      }
    });
  }

  onSendReply() {
    if (!this.selectedTicket || !this.replyMessage) return;

    this.isSendingReply = true;
    this.ticketService.addResponse(this.selectedTicket.id, this.replyMessage).subscribe({
      next: (response) => {
        this.isSendingReply = false;
        this.replyMessage = '';
        
        // Reload details to get new response and potential auto-status change
        this.selectTicket(this.selectedTicket!);
        this.loadTickets();
      },
      error: (err) => {
        this.isSendingReply = false;
        console.error('Failed to add response', err);
      }
    });
  }

  onStatusChange(event: any) {
    if (!this.selectedTicket) return;
    const newStatus = event.target.value;

    this.ticketService.updateStatus(this.selectedTicket.id, newStatus).subscribe({
      next: (updatedTicket) => {
        this.selectedTicket!.status = updatedTicket.status;
        this.loadTickets();
      },
      error: (err) => {
        console.error('Failed to update status', err);
      }
    });
  }

  onLogout() {
    this.authService.logout();
  }

  isAdminOrSupport(): boolean {
    if (!this.currentUser) return false;
    return this.currentUser.role === 'Admin' || this.currentUser.role === 'SupportStaff';
  }
}
