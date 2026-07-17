import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { Router } from '@angular/router';

export interface User {
  email: string;
  fullName: string;
  role: 'Customer' | 'SupportStaff' | 'Admin';
  token: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = 'http://localhost:5100/api/auth';
  currentUser = signal<User | null>(null);

  constructor(private http: HttpClient, private router: Router) {
    this.loadUserFromStorage();
  }

  private loadUserFromStorage() {
    const stored = localStorage.getItem('resolvedesk_user');
    if (stored) {
      try {
        this.currentUser.set(JSON.parse(stored));
      } catch (e) {
        localStorage.removeItem('resolvedesk_user');
      }
    }
  }

  register(email: string, password: string, fullName: string, role: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/register`, { email, password, fullName, role }).pipe(
      tap(res => {
        if (res.success) {
          const user: User = {
            email: res.email,
            fullName: res.fullName,
            role: res.role as any,
            token: res.token
          };
          localStorage.setItem('resolvedesk_user', JSON.stringify(user));
          this.currentUser.set(user);
        }
      })
    );
  }

  login(email: string, password: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/login`, { email, password }).pipe(
      tap(res => {
        if (res.success) {
          const user: User = {
            email: res.email,
            fullName: res.fullName,
            role: res.role as any,
            token: res.token
          };
          localStorage.setItem('resolvedesk_user', JSON.stringify(user));
          this.currentUser.set(user);
        }
      })
    );
  }

  oauthLogin(provider: string, token: string, email?: string, fullName?: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/oauth-login`, { provider, token, email, fullName }).pipe(
      tap(res => {
        if (res.success) {
          const user: User = {
            email: res.email,
            fullName: res.fullName,
            role: res.role as any,
            token: res.token
          };
          localStorage.setItem('resolvedesk_user', JSON.stringify(user));
          this.currentUser.set(user);
        }
      })
    );
  }

  logout() {
    localStorage.removeItem('resolvedesk_user');
    this.currentUser.set(null);
    this.router.navigate(['/login']);
  }

  isAuthenticated(): boolean {
    return this.currentUser() !== null;
  }
}
